// File: MainForm.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Newtonsoft.Json;

namespace ScheduleISync
{
    public partial class MainForm : Form
    {
        private GCloudConsoleConfig gcloudConfig;
        private UserSettings userSettings;
        private AccountInfo currentAccount = null;
        private readonly string appName = "ScheduleISync";

        private System.Windows.Forms.Timer processCheckTimer;

        private string LocalSavesPath
        {
            get
            {
                string steamId = textBoxSteamID.Text.Trim();
                if (string.IsNullOrEmpty(steamId))
                    return "";
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    @"AppData\LocalLow\TVGS\Schedule I\Saves",
                    steamId
                );
            }
        }

        public MainForm()
        {
            InitializeComponent();
            this.Load += MainForm_Load;

            comboBoxSlot.Items.AddRange(new string[] {
                "SaveGame_1","SaveGame_2","SaveGame_3","SaveGame_4","SaveGame_5"
            });

            userSettings = UserSettingsManager.LoadSettings();
            if (!string.IsNullOrEmpty(userSettings.SelectedSlot))
                comboBoxSlot.SelectedItem = userSettings.SelectedSlot;
            else
                comboBoxSlot.SelectedIndex = 0;

            processCheckTimer = new System.Windows.Forms.Timer();
            processCheckTimer.Interval = 5000;
            processCheckTimer.Tick += ProcessCheckTimer_Tick;
            processCheckTimer.Start();

            UpdateUIState();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            gcloudConfig = GCloudConsoleConfig.LoadConfig();
            if (gcloudConfig == null)
            {
                MessageBox.Show(
                    "GCloudConsoleConfig.json file is missing or empty. Please fill it in and restart the application.",
                    "Configuration Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                Environment.Exit(0);
            }

            userSettings = UserSettingsManager.LoadSettings();
            textBoxSteamID.Text = userSettings.SteamID;
            textBoxFolderLink.Text = userSettings.FolderLink ?? "";
            textBoxSheetLink.Text = userSettings.SheetLink ?? "";
            if (!string.IsNullOrEmpty(userSettings.SelectedSlot))
                comboBoxSlot.SelectedItem = userSettings.SelectedSlot;

            UpdateUIState();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            userSettings.SteamID = textBoxSteamID.Text.Trim();
            userSettings.FolderLink = textBoxFolderLink.Text.Trim();
            userSettings.SheetLink = textBoxSheetLink.Text.Trim();
            userSettings.SelectedSlot = comboBoxSlot.SelectedItem.ToString();
            UserSettingsManager.SaveSettings(userSettings);
            base.OnFormClosing(e);
        }

        private void UpdateUIState()
        {
            bool gameRunning = IsGameRunning();
            bool enableUI = (currentAccount != null) && !gameRunning;

            Action act = () =>
            {
                buttonSignIn.Enabled = !enableUI;
                textBoxSteamID.Enabled = enableUI;
                textBoxFolderLink.Enabled = enableUI;
                textBoxSheetLink.Enabled = enableUI;
                comboBoxSlot.Enabled = enableUI;
                buttonUpload.Enabled = enableUI;
                buttonDownload.Enabled = enableUI;
            };

            if (InvokeRequired) Invoke(act);
            else act();
        }

        private bool IsGameRunning()
        {
            try
            {
                return Process.GetProcessesByName("Schedule I").Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private void ProcessCheckTimer_Tick(object sender, EventArgs e)
        {
            Task.Run(() => UpdateUIState());
        }

        private void UpdateProgress(string message, int percent)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    labelProgress.Text = message;
                    progressBarStatus.Value = Math.Min(percent, progressBarStatus.Maximum);
                }));
            }
            else
            {
                labelProgress.Text = message;
                progressBarStatus.Value = Math.Min(percent, progressBarStatus.Maximum);
            }
        }

        private void Log(string message)
        {
            if (textBoxLog.InvokeRequired)
                textBoxLog.Invoke(new Action(() => textBoxLog.AppendText(message + Environment.NewLine)));
            else
                textBoxLog.AppendText(message + Environment.NewLine);
        }

        private async void buttonSignIn_Click(object sender, EventArgs e)
        {
            await SignInAsync();
        }

        private async void buttonUpload_Click(object sender, EventArgs e)
        {
            await UploadSaveAsync();
        }

        private async void buttonDownload_Click(object sender, EventArgs e)
        {
            await DownloadFromSheetAsync();
        }

        private async Task SignInAsync()
        {
            try
            {
                string storeKey = Guid.NewGuid().ToString();
                var oauthJson = @"{
  ""installed"": {
    ""client_id"": """ + gcloudConfig.ClientId + @""",
    ""project_id"": """ + gcloudConfig.ProjectId + @""",
    ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
    ""token_uri"": ""https://oauth2.googleapis.com/token"",
    ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
    ""client_secret"": """ + gcloudConfig.ClientSecret + @""",
    ""redirect_uris"": [ ""urn:ietf:wg:oauth:2.0:oob"", ""http://localhost"" ]
  }
}";
                using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(oauthJson)))
                {
                    var secrets = GoogleClientSecrets.FromStream(ms).Secrets;
                    var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        secrets,
                        new[] { DriveService.Scope.DriveFile },
                        storeKey,
                        CancellationToken.None,
                        new FileDataStore("Drive.Auth.Store")
                    );
                    var service = new DriveService(new BaseClientService.Initializer
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = appName
                    });

                    var about = service.About.Get();
                    about.Fields = "user(emailAddress,photoLink)";
                    var info = about.Execute();

                    currentAccount = new AccountInfo
                    {
                        StoreKey = storeKey,
                        Email = info.User.EmailAddress,
                        Service = service,
                        AvatarUrl = info.User.PhotoLink
                    };
                    userSettings.LastUsedAccount = storeKey;
                    UserSettingsManager.SaveSettings(userSettings);

                    Log("Signed in as " + info.User.EmailAddress);
                    await UpdateAvatarAsync(info.User.PhotoLink);
                    buttonSignIn.Visible = false;
                    UpdateUIState();
                }
            }
            catch (Exception ex)
            {
                Log("Sign in error: " + ex.Message);
            }
        }

        private async Task UpdateAvatarAsync(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                {
                    pictureBoxAvatar.Image = null;
                    return;
                }
                using (var wc = new WebClient())
                {
                    var data = await wc.DownloadDataTaskAsync(imageUrl);
                    using (var ms = new MemoryStream(data))
                    {
                        var src = Image.FromStream(ms);
                        var circ = CropToCircle(src, pictureBoxAvatar.Width, this.BackColor);
                        pictureBoxAvatar.Image = circ;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Avatar load error: " + ex.Message);
            }
        }

        private Image CropToCircle(Image src, int size, Color backColor)
        {
            var dst = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(dst))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var b = new SolidBrush(backColor))
                    g.FillRectangle(b, 0, 0, size, size);
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(0, 0, size, size);
                    g.SetClip(path);
                    g.DrawImage(src, 0, 0, size, size);
                }
            }
            return dst;
        }

        private string GetFolderIdFromLink(string link)
        {
            if (string.IsNullOrEmpty(link)) return null;
            var idx = link.IndexOf("/folders/");
            if (idx < 0) return null;
            var start = idx + "/folders/".Length;
            var end = link.IndexOfAny(new char[] { '/', '?' }, start);
            return end < 0 ? link.Substring(start) : link.Substring(start, end - start);
        }

        private async Task UploadSaveAsync()
        {
            try
            {
                if (currentAccount == null)
                {
                    Log("No account signed in.");
                    return;
                }
                if (string.IsNullOrEmpty(LocalSavesPath) || !Directory.Exists(LocalSavesPath))
                {
                    Log("Local save folder not found.");
                    return;
                }
                string folderLink = textBoxFolderLink.Text.Trim();
                string folderId = GetFolderIdFromLink(folderLink);
                if (string.IsNullOrEmpty(folderId))
                {
                    Log("Invalid shared folder URL.");
                    return;
                }
                string slot = comboBoxSlot.SelectedItem.ToString();
                string saveFolder = Path.Combine(LocalSavesPath, slot);
                if (!Directory.Exists(saveFolder))
                {
                    Log($"Save folder '{saveFolder}' not found.");
                    return;
                }

                string zipFile = Path.Combine(Path.GetTempPath(), slot + ".zip");
                if (File.Exists(zipFile)) File.Delete(zipFile);
                ZipFile.CreateFromDirectory(saveFolder, zipFile);

                UpdateProgress("Uploading Save Files... 0%", 0);
                Log("Uploading Save Files...");
                Log("Upload time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = slot + ".zip",
                    Parents = new List<string> { folderId }
                };

                using (var fs = new FileStream(zipFile, FileMode.Open))
                {
                    var request = currentAccount.Service.Files.Create(fileMetadata, fs, "application/zip");
                    request.Fields = "id, modifiedTime";
                    request.ProgressChanged += progress =>
                    {
                        if (progress.Status == Google.Apis.Upload.UploadStatus.Uploading)
                        {
                            int percent = (int)(progress.BytesSent * 100 / fs.Length);
                            UpdateProgress("Uploading Save Files... " + percent + "%", percent);
                        }
                        else if (progress.Status == Google.Apis.Upload.UploadStatus.Completed)
                        {
                            UpdateProgress("Upload completed at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 100);
                        }
                    };
                    await request.UploadAsync();
                    var f = request.ResponseBody;
                    Log("Upload completed at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    Log("Save file uploaded. File ID: " + f.Id);
                }

                Thread.Sleep(1000);
                UpdateProgress("", 0);
            }
            catch (Exception ex)
            {
                Log("Upload error: " + ex.Message);
            }
        }

        private async Task DownloadFromSheetAsync()
        {
            try
            {
                string sheetUrl = textBoxSheetLink.Text.Trim();
                if (string.IsNullOrEmpty(sheetUrl))
                {
                    Log("Google Sheet URL not provided.");
                    return;
                }

                string csvData;
                using (var wc = new WebClient())
                    csvData = await wc.DownloadStringTaskAsync(sheetUrl);

                if (string.IsNullOrEmpty(csvData))
                {
                    Log("Failed to retrieve data from the sheet.");
                    return;
                }

                List<FileRecord> records;
                using (var reader = new StringReader(csvData))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    records = csv.GetRecords<FileRecord>().ToList();
                }

                if (records == null || records.Count == 0)
                {
                    Log("No records found in the sheet.");
                    return;
                }

                // Сортировка по дате
                var latestRecord = records
                  .OrderByDescending(r =>
                  {
                      if (DateTime.TryParseExact(
                              r.DateTime,
                              "dd.MM.yyyy HH:mm:ss",
                              CultureInfo.InvariantCulture,
                              DateTimeStyles.None,
                              out var dt1))
                          return dt1;
                      if (DateTime.TryParse(
                              r.DateTime,
                              CultureInfo.CurrentCulture,
                              DateTimeStyles.None,
                              out var dt2))
                          return dt2;
                      return DateTime.MinValue;
                  })
                  .First();

                Log("Downloading Save File from: " + latestRecord.DateTime);

                string slot = comboBoxSlot.SelectedItem.ToString();
                string saveFolder = Path.Combine(LocalSavesPath, slot);

                if (Directory.Exists(saveFolder))
                {
                    var dr = MessageBox.Show(
                        $"The slot {slot} already contains save data. Do you really want to overwrite it? This action is irreversible.",
                        "Confirm Overwrite",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (dr == DialogResult.Yes)
                    {
                        Directory.Delete(saveFolder, true);
                        Directory.CreateDirectory(saveFolder);
                    }
                    else
                    {
                        Log("Download canceled by user.");
                        return;
                    }
                }
                else
                {
                    Directory.CreateDirectory(saveFolder);
                }

                string fileId = ExtractFileIdFromUrl(latestRecord.DownloadURL);
                if (string.IsNullOrEmpty(fileId))
                {
                    Log("Failed to extract file ID from URL: " + latestRecord.DownloadURL);
                    return;
                }

                string directDownloadUrl = $"https://drive.google.com/uc?export=download&id={fileId}";
                Log("Direct download URL: " + directDownloadUrl);

                string tempZip = Path.Combine(Path.GetTempPath(), latestRecord.FileName);
                using (var wc = new WebClient())
                {
                    wc.DownloadProgressChanged += (s, e) =>
                    {
                        UpdateProgress("Downloading Save File... " + e.ProgressPercentage + "%", e.ProgressPercentage);
                    };
                    await wc.DownloadFileTaskAsync(new Uri(directDownloadUrl), tempZip);
                }
                Log("Download completed at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                UpdateProgress("", 0);

                ZipFile.ExtractToDirectory(tempZip, saveFolder);
                Log("Downloaded Save File applied.");

                FixPlayerFolder(saveFolder);
            }
            catch (Exception ex)
            {
                Log("Download error: " + ex.Message);
            }
        }

        private void FixPlayerFolder(string saveFolder)
        {
            try
            {
                string steamId = textBoxSteamID.Text.Trim();
                string playersFolder = Path.Combine(saveFolder, "Players");
                string player0Folder = Path.Combine(playersFolder, "Player_0");
                string playerJsonPath = Path.Combine(player0Folder, "Player.json");

                if (!File.Exists(playerJsonPath))
                {
                    Log("Player.json not found in Player_0 folder.");
                    return;
                }

                string json = File.ReadAllText(playerJsonPath);
                dynamic obj = JsonConvert.DeserializeObject(json);
                string playerCode = (string)obj.PlayerCode;

                if (playerCode == steamId)
                {
                    Log("Player configuration is correct.");
                    return;
                }
                else
                {
                    string newNameForPlayer0 = "Player_" + playerCode;
                    string newPathForPlayer0 = Path.Combine(playersFolder, newNameForPlayer0);
                    if (Directory.Exists(newPathForPlayer0))
                        Directory.Delete(newPathForPlayer0, true);
                    Directory.Move(player0Folder, newPathForPlayer0);

                    string targetFolder = Path.Combine(playersFolder, "Player_" + steamId);
                    if (Directory.Exists(targetFolder))
                        Directory.Move(targetFolder, player0Folder);

                    Log("Applied Player Configuration");
                }
            }
            catch (Exception ex)
            {
                Log("Error in fixing player folder: " + ex.Message);
            }
        }

        private string ExtractFileIdFromUrl(string url)
        {
            try
            {
                string marker = "/d/";
                int index = url.IndexOf(marker);
                if (index == -1) return null;
                int start = index + marker.Length;
                int end = url.IndexOf('/', start);
                if (end == -1) end = url.Length;
                return url.Substring(start, end - start);
            }
            catch
            {
                return null;
            }
        }
    }

    public class AccountInfo
    {
        public string StoreKey { get; set; }
        public string Email { get; set; }
        public DriveService Service { get; set; }
        public string AvatarUrl { get; set; }
    }

    public class FileRecord
    {
        [Name("File Name")]
        public string FileName { get; set; }

        [Name("Download URL")]
        public string DownloadURL { get; set; }

        [Name("Date and Time")]
        public string DateTime { get; set; }

        [Name("Uploader Email")]
        public string UploaderEmail { get; set; }
    }
}
