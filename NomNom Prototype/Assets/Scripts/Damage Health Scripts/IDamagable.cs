/// <summary>
/// Any GameObject that can be damaged by projectiles should implement this interface.
/// </summary>
public interface IDamagable
{
    /// <summary>
    /// Called when something (e.g. a projectile) wants to apply damage to this object.
    /// </summary>
    /// <param name="amount">The amount of damage to apply.</param>
    void TakeDamage(float amount);
}
