namespace ScheduleISync
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.comboBoxSlot = new System.Windows.Forms.ComboBox();
            this.labelSlot = new System.Windows.Forms.Label();
            this.buttonUpload = new System.Windows.Forms.Button();
            this.buttonDownload = new System.Windows.Forms.Button();
            this.textBoxLog = new System.Windows.Forms.TextBox();
            this.labelFolderLink = new System.Windows.Forms.Label();
            this.textBoxFolderLink = new System.Windows.Forms.TextBox();
            this.buttonSignIn = new System.Windows.Forms.Button();
            this.pictureBoxAvatar = new System.Windows.Forms.PictureBox();
            this.labelSteamID = new System.Windows.Forms.Label();
            this.textBoxSteamID = new System.Windows.Forms.TextBox();
            this.labelSheetLink = new System.Windows.Forms.Label();
            this.textBoxSheetLink = new System.Windows.Forms.TextBox();
            this.progressBarStatus = new System.Windows.Forms.ProgressBar();
            this.labelProgress = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxAvatar)).BeginInit();
            this.SuspendLayout();
            // 
            // comboBoxSlot
            // 
            this.comboBoxSlot.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSlot.FormattingEnabled = true;
            this.comboBoxSlot.Location = new System.Drawing.Point(12, 178);
            this.comboBoxSlot.Name = "comboBoxSlot";
            this.comboBoxSlot.Size = new System.Drawing.Size(200, 21);
            this.comboBoxSlot.TabIndex = 7;
            // 
            // labelSlot
            // 
            this.labelSlot.AutoSize = true;
            this.labelSlot.Location = new System.Drawing.Point(12, 160);
            this.labelSlot.Name = "labelSlot";
            this.labelSlot.Size = new System.Drawing.Size(61, 13);
            this.labelSlot.TabIndex = 6;
            this.labelSlot.Text = "Select Slot:";
            // 
            // buttonUpload
            // 
            this.buttonUpload.Location = new System.Drawing.Point(12, 260);
            this.buttonUpload.Name = "buttonUpload";
            this.buttonUpload.Size = new System.Drawing.Size(200, 23);
            this.buttonUpload.TabIndex = 9;
            this.buttonUpload.Text = "Upload Save Files";
            this.buttonUpload.UseVisualStyleBackColor = true;
            this.buttonUpload.Click += new System.EventHandler(this.buttonUpload_Click);
            // 
            // buttonDownload
            // 
            this.buttonDownload.Location = new System.Drawing.Point(12, 300);
            this.buttonDownload.Name = "buttonDownload";
            this.buttonDownload.Size = new System.Drawing.Size(200, 23);
            this.buttonDownload.TabIndex = 10;
            this.buttonDownload.Text = "Download Save Files";
            this.buttonDownload.UseVisualStyleBackColor = true;
            this.buttonDownload.Click += new System.EventHandler(this.buttonDownload_Click);
            // 
            // textBoxLog
            // 
            this.textBoxLog.Location = new System.Drawing.Point(230, 12);
            this.textBoxLog.Multiline = true;
            this.textBoxLog.Name = "textBoxLog";
            this.textBoxLog.ReadOnly = true;
            this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxLog.Size = new System.Drawing.Size(400, 311);
            this.textBoxLog.TabIndex = 11;
            // 
            // labelFolderLink
            // 
            this.labelFolderLink.AutoSize = true;
            this.labelFolderLink.Location = new System.Drawing.Point(12, 12);
            this.labelFolderLink.Name = "labelFolderLink";
            this.labelFolderLink.Size = new System.Drawing.Size(101, 13);
            this.labelFolderLink.TabIndex = 0;
            this.labelFolderLink.Text = "Shared Folder URL:";
            // 
            // textBoxFolderLink
            // 
            this.textBoxFolderLink.Location = new System.Drawing.Point(12, 30);
            this.textBoxFolderLink.Name = "textBoxFolderLink";
            this.textBoxFolderLink.Size = new System.Drawing.Size(200, 20);
            this.textBoxFolderLink.TabIndex = 1;
            // 
            // buttonSignIn
            // 
            this.buttonSignIn.Location = new System.Drawing.Point(12, 220);
            this.buttonSignIn.Name = "buttonSignIn";
            this.buttonSignIn.Size = new System.Drawing.Size(200, 23);
            this.buttonSignIn.TabIndex = 8;
            this.buttonSignIn.Text = "Sign In";
            this.buttonSignIn.UseVisualStyleBackColor = true;
            this.buttonSignIn.Click += new System.EventHandler(this.buttonSignIn_Click);
            // 
            // pictureBoxAvatar
            // 
            this.pictureBoxAvatar.Location = new System.Drawing.Point(636, 12);
            this.pictureBoxAvatar.Name = "pictureBoxAvatar";
            this.pictureBoxAvatar.Size = new System.Drawing.Size(48, 48);
            this.pictureBoxAvatar.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxAvatar.TabIndex = 12;
            this.pictureBoxAvatar.TabStop = false;
            // 
            // labelSteamID
            // 
            this.labelSteamID.AutoSize = true;
            this.labelSteamID.Location = new System.Drawing.Point(12, 110);
            this.labelSteamID.Name = "labelSteamID";
            this.labelSteamID.Size = new System.Drawing.Size(63, 13);
            this.labelSteamID.TabIndex = 4;
            this.labelSteamID.Text = "SteamID64:";
            // 
            // textBoxSteamID
            // 
            this.textBoxSteamID.Location = new System.Drawing.Point(12, 128);
            this.textBoxSteamID.Name = "textBoxSteamID";
            this.textBoxSteamID.Size = new System.Drawing.Size(200, 20);
            this.textBoxSteamID.TabIndex = 5;
            // 
            // labelSheetLink
            // 
            this.labelSheetLink.AutoSize = true;
            this.labelSheetLink.Location = new System.Drawing.Point(12, 60);
            this.labelSheetLink.Name = "labelSheetLink";
            this.labelSheetLink.Size = new System.Drawing.Size(100, 13);
            this.labelSheetLink.TabIndex = 2;
            this.labelSheetLink.Text = "Google Sheet URL:";
            // 
            // textBoxSheetLink
            // 
            this.textBoxSheetLink.Location = new System.Drawing.Point(12, 78);
            this.textBoxSheetLink.Name = "textBoxSheetLink";
            this.textBoxSheetLink.Size = new System.Drawing.Size(200, 20);
            this.textBoxSheetLink.TabIndex = 3;
            // 
            // progressBarStatus
            // 
            this.progressBarStatus.Location = new System.Drawing.Point(230, 339);
            this.progressBarStatus.Name = "progressBarStatus";
            this.progressBarStatus.Size = new System.Drawing.Size(400, 23);
            this.progressBarStatus.TabIndex = 13;
            // 
            // labelProgress
            // 
            this.labelProgress.AutoSize = true;
            this.labelProgress.Location = new System.Drawing.Point(12, 370);
            this.labelProgress.Name = "labelProgress";
            this.labelProgress.Size = new System.Drawing.Size(0, 13);
            this.labelProgress.TabIndex = 14;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(576, 370);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(103, 13);
            this.label1.TabIndex = 15;
            this.label1.Text = "Made by Rider, v1.1";
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(691, 392);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.labelProgress);
            this.Controls.Add(this.progressBarStatus);
            this.Controls.Add(this.pictureBoxAvatar);
            this.Controls.Add(this.textBoxLog);
            this.Controls.Add(this.buttonDownload);
            this.Controls.Add(this.buttonUpload);
            this.Controls.Add(this.comboBoxSlot);
            this.Controls.Add(this.labelSlot);
            this.Controls.Add(this.textBoxSteamID);
            this.Controls.Add(this.labelSteamID);
            this.Controls.Add(this.buttonSignIn);
            this.Controls.Add(this.textBoxSheetLink);
            this.Controls.Add(this.labelSheetLink);
            this.Controls.Add(this.textBoxFolderLink);
            this.Controls.Add(this.labelFolderLink);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Schedule I Save Sync";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxAvatar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxSlot;
        private System.Windows.Forms.Label labelSlot;
        private System.Windows.Forms.Button buttonUpload;
        private System.Windows.Forms.Button buttonDownload;
        private System.Windows.Forms.TextBox textBoxLog;
        private System.Windows.Forms.Label labelFolderLink;
        private System.Windows.Forms.TextBox textBoxFolderLink;
        private System.Windows.Forms.Button buttonSignIn;
        private System.Windows.Forms.PictureBox pictureBoxAvatar;
        private System.Windows.Forms.Label labelSteamID;
        private System.Windows.Forms.TextBox textBoxSteamID;
        private System.Windows.Forms.Label labelSheetLink;
        private System.Windows.Forms.TextBox textBoxSheetLink;
        private System.Windows.Forms.ProgressBar progressBarStatus;
        private System.Windows.Forms.Label labelProgress;
        private System.Windows.Forms.Label label1;
    }
}
