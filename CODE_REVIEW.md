# Полный отчёт о ревью проекта Yeondo v1.10.7

> **Дата:** 2026-07-04
> **Ревьювер:** GitHub Copilot (OpenCode Zen / Deepseek V4 Flash Free)
> **Источники:** Анализ кода + официальная документация Microsoft Learn

---

## 📋 Содержание

1. [Общая информация](#1-общая-информация)
2. [Архитектура и структура](#2-архитектура-и-структура)
3. [Критические проблемы (P0)](#3-критические-проблемы-p0)
4. [Проблемы средней важности (P1)](#4-проблемы-средней-важности-p1)
5. [Замечания и улучшения (P2)](#5-замечания-и-улучшения-p2)
6. [Актуальность технологий (Microsoft Docs)](#6-актуальность-технологий-microsoft-docs)
7. [Итоговая оценка](#7-итоговая-оценка)

---

## 1. Общая информация

| Мета | Значение |
|------|----------|
| **Приложение** | Yeondo — утилита для массового создания символических ссылок |
| **Стек** | .NET 10 / WPF / C# 13 / MVVM |
| **Строк кода** | ~1500 |
| **Файлов** | ~15 |
| **Версия** | 1.10.7 |
| **Платформа** | Windows 10/11 x64 |
| **Тип сборки** | Self-contained single-file |

---

## 2. Архитектура и структура

### 2.1 Проект

```
yeondo-app/
├── yeondo-app.slnx                          # Solution file (новый формат)
├── yeondo-app/
│   ├── App.xaml(.cs)                        # Entry point, Single Instance
│   ├── MainWindow.xaml(.cs)                 # Main window
│   ├── AssemblyInfo.cs                      # Метаданные сборки
│   ├── ViewModels/
│   │   └── MainViewModel.cs                 # Логика приложения + RelayCommand + NativeMethods
│   ├── Models/
│   │   ├── LinkItem.cs                      # Модель элемента ссылки
│   │   └── AppSettings.cs                   # Настройки приложения
│   ├── Converters/
│   │   ├── Converters.cs                    # Value converters
│   │   └── LocConverter.cs                  # Конвертер локализации
│   ├── Services/
│   │   └── LocalizationService.cs           # Сервис локализации
│   ├── Assets/
│   │   └── app.ico                          # Иконка приложения
│   └── yeondo-app.csproj                    # Проектный файл
├── .github/workflows/release.yml            # CI/CD
├── DEVELOPMENT.md / DEVELOPMENT.ru.md       # Документация разработчика
├── README.md / README.ru.md                 # Пользовательская документация
└── RELEASE_NOTES.md                         # Примечания к релизу
```

### 2.2 Паттерн MVVM

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

---

## 3. Критические проблемы (P0)

### 3.1 `async void` в `CreateLinks()` — риск необработанных исключений

- **Файл:** `yeondo-app/ViewModels/MainViewModel.cs`, строка ~199
- **Проблема:** Метод объявлен как `private async void CreateLinks()`. `async void` допустим **только** для event-handler'ов. Исключения вне `try/catch` вызывают необработанное исключение и краш приложения.
- **Риск:** Любая непредвиденная ошибка (AccessViolation, OutOfMemory) убьёт процесс без возможности перехвата.
- **Решение:** 
  ```csharp
  // Было:
  private async void CreateLinks()
  
  // Стало:
  private async Task CreateLinks()
  ```
  Использовать `AsyncRelayCommand` из `CommunityToolkit.Mvvm`.

### 3.2 Команды не кэшируются — создаются при каждом обращении

- **Файл:** `yeondo-app/ViewModels/MainViewModel.cs`, строки ~96-106
- **Проблема:** Каждое обращение к свойству команды создаёт новый объект `RelayCommand`:
  ```csharp
  public ICommand AddFilesCommand => new RelayCommand(AddFiles, () => CanAddItems);
  ```
- **Последствия:** 
  - Лишние аллокации
  - Нестабильность `CanExecute` — команды, созданные в разное время, могут иметь разное состояние
- **Решение:** Кэшировать команды в `readonly` полях:
  ```csharp
  private readonly ICommand _addFilesCommand;
  public ICommand AddFilesCommand => _addFilesCommand;
  ```

### 3.3 Прямые вызовы UI-диалогов из ViewModel — нарушение MVVM

- **Файл:** `yeondo-app/ViewModels/MainViewModel.cs`, строки 128, 141, 166, 236
- **Проблема:** ViewModel напрямую вызывает:
  - `Microsoft.Win32.OpenFileDialog`
  - `Microsoft.Win32.OpenFolderDialog`
  - `System.Windows.MessageBox.Show(...)`
- **Последствия:** 
  - Невозможно модульное тестирование
  - Жёсткая связь с WPF
  - При смене UI-фреймворка придётся переписывать ViewModel
- **Решение:** Внедрить абстракцию `IDialogService` / `IFileDialogService`.

### 3.4 Отсутствующие ключи локализации в JSON-файлах

- **Файлы:** `i18n/en.json`, `i18n/ru.json`
- **Проблема:** JSON-файлы не содержат 7 ключей: `LinkTypeUnknown`, `LogHeader`, `LogTargetFolder`, `LogItemCount`, `LogSuccess`, `LogError`, `LogSummary`
- **Последствия:** 
  - Ключи восстанавливаются через fallback/дефолт
  - При удалении кода fallback'а локализация сломается
  - Файлы несамодостаточны
- **Решение:** Добавить недостающие ключи в оба JSON-файла.

### 3.5 Реализация Junction не отличается от Symbolic

- **Файл:** `yeondo-app/ViewModels/MainViewModel.cs`, строки ~371-379
- **Проблема:** `CreateJunctionLink()` вызывает `NativeMethods.CreateSymbolicLink()` с теми же флагами. Настоящая Junction (точка повторной обработки NTFS) — другой тип.
- **Подтверждение из документации Microsoft:** `CreateSymbolicLink` создаёт символическую ссылку, а не junction. Для junction требуется манипуляция с `IO_REPARSE_TAG_MOUNT_POINT`.
- **Решение:** Либо реализовать честную Junction (через `FSCTL_SET_REPARSE_POINT`), либо удалить опцию.

---

## 4. Проблемы средней важности (P1)

### 4.1 Блокировка UI-потока при создании ссылок

- **Файл:** `yeondo-app/ViewModels/MainViewModel.cs`, строки ~249-290
- **Проблема:** Все операции (P/Invoke, файловый ввод-вывод) выполняются в UI-потоке
- **Детали:**
  - `Directory.CreateDirectory(TargetFolder)` — синхронно
  - `CreateSymbolicLink` / `CreateHardLink` — синхронно, P/Invoke
  - `File.WriteAllLines(_logFilePath, logLines)` — синхронно
  - `Task.Delay(50)` — только маскирует проблему и замедляет работу
- **Решение:** Вынести в `Task.Run()` или использовать async-обёртки.

### 4.2 Жёстко зашитые русские строки в коде

- **Файл:** `yeondo-app/ViewModels/MainViewModel.cs`, строки 374, 376, 383, 385
- **Проблема:**
  ```csharp
  return (false, "Junction работает только с папками");
  return (false, "Источник должен существовать для Junction");
  return (false, "Hard Link работает только с файлами");
  return (false, "Файл источник не найден");
  ```
- **Решение:** Перенести в `LocalizationService.Keys` и `LocalizationModel`.

### 4.3 XAML: DataTrigger без Setter для иконок типов ссылок

- **Файл:** `yeondo-app/MainWindow.xaml`, строки 63-72
- **Проблема:** В `ComboBox.ItemTemplate` DataTrigger'ы для `LinkType.Junction` и `LinkType.HardLink` не имеют Setter'ов:
  ```xaml
  <DataTrigger Binding="{Binding}" Value="{x:Static vm:LinkType.Junction}" />
  <DataTrigger Binding="{Binding}" Value="{x:Static vm:LinkType.HardLink}" />
  ```
- **Последствия:** Иконка не меняется для разных типов ссылок (всегда пустая).
- **Документация Microsoft:** Segoe Fluent Icons — использовать символы из диапазона `&#xE000`–`&#xF8FF`.
- **Решение:** Добавить Setter с иконками для каждого типа.

### 4.4 Отсутствует виртуализация списка элементов

- **Файл:** `yeondo-app/MainWindow.xaml`, строки ~161-232
- **Проблема:** `ItemsControl` внутри `ScrollViewer` не использует виртуализацию.
- **Последствия:** При 1000+ элементах — просадка производительности и памяти.
- **Решение:** Использовать `ListBox` или `VirtualizingStackPanel.IsVirtualizing="True"`.

### 4.5 Неиспользуемые конвертеры

- **Файлы:** `yeondo-app/MainWindow.xaml` (ресурсы), `yeondo-app/Converters/Converters.cs`
- **Проблема:** `DetailsVisibilityConverter` и `StatusToColorConverter` объявлены, но не используются.
- **Решение:** Удалить мёртвый код.

### 4.6 Неиспользуемые свойства моделей

- **Файлы:**
  - `yeondo-app/Models/LinkItem.cs` — свойство `LinkPath` никогда не заполняется
  - `yeondo-app/Models/AppSettings.cs` — свойства `CreateForFiles`, `CreateForDirectories` никогда не читаются
- **Решение:** Удалить или реализовать.

### 4.7 Отсутствует проверка существования целевого пути

- **Файл:** `yeondo-app/ViewModels/MainViewModel.cs`, строка ~255
- **Проблема:** `Path.GetFileName(item.SourcePath)` может вернуть пустую строку для корневых путей, и `CreateLinks` не проверяет, существует ли уже `linkPath`.
- **Последствия:** Возможно перезапись существующих файлов/папок или создание ссылки с пустым именем.
- **Решение:** Добавить проверку `File.Exists()`/`Directory.Exists()` до создания.

---

## 5. Замечания и улучшения (P2)

### 5.1 Дублирование `RootNamespace` в csproj

- **Файл:** `yeondo-app/yeondo-app.csproj`, строки 6 и 10
- Дважды указано `<RootNamespace>Yeondo</RootNamespace>`.
- **Решение:** Удалить дубликат.

### 5.2 `AllowUnsafeBlocks=true` без необходимости

- **Файл:** `yeondo-app/yeondo-app.csproj`, строка 10
- **Подтверждение из документации Microsoft:** `LibraryImport` генерирует код через source generator, `unsafe`-блоки не требуются.
- **Решение:** Удалить `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`.

### 5.3 Настройки оптимизации в общих свойствах

- **Файл:** `yeondo-app/yeondo-app.csproj`, строки 43-44
- **Проблема:** `<Optimize>true</Optimize>` и `<DebugType>none</DebugType>` применяются ко **всем** конфигурациям, включая Debug.
- **Последствия:** Невозможно нормально отлаживать (нет debug-символов, код оптимизирован).
- **Решение:** Вынести в `Release` PropertyGroup.

### 5.4 LocalizationService пишет файлы в `AppContext.BaseDirectory`

- **Файл:** `yeondo-app/Services/LocalizationService.cs`, строка ~89
- **Проблема:** При установке в `Program Files` запись JSON-файлов упадёт с ошибкой прав доступа.
- **Решение:** Использовать `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)`.

### 5.5 `GetString()` использует Reflection при каждом вызове

- **Файл:** `yeondo-app/Services/LocalizationService.cs`, строка ~286
- **Проблема:** Каждый вызов делает `typeof(LocalizationModel).GetProperty(key)`.
- **Решение:** Кэшировать `PropertyInfo` в `Dictionary<string, PropertyInfo>`.

### 5.6 RelayCommand и NativeMethods внутри MainViewModel.cs

- **Файл:** `yeondo-app/ViewModels/MainViewModel.cs`
- **Проблема:** Два отдельных класса (`RelayCommand`, `RelayCommand<T>`, `NativeMethods`) находятся внутри одного файла с ViewModel.
- **Решение:** Вынести в отдельные файлы: `Commands/RelayCommand.cs`, `Services/NativeMethods.cs`.

### 5.7 GitHub Workflow: i18n не включены в publish

- **Файл:** `.github/workflows/release.yml`
- **Проблема:** JSON-файлы локализации не копируются в выходную папку publish.
- **Решение:** Добавить в csproj:
  ```xml
  <ItemGroup>
    <Content Include="i18n\**\*.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
  ```

### 5.8 Логгер использует `notepad.exe` для открытия логов

- **Файл:** `yeondo-app/ViewModels/MainViewModel.cs`, строка ~355
- **Проблема:** Жёсткая привязка к `notepad.exe`. На некоторых системах notepad может отсутствовать или быть заменён.
- **Решение:** Использовать `Process.Start("explorer.exe", logFilePath)` или shell execute.

### 5.9 Кэширование `JsonSerializerOptions`

- **Файлы:** `yeondo-app/Models/AppSettings.cs`, `yeondo-app/Services/LocalizationService.cs`
- **Проблема:** Новый экземпляр `JsonSerializerOptions` создаётся при каждом вызове `Save()`.
- **Подтверждение из документации Microsoft:** "If you use `JsonSerializerOptions` repeatedly, don't create a new instance each time."
- **Решение:** Создать `static readonly` экземпляр.

### 5.10 Улучшение: использовать `LibraryImport` для `SetForegroundWindow`/`ShowWindow`

- **Файл:** `yeondo-app/App.xaml.cs`, строки 14-15
- **Проблема:** Используется `[DllImport]` вместо `[LibraryImport]` для user32 функций.
- **Решение:** Конвертировать в `LibraryImport` для единообразия.

---

## 6. Актуальность технологий (Microsoft Docs)

### 6.1 WPF .NET 10 Fluent Design ✅

| Компонент | Статус | Источник |
|-----------|--------|----------|
| `ThemeMode="System"` | ✅ **Корректно** | `learn.microsoft.com/dotnet/api/system.windows.thememode` |
| `PresentationFramework.Fluent` | ✅ **Корректно** | `.NET 9+ Fluent theme` |
| Grid shorthand `.NET 10` | ⚠️ **Можно добавить** | `ColumnDefinitions="1*, Auto"` |

> `ThemeMode` помечен как `[Experimental("WPF0001")]` даже в .NET 10.

### 6.2 LibraryImport / P/Invoke ✅

| Аспект | Статус | Источник |
|--------|--------|----------|
| `[LibraryImport]` вместо `[DllImport]` | ✅ **Корректно** | Рекомендация Microsoft для .NET 7+ |
| `SetLastError = true` | ✅ **Корректно** | `learn.microsoft.com/dotnet/standard/native-interop/best-practices` |
| `StringMarshalling.Utf16` | ✅ **Корректно** | Win32 W-суффикс функций |
| `AllowUnsafeBlocks` | ❌ **Не нужен** | `LibraryImport` — source generation, unsafe не требуется |

### 6.3 Win32 CreateSymbolicLink / CreateHardLink ✅

| Параметр | Статус | Источник |
|----------|--------|----------|
| `SYMBOLIC_LINK_FLAG_DIRECTORY` (0x1) | ✅ **Корректно** | `learn.microsoft.com/windows/win32/api/winbase/nf-winbase-createsymboliclinkw` |
| `SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE` (0x2) | ✅ **Корректно** | Требует Developer Mode |
| `IntPtr.Zero` для `lpSecurityAttributes` | ✅ **Корректно** | NULL — отсутствие атрибутов |

### 6.4 Single-file Publishing ✅

| Параметр | Статус | Источник |
|----------|--------|----------|
| `PublishSingleFile=true` | ✅ **Корректно** | Стандартный механизм |
| `SelfContained=true` | ✅ **Необходимо** | С .NET 8+ RID не подразумевает SelfContained |
| `PublishReadyToRun=true` | ✅ **Корректно** | Улучшает запуск |
| `EnableCompressionInSingleFile=true` | ✅ **Поддерживается** | С .NET 6+ |

### 6.5 OpenFolderDialog (WPF) ✅

| Аспект | Статус | Источник |
|--------|--------|----------|
| `Microsoft.Win32.OpenFolderDialog` | ✅ **Корректно** | Доступен с .NET 8 |
| `Multiselect=true` | ✅ **Корректно** | Поддерживается |
| `FolderName` / `FolderNames` | ✅ **Корректно** | `learn.microsoft.com/dotnet/api/microsoft.win32.openfolderdialog` |

### 6.6 MVVM Toolkit ⚠️

| Аспект | Статус | Источник |
|--------|--------|----------|
| Кастомный `RelayCommand` | ⚠️ **Работает** | Функционален, но Microsoft рекомендует `CommunityToolkit.Mvvm` |
| `AsyncRelayCommand` | ❌ **Отсутствует** | `learn.microsoft.com/dotnet/communitytoolkit/mvvm/relaycommand` |
| Source generators | ❌ **Не используются** | `[RelayCommand]` атрибут генерирует команды автоматически |

### 6.7 System.Text.Json ✅

| Аспект | Статус | Источник |
|--------|--------|----------|
| `WriteIndented = true` | ✅ **Корректно** | Pretty-print |
| `UnsafeRelaxedJsonEscaping` | ✅ **Корректно** | Для кириллицы |
| Кэширование options | ⚠️ **Рекомендуется** | `learn.microsoft.com/dotnet/standard/serialization/system-text-json/configure-options` |

---

## 7. Итоговая оценка

### 7.1 Сводка найденных проблем

| Категория | P0 (Крит.) | P1 (Среди.) | P2 (Мелк.) | Всего |
|-----------|-----------|-------------|------------|-------|
| Логика/Баги | 1 | 2 | — | 3 |
| MVVM/Архитектура | 2 | — | 1 | 3 |
| Локализация | 1 | 1 | 1 | 3 |
| XAML/UI | — | 2 | — | 2 |
| Производительность | — | 1 | 2 | 3 |
| .NET/csproj | — | — | 3 | 3 |
| CI/CD | — | — | 1 | 1 |
| **Итого** | **4** | **6** | **8** | **18** |

### 7.2 Легенда приоритетов

| Приоритет | Описание |
|-----------|----------|
| **🔴 P0 (Critical)** | Может вызвать crash, потерю данных или неработоспособность функционала |
| **🟠 P1 (Major)** | Серьёзное нарушение best practices, архитектуры или производительности |
| **🟡 P2 (Minor)** | Косметические улучшения, мелкие оптимизации |

### 7.3 Рекомендованный порядок исправления

1. **P0-1:** `async void` → `async Task` + `AsyncRelayCommand`
2. **P0-2:** Кэшировать команды (readonly поля)
3. **P0-3:** `IDialogService` для UI-диалогов
4. **P0-4:** Дополнить i18n JSON-файлы недостающими ключами
5. **P0-5:** Исправить/удалить Junction
6. **P1-1:** Вынести создание ссылок в background thread
7. **P1-2:** Локализовать жёстко зашитые строки
8. **P1-3:** Исправить DataTrigger в ComboBox
9. **P1-4:** Добавить виртуализацию списка
10. **P1-5:** Удалить мёртвый код (конвертеры, свойства)
11. ... остальные P2

### 7.4 Что сделано хорошо ✅

- **Современный стек:** .NET 10, C# 13, WPF, LibraryImport
- **Чистая MVVM-архитектура** с разделением ответственности (с оговорками)
- **Качественная локализация** с автодетектом, JSON-файлами, fallback
- **Грамотная Single Instance** через Mutex
- **Полная документация** на двух языках (DEVELOPMENT.md, README.md)
- **Fluent Design** с поддержкой светлой/тёмной темы
- **CI/CD** через GitHub Actions
- **Портативность:** все файлы рядом с exe
- **Обработка ошибок** через Win32 `GetLastError`
- **Современный синтаксис:** primary constructors, collection expressions

---

*Ревью выполнено с использованием статического анализа кода и официальной документации Microsoft Learn по .NET 10, WPF, P/Invoke/LibraryImport, Win32 API, System.Text.Json и Single-file deployment.*

---

## 8. Прогресс рефакторинга

> Последнее обновление: 2026-07-04

### Выполнено ✅

| # | Задача | Статус | Комментарий |
|---|--------|--------|-------------|
| P0-1 | `async void` → `async Task` + `AsyncRelayCommand` | ✅ **Готово** | CreateLinks → CreateLinksAsync, Task.Run для фона |
| P0-2 | Кэширование команд | ✅ **Готово** | 8 readonly полей, инициализация в конструкторе |
| P0-3 | IDialogService | ✅ **Готово** | Интерфейс + WPF реализация, constructor injection |
| P0-4 | i18n ключи | ✅ **Готово** | Добавлены Keys, локализованы строки (cr. #3.4) |
| P0-5 | Junction исправление | ✅ **Готово** | Реальная реализация через DeviceIoControl + FSCTL_SET_REPARSE_POINT |
| P1-3 | DataTrigger в ComboBox | ✅ **Готово** | Добавлены Setter для иконок (Link,Glyph,File) |
| P1-4 | Виртуализация списка | ✅ **Готово** | ItemsControl с VirtualizingStackPanel, удалён ScrollViewer |
| P1-5 | Мёртвый код | ✅ **Готово** | Удалены DetailsVisibilityConverter, StatusToColorConverter, LinkPath, CreateForFiles/Directories |
| P1-6 | Background thread | ✅ **Готово** | Task.Run, Interlocked.Increment, Dispatcher.Invoke |
| P2-8 | OpenLogs shell execute | ✅ **Готово** | Вместо notepad.exe - UseShellExecute через ProcessStartInfo |
| P2-11 | csproj чистка | ✅ **Готово** | Убран дубль RootNamespace, оптимизации в Release, DebugType=portable для Debug |
| P2-12 | Вынос RelayCommand/NativeMethods | ✅ **Готово** | Commands/RelayCommand.cs, Services/NativeMethods.cs |
| P2-13 | JsonSerializerOptions кэш | ✅ **Готово** | Static readonly instance в AppSettings и LocalizationService |
| P2-14 | i18n Content в csproj | ✅ **Готово** | Добавлен Content Include для i18n\**\*.json |
| P2-15 | DllImport → LibraryImport | ✅ **Готово** | App.xaml.cs конвертирован в LibraryImport |

### Осталось ⏳

| # | Задача | Приоритет | Комментарий |
|---|--------|-----------|-------------|
| P2-9 | AllowUnsafeBlocks | P2 | Оставлен — LibraryImport требует unsafe (SYSLIB1062) |
| P2-? | `GetString()` Reflection | P2 | Можно заменить на `Dictionary<string, PropertyInfo>` кэш |
| P2-? | i18n в `LocalApplicationData` | P2 | Запись JSON в `AppContext.BaseDirectory` может падать в `Program Files` |
