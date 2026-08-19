using System;
using UnityEngine;

namespace Jeomseon.Unity.VFX
{
    public readonly struct VFXHandle : IEquatable<VFXHandle>
    {
        private readonly IVFXHandleOwner _owner;
        private readonly int _instanceId;
        private readonly uint _generation;

        public bool IsValid => _owner?.IsValid(_instanceId, _generation) == true;

        internal VFXHandle(IVFXHandleOwner owner, int instanceId, uint generation)
        {
            _owner = owner;
            _instanceId = instanceId;
            _generation = generation;
        }

        internal bool TryRelease(IVFXHandleOwner owner) =>
            ReferenceEquals(_owner, owner) &&
            owner.TryRelease(_instanceId, _generation);

        public bool TryRelease() => _owner?.TryRelease(_instanceId, _generation) == true;

        public bool TrySetPose(Vector3 position, Quaternion rotation) =>
            _owner?.TrySetPose(_instanceId, _generation, position, rotation) == true;

        public bool TrySetScale(Vector3 scale) =>
            _owner?.TrySetScale(_instanceId, _generation, scale) == true;

        public bool TrySetParent(Transform parent, bool worldPositionStays = true) =>
            _owner?.TrySetParent(
                _instanceId,
                _generation,
                parent,
                worldPositionStays) == true;

        public bool TryPause() => _owner?.TryPause(_instanceId, _generation) == true;
        public bool TryResume() => _owner?.TryResume(_instanceId, _generation) == true;

        public bool TryStopEmission(bool clearParticles = false) =>
            _owner?.TryStopEmission(
                _instanceId,
                _generation,
                clearParticles) == true;

        public bool Equals(VFXHandle other) =>
            ReferenceEquals(_owner, other._owner) &&
            _instanceId == other._instanceId &&
            _generation == other._generation;

        public override bool Equals(object obj) => obj is VFXHandle other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(_owner, _instanceId, _generation);
        public static bool operator ==(VFXHandle left, VFXHandle right) => left.Equals(right);
        public static bool operator !=(VFXHandle left, VFXHandle right) => !left.Equals(right);
    }
}
