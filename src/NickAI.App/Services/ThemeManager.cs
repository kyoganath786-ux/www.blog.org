using System.Windows;
using Microsoft.Win32;

namespace NickAI.App.Services;

public enum ThemeMode
{
    Dark,
    Light,
    System,
}

/// <summary>Applies the active theme by swapping the merged theme dictionary.</summary>
public static class ThemeManager
{
    public static ThemeMode Current { get; private set; } = ThemeMode.Dark;

    public static void Apply(ThemeMode mode)
    {
        Current = mode;
        var resolved = mode == ThemeMode.System ? DetectSystemTheme() : mode;
        var source = resolved == ThemeMode.Light ? "Themes/Light.xaml" : "Themes/Dark.xaml";

        var dictionaries = Application.Current?.Resources.MergedDictionaries;
        if (dictionaries is null) return;

        var themeDictionary = dictionaries.FirstOrDefault(d =>
            d.Source is not null &&
            (d.Source.OriginalString.Contains("Themes/Dark.xaml", StringComparison.OrdinalIgnoreCase) ||
             d.Source.OriginalString.Contains("Themes/Light.xaml", StringComparison.OrdinalIgnoreCase)));

        var replacement = new ResourceDictionary { Source = new Uri(source, UriKind.Relative) };

        if (themeDictionary is null)
        {
            dictionaries.Add(replacement);
            return;
        }

        var index = dictionaries.IndexOf(themeDictionary);
        dictionaries[index] = replacement;
    }

    private static ThemeMode DetectSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int light && light == 1) return ThemeMode.Light;
        }
        catch (Exception)
        {
            // Registry unavailable; fall back to dark.
        }

        return ThemeMode.Dark;
    }
}
