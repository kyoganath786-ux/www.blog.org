using NickAI.Core.Abstractions;
using NickAI.Core.Models;

namespace NickAI.Core.Services;

/// <summary>
/// Central permission gate. Defaults are conservative; the UI surfaces ASK
/// decisions as confirmation prompts and can persist ALLOW choices.
/// </summary>
public sealed class PermissionService : IPermissionService
{
    private readonly Dictionary<PermissionCategory, PermissionMode> _modes;
    private readonly HashSet<PermissionCategory> _allowForever = new();
    private readonly object _gate = new();

    public PermissionService()
    {
        // Terminal is allowed by default because building the user's own project
        // is the app's core job. High-impact categories require confirmation.
        _modes = Enum.GetValues<PermissionCategory>()
            .ToDictionary(c => c, _ => PermissionMode.Ask);

        _modes[PermissionCategory.Terminal] = PermissionMode.Allow;
    }

    public PermissionMode ModeFor(PermissionCategory category)
    {
        lock (_gate)
        {
            return _modes.TryGetValue(category, out var mode) ? mode : PermissionMode.Ask;
        }
    }

    public void SetMode(PermissionCategory category, PermissionMode mode)
    {
        lock (_gate)
        {
            _modes[category] = mode;
        }
    }

    public Task<PermissionDecision> RequestAsync(PermissionCategory category, string action, CancellationToken ct = default)
    {
        var mode = ModeFor(category);
        return Task.FromResult(mode switch
        {
            PermissionMode.Allow => PermissionDecision.Allow(),
            PermissionMode.Deny => PermissionDecision.Deny($"{category} access is denied in the permission center."),
            _ => PermissionDecision.Confirm($"{category}: {action}"),
        });
    }

    public void RememberChoice(PermissionCategory category, bool allowedForever)
    {
        lock (_gate)
        {
            if (!allowedForever) return;
            _allowForever.Add(category);
            _modes[category] = PermissionMode.Allow;
        }
    }
}
