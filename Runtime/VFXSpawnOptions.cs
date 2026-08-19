using UnityEngine;

namespace Jeomseon.Unity.VFX
{
    public readonly struct VFXSpawnOptions
    {
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Scale { get; }
        public Transform Parent { get; }

        public VFXSpawnOptions(
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            Vector3? scale = null)
        {
            Position = position;
            Rotation = rotation;
            Parent = parent;
            Scale = scale ?? Vector3.one;
        }

        public static VFXSpawnOptions At(
            Vector3 position,
            Quaternion rotation,
            Transform parent = null) =>
            new(position, rotation, parent);
    }
}
