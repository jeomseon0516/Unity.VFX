namespace Jeomseon.Unity.VFX
{
    public interface IVFXProvider
    {
        VFXHandle Spawn(in VFXSpawnOptions options);

        bool TryRelease(VFXHandle handle);
    }
}
