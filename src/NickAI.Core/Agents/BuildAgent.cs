using NickAI.Core.Abstractions;
using NickAI.Core.Models;
using NickAI.Core.Planning;
using NickAI.Core.Services;

namespace NickAI.Core.Agents;

/// <summary>
/// Builds the selected project with its real toolchain, captures the actual
/// output and reports the true exit code. Never claims success without it.
/// </summary>
public sealed class BuildAgent : IAgent
{
    private readonly IProcessRunner _runner;
    private readonly IPermissionService _permissions;

    public BuildAgent(IProcessRunner runner, IPermissionService permissions)
    {
        _runner = runner;
        _permissions = permissions;
    }

    public string Name => "BuildAgent";

    public string Capability => Capabilities.Build;

    public async Task<AgentTask> ExecuteAsync(AgentTask task, AgentContext context, CancellationToken ct = default)
    {
        var project = context.Project;
        if (project is null || string.IsNullOrWhiteSpace(project.RootPath) || !Directory.Exists(project.RootPath))
        {
            task.Status = AgentTaskStatus.Failed;
            task.Error = "No project folder is selected. Open a project first.";
            return task;
        }

        var command = ResolveCommand(project);
        if (command is null)
        {
            task.Status = AgentTaskStatus.Failed;
            task.Error = $"No build command is configured for a {project.Kind} project. Set one in Project settings.";
            return task;
        }

        var decision = await _permissions.RequestAsync(
            PermissionCategory.Terminal,
            $"Run `{command.Value.FileName} {command.Value.Arguments}` in {project.RootPath}",
            ct).ConfigureAwait(false);

        if (!decision.Allowed)
        {
            task.Status = AgentTaskStatus.Failed;
            task.Error = decision.Reason ?? "Terminal permission denied.";
            return task;
        }

        if (decision.RequiresConfirmation)
        {
            task.Status = AgentTaskStatus.Failed;
            task.Error = "Running a build requires confirmation. Allow Terminal access in the permission center.";
            return task;
        }

        context.Progress?.Report("Building...");

        var result = await _runner.RunAsync(
            command.Value.FileName,
            command.Value.Arguments,
            project.RootPath,
            TimeSpan.FromMinutes(10),
            ct).ConfigureAwait(false);

        var log = $"""
            $ {command.Value.FileName} {command.Value.Arguments}
            # working directory: {project.RootPath}
            # exit code: {result.ExitCode}  duration: {result.Duration.TotalSeconds:0.0}s  timed out: {result.TimedOut}

            {result.StandardOutput}

            {result.StandardError}
            """;

        context.Artifacts.CreateFile("build-log.txt", ArtifactType.Text, log, project.Id);

        if (result.Succeeded)
        {
            task.Status = AgentTaskStatus.Succeeded;
            task.Result = $"Build succeeded (exit 0, {result.Duration.TotalSeconds:0.0}s).";
        }
        else
        {
            var detail = result.TimedOut
                ? "Build timed out."
                : $"Build failed with exit code {result.ExitCode}.";
            task.Status = AgentTaskStatus.Failed;
            task.Error = detail;
            task.Result = SummarizeErrors(result);
        }

        return task;
    }

    private static (string FileName, string Arguments)? ResolveCommand(Project project)
    {
        if (!string.IsNullOrWhiteSpace(project.BuildCommand))
        {
            var parts = project.BuildCommand.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? null : (parts[0], parts.Length > 1 ? parts[1] : string.Empty);
        }

        return project.Kind switch
        {
            ProjectKind.DotNet => ("dotnet", "build -c Release"),
            ProjectKind.Node => ("npm", "run build"),
            ProjectKind.Python => ("python", "-m compileall ."),
            ProjectKind.Rust => ("cargo", "build --release"),
            ProjectKind.Go => ("go", "build ./..."),
            ProjectKind.Flutter => ("flutter", "build windows"),
            _ => null,
        };
    }

    private static string SummarizeErrors(ProcessResult result)
    {
        var lines = result.CombinedOutput
            .Split('\n')
            .Where(l => l.Contains("error", StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();

        return lines.Count == 0 ? "No error lines were captured." : string.Join(Environment.NewLine, lines);
    }
}
