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
using System.Globalization; // For CsvHelper

namespace ScheduleISync
{
    public partial class MainForm : Form
    {
        private GCloudConsoleConfig gcloudConfig;   // Loaded from GCloudConsoleConfig.json
        private UserSettings userSettings;          // Settings: SteamID, FolderLink, SheetLink, SelectedSlot, LastUsedAccount
        private AccountInfo currentAccount = null;  // Only one account is supported – sign in via button
        private readonly string appName = "ScheduleISync";

        // Timer for checking if the game is running (every 5 seconds)
        private System.Windows.Forms.Timer processCheckTimer;

        // Local save path based on SteamID
        private string LocalSavesPath
        {
            get
            {
                string steamId = string.Empty;
                if (this.IsHandleCreated)
                    this.Invoke(new Action(() => { steamId = textBoxSteamID.Text.Trim(); }));
                else
                    steamId = textBoxSteamID.Text.Trim();
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

            // Set avatar in upper right corner (see Designer)
            // Subscribe to Load event
            this.Load += MainForm_Load;

            // Fill ComboBox with slots
            comboBoxSlot.Items.AddRange(new string[] { "SaveGame_1", "SaveGame_2", "SaveGame_3", "SaveGame_4", "SaveGame_5" });
            userSettings = UserSettingsManager.LoadSettings();
            if (!string.IsNullOrEmpty(userSettings.SelectedSlot))
                comboBoxSlot.SelectedItem = userSettings.SelectedSlot;
            else
                comboBoxSlot.SelectedIndex = 0;

            // Start timer to check if the game is running (every 5 seconds)
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
                MessageBox.Show("GCloudConsoleConfig.json file is missing or empty. Please fill it in and restart the application.",
                    "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Environment.Exit(0);
            }

            userSettings = UserSettingsManager.LoadSettings();
            this.Invoke(new Action(() =>
            {
                textBoxSteamID.Text = userSettings.SteamID;
                textBoxFolderLink.Text = userSettings.FolderLink ?? "";
                textBoxSheetLink.Text = userSettings.SheetLink ?? "";
                if (!string.IsNullOrEmpty(userSettings.SelectedSlot))
                    comboBoxSlot.SelectedItem = userSettings.SelectedSlot;
            }));

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

        // Block UI if the game is running
        private void UpdateUIState()
        {
            bool gameRunning = IsGameRunning();
            bool enableUI = (currentAccount != null) && !gameRunning;
            Action updateAction = () =>
            {
                buttonSignIn.Enabled = !enableUI;
                textBoxSteamID.Enabled = enableUI;
                textBoxFolderLink.Enabled = enableUI;
                textBoxSheetLink.Enabled = enableUI;
                comboBoxSlot.Enabled = enableUI;
                buttonUpload.Enabled = enableUI;
                buttonDownload.Enabled = enableUI;
            };

            if (this.IsHandleCreated)
                this.Invoke(updateAction);
            else
                updateAction();
        }

        // Check if the game (process "Schedule I") is running
        private bool IsGameRunning()
        {
            try
            {
                var processes = Process.GetProcessesByName("Schedule I");
                return processes.Length > 0;
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

        // Update progress display (progress bar and label) on the same line
        private void UpdateProgress(string message, int percent)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
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

        // Simplified logging for non-progress messages
        private void Log(string message)
        {
            if (textBoxLog.InvokeRequired)
                textBoxLog.Invoke(new Action(() => textBoxLog.AppendText(message + Environment.NewLine)));
            else
                textBoxLog.AppendText(message + Environment.NewLine);
        }

        // Sign in button click
        private async void buttonSignIn_Click(object sender, EventArgs e)
        {
            await SignInAsync();
        }

        // Upload button click – uploads local save file to Google Drive
        private void buttonUpload_Click(object sender, EventArgs e)
        {
            Task.Run(() => UploadSaveAsync());
        }

        // Download button click – downloads save file using data from Google Sheet
        private void buttonDownload_Click(object sender, EventArgs e)
        {
            Task.Run(() => DownloadFromSheetAsync());
        }

        // Sign in using Google Drive API
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
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(oauthJson);
                using (var ms = new MemoryStream(bytes))
                {
                    var clientSecrets = GoogleClientSecrets.FromStream(ms).Secrets;
                    var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        clientSecrets,
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
                    var aboutRequest = service.About.Get();
                    aboutRequest.Fields = "user(emailAddress,photoLink)";
                    var about = aboutRequest.Execute();
                    string email = about.User.EmailAddress;
                    string photoLink = about.User.PhotoLink;
                    Log("Signed in as " + email);

                    currentAccount = new AccountInfo
                    {
                        StoreKey = storeKey,
                        Email = email,
                        Service = service,
                        AvatarUrl = photoLink
                    };

                    userSettings.LastUsedAccount = storeKey;
                    UserSettingsManager.SaveSettings(userSettings);

                    await UpdateAvatarAsync(photoLink);
                    UpdateUIState();

                    // Hide Sign In button after successful sign in
                    if (this.InvokeRequired)
                        this.Invoke(new Action(() => { buttonSignIn.Visible = false; }));
                    else
                        buttonSignIn.Visible = false;
                }
            }
            catch (Exception ex)
            {
                Log("Sign in error: " + ex.Message);
            }
        }

        // Download avatar and crop to circle
        private async Task UpdateAvatarAsync(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                {
                    if (this.IsHandleCreated)
                        this.Invoke(new Action(() => pictureBoxAvatar.Image = null));
                    else
                        pictureBoxAvatar.Image = null;
                    return;
                }
                using (WebClient wc = new WebClient())
                {
                    byte[] data = await wc.DownloadDataTaskAsync(imageUrl);
                    using (var mem = new MemoryStream(data))
                    {
                        Image src = Image.FromStream(mem);
                        var circle = CropToCircle(src, pictureBoxAvatar.Width, this.BackColor);
                        if (this.IsHandleCreated)
                            this.Invoke(new Action(() => pictureBoxAvatar.Image = circle));
                        else
                            pictureBoxAvatar.Image = circle;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Avatar load error: " + ex.Message);
            }
        }

        // Crop image to circle
        private Image CropToCircle(Image src, int size, Color backColor)
        {
            Bitmap dst = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(dst))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (SolidBrush brush = new SolidBrush(backColor))
                {
                    g.FillRectangle(brush, 0, 0, size, size);
                }
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(0, 0, size, size);
                    g.SetClip(path);
                    g.DrawImage(src, 0, 0, size, size);
                }
            }
            return dst;
        }

        // Extract folderId from a URL (e.g., https://drive.google.com/drive/folders/ABC123?usp=drive_link)
        private string GetFolderIdFromLink(string link)
        {
            try
            {
                int index = link.IndexOf("/folders/");
                if (index == -1)
                    return null;
                int start = index + "/folders/".Length;
                int end = link.IndexOfAny(new char[] { '/', '?' }, start);
                if (end == -1)
                    return link.Substring(start);
                return link.Substring(start, end - start);
            }
            catch
            {
                return null;
            }
        }

        // Upload save file to Google Drive via API, with progress updates
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
                string folderLink = string.Empty;
                this.Invoke(new Action(() => { folderLink = textBoxFolderLink.Text.Trim(); }));
                string folderId = GetFolderIdFromLink(folderLink);
                if (string.IsNullOrEmpty(folderId))
                {
                    Log("Invalid shared folder URL.");
                    return;
                }
                string slot = string.Empty;
                this.Invoke(new Action(() => { slot = comboBoxSlot.SelectedItem.ToString(); }));
                string saveFolder = System.IO.Path.Combine(LocalSavesPath, slot);
                if (!Directory.Exists(saveFolder))
                {
                    Log($"Save folder '{saveFolder}' not found.");
                    return;
                }
                string zipFile = System.IO.Path.Combine(Path.GetTempPath(), slot + ".zip");
                if (File.Exists(zipFile))
                    File.Delete(zipFile);
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

        // Download save file using data from Google Sheet, with progress updates and folder existence check BEFORE downloading
        private async Task DownloadFromSheetAsync()
        {
            try
            {
                // Get the Google Sheet URL from textBoxSheetLink
                string sheetUrl = string.Empty;
                this.Invoke(new Action(() => { sheetUrl = textBoxSheetLink.Text.Trim(); }));
                if (string.IsNullOrEmpty(sheetUrl))
                {
                    Log("Google Sheet URL not provided.");
                    return;
                }

                // Download CSV data from the sheet
                string csvData = "";
                using (WebClient wc = new WebClient())
                {
                    csvData = await wc.DownloadStringTaskAsync(sheetUrl);
                }
                if (string.IsNullOrEmpty(csvData))
                {
                    Log("Failed to retrieve data from the sheet.");
                    return;
                }

                // Parse CSV using CsvHelper
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

                // Select the record with the latest date
                var latestRecord = records.OrderByDescending(r => DateTime.Parse(r.DateTime, CultureInfo.InvariantCulture)).First();
                Log("Downloading Save File from: " + latestRecord.DateTime);

                // Determine local save folder BEFORE downloading
                string saveFolder = string.Empty;
                this.Invoke(new Action(() => { saveFolder = Path.Combine(LocalSavesPath, comboBoxSlot.SelectedItem.ToString()); }));

                // Check if the save folder exists and prompt the user BEFORE downloading the file
                if (Directory.Exists(saveFolder))
                {
                    DialogResult dr = DialogResult.None;
                    this.Invoke(new Action(() =>
                    {
                        dr = MessageBox.Show(
                            $"The slot {comboBoxSlot.SelectedItem} already contains save data. Do you really want to overwrite it? This action is irreversible.",
                            "Confirm Overwrite",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);
                    }));
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

                // Extract FILE_ID from DownloadURL
                string fileId = ExtractFileIdFromUrl(latestRecord.DownloadURL);
                if (string.IsNullOrEmpty(fileId))
                {
                    Log("Failed to extract file ID from URL: " + latestRecord.DownloadURL);
                    return;
                }
                // Form direct download URL
                string directDownloadUrl = $"https://drive.google.com/uc?export=download&id={fileId}";
                Log("Direct download URL: " + directDownloadUrl);

                // Download file with progress using WebClient
                string tempZip = Path.Combine(Path.GetTempPath(), latestRecord.FileName);
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadProgressChanged += (s, e) =>
                    {
                        UpdateProgress("Downloading Save File... " + e.ProgressPercentage + "%", e.ProgressPercentage);
                    };
                    await wc.DownloadFileTaskAsync(new Uri(directDownloadUrl), tempZip);
                }
                Log("Download completed at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                UpdateProgress("", 0);

                // Extract downloaded ZIP to the save folder
                ZipFile.ExtractToDirectory(tempZip, saveFolder);
                Log("Downloaded Save File applied.");

                // Fix player folder configuration if needed
                FixPlayerFolder(saveFolder);
            }
            catch (Exception ex)
            {
                Log("Download error: " + ex.Message);
            }
        }

        // Fix player folder configuration:
        // Check Players\Player_0\Player.json for "PlayerCode" and adjust folder names if necessary.
        // After adjustments, output a single message "Applied Player Configuration".
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
                    // Rename folders as required
                    string newNameForPlayer0 = "Player_" + playerCode;
                    string newPathForPlayer0 = Path.Combine(playersFolder, newNameForPlayer0);
                    if (Directory.Exists(newPathForPlayer0))
                        Directory.Delete(newPathForPlayer0, true);
                    Directory.Move(player0Folder, newPathForPlayer0);

                    string targetFolder = Path.Combine(playersFolder, "Player_" + steamId);
                    if (Directory.Exists(targetFolder))
                    {
                        Directory.Move(targetFolder, player0Folder);
                    }
                    Log("Applied Player Configuration");
                }
            }
            catch (Exception ex)
            {
                Log("Error in fixing player folder: " + ex.Message);
            }
        }

        // Extract file ID from a URL of the form "https://drive.google.com/file/d/FILE_ID/view?usp=sharing"
        private string ExtractFileIdFromUrl(string url)
        {
            try
            {
                string marker = "/d/";
                int index = url.IndexOf(marker);
                if (index == -1)
                    return null;
                int start = index + marker.Length;
                int end = url.IndexOf('/', start);
                if (end == -1)
                    end = url.Length;
                return url.Substring(start, end - start);
            }
            catch
            {
                return null;
            }
        }

        // AutoSync is disabled – synchronization is done manually via the "Download Save Files" button
        private async Task AutoSyncAsync()
        {
            await Task.CompletedTask;
        }

        private async void SyncTimer_Tick(object sender, EventArgs e)
        {
            UpdateUIState();
            await Task.CompletedTask;
        }
    }

    // Place these classes after MainForm

    public class AccountInfo
    {
        public string StoreKey { get; set; }  // Key for FileDataStore
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
