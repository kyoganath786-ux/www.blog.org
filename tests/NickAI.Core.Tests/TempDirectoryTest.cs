namespace NickAI.Core.Tests;

/// <summary>
/// Gives every test its own temporary directory and removes it afterwards so the
/// file-based stores can be exercised against the real file system safely.
/// Each test method gets a fresh instance (and therefore a fresh directory).
/// </summary>
public abstract class TempDirectoryTest : IDisposable
{
    protected TempDirectoryTest()
    {
        Root = Path.Combine(Path.GetTempPath(), "nickai-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Root);
    }

    /// <summary>Absolute path of this test's throwaway directory.</summary>
    protected string Root { get; }

    protected string PathIn(string relativePath) => Path.Combine(Root, relativePath);

    protected string CreateDirectoryIn(string relativePath)
    {
        var path = PathIn(relativePath);
        Directory.CreateDirectory(path);
        return path;
    }

    protected string WriteFileIn(string relativePath, string content = "test content")
    {
        var path = PathIn(relativePath);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Creates a folder that looks like a project. A marker ending in '/' is
    /// created as a directory (Unity uses a ProjectSettings folder), anything
    /// else as a file.
    /// </summary>
    protected string CreateProjectFolder(string name, params string[] markers)
    {
        var path = CreateDirectoryIn(name);
        foreach (var marker in markers)
        {
            if (string.IsNullOrEmpty(marker)) continue;

            if (marker.EndsWith('/'))
                Directory.CreateDirectory(Path.Combine(path, marker.TrimEnd('/')));
            else
                File.WriteAllText(Path.Combine(path, marker), "marker");
        }

        return path;
    }

    public void Dispose()
    {
        // Windows can hold a handle briefly after a file is written, so deleting
        // is retried a few times before giving up rather than failing the test.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (!Directory.Exists(Root)) return;
                Directory.Delete(Root, recursive: true);
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(25);
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(25);
            }
        }
    }
}
