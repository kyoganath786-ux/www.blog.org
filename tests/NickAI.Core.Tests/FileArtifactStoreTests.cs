using NickAI.Core.Artifacts;
using NickAI.Core.Models;
using Xunit;

namespace NickAI.Core.Tests;

public sealed class FileArtifactStoreTests : TempDirectoryTest
{
    private string StoreRoot => PathIn("artifacts");

    private FileArtifactStore NewStore() => new(StoreRoot);

    // ---------- construction ----------

    [Fact]
    public void Constructor_creates_the_root_directory_including_missing_parents()
    {
        var root = PathIn(Path.Combine("nested", "deeper", "artifacts"));

        var store = new FileArtifactStore(root);

        Assert.Equal(root, store.RootPath);
        Assert.True(Directory.Exists(root));
        Assert.Empty(store.All);
    }

    [Fact]
    public void Constructor_is_idempotent_for_an_existing_directory()
    {
        var store = NewStore();
        store.CreateFile("keep.md", ArtifactType.Markdown, "keep me");

        var reopened = NewStore();

        // A new store starts empty; existing files on disk are left alone.
        Assert.Empty(reopened.All);
        Assert.True(System.IO.File.Exists(PathIn(Path.Combine("artifacts", "keep.md"))));
    }

    // ---------- CreateFile ----------

    [Fact]
    public void CreateFile_writes_the_file_to_disk_and_records_the_artifact()
    {
        var store = NewStore();

        var artifact = store.CreateFile("report.md", ArtifactType.Markdown, "# Report");

        Assert.Equal("report.md", artifact.Name);
        Assert.Equal(ArtifactType.Markdown, artifact.Type);
        Assert.Equal("text/markdown", artifact.MimeType);
        Assert.Equal(StoreRoot, Path.GetDirectoryName(artifact.FilePath));
        Assert.True(artifact.HasFile);
        Assert.Equal("# Report", System.IO.File.ReadAllText(artifact.FilePath!));

        Assert.Single(store.All);
        Assert.Same(artifact, store.Get(artifact.Id));
    }

    [Theory]
    [InlineData(ArtifactType.Text, "text/plain")]
    [InlineData(ArtifactType.Code, "text/plain")]
    [InlineData(ArtifactType.Markdown, "text/markdown")]
    [InlineData(ArtifactType.Csv, "text/csv")]
    [InlineData(ArtifactType.Pdf, "application/pdf")]
    [InlineData(ArtifactType.Zip, "application/zip")]
    [InlineData(ArtifactType.Exe, "application/vnd.microsoft.portable-executable")]
    // Types without a registered MIME type fall back honestly.
    [InlineData(ArtifactType.Installer, "application/octet-stream")]
    [InlineData(ArtifactType.Website, "application/octet-stream")]
    public void CreateFile_assigns_a_mime_type_for_the_artifact_type(ArtifactType type, string expectedMime)
    {
        var store = NewStore();

        var artifact = store.CreateFile("thing.bin", type, "x");

        Assert.Equal(expectedMime, artifact.MimeType);
    }

    [Fact]
    public void CreateFile_keeps_the_artifact_inside_the_root_even_for_hostile_names()
    {
        var store = NewStore();

        var traversal = store.CreateFile(Path.Combine("..", "..", "escape.txt"), ArtifactType.Text, "nope");
        var absolute = store.CreateFile("/etc/passwd", ArtifactType.Text, "nope");

        Assert.Equal(StoreRoot, Path.GetDirectoryName(traversal.FilePath));
        Assert.Equal("escape.txt", traversal.Name);
        Assert.Equal(StoreRoot, Path.GetDirectoryName(absolute.FilePath));
        Assert.Equal("passwd", absolute.Name);

        // Nothing was written outside the artifact root.
        Assert.False(System.IO.File.Exists(PathIn("escape.txt")));
    }

    [Fact]
    public void CreateFile_never_reuses_a_name_and_keeps_every_content()
    {
        var store = NewStore();

        var first = store.CreateFile("report.md", ArtifactType.Markdown, "first");
        var second = store.CreateFile("report.md", ArtifactType.Markdown, "second");
        var third = store.CreateFile("report.md", ArtifactType.Markdown, "third");

        Assert.Equal("report.md", first.Name);
        Assert.Equal("report-2.md", second.Name);
        Assert.Equal("report-3.md", third.Name);

        Assert.Equal("first", System.IO.File.ReadAllText(first.FilePath!));
        Assert.Equal("second", System.IO.File.ReadAllText(second.FilePath!));
        Assert.Equal("third", System.IO.File.ReadAllText(third.FilePath!));
    }

    [Fact]
    public void CreateFile_copies_metadata_and_project_id()
    {
        var store = NewStore();

        var artifact = store.CreateFile(
            "chart.csv",
            ArtifactType.Csv,
            "a,b",
            projectId: "project-1",
            metadata: new Dictionary<string, string> { ["rows"] = "2" });

        Assert.Equal("project-1", artifact.ProjectId);
        Assert.Equal("2", artifact.Metadata["rows"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateFile_generates_a_name_when_the_supplied_one_is_blank(string name)
    {
        var store = NewStore();

        var artifact = store.CreateFile(name, ArtifactType.Text, "x");

        Assert.StartsWith("artifact-", artifact.Name);
        Assert.True(artifact.HasFile);
    }

    [Fact]
    public void CreateFile_writes_empty_and_unicode_content_verbatim()
    {
        var store = NewStore();

        var empty = store.CreateFile("empty.txt", ArtifactType.Text, string.Empty);
        var unicode = store.CreateFile("unicode.txt", ArtifactType.Text, "héllo ⚡ 日本語");

        Assert.True(empty.HasFile);
        Assert.Equal(string.Empty, System.IO.File.ReadAllText(empty.FilePath!));
        Assert.Equal("héllo ⚡ 日本語", System.IO.File.ReadAllText(unicode.FilePath!));
    }

    [Fact]
    public void CreateFile_recreates_the_root_directory_if_it_was_deleted()
    {
        var store = NewStore();
        Directory.Delete(StoreRoot, recursive: true);

        var artifact = store.CreateFile("recovered.txt", ArtifactType.Text, "back");

        Assert.True(artifact.HasFile);
        Assert.Equal("back", System.IO.File.ReadAllText(artifact.FilePath!));
    }

    // ---------- Add / Get / All ----------

    [Fact]
    public void Add_registers_the_artifact_and_raises_the_event()
    {
        var store = NewStore();
        var raised = new List<Artifact>();
        store.ArtifactAdded += (_, a) => raised.Add(a);

        var artifact = new Artifact { Name = "summary", Type = ArtifactType.Report, Preview = "preview" };
        var id = store.Add(artifact);

        Assert.Equal(artifact.Id, id);
        var notified = Assert.Single(raised);
        Assert.Same(artifact, notified);

        // An artifact with no file is still valid; it just has no backing file.
        Assert.False(artifact.HasFile);
        Assert.Same(artifact, store.Get(id));
    }

    [Fact]
    public void All_returns_a_snapshot_that_is_unaffected_by_later_writes()
    {
        var store = NewStore();
        store.CreateFile("one.txt", ArtifactType.Text, "1");

        var snapshot = store.All;
        store.CreateFile("two.txt", ArtifactType.Text, "2");

        Assert.Single(snapshot);
        Assert.Equal(2, store.All.Count);
    }

    [Fact]
    public void Get_returns_null_for_an_unknown_id()
    {
        var store = NewStore();

        Assert.Null(store.Get("does-not-exist"));
    }

    // ---------- Remove ----------

    [Fact]
    public void Remove_deletes_the_entry_and_the_backing_file()
    {
        var store = NewStore();
        var artifact = store.CreateFile("gone.txt", ArtifactType.Text, "bye");
        var path = artifact.FilePath!;

        var removed = store.Remove(artifact.Id);

        Assert.True(removed);
        Assert.Null(store.Get(artifact.Id));
        Assert.Empty(store.All);
        Assert.False(System.IO.File.Exists(path));
    }

    [Fact]
    public void Remove_returns_false_for_an_unknown_id()
    {
        var store = NewStore();

        Assert.False(store.Remove("nope"));
    }

    [Fact]
    public void Remove_handles_an_artifact_that_has_no_backing_file()
    {
        var store = NewStore();
        var artifact = new Artifact { Name = "text-only", Type = ArtifactType.Text };
        store.Add(artifact);

        Assert.True(store.Remove(artifact.Id));
        Assert.Empty(store.All);
    }

    [Fact]
    public void Remove_frees_the_name_for_reuse()
    {
        var store = NewStore();
        var first = store.CreateFile("report.md", ArtifactType.Markdown, "one");

        store.Remove(first.Id);
        var second = store.CreateFile("report.md", ArtifactType.Markdown, "two");

        Assert.Equal("report.md", second.Name);
        Assert.Equal("two", System.IO.File.ReadAllText(second.FilePath!));
    }

    [Fact]
    public void HasFile_reflects_the_file_disappearing_from_disk()
    {
        var store = NewStore();
        var artifact = store.CreateFile("volatile.txt", ArtifactType.Text, "x");
        Assert.True(artifact.HasFile);

        System.IO.File.Delete(artifact.FilePath!);

        Assert.False(artifact.HasFile);

        // Removing it must not throw just because the file is already gone.
        Assert.True(store.Remove(artifact.Id));
    }

    // ---------- ReservePath ----------

    [Theory]
    [InlineData("plain.txt")]
    [InlineData("sub/dir/plain.txt")]
    [InlineData("../../escape.txt")]
    [InlineData("/etc/passwd")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("weird:na*me?.txt")]
    public void ReservePath_always_returns_a_path_inside_the_root(string fileName)
    {
        var store = NewStore();

        var path = store.ReservePath(fileName);

        Assert.Equal(StoreRoot, Path.GetDirectoryName(path));
        Assert.StartsWith(StoreRoot, path);
    }

    [Fact]
    public void ReservePath_returns_the_first_free_suffix()
    {
        var store = NewStore();
        store.CreateFile("report.md", ArtifactType.Markdown, "one");

        var reserved = store.ReservePath("report.md");

        Assert.Equal(Path.Combine(StoreRoot, "report-2.md"), reserved);
        Assert.False(System.IO.File.Exists(reserved));
    }

    // ---------- concurrency ----------

    [Fact]
    public void CreateFile_is_safe_when_many_writes_race_for_the_same_name()
    {
        var store = NewStore();
        const int writers = 100;

        Parallel.For(0, writers, i =>
            store.CreateFile("report.md", ArtifactType.Markdown, $"content {i}"));

        var artifacts = store.All.ToList();
        Assert.Equal(writers, artifacts.Count);

        // Every writer must have claimed its own file, not overwritten a peer's.
        Assert.Equal(writers, artifacts.Select(a => a.FilePath).Distinct().Count());
        Assert.All(artifacts, a => Assert.True(a.HasFile));

        var contents = artifacts
            .Select(a => System.IO.File.ReadAllText(a.FilePath!))
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(
            Enumerable.Range(0, writers).Select(i => $"content {i}").OrderBy(t => t, StringComparer.Ordinal),
            contents);
    }
}
