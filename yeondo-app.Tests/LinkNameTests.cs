using Yeondo.ViewModels;

namespace Yeondo.Tests;

/// <summary>
/// Регрессионные тесты вывода имени ссылки.
///
/// Регрессия, которую они закрывают: <c>Path.GetFileName("C:\\target\\")</c> возвращает
/// пустую строку, из-за чего путь ссылки схлопывался в сам каталог назначения и junction
/// превращал выбранную папку в reparse point с результатом "Success".
/// </summary>
public sealed class LinkNameTests
{
    [Theory]
    [InlineData(@"C:\src\report.txt", "report.txt")]
    [InlineData(@"C:\src\folder", "folder")]
    [InlineData(@"\\server\share\folder", "folder")]
    public void GetLinkName_ReturnsFileNameForRegularPaths(string source, string expected)
    {
        Assert.Equal(expected, MainViewModel.GetLinkName(source));
    }

    [Theory]
    [InlineData(@"C:\src\report.txt\")]
    [InlineData(@"C:\src\folder\")]
    [InlineData(@"C:\src\folder\\")]
    public void GetLinkName_IgnoresTrailingSeparators(string source)
    {
        // Регрессия: без TrimEnd имя было пустым, и путь ссылки становился пустым.
        Assert.NotNull(MainViewModel.GetLinkName(source));
        Assert.Equal(Path.GetFileName(source.TrimEnd('\\')), MainViewModel.GetLinkName(source));
    }

    [Theory]
    [InlineData(@"C:\")]
    [InlineData(@"C:\\")]
    [InlineData(@"\\server\share\")]
    [InlineData("\\")]
    public void GetLinkName_ReturnsNullForRootsWithoutNameComponent(string source)
    {
        Assert.Null(MainViewModel.GetLinkName(source));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetLinkName_ReturnsNullForBlankInput(string source)
    {
        Assert.Null(MainViewModel.GetLinkName(source));
    }

    [Fact]
    public void GetLinkName_NeverReturnsEmptyOrCollapsesToDirectory()
    {
        // Ключевое свойство: непустой исходный путь обязан дать непустое имя ссылки,
        // иначе linkPath == targetFolder и операция ударит по папке назначения.
        string[] sources =
        [
            @"C:\a\b\", @"D:\very\long\path\", @"\\nas\vol\dir\", @"C:\a\b", @"\\nas\vol\dir"
        ];

        foreach (var source in sources)
        {
            var name = MainViewModel.GetLinkName(source);
            Assert.False(string.IsNullOrEmpty(name), $"Empty link name for '{source}'");
        }
    }

    [Theory]
    [InlineData(@"C:\target", @"C:\target\", true)]
    [InlineData(@"C:\target\", @"C:\target", true)]
    [InlineData(@"C:\target", @"C:\Target", true)]
    [InlineData(@"C:\target", @"C:\other", false)]
    [InlineData(@"C:\a\b", @"C:\a\b\", true)]
    public void PathsEqual_ComparesIgnoringTrailingSeparatorsAndCase(string left, string right, bool expected)
    {
        Assert.Equal(expected, MainViewModel.PathsEqual(left, right));
    }

    [Fact]
    public void PathsEqual_SurvivesInvalidInput()
    {
        // Не должно бросать: сравнение вызывается из цикла создания ссылок.
        Assert.True(MainViewModel.PathsEqual("\0bad", "\0bad"));
        Assert.False(MainViewModel.PathsEqual("\0bad", @"C:\other"));
    }
}
