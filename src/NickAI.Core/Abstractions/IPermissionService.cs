using NickAI.Core.Models;

namespace NickAI.Core.Abstractions;

/// <summary>
/// Central permission gate. High-impact actions are routed through here before
/// they run; the UI decides ASK prompts.
/// </summary>
public interface IPermissionService
{
    PermissionMode ModeFor(PermissionCategory category);

    void SetMode(PermissionCategory category, PermissionMode mode);

    /// <summary>
    /// Evaluates whether an action in a category may proceed. When
    /// <see cref="PermissionDecision.RequiresConfirmation"/> is true the caller
    /// must ask the user before continuing.
    /// </summary>
    Task<PermissionDecision> RequestAsync(PermissionCategory category, string action, CancellationToken ct = default);

    /// <summary>Feeds the result of a confirmation prompt back into the service.</summary>
    void RememberChoice(PermissionCategory category, bool allowedForever);
}
