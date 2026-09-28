using System.Text.Json;
using Yeondo.Services;
namespace Yeondo.Tests;

/// <summary>
/// Проверки целостности файлов перевода и поведения fallback'а.
///
/// Требование: отсутствующий, повреждённый или частичный языковой файл не должен ломать
/// интерфейс — используются встроенные английские значения.
/// </summary>
public sealed class LocalizationTests
{
    private static readonly string I18nDir = Path.Combine(AppContext.BaseDirectory, "i18n");

    private static JsonElement LoadJson(string language)
    {
        var path = Path.Combine(I18nDir, $"{language}.json");
        Assert.True(File.Exists(path), $"Missing translation file: {path}");
        return JsonDocument.Parse(File.ReadAllText(path)).RootElement.Clone();
    }

    private static string[] KeysOf(JsonElement element) =>
        [.. element.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal)];

    [Fact]
    public void BothTranslationFilesAreValidJson()
    {
        // Если файл невалиден, LocalizationService откатится на встроенные значения,
        // и пользователь потеряет все свои правки молча — поэтому ловим это тестом.
        LoadJson("en");
        LoadJson("ru");
    }

    [Fact]
    public void EnglishAndRussianExposeTheSameKeys()
    {
        var en = KeysOf(LoadJson("en"));
        var ru = KeysOf(LoadJson("ru"));

        var missingInRu = en.Except(ru, StringComparer.Ordinal).ToArray();
        var missingInEn = ru.Except(en, StringComparer.Ordinal).ToArray();

        Assert.True(missingInRu.Length == 0,
            $"Keys present in en.json but missing from ru.json: {string.Join(", ", missingInRu)}");
        Assert.True(missingInEn.Length == 0,
            $"Keys present in ru.json but missing from en.json: {string.Join(", ", missingInEn)}");
    }

    [Fact]
    public void NoTranslationValueIsBlank()
    {
        foreach (var language in new[] { "en", "ru" })
        {
            var root = LoadJson(language);
            foreach (var property in root.EnumerateObject())
            {
                Assert.False(string.IsNullOrWhiteSpace(property.Value.GetString()),
                    $"Blank value for '{property.Name}' in {language}.json");
            }
        }
    }

    [Fact]
    public void RussianIsActuallyTranslated()
    {
        // Защита от копирования en.json в ru.json: ключевые строки должны быть на русском.
        var ru = LoadJson("ru");
        foreach (var key in new[] { "CreateButton", "CancelButton", "BrowseButton" })
        {
            var value = ru.GetProperty(key).GetString();
            Assert.NotNull(value);
            Assert.True(value.Any(c => c is >= '\u0400' and <= '\u04FF'),
                $"'{key}' is not translated in ru.json: {value}");
        }
    }

    [Fact]
    public void EveryTranslationKeyIsDeclaredOnTheModel()
    {
        // Новый ключ в JSON, о котором забыли в LocalizationModel, не отобразится.
        var declared = typeof(LocalizationModel)
            .GetProperties()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var language in new[] { "en", "ru" })
        {
            foreach (var key in KeysOf(LoadJson(language)))
            {
                Assert.True(declared.Contains(key), $"'{key}' from {language}.json is not declared on LocalizationModel");
            }
        }
    }

    [Fact]
    public void BuiltInDefaultsAreEnglishAndComplete()
    {
        // Экземпляр без Initialize() — это состояние при недоступном/битом файле.
        // Значения по умолчанию обязаны быть английскими и непустыми.
        var model = new LocalizationModel();
        var blank = typeof(LocalizationModel)
            .GetProperties()
            .Where(p => p.PropertyType == typeof(string))
            .Where(p => string.IsNullOrWhiteSpace((string?)p.GetValue(model)))
            .Select(p => p.Name)
            .ToArray();

        Assert.True(blank.Length == 0, $"Built-in defaults are blank for: {string.Join(", ", blank)}");
        Assert.Equal("Create", model.CreateButton);
        Assert.Equal("Cancel", model.CancelButton);
    }
}
