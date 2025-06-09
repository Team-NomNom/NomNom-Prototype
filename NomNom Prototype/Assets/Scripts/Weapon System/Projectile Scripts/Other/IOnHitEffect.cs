using UnityEngine;

public interface IOnHitEffect
{
    void ApplyEffect(GameObject target, Vector3 hitPoint, float baseDamage);
}

