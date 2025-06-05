public interface IProjectile
{
    void Initialize(ulong shooterId);
    void ApplyConfig(ProjectileConfig config);
}