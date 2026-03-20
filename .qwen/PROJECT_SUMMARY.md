# Yeondo — Project Summary

## 📌 Overall Goal

Разработка компактного портативного WPF-приложения для массового создания символических ссылок (Symbolic/Junction/HardLink) на Windows с современным UI в стиле Windows 11.

---

## 🛠️ Technology Stack

| Component | Version / Technology |
|-----------|---------------------|
| **Framework** | .NET 10 |
| **UI** | WPF + Fluent Design |
| **Language** | C# 13 |
| **Architecture** | MVVM |
| **Win32 API** | P/Invoke (LibraryImport) |
| **Styles** | Windows 11 Fluent Design |

---

## 📁 Project Structure

```
yeondo-app/
├── .github/
│   ├── workflows/
│   │   └── release.yml       # GitHub Actions для релизов
│   └── release.yml           # Конфигурация release notes
│
├── .gitignore
├── README.md                 # Документация (English)
├── README.ru.md              # Документация (Russian)
├── DEVELOPMENT.md            # Для разработчиков (English)
├── DEVELOPMENT.ru.md         # Для разработчиков (Russian)
├── RELEASE_NOTES.md          # Шаблон описания релиза
│
└── yeondo-app/
    ├── App.xaml(.cs)         # Точка входа, Single Instance
    ├── MainWindow.xaml(.cs)  # Главное окно
    ├── AssemblyInfo.cs       # Метаданные сборки
    │
    ├── ViewModels/
    │   └── MainViewModel.cs  # Основная логика
    │
    ├── Models/
    │   ├── LinkItem.cs       # Модель элемента
    │   └── AppSettings.cs    # Настройки
    │
    ├── Converters/
    │   ├── Converters.cs     # Конвертеры значений
    │   └── LocConverter.cs   # Конвертер локализации
    │
    ├── Services/
    │   └── LocalizationService.cs  # Локализация
    │
    ├── Assets/
    │   └── app.ico           # Иконка приложения
    │
    └── yeondo-app.csproj
```

---

## ✨ Features

### Link Creation
- **Symbolic Link** — универсальные ссылки (файлы + папки)
- **Junction** — только для папок
- **Hard Link** — только для файлов (NTFS, один том)

### User Interface
- Компактное окно 400×500 (`ResizeMode="NoResize"`)
- Windows 11 Fluent Design
- Segoe Fluent Icons
- Drag & Drop файлов/папок
- Контекстное меню для элементов

### Localization
- Автоматическое определение языка (ru/en)
- JSON файлы в папке `i18n/`
- `ru.json` — русский, `en.json` — английский

### Portable Design
- **Все файлы рядом с исполняемым:**
  - `settings.json` — настройки
  - `i18n/` — локализация
  - `logs/` — логи
- Никаких системных папок

### Single Instance
- Только один экземпляр приложения
- Повторный запуск активирует существующее окно

---

## 🔧 Build & Publish

### Commands

```bash
# Сборка
dotnet build "yeondo-app/yeondo-app.csproj"

# Запуск
dotnet run --project "yeondo-app/yeondo-app.csproj"

# Публикация
dotnet publish "yeondo-app/yeondo-app.csproj" -c Release
```

### Publish Settings (.csproj)

```xml
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
<PublishReadyToRun>true</PublishReadyToRun>
<DebugType>none</DebugType>
<StripSymbols>true</StripSymbols>
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
```

### Output

| Type | Size | Requires .NET |
|------|------|---------------|
| Self-contained | ~66 МБ | ❌ No |
| Framework-dependent | ~200 КБ | ✅ Yes |

---

## 🏗️ Architecture

### MVVM Pattern

```
View (MainWindow.xaml)
    │
    │ Bindings / Commands
    ▼
ViewModel (MainViewModel)
    │
    │ Data Access
    ▼
Model (LinkItem, AppSettings)
```

### Key Components

**MainViewModel:**
- `Items` — коллекция элементов
- `TargetFolder` — целевая папка
- `SelectedLinkType` — тип ссылки
- `CreateLinks()` — создание ссылок
- `AddFiles()`, `AddFolders()` — добавление
- `RemoveItem()` — удаление

**LinkItem:**
- `SourcePath` — путь к файлу/папке
- `IsDirectory` — является ли папкой
- `Status` — Pending/InProgress/Success/Error
- `ErrorMessage` — сообщение об ошибке

**LocalizationService:**
- Определение языка системы
- Загрузка JSON файлов
- Создание файлов по умолчанию

---

## 🔌 Win32 API

### LibraryImport (современный P/Invoke)

```csharp
[System.Runtime.InteropServices.LibraryImport("kernel32.dll", 
    SetLastError = true, 
    StringMarshalling = System.Runtime.InteropServices.StringMarshalling.Utf16, 
    EntryPoint = "CreateSymbolicLinkW")]
[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
private static partial bool CreateSymbolicLinkNative(...);
```

### Flags

```csharp
SYMBOLIC_LINK_FLAG_FILE = 0
SYMBOLIC_LINK_FLAG_DIRECTORY = 1
SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 2
```

---

## 🌐 Localization

### Files Location
```
./i18n/ru.json
./i18n/en.json
```

### Auto Detection
```csharp
var culture = CultureInfo.CurrentUICulture;
_currentLanguage = culture.TwoLetterISOLanguageName == "ru" ? "ru" : "en";
```

---

## 📝 Logging

### Location
```
./logs/symlink_ГГГГММДД_ЧЧММСС.log
```

### Format
```
=== Создание символических ссылок [19.03.2026 12:00:00] ===
Целевая папка: C:\Links
Элементов: 5

[OK] C:\file.txt -> C:\Links\file.txt
[ERROR] C:\folder -> Access denied

=== Итог: Успешно 4, Ошибок 1 ===
```

---

## 🚀 GitHub Actions Release

### Workflow: `.github/workflows/release.yml`

```yaml
on:
  workflow_dispatch:
    inputs:
      version:
        description: 'Version number'
        required: true
        default: '1.10.5'
```

### Steps
1. Checkout code
2. Setup .NET 10
3. Restore & Build
4. Publish (Release, single file)
5. Create ZIP archive
6. Upload to GitHub Releases

### Usage
1. Actions → Build Release → Run workflow
2. Enter version: `1.10.5`
3. Run workflow
4. Release created with ZIP + Full Changelog

---

## ✅ Completed Tasks

- [x] MVVM архитектура
- [x] Создание всех типов ссылок
- [x] Drag & Drop
- [x] Локализация (ru/en)
- [x] Single Instance
- [x] Контекстное меню
- [x] Индикаторы прогресса
- [x] Логирование
- [x] Портативный дизайн
- [x] GitHub Actions workflow
- [x] Документация (4 файла)
- [x] Оптимизации (LibraryImport, Primary Constructors)

---

## 📊 Project Metrics

| Metric | Value |
|--------|-------|
| Lines of code | ~1500 |
| Files | ~15 |
| Classes | ~10 |
| Publish size | 66 МБ |
| Launch time | < 1 сек |

---

## 📄 Documentation Files

| File | Language | Purpose |
|------|----------|---------|
| `README.md` | EN | User guide |
| `README.ru.md` | RU | Руководство пользователя |
| `DEVELOPMENT.md` | EN | Developer docs |
| `DEVELOPMENT.ru.md` | RU | Документация разработчика |
| `RELEASE_NOTES.md` | EN | Release template |

---

## 📜 License

Copyright © 2026 vanja-san. All rights reserved.

---

## Summary Metadata

**Last Updated:** 2026-03-19  
**Version:** 1.10.5  
**Status:** Ready for Release
