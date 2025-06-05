using UnityEngine;

public interface IProjectile
{
    void Initialize(ulong shooterId, GameObject shooterRoot);
    void ApplyConfig(ProjectileConfig config);
}