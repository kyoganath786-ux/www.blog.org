using NickAI.Core.Models;
using NickAI.Core.Projects;
using Xunit;

namespace NickAI.Core.Tests;

public sealed class FileProjectStoreTests : TempDirectoryTest
{
    private string StoreRoot => PathIn("store");

    private string IndexPath => Path.Combine(StoreRoot, "projects.json");

    private FileProjectStore NewStore() => new(StoreRoot);

    // ---------- construction ----------

    [Fact]
    public void Constructor_creates_the_base_directory_and_starts_empty()
    {
        var store = NewStore();

        Assert.True(Directory.Exists(StoreRoot));
        Assert.Empty(store.All);
        // Nothing is written until the first project exists.
        Assert.False(System.IO.File.Exists(IndexPath));
    }

    [Fact]
    public void Constructor_ignores_a_missing_index_file()
    {
        Assert.False(System.IO.File.Exists(IndexPath));

        var store = NewStore();

        Assert.Empty(store.All);
    }

    // ---------- Create ----------

    [Fact]
    public void Create_persists_the_project_immediately()
    {
        var store = NewStore();

        var project = store.Create("My App");

        Assert.Equal("My App", project.Name);
        Assert.Equal(ProjectKind.Unknown, project.Kind);
        Assert.True(System.IO.File.Exists(IndexPath));
        Assert.Same(project, store.Get(project.Id));
        Assert.Single(store.All);
    }

    [Fact]
    public void Create_raises_the_project_created_event()
    {
        var store = NewStore();
        var raised = new List<Project>();
        store.ProjectCreated += (_, p) => raised.Add(p);

        var project = store.Create("Eventful");

        Assert.Same(project, Assert.Single(raised));
    }

    [Fact]
    public void Create_uses_the_explicit_kind_without_detecting()
    {
        // The folder looks like a Node project, but the caller knows better.
        var folder = CreateProjectFolder("looks-like-node", "package.json");
        var store = NewStore();

        var project = store.Create("Typed", folder, ProjectKind.Unity);

        Assert.Equal(ProjectKind.Unity, project.Kind);
    }

    [Theory]
    [InlineData("godot", "project.godot", ProjectKind.Godot)]
    [InlineData("unity", "ProjectSettings/", ProjectKind.Unity)]
    [InlineData("dotnet-sln", "App.sln", ProjectKind.DotNet)]
    [InlineData("dotnet-csproj", "App.csproj", ProjectKind.DotNet)]
    [InlineData("dotnet-source", "Program.cs", ProjectKind.DotNet)]
    [InlineData("node", "package.json", ProjectKind.Node)]
    [InlineData("python-pyproject", "pyproject.toml", ProjectKind.Python)]
    [InlineData("python-requirements", "requirements.txt", ProjectKind.Python)]
    [InlineData("rust", "Cargo.toml", ProjectKind.Rust)]
    [InlineData("go", "go.mod", ProjectKind.Go)]
    [InlineData("flutter", "pubspec.yaml", ProjectKind.Flutter)]
    [InlineData("web", "index.html", ProjectKind.Web)]
    [InlineData("empty", "", ProjectKind.Generic)]
    public void DetectKind_identifies_the_project_technology(string folder, string marker, ProjectKind expected)
    {
        var path = CreateProjectFolder(folder, marker);
        var store = NewStore();

        Assert.Equal(expected, store.DetectKind(path));

        // Create() picks the same answer up automatically.
        Assert.Equal(expected, store.Create(folder, path).Kind);
    }

    [Fact]
    public void DetectKind_returns_unknown_for_a_missing_folder()
    {
        var store = NewStore();

        Assert.Equal(ProjectKind.Unknown, store.DetectKind(PathIn("does-not-exist")));
    }

    [Fact]
    public void DetectKind_prefers_the_engine_marker_over_node_and_web()
    {
        var path = CreateProjectFolder("mixed-engine", "project.godot", "package.json", "index.html");

        Assert.Equal(ProjectKind.Godot, NewStore().DetectKind(path));
    }

    [Fact]
    public void DetectKind_prefers_node_over_web()
    {
        var path = CreateProjectFolder("mixed-node-web", "package.json", "index.html");

        Assert.Equal(ProjectKind.Node, NewStore().DetectKind(path));
    }

    [Fact]
    public void DetectKind_prefers_dotnet_over_web()
    {
        var path = CreateProjectFolder("mixed-dotnet-web", "App.sln", "index.html");

        Assert.Equal(ProjectKind.DotNet, NewStore().DetectKind(path));
    }

    [Fact]
    public void Create_without_a_root_path_or_a_missing_folder_is_unknown()
    {
        var store = NewStore();

        Assert.Equal(ProjectKind.Unknown, store.Create("no path").Kind);
        Assert.Equal(ProjectKind.Unknown, store.Create("bad path", PathIn("missing-folder")).Kind);
    }

    // ---------- persistence ----------

    [Fact]
    public void Projects_survive_a_restart_including_memory_and_commands()
    {
        var first = NewStore();
        var project = first.Create("Persisted", CreateProjectFolder("app", "App.csproj"));
        project.BuildCommand = "dotnet build";
        project.TestCommand = "dotnet test";
        project.ArtifactIds.Add("artifact-1");
        project.Memory.Requirements.Add("Must work offline");
        project.Memory.KnownErrors.Add("CS0103 missing System.IO");
        project.Memory.DesignDecisions.Add("Core stays UI-free");
        first.Save(project);

        var reopened = NewStore();
        var loaded = Assert.Single(reopened.All);

        Assert.Equal(project.Id, loaded.Id);
        Assert.Equal("Persisted", loaded.Name);
        Assert.Equal(project.RootPath, loaded.RootPath);
        Assert.Equal(ProjectKind.DotNet, loaded.Kind);
        Assert.Equal("dotnet build", loaded.BuildCommand);
        Assert.Equal("dotnet test", loaded.TestCommand);
        Assert.Equal(new[] { "artifact-1" }, loaded.ArtifactIds);
        Assert.Equal(new[] { "Must work offline" }, loaded.Memory.Requirements);
        Assert.Equal(new[] { "CS0103 missing System.IO" }, loaded.Memory.KnownErrors);
        Assert.Equal(new[] { "Core stays UI-free" }, loaded.Memory.DesignDecisions);
    }

    [Fact]
    public void Load_ignores_a_corrupt_index_file_and_recovers_on_the_next_write()
    {
        Directory.CreateDirectory(StoreRoot);
        System.IO.File.WriteAllText(IndexPath, "{ this is not valid json");

        var store = NewStore();

        Assert.Empty(store.All);

        store.Create("Recovered");

        Assert.Single(NewStore().All);
    }

    // ---------- Get / All ----------

    [Fact]
    public void Get_returns_null_for_an_unknown_id()
    {
        var store = NewStore();

        Assert.Null(store.Get("unknown"));
    }

    [Fact]
    public void All_returns_a_snapshot_that_is_unaffected_by_later_creates()
    {
        var store = NewStore();
        store.Create("first");

        var snapshot = store.All;
        store.Create("second");

        Assert.Single(snapshot);
        Assert.Equal(2, store.All.Count);
    }

    // ---------- Save ----------

    [Fact]
    public void Save_persists_edits_to_an_existing_project()
    {
        var store = NewStore();
        var project = store.Create("Before");

        project.Name = "After";
        project.Memory.Notes.Add("edited");
        store.Save(project);

        Assert.Single(store.All);

        var loaded = Assert.Single(NewStore().All);
        Assert.Equal("After", loaded.Name);
        Assert.Equal(new[] { "edited" }, loaded.Memory.Notes);
    }

    [Fact]
    public void Save_does_not_duplicate_a_project_that_is_already_stored()
    {
        var store = NewStore();
        var project = store.Create("Once");

        store.Save(project);
        store.Save(project);

        Assert.Single(store.All);
        Assert.Single(NewStore().All);
    }

    [Fact]
    public void Save_replaces_a_stored_project_that_shares_the_same_id()
    {
        var store = NewStore();
        var original = store.Create("Original");

        var edited = new Project { Id = original.Id, Name = "Edited" };
        store.Save(edited);

        Assert.Single(store.All);
        Assert.Equal("Edited", store.Get(original.Id)!.Name);
        Assert.Equal("Edited", Assert.Single(NewStore().All).Name);
    }

    [Fact]
    public void ProjectCreated_does_not_fire_for_saves()
    {
        var store = NewStore();
        var events = 0;
        store.ProjectCreated += (_, _) => events++;

        var project = store.Create("Quiet");
        store.Save(project);

        Assert.Equal(1, events);
    }

    // ---------- Delete ----------

    [Fact]
    public void Delete_removes_the_project_from_memory_and_from_disk()
    {
        var store = NewStore();
        var project = store.Create("Doomed");

        var deleted = store.Delete(project.Id);

        Assert.True(deleted);
        Assert.Null(store.Get(project.Id));
        Assert.Empty(store.All);
        Assert.Empty(NewStore().All);
    }

    [Fact]
    public void Delete_returns_false_for_an_unknown_id()
    {
        var store = NewStore();

        Assert.False(store.Delete("unknown"));
    }

    [Fact]
    public void Delete_never_touches_the_users_files_on_disk()
    {
        var folder = CreateProjectFolder("precious", "App.csproj", "Program.cs");
        var store = NewStore();
        var project = store.Create("Precious", folder);

        Assert.True(store.Delete(project.Id));

        Assert.True(Directory.Exists(folder));
        Assert.True(System.IO.File.Exists(Path.Combine(folder, "Program.cs")));
    }
}
