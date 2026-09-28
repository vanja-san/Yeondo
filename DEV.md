# Yeondo — Development Guide

Documentation for development, building, and publishing the application.

## 📋 Table of Contents

- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Build and Run](#build-and-run)
- [Publishing](#publishing)
- [Architecture](#architecture)
- [Localization](#localization)
- [Optimizations](#optimizations)

---

## 🔧 Technology Stack

| Component | Version / Technology |
|-----------|---------------------|
| **Framework** | .NET 10 |
| **UI** | WPF (Windows Presentation Foundation) |
| **Language** | C# 14 |
| **Architecture** | MVVM (Model-View-ViewModel) |
| **Win32 API** | P/Invoke (LibraryImport) |
| **Styles** | Fluent Design (Windows 11) |

---

## 📁 Project Structure

```
yeondo-app/
├── App.xaml(.cs)              # Entry point, single instance
├── MainWindow.xaml(.cs)       # Main window
├── AssemblyInfo.cs            # Assembly metadata
├── yeondo-app.csproj          # Project file
│
├── Commands/
│   └── RelayCommand.cs        # ICommand implementations (sync + async)
│
├── ViewModels/
│   └── MainViewModel.cs       # Main application logic
│
├── Models/
│   ├── LinkItem.cs            # Link item model
│   └── AppSettings.cs         # Application settings
│
├── Converters/
│   └── Converters.cs          # Value converters
│
├── Services/
│   ├── NativeMethods.cs       # P/Invoke: symlink, junction, hard link
│   ├── LocalizationService.cs # Localization service
│   ├── IDialogService.cs      # Dialog abstraction
│   └── DialogService.cs       # WPF dialog implementation
│
├── i18n/                      # Translation files (en.json, ru.json)
│
└── Assets/
    └── app.ico                # Application icon

DEV.md                          # This file
```

---

## ⚙️ Build and Run

### Development Requirements

- .NET 10 SDK
- Visual Studio 2022 / VS Code / Rider
- Windows 10/11 x64

### CLI Commands

```bash
# Build (Debug)
dotnet build "yeondo-app/yeondo-app.csproj"

# Build (Release)
dotnet build "yeondo-app/yeondo-app.csproj" -c Release

# Run
dotnet run --project "yeondo-app/yeondo-app.csproj"

# Clean
dotnet clean "yeondo-app/yeondo-app.csproj"
```

### Visual Studio

1. Open `yeondo-app.slnx`
2. Select **Debug** or **Release** configuration
3. Press **F5** to run or **Ctrl+Shift+B** to build

---

## 📦 Publishing

### Self-contained publishing (recommended)

Publish as a single file with embedded .NET Runtime:

```bash
dotnet publish "yeondo-app/yeondo-app.csproj" -c Release
```

**Result:**
- File: `bin/Release/net10.0-windows/win-x64/publish/Yeondo.exe`
- Size: ~66 MB
- Does not require .NET installation

### Publishing settings (.csproj)

```xml
<PropertyGroup>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
    <PublishReadyToRun>true</PublishReadyToRun>
    <DebugType>none</DebugType>
    <StripSymbols>true</StripSymbols>
</PropertyGroup>
```

### Size comparison

`Release` sets `SelfContained` and `PublishSingleFile` unconditionally, so a Release
publish is always self-contained. A framework-dependent output only exists when those
properties are overridden (for example by publishing `Debug`).

| Publishing type | Size | Requires .NET |
|-----------------|------|---------------|
| Release, self-contained single-file | ~66.6 MB | ❌ No |
| Debug, framework-dependent | ~1.0 MB (7 files) | ✅ Yes |

---

## 🏗️ Architecture

### MVVM Pattern

```
┌─────────────────────────────────────────────────────┐
│                      View                           │
│                  (MainWindow.xaml)                  │
│                                                     │
│  ┌─────────────┐    ┌─────────────────────────┐   │
│  │  Bindings   │◄──►│     ViewModel           │   │
│  │  Commands   │    │   (MainViewModel)       │   │
│  └─────────────┘    └───────────┬─────────────┘   │
│                                 │                 │
│                                 ▼                 │
│                      ┌─────────────────┐          │
│                      │      Model      │          │
│                      │  (LinkItem)     │          │
│                      └─────────────────┘          │
└─────────────────────────────────────────────────────┘
```

### Key Components

**MainViewModel**
- `Items` — collection of link creation items
- `TargetFolder` — target folder path
- `SelectedLinkType` — selected link type
- `CreateLinks()` — asynchronous link creation
- `AddFiles()`, `AddFolders()` — add items
- `RemoveItem()` — remove item

**LinkItem (Model)**
- `SourcePath` — path to source file/folder
- `IsDirectory` — is a folder
- `Status` — status (Pending/InProgress/Success/Error)
- `ErrorMessage` — error message

**LocalizationService**
- Automatic system language detection
- JSON localization file loading
- Default file creation on first run

---

## 🌐 Localization

### Adding a new language

1. Create file `i18n/{code}.json` (e.g., `fr.json` for French)
2. Copy structure from `en.json`
3. Translate values

**Example (fr.json):**
```json
{
  "AppTitle": "Yeondo - Créateur de liens symboliques",
  "AddFilesTooltip": "Ajouter des fichiers",
  "CreateButton": "Créer",
  ...
}
```

### LocalizationModel structure

```csharp
public class LocalizationModel
{
    public string AppTitle { get; set; }
    public string AddFilesTooltip { get; set; }
    public string CreateButton { get; set; }
    public string OutputPathLabel { get; set; }
    // ... 20+ properties
}
```

### Usage in XAML

```xaml
<TextBlock Text="{Binding Source={x:Static services:LocalizationService.Instance}, 
                          Path=Resources.CreateButton}" />
```

---

## ⚡ Optimizations

### P/Invoke with LibraryImport

Modern approach for Win32 API calls (.NET 10):

```csharp
[System.Runtime.InteropServices.LibraryImport("kernel32.dll", 
    SetLastError = true, 
    StringMarshalling = System.Runtime.InteropServices.StringMarshalling.Utf16, 
    EntryPoint = "CreateSymbolicLinkW")]
[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
private static partial bool CreateSymbolicLinkNative(...);
```

**Benefits:**
- Compile-time marshaling code generation
- Better performance
- Less overhead

### Single Instance

Application uses Mutex to prevent multiple instances:

```csharp
private static Mutex? _mutex;
private const string MutexName = "Yeondo-SymLink-Creator-SingleInstance";

protected override void OnStartup(StartupEventArgs e)
{
    _mutex = new Mutex(true, MutexName, out bool createdNew);
    
    if (!createdNew)
    {
        // Activate existing window
        ActivateExistingInstance();
        Shutdown();
        return;
    }
}
```

### Collection Expressions (C# 12)

```csharp
// Before
Items = new ObservableCollection<LinkItem>();

// After
public ObservableCollection<LinkItem> Items { get; } = [];
```

### Primary Constructors

```csharp
// Before
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    public RelayCommand(Action execute) => _execute = execute;
}

// After
public class RelayCommand(Action execute) : ICommand
{
    private readonly Action _execute = execute;
}
```

---

## 🔒 Security

### Symbolic Link Creation

Link creation requires privileges:
- **Developer Mode** (Windows 10/11) — no admin rights needed
- **Run as Administrator** — if Developer Mode is disabled

### Win32 Flags

```csharp
SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 2  // Allow without admin
SYMBOLIC_LINK_FLAG_FILE = 0                        // File link
SYMBOLIC_LINK_FLAG_DIRECTORY = 1                   // Folder link
```

---

## 📊 Project Metrics

| Metric | Value |
|--------|-------|
| C# files | 12 |
| C# lines of code | 1607 |
| Types (classes/interfaces/records/enums) | 22 |
| Tracked project files | 16 |
| Publish size (Release, self-contained) | ~66.6 MB |
| Launch time | < 1 sec |

---

## 🐛 Debugging

### Logging

Logs are saved next to the application:
```
./logs/symlink_YYYYMMDD_HHMMSS.log
./settings.json
./i18n/ru.json
./i18n/en.json
```

**All files are created next to the executable** — no system folders!

### Debugging in Visual Studio

1. Set a breakpoint
2. Press **F5**
3. Use **Debug → Windows** to inspect variables

---

## 📝 Pre-release Checklist

- [ ] Build without errors or warnings
- [ ] All IDE warnings resolved
- [ ] Localization works (ru/en)
- [ ] Single Instance works
- [ ] Drag & Drop works
- [ ] All link types create correctly
- [ ] Logging works
- [ ] i18n files are created
- [ ] File metadata is filled
- [ ] Publish size is normal

---

**Yeondo Development Team** © 2026
