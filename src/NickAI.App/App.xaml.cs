using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NickAI.App.Services;
using NickAI.App.ViewModels;
using NickAI.Core.Abstractions;
using NickAI.Core.Agents;
using NickAI.Core.Artifacts;
using NickAI.Core.Planning;
using NickAI.Core.Projects;
using NickAI.Core.Providers;
using NickAI.Core.Services;

namespace NickAI.App;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ThemeManager.Apply(ThemeMode.Dark);

        var dataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NickAI");
        Directory.CreateDirectory(dataRoot);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => ConfigureServices(services, dataRoot))
            .Build();

        await _host.StartAsync();

        var window = _host.Services.GetRequiredService<Views.MainWindow>();
        window.Show();

        if (window.DataContext is MainViewModel mainViewModel)
            await mainViewModel.InitializeAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services, string dataRoot)
    {
        // Infrastructure
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<IPermissionService, PermissionService>();
        services.AddSingleton<IArtifactStore>(_ => new FileArtifactStore(Path.Combine(dataRoot, "artifacts")));
        services.AddSingleton<IProjectStore>(_ => new FileProjectStore(dataRoot));

        // Models
        services.AddSingleton<ModelManager>(sp =>
        {
            var manager = new ModelManager(sp.GetRequiredService<ILogger<ModelManager>>());
            manager.Register(new OllamaModelProvider(logger: sp.GetRequiredService<ILogger<OllamaModelProvider>>()));
            return manager;
        });
        services.AddSingleton<ModelRouter>();

        // Agents
        services.AddSingleton<RequestClassifier>();
        services.AddSingleton<AgentRegistry>(sp =>
        {
            var registry = new AgentRegistry();
            registry.Register(new ChatAgent());
            registry.Register(new CodeAgent());
            registry.Register(new DocumentationAgent());
            registry.Register(new BuildAgent(
                sp.GetRequiredService<IProcessRunner>(),
                sp.GetRequiredService<IPermissionService>()));
            return registry;
        });
        services.AddSingleton<OrchestratorAgent>();
        services.AddSingleton<ChatService>();

        // App services
        services.AddSingleton<ShellService>();
        services.AddSingleton<SessionState>();

        // View models
        services.AddSingleton<ChatViewModel>();
        services.AddSingleton<ProjectsViewModel>();
        services.AddSingleton<ArtifactsViewModel>();
        services.AddSingleton<ModelsViewModel>();
        services.AddSingleton<PermissionsViewModel>();
        services.AddSingleton<MainViewModel>();

        // Windows
        services.AddSingleton<Views.MainWindow>();
    }
}
