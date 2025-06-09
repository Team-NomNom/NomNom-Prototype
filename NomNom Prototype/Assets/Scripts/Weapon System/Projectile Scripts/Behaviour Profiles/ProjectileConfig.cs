using System.Collections;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapon System/Projectile Scripts", fileName = "New ProjectileProfile")]
public class ProjectileConfig : ScriptableObject
{
    public float speed = 20f;
    public float damage = 10f;
    public float lifetime = 5f;
    public bool affectsOwner = false;
    public GameObject hitEffectPrefab;
}