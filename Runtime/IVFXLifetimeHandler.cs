namespace Jeomseon.Unity.VFX
{
    public interface IVFXLifetimeHandler
    {
        bool CanHandle(IVFXLifetimeConfiguration configuration);

        IVFXLifetimeSession Begin(
            IVFXLifetimeConfiguration configuration,
            in VFXLifetimeContext context);
    }
}
