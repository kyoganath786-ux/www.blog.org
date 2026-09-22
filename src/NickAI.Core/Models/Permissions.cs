namespace NickAI.Core.Models;

public enum PermissionCategory
{
    Browser,
    Files,
    Applications,
    Terminal,
    Network,
    Screen,
    Microphone,
    Camera,
    Downloads,
    Uploads,
    Git,
    Publishing,
}

/// <summary>Permission modes as defined by the permission center.</summary>
public enum PermissionMode
{
    Allow,
    Ask,
    Deny,
}

/// <summary>Result of asking the permission center whether an action may run.</summary>
public readonly record struct PermissionDecision(bool Allowed, bool RequiresConfirmation, string? Reason = null)
{
    public static PermissionDecision Allow() => new(true, false);
    public static PermissionDecision Confirm(string reason) => new(true, true, reason);
    public static PermissionDecision Deny(string reason) => new(false, false, reason);
}
