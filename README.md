# Schedule-I-Save-Sync

**Schedule-I-Save-Sync** allows you to cloud-sync save files between Schedule I players.

Many players are left waiting for a host because Schedule I cannot be played without one? Schedule-I-Save-Sync solves this problem by synchronizing save files through Google Drive.

---

## Part I. Cloud-side Preparation  
*(Perform these steps only if YOU are the host of the Google Drive cloud storage.)*

1. Go to the [Google Cloud Console](https://console.cloud.google.com/).
2. Create a **New Project** (give it a title; location/organization is not required).
3. Choose your project if it is not selected already.
4. Navigate to **APIs & Services**.
5. Go to **OAuth consent screen**.
6. Click **Get started** and fill in the required fields:  
   - **App name**  
   - **User support email**  
   - **Audience type:** choose **External**  
   - In **Contact information**, provide your user support email  
   - Tick the **I agree to the Google API Services: User Data Policy** checkbox.
7. Click **Continue** and then **Create**.
8. Return to **APIs & Services**.
9. Go to the **Library**.
10. Search for and enable the **Google Drive API**.
11. Navigate to **Credentials**.
12. Click **Create credentials** and choose **OAuth client ID**.
13. Choose **Desktop app** as the Application type and name it as you wish.
14. Copy the **Client ID** and **Client Secret** and save them somewhere (you will need these later).
15. Go back to **OAuth consent screen**, then to **Data Access** and click **Add or remove scopes**.
16. Select the following scopes:  
    - `.../auth/userinfo.email`  
    - `.../auth/userinfo.profile`  
    - `.../auth/drive.file`  
    Click **Update** and then **Save**.
17. Ask your friends (with whom you'll share saves) for their Gmail addresses.
18. In the **Audience** section, click **Add users** and paste your friends' emails one by one.
19. Return to your project's main page and copy your **Project ID**.

20. Download the latest release of **Schedule-I-Save-Sync** and unpack it.
21. Launch and close `ScheduleISync.exe`.
22. Open the generated `GCloudConsoleConfig.json` file and fill in the required fields with the previously copied variables.
23. Launch `ScheduleISync.exe` again and try to **Sign In**. If everything is configured correctly, you will be able to log in.
24. In your Google Drive, create a new folder and share it with anyone who has the link, granting **Editor** access.
25. Copy the link to this folder and save it somewhere.
26. Outside of this folder, create a new empty Google Sheet.
27. Go to **Extensions → Apps Script** in the Google Sheet and paste the code below (replace `"your_folder_id"` with your shared folder ID):

    ```js
    function updateFileList() {
      // Set the ID of your shared folder
      var FOLDER_ID = "your_folder_id";
      
      var folder = DriveApp.getFolderById(FOLDER_ID);
      var files = folder.getFiles();
      var ss = SpreadsheetApp.getActiveSpreadsheet();
      var sheet = ss.getSheets()[0];
      
      sheet.clearContents();
      sheet.appendRow(["File Name", "Download URL", "Date and Time", "Uploader Email"]);
      
      while (files.hasNext()) {
        var file = files.next();
        var fileName = file.getName();
        var fileUrl = file.getUrl(); 
        var dateTime = file.getLastUpdated(); 
        var ownerEmail = "";
        try {
          ownerEmail = file.getOwner().getEmail();
        } catch (e) {
          ownerEmail = "Unknown";
        }
        sheet.appendRow([fileName, fileUrl, dateTime, ownerEmail]);
      }
    }
    ```

28. Save this script and run it for the first time.
29. In the **Triggers** section, click **Add trigger**.
30. Choose the function `updateFileList`, set it as a **Time-driven trigger** to run **Once per minute**, and then click **Save**.
31. Go back to your Google Sheet, click **File → Share → Publish to the web**, choose **CSV file** (instead of Web page), expand the “Published content and settings” section, click **Start Publishing**, and copy the resulting link.
32. Ensure that anyone with the link has **Reader** access.
33. Send your friends both the shared folder link and the published CSV link of the sheet, along with your `GCloudConsoleConfig.json` file.

---

## Part II. Client-side Setup

1. Download the latest release of **Schedule-I-Save-Sync** and unpack it.
2. Place the `GCloudConsoleConfig.json` file in the same folder as `ScheduleISync.exe`.
3. Launch `ScheduleISync.exe`.
4. Sign in.
5. Fill in the required fields (Steam ID, Shared Folder URL, Google Sheet URL, and select a slot).

Now you can upload and download save files seamlessly!


Happy syncing!
