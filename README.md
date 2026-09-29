<div align="center">

<h1>Yeondo</h1>
<p>Mass creator of symbolic links, junctions, and hard links for Windows.</p>

<p>
  <img src="https://img.shields.io/badge/version-1.12.0-blue" alt="Version 1.12.0" />
  <img src="https://img.shields.io/badge/.NET-10-purple" alt=".NET 10" />
  <img src="https://img.shields.io/badge/platform-Windows%20x64-lightgrey" alt="Windows x64" />
</p>

<img width="235" alt="Main window" src="https://github.com/user-attachments/assets/daa8ea4a-2874-4397-9223-e10f1ea9279f" />
<img width="235" alt="Item list" src="https://github.com/user-attachments/assets/23ce2ecd-0df2-4e20-9174-091f6b51249d" />
<img width="235" alt="Status" src="https://github.com/user-attachments/assets/10e117ab-6012-4337-ab32-94064cd5b2d1" />

</div>

## Features

- **Three link types** — symbolic, junction, hard link
- **Batch processing** — thousands of files and folders in one run
- **Drag & drop** — drop items straight onto the window
- **Cancellable** — the Create button turns into Cancel; finished links are kept
- **Grouped failures** — errors are summarized by cause instead of repeated hundreds of times
- **Logging** — every run writes a detailed report
- **Localized** — English and Russian, with user-editable files

## Quick Start

1. Download the archive, extract it anywhere, and run `Yeondo.exe`.
2. Add items with the 📄 / 📁 buttons, or drag them onto the window.
3. Pick a link type, choose a target folder, press **Create**.

`Enter` creates the run when the button is active; right-click an item for its context menu.
If a run is cancelled, links already created stay in place and the rest are skipped.

## Link Types

| Type | Works with | Notes |
|------|-----------|-------|
| **Symbolic** | Files & folders | Needs Administrator, or Developer Mode enabled |
| **Junction** | Folders only | No privileges required |
| **Hard link** | Files only | Source must exist, same NTFS volume only |

## Localization

The interface follows the system language. Translations live in `i18n/` next to the
executable — edit `en.json` or `ru.json` freely, no tools required.

To add a language, drop a `{code}.json` file into `i18n/` using `en.json` as the template.
Missing keys fall back to the built-in English text, so partial files are safe: the
interface stays readable instead of going blank. The same fallback applies if a file is
deleted, unreadable, or not valid JSON — the application still starts.

All application files (settings, translations, logs) stay next to the executable. Nothing
is written to system folders, so the app works from a read-only location.

## Troubleshooting

| Problem | Cause | Fix |
|---------|-------|-----|
| Hard link fails | Source is on another volume | Hard links only work inside one NTFS volume |
| Junction fails | A file was selected | Junctions accept folders only |
| *Access denied* | No privilege for symbolic links | Run as Administrator or enable Developer Mode — junctions need neither |
| *Link name already used* | Two sources share a file name, e.g. `A\report.txt` and `B\report.txt` | Rename one, or run them separately; the first is still created |
| *Cannot derive a link name* | A drive or share root was selected (`C:\`, `\\server\share\`) | Select a folder inside it instead |
| Won't start | .NET 10 Desktop Runtime missing | Install it from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0) |

Logs for every run are written to `logs/symlink_YYYYMMDD_HHMMSS.log`; the **Logs** button in
the status bar appears when there is something to show.

## Disclaimer

**No warranty or liability.** Yeondo is provided "as is" with no warranty of any kind
(Apache-2.0 §7), and the author is not liable for damage arising from its use (§8).

**No affiliation.** An independent project — not affiliated with, endorsed by, or sponsored
by Microsoft or any other company. Windows, NTFS, and .NET are trademarks of their respective
owners, used only to state compatibility; Apache-2.0 grants no trademark rights (§6).

**Links affect the whole system, not just this app.** Hard links share file data, so editing
one path changes all of them. Junctions and symbolic links are followed transparently by any
program, so scanners, sync tools, backup software, and cleaners can reach data outside the
folder you pointed them at. Deleting a link is normally safe, but a path that was replaced
by one keeps resolving to the link target until the link itself is removed.

**Back up anything important before creating links, and use at your own risk.**

---

Apache-2.0 — see [`LICENSE`](LICENSE) and [`NOTICE`](NOTICE).
