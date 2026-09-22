using System.Diagnostics;
using System.IO;

namespace NickAI.App.Services;

/// <summary>Opens artifacts and folders using the Windows shell.</summary>
public sealed class ShellService
{
    public void OpenFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    public void OpenFolder(string? path)
    {
        var target = Directory.Exists(path)
            ? path
            : path is not null ? Path.GetDirectoryName(path) : null;
        if (string.IsNullOrWhiteSpace(target) || !Directory.Exists(target)) return;
        Start(new ProcessStartInfo("explorer.exe", $"\"{target}\"") { UseShellExecute = true });
    }

    public void RevealInExplorer(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
    }

    public void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private static void Start(ProcessStartInfo startInfo)
    {
        try
        {
            Process.Start(startInfo);
        }
        catch (Exception)
        {
            // Nothing sensible to do if the shell rejects the request.
        }
    }
}
