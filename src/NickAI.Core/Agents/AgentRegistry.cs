using NickAI.Core.Abstractions;

namespace NickAI.Core.Agents;

/// <summary>
/// Maps capability keys to agents. Capabilities with no registered agent are
/// reported as unsupported rather than silently faked.
/// </summary>
public sealed class AgentRegistry
{
    private readonly Dictionary<string, IAgent> _agents = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IAgent agent) => _agents[agent.Capability] = agent;

    public bool Has(string capability) => _agents.ContainsKey(capability);

    public bool TryGet(string capability, out IAgent agent) => _agents.TryGetValue(capability, out agent!);

    public IReadOnlyCollection<IAgent> Agents => _agents.Values;

    /// <summary>Capabilities currently backed by a real agent implementation.</summary>
    public IReadOnlyCollection<string> ImplementedCapabilities => _agents.Keys;
}
