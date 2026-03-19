# Yeondo — Symbolic Link Creator

A simple and convenient utility for mass creation of symbolic links in Windows.

![Version](https://img.shields.io/badge/version-1.10.5-blue)
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

_Screenshots will be added soon._

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

- **English** — for all other languages
- **Russian** — if system language is Russian

Localization files are stored in the `i18n/` folder next to the executable:

- `ru.json` — Russian language
- `en.json` — English language

You can edit these files to customize interface texts.

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

## 📄 License

Copyright © 2026 vanja-san. All rights reserved.

## 📬 Contact

For questions and suggestions, please contact the developer.

---

**Yeondo** — Fast, Convenient, Reliable.
