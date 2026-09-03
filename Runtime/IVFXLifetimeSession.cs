using System;

namespace Jeomseon.Unity.VFX
{
    /// <summary>
    /// Holds the mutable state of one active VFX lease. Returning <see langword="true"/> from
    /// <see cref="Tick"/> requests release of that lease.
    /// </summary>
    public interface IVFXLifetimeSession : IDisposable
    {
        bool Tick(
            in VFXLifetimeContext context,
            float deltaTime,
            float unscaledDeltaTime);
    }
}
