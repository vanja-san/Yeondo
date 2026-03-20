# Yeondo — Symbolic Link Creator

A simple and convenient utility for mass creation of symbolic links in Windows.

![Version](https://img.shields.io/badge/version-1.10.7-blue)
![.NET](https://img.shields.io/badge/.NET-10-purple)
![Platform](https://img.shields.io/badge/platform-Windows%20x64-lightgrey)

## 📌 Features

- **Three link types:** Symbolic Link, Junction, Hard Link
- **Batch creation:** Add files and folders in bulk
- **Drag & Drop:** Drag files directly into the application window
- **Auto language detection:** Russian or English interface
- **Compact UI:** Modern Windows 11 style design
- **Logging:** Detailed report on link creation results

## 🚀 Quick Start

### 1. Installation

Download and extract the application archive to any folder.

**Requirements:**
- Windows 10/11 x64
- .NET 10 Desktop Runtime *(if using version without bundled runtime)*

### 2. Launch

Run `Yeondo.exe`

### 3. Create a Link

1. **Add files or folders:**
   - Click 📄 (files) or 📁 (folders) button
   - Or drag files into the application window

2. **Select link type:**
   - **Symbolic** — universal links (files and folders)
   - **Junction** — folders only
   - **Hard Link** — files only

3. **Specify target folder:**
   - Click "Browse" and select destination folder
   - Or click the path to open it in Explorer

4. **Click "Create"**

---

## 📸 Screenshots

<img width="248" alt="Main" src="https://github.com/user-attachments/assets/daa8ea4a-2874-4397-9223-e10f1ea9279f" /> 
<img width="248" alt="List" src="https://github.com/user-attachments/assets/23ce2ecd-0df2-4e20-9174-091f6b51249d" />
<img width="248" alt="Status" src="https://github.com/user-attachments/assets/10e117ab-6012-4337-ab32-94064cd5b2d1" />

---

## 📖 Link Types

| Type | For | Features |
|------|-----|----------|
| **Symbolic** | Files & Folders | Works like a shortcut, requires admin privileges (without Developer Mode) |
| **Junction** | Folders only | Works at filesystem level, no admin required |
| **Hard Link** | Files only | File must exist, works only within same NTFS volume |

## ⌨️ Hotkeys

| Action | Keys |
|--------|------|
| Add files | — |
| Add folders | — |
| Create links | Enter (when button is active) |
| Open context menu | Right mouse button on item |

## 🌐 Localization

The application automatically detects the system language:

- **Russian** — if system language is Russian
- **English** — for all other languages

Localization files are stored in the `i18n/` folder next to the executable:

- `ru.json` — Russian language
- `en.json` — English language

You can edit these files to customize interface texts.

### Adding a Custom Language

To add your own language:

1. Create a file `i18n/{code}.json` (e.g., `fr.json` for French)
2. Copy the structure from `en.json`
3. Translate the values

**Example (fr.json):**
```json
{
  "AppTitle": "Yeondo - Créateur de liens symboliques",
  "AddFilesTooltip": "Ajouter des fichiers",
  "CreateButton": "Créer",
  "OutputPathLabel": "Chemin de sortie",
  "SelectPath": "Non sélectionné",
  ...
}
```

**Required keys:** `AppTitle`, `AddFilesTooltip`, `AddFoldersTooltip`, `CreateButton`, `OutputPathLabel`, `SelectPath`, `BrowseButton`, `BrowseTooltip`, `ClearButton`, `LogsButton`, `ReadyStatus`, `CreatedCount`, `FailedCount`, `SuccessMessage`, `RemoveMenuItem`, `OpenFolderTooltip`, `SelectFilesTitle`, `SelectFoldersTitle`, `SelectTargetTitle`, `ErrorTitle`, `CreateTargetFolderError`, `LinkTypeSymbolic`, `LinkTypeJunction`, `LinkTypeHardLink`, `LinkTypeUnknown`, `LogHeader`, `LogTargetFolder`, `LogItemCount`, `LogSuccess`, `LogError`, `LogSummary`

---

## ❓ Troubleshooting

### Hard Link Creation Error

**Cause:** File is on a different volume or drive.

**Solution:** Hard Link works only within a single NTFS volume.

---

### Junction Creation Error

**Cause:** A file was selected instead of a folder.

**Solution:** Junction works only with folders.

---

### "Access Denied" Error

**Cause:** Insufficient privileges to create symbolic links.

**Solution:** Run the application as Administrator or enable "Developer Mode" in Windows 10/11.

---

### Application Won't Start

**Cause:** .NET 10 Runtime is not installed.

**Solution:** Download and install [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

## 📝 Logging

Log files are created next to the application:
```
./logs/symlink_YYYYMMDD_HHMMSS.log
```

To view logs, click the **"Logs"** button in the status bar (appears when errors occur).

**All application files (settings, localization, logs) are created next to the executable** — no system folders!

## ⚠️ Disclaimer

This application is provided "as is" without any warranties. The author is not responsible for any damages or data losses resulting from the use of this software. Always backup important data before creating symbolic links.

## 📄 License

Copyright © 2026 vanja-san. All rights reserved.

**Code written with assistance from Qwen Code (AI Assistant)**

## 🙏 Acknowledgements

- **[Qwen Code](https://github.com/QwenLM/qwen-code)** — AI coding assistant that helped write this application

---

**Yeondo** — Fast, Convenient, Reliable.
