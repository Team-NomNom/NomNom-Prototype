using UnityEngine;

/// <summary>
/// ScriptableObject holding core data for a projectile type.
/// Behaviors can reference this data to determine aspects like speed, damage, etc.
/// This class can be inherited from to add custom data specific to a game.
/// </summary>
[CreateAssetMenu(fileName = "NewProjectileData", menuName = "Projectile Factory/Projectile Data/Standard Projectile Data")]
public class ProjectileData : ScriptableObject
{
    [Header("Core Stats")]
    [Tooltip("Base movement speed of the projectile. Behaviors determine how this is used.")]
    [SerializeField]
    private float _speed = 10f;
    public float Speed { get => _speed; protected set => _speed = value; } // Allow derived classes to set

    [Tooltip("Base damage value of the projectile.")]
    [SerializeField]
    private float _damage = 10f;
    public float Damage { get => _damage; protected set => _damage = value; }

    // Add other common projectile properties here as needed, e.g.:
    // public float lifetime = 5f;
    // public float impactForce = 100f;
    // public GameObject hitEffectPrefab;

    /// <summary>
    /// Calculates the damage this projectile should deal.
    /// This can be overridden in derived classes for more complex damage calculations
    /// (e.g., critical hits, damage over time adjustments).
    /// </summary>
    /// <returns>The calculated damage value.</returns>
    public virtual float CalculateDamage()
    {
        return Damage;
    }

    /// <summary>
    /// Calculates the speed of the projectile.
    /// Can be overridden if speed needs to be dynamic (e.g., affected by game time scale).
    /// </summary>
    /// <returns>The calculated speed value.</returns>
    public virtual float GetSpeed()
    {
        return Speed;
    }

#if UNITY_EDITOR
    // Optional: Add some validation or helper methods for the editor if desired
    protected virtual void OnValidate()
    {
        if (_speed < 0)
        {
            _speed = 0;
            Debug.LogWarning("ProjectileData: Speed cannot be negative. Clamped to 0.", this);
        }
        if (_damage < 0)
        {
            _damage = 0;
            Debug.LogWarning("ProjectileData: Damage cannot be negative. Clamped to 0.", this);
        }
    }
#endif
}