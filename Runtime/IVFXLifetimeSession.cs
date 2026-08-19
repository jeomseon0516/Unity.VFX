using System;

namespace Jeomseon.Unity.VFX
{
    public interface IVFXLifetimeSession : IDisposable
    {
        bool Tick(
            in VFXLifetimeContext context,
            float deltaTime,
            float unscaledDeltaTime);
    }
}
