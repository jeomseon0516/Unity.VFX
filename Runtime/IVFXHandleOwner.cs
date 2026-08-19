using UnityEngine;

namespace Jeomseon.Unity.VFX
{
    public interface IVFXHandleOwner
    {
        bool IsValid(int instanceId, uint generation);
        bool TryRelease(int instanceId, uint generation);
        bool TrySetPose(int instanceId, uint generation, Vector3 position, Quaternion rotation);
        bool TrySetScale(int instanceId, uint generation, Vector3 scale);
        bool TrySetParent(int instanceId, uint generation, Transform parent, bool worldPositionStays);
        bool TryPause(int instanceId, uint generation);
        bool TryResume(int instanceId, uint generation);
        bool TryStopEmission(int instanceId, uint generation, bool clearParticles);
    }
}
