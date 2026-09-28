# Yeondo — Symbolic Link Creator

A simple and convenient utility for mass creation of symbolic links in Windows.

![Version](https://img.shields.io/badge/version-1.11.0-blue)
![.NET](https://img.shields.io/badge/.NET-10-purple)
![Platform](https://img.shields.io/badge/platform-Windows%20x64-lightgrey)

## 📌 Features

- **Three link types:** Symbolic Link, Junction, Hard Link
- **Batch creation:** Add files and folders in bulk
- **Drag & Drop:** Drag files directly into the application window
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
   - While links are being created the button turns into **"Cancel"** — click it to stop
     the run. Items already created are kept; the rest stay untouched.
   - If some items fail, a summary dialog groups the reasons by count so repeated
     Windows errors are not listed hundreds of times. Full details go to the log.

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

Any key you omit falls back to its built-in English text, so a partial file is safe — the
interface stays readable instead of showing blanks or a crash.

**Full key list (44):**

`AppTitle`, `AddFilesTooltip`, `AddFoldersTooltip`, `CreateButton`, `CancelButton`,
`OutputPathLabel`, `SelectPath`, `BrowseButton`, `BrowseTooltip`, `ClearButton`,
`LogsButton`, `ReadyStatus`, `CreatedCount`, `FailedCount`, `SuccessMessage`,
`RemoveMenuItem`, `OpenFolderTooltip`, `SelectFilesTitle`, `SelectFoldersTitle`,
`SelectTargetTitle`, `ErrorTitle`, `CreateTargetFolderError`, `LinkTypeSymbolic`,
`LinkTypeJunction`, `LinkTypeHardLink`, `LinkTypeUnknown`, `LogHeader`, `LogTargetFolder`,
`LogItemCount`, `LogSuccess`, `LogError`, `LogSummary`, `LogCancelled`, `StatusCancelled`,
`JunctionFolderOnly`, `JunctionSourceRequired`, `HardLinkFilesOnly`,
`HardLinkSourceNotFound`, `LinkNameUnavailable`, `LinkNameConflict`, `FailureSummaryTitle`,
`FailureReasonLine`, `FailureSummaryHint`, `ItemsAdded`

If the file is missing, unreadable, or not valid JSON, the built-in English texts are used
and the interface starts normally.

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
Junctions do not need Administrator.

---

### "Another selected item already has this link name"

**Cause:** Two selected sources have the same file name — for example `A\report.txt` and
`B\report.txt`. A link name is taken from the source name only, so both would land on the
same path.

**Solution:** Split them into separate runs, or rename one of them beforehand. The first
item is still created; the conflicting one is skipped and reported.

---

### "Cannot derive a link name from this path"

**Cause:** A drive or share root was selected (`C:\`, `\\server\share\`). These have no name
component, so there is nothing to name the link after.

**Solution:** Select a folder inside it rather than the root itself.

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

**No warranty.** This application is provided "as is", without warranty of any kind, either
express or implied, including but not limited to the warranties of merchantability, fitness
for a particular purpose, and non-infringement. This mirrors sections 7 and 8 of the Apache
License 2.0, which governs this program.

**No liability.** To the maximum extent permitted by applicable law, the author and
copyright holders shall not be liable for any claim, damages, or other liability — whether in
an action of contract, tort, or otherwise — arising from, out of, or in connection with the
software or its use.

**No affiliation.** Yeondo is an independent project created by an individual contributor.
It is not affiliated with, associated with, endorsed by, sponsored by, or supervised by
Microsoft Corporation or any other company, organization, or individual. If you use this
software, you do so on your own initiative and at your own risk; the author cannot be held
responsible for how third parties use or distribute it.

**Trademarks.** Windows, NTFS, .NET, and other product and company names mentioned in this
document are the trademarks or registered trademarks of their respective owners. They are
used only to identify the platforms and technologies this software is compatible with. The
Apache License 2.0 grants no permission to use the names or logos of the author or
contributors, as noted in section 6 of that license.

**Use at your own risk.** Yeondo creates filesystem links, and these are visible to the whole
system, not just to this application:

- **Hard links** make several paths point to the same file data. Editing a file through one
  path changes it for all of them.
- **Junctions and symbolic links** are followed transparently by other programs. Software
  that scans, synchronizes, backs up, or deletes files may traverse them and act on data
  outside the folder you pointed them at.
- Removing a link is usually safe, but a path that was replaced by a junction or symbolic
  link will keep resolving to the new target until the link itself is deleted.

Before creating links over existing or important data, **make a backup**. The author cannot
be responsible for data loss, corrupted files, or damage caused by other software following
these links.

---

Made with ❤️!
