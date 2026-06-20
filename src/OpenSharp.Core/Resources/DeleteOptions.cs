using OpenSharp.Core.Abstractions;

namespace OpenSharp.Core.Resources;

/// <summary>
/// Options controlling how a resource is deleted: dependent propagation, an optional grace
/// period, and an optional force flag for immediate removal.
/// </summary>
public sealed class DeleteOptions
{
    /// <summary>
    /// How to handle dependent resources. Defaults to
    /// <see cref="DeletePropagationPolicy.Background"/>.
    /// </summary>
    public DeletePropagationPolicy Propagation { get; init; } = DeletePropagationPolicy.Background;

    /// <summary>
    /// Number of seconds to wait before forcibly terminating the resource. <c>0</c> requests
    /// immediate deletion; <see langword="null"/> uses the server default.
    /// </summary>
    public int? GracePeriodSeconds { get; init; }

    /// <summary>
    /// When <see langword="true"/>, requests immediate deletion (an effective grace period of
    /// <c>0</c>), equivalent to <c>--force --grace-period=0</c>. Overrides
    /// <see cref="GracePeriodSeconds"/>.
    /// </summary>
    public bool Force { get; init; }
}
