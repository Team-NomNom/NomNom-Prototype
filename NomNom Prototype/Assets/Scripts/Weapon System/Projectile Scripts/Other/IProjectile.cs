using UnityEngine;

public interface IProjectile
{
    void Initialize(ulong shooterId, GameObject shooterRoot, IProjectileFactoryUser factoryUser = null, int weaponIndex = -1);
    void ApplyConfig(ProjectileConfig config);
}
