using System.Globalization;

namespace Milvus.Client.V2.Utils;

/// <summary>
/// One <c>_grpc._tcp.</c> SRV record resolved for a global-cluster endpoint's ha-manager seeds. Mirrors the Java
/// SDK's <c>SrvTarget</c>.
/// </summary>
/// <remarks>
/// Zilliz Cloud only.
/// </remarks>
internal sealed class SrvTarget : IEquatable<SrvTarget>
{
    internal SrvTarget(int priority, int weight, int port, string target)
    {
        Priority = priority;
        Weight = weight;
        Port = port;
        Target = target;
    }

    /// <summary>
    /// The SRV priority; lower is preferred.
    /// </summary>
    internal int Priority { get; }

    /// <summary>
    /// The SRV weight, used to break ties within the same priority.
    /// </summary>
    internal int Weight { get; }

    /// <summary>
    /// The port of the ha-manager seed.
    /// </summary>
    internal int Port { get; }

    /// <summary>
    /// The hostname of the ha-manager seed (trailing dot stripped).
    /// </summary>
    internal string Target { get; }

    public bool Equals(SrvTarget? other)
        => other is not null && Port == other.Port && string.Equals(Target, other.Target, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is SrvTarget other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Port, Target);

    public override string ToString()
        => $"{Target}:{Port.ToString(CultureInfo.InvariantCulture)} (priority={Priority.ToString(CultureInfo.InvariantCulture)}, weight={Weight.ToString(CultureInfo.InvariantCulture)})";
}
