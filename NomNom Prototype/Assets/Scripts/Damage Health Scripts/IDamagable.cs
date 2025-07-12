/// <summary>Objects that can take damage.</summary>
public interface IDamagable
{
    /// <summary>Apply damage from an unknown source (legacy).</summary>
    void TakeDamage(float amount);

    /// <summary>Apply damage and tell me who dealt it.</summary>
    void TakeDamage(float amount, ulong attackerId);
}
