namespace Jeomseon.Unity.VFX
{
    /// <summary>
    /// Defines immutable lifetime settings and creates one mutable session for each VFX lease.
    /// Implement this interface to add a custom lifetime policy without registering a separate
    /// handler. A returned session belongs to that lease and is disposed when the lease ends.
    /// </summary>
    public interface IVFXLifetimeConfiguration
    {
        IVFXLifetimeSession CreateSession(in VFXLifetimeContext context);
    }
}
