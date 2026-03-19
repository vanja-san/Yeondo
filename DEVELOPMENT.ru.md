# Yeondo — Руководство для разработчиков

Документация по разработке, сборке и публикации приложения.

## 📋 Содержание

- [Технологический стек](#технологический-стек)
- [Структура проекта](#структура-проекта)
- [Сборка и запуск](#сборка-и-запуск)
- [Публикация](#публикация)
- [Архитектура](#архитектура)
- [Локализация](#локализация)
- [Оптимизации](#оптимизации)

---

## 🔧 Технологический стек

| Компонент | Версия / Технология |
|-----------|---------------------|
| **Фреймворк** | .NET 10 |
| **UI** | WPF (Windows Presentation Foundation) |
| **Язык** | C# 13 |
| **Архитектура** | MVVM (Model-View-ViewModel) |
| **Win32 API** | P/Invoke (LibraryImport) |
| **Стили** | Fluent Design (Windows 11) |

---

## 📁 Структура проекта

```
yeondo-app/
├── yeondo-app/
│   ├── App.xaml(.cs)           # Точка входа, Single Instance
│   ├── MainWindow.xaml(.cs)    # Главное окно
│   ├── AssemblyInfo.cs         # Метаданные сборки
│   │
│   ├── ViewModels/
│   │   └── MainViewModel.cs    # Основная логика приложения
│   │
│   ├── Models/
│   │   ├── LinkItem.cs         # Модель элемента ссылки
│   │   └── AppSettings.cs      # Настройки приложения
│   │
│   ├── Converters/
│   │   ├── Converters.cs       # Конвертеры значений
│   │   └── LocConverter.cs     # Конвертер локализации
│   │
│   ├── Services/
│   │   └── LocalizationService.cs  # Сервис локализации
│   │
│   ├── Assets/
│   │   └── app.ico             # Иконка приложения
│   │
│   └── yeondo-app.csproj       # Файл проекта
│
├── README.md                   # Документация для пользователей (EN)
├── README.ru.md                # Документация для пользователей (RU)
├── DEVELOPMENT.md              # Документация для разработчиков (EN)
└── DEVELOPMENT.ru.md           # Этот файл (RU)
```

---

## ⚙️ Сборка и запуск

### Требования для разработки

- .NET 10 SDK
- Visual Studio 2022 / VS Code / Rider
- Windows 10/11 x64

### Команды CLI

```bash
# Сборка (Debug)
dotnet build "yeondo-app/yeondo-app.csproj"

# Сборка (Release)
dotnet build "yeondo-app/yeondo-app.csproj" -c Release

# Запуск
dotnet run --project "yeondo-app/yeondo-app.csproj"

# Очистка
dotnet clean "yeondo-app/yeondo-app.csproj"
```

### Visual Studio

1. Откройте `yeondo-app.slnx`
2. Выберите конфигурацию **Debug** или **Release**
3. Нажмите **F5** для запуска или **Ctrl+Shift+B** для сборки

---

## 📦 Публикация

### Самодостаточная публикация (рекомендуется)

Публикация в один файл со встроенным .NET Runtime:

```bash
dotnet publish "yeondo-app/yeondo-app.csproj" -c Release
```

**Результат:**
- Файл: `bin/Release/net10.0-windows/win-x64/publish/Yeondo.exe`
- Размер: ~66 МБ
- Не требует установленного .NET

### Настройки публикации (.csproj)

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

### Сравнение размеров

| Тип публикации | Размер | Требует .NET |
|---------------|--------|--------------|
| Самодостаточная (сжатая) | ~66 МБ | ❌ Нет |
| Зависимая от фреймворка | ~200 КБ | ✅ Да |

---

## 🏗️ Архитектура

### Паттерн MVVM

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

### Ключевые компоненты

**MainViewModel**
- `Items` — коллекция элементов для создания ссылок
- `TargetFolder` — целевая папка
- `SelectedLinkType` — выбранный тип ссылки
- `CreateLinks()` — асинхронное создание ссылок
- `AddFiles()`, `AddFolders()` — добавление элементов
- `RemoveItem()` — удаление элемента

**LinkItem (Model)**
- `SourcePath` — путь к исходному файлу/папке
- `IsDirectory` — является ли папкой
- `Status` — статус (Pending/InProgress/Success/Error)
- `ErrorMessage` — сообщение об ошибке

**LocalizationService**
- Автоматическое определение языка системы
- Загрузка JSON файлов локализации
- Создание файлов по умолчанию при первом запуске

---

## 🌐 Локализация

### Добавление нового языка

1. Создайте файл `i18n/{код}.json` (например, `fr.json` для французского)
2. Скопируйте структуру из `en.json`
3. Переведите значения

**Пример (fr.json):**
```json
{
  "AppTitle": "Yeondo - Créateur de liens symboliques",
  "AddFilesTooltip": "Ajouter des fichiers",
  "CreateButton": "Créer",
  ...
}
```

### Структура LocalizationModel

```csharp
public class LocalizationModel
{
    public string AppTitle { get; set; }
    public string AddFilesTooltip { get; set; }
    public string CreateButton { get; set; }
    public string OutputPathLabel { get; set; }
    // ... 20+ свойств
}
```

### Использование в XAML

```xaml
<TextBlock Text="{Binding Source={x:Static services:LocalizationService.Instance}, 
                          Path=Resources.CreateButton}" />
```

---

## ⚡ Оптимизации

### P/Invoke с LibraryImport

Современный подход для вызова Win32 API (.NET 10):

```csharp
[System.Runtime.InteropServices.LibraryImport("kernel32.dll", 
    SetLastError = true, 
    StringMarshalling = System.Runtime.InteropServices.StringMarshalling.Utf16, 
    EntryPoint = "CreateSymbolicLinkW")]
[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
private static partial bool CreateSymbolicLinkNative(...);
```

**Преимущества:**
- Генерация кода маршализации во время компиляции
- Лучшая производительность
- Меньше накладных расходов

### Single Instance

Приложение использует Mutex для предотвращения запуска нескольких экземпляров:

```csharp
private static Mutex? _mutex;
private const string MutexName = "Yeondo-SymLink-Creator-SingleInstance";

protected override void OnStartup(StartupEventArgs e)
{
    _mutex = new Mutex(true, MutexName, out bool createdNew);
    
    if (!createdNew)
    {
        // Активация существующего окна
        ActivateExistingInstance();
        Shutdown();
        return;
    }
}
```

### Collection Expressions (.NET 13)

```csharp
// Было
Items = new ObservableCollection<LinkItem>();

// Стало
public ObservableCollection<LinkItem> Items { get; } = [];
```

### Primary Constructors

```csharp
// Было
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    public RelayCommand(Action execute) => _execute = execute;
}

// Стало
public class RelayCommand(Action execute) : ICommand
{
    private readonly Action _execute = execute;
}
```

---

## 🔒 Безопасность

### Создание символических ссылок

Для создания ссылок требуются права:
- **Режим разработчика** (Windows 10/11) — без прав администратора
- **Запуск от администратора** — если режим разработчика отключен

### Win32 Flags

```csharp
SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 2  // Разрешить без прав админа
SYMBOLIC_LINK_FLAG_FILE = 0                        // Ссылка на файл
SYMBOLIC_LINK_FLAG_DIRECTORY = 1                   // Ссылка на папку
```

---

## 📊 Метрики проекта

| Показатель | Значение |
|------------|----------|
| Строк кода | ~1500 |
| Файлов | ~15 |
| Классов | ~10 |
| Размер публикации | 66 МБ |
| Время запуска | < 1 сек |

---

## 🐛 Отладка

## 📝 Логирование

Логи сохраняются в папке с приложением:
```
./logs/symlink_ГГГГММДД_ЧЧММСС.log
./settings.json
./i18n/ru.json
./i18n/en.json
```

**Все файлы создаются рядом с исполняемым файлом** — никаких системных папок!

### Отладка в Visual Studio

1. Установите точку останова
2. Нажмите **F5**
3. Используйте **Debug → Windows** для просмотра переменных

---

## 📝 Чек-лист перед релизом

- [ ] Сборка без ошибок и предупреждений
- [ ] Все предупреждения IDE устранены
- [ ] Локализация работает (ru/en)
- [ ] Single Instance работает
- [ ] Drag & Drop работает
- [ ] Все типы ссылок создаются
- [ ] Логирование работает
- [ ] Файлы i18n создаваются
- [ ] Метаданные файла заполнены
- [ ] Размер публикации в норме

---

**Yeondo Development Team** © 2026
