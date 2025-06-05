using System.Collections;
using Unity.Netcode;
using UnityEngine;
public class SimpleProjectile : ProjectileBase
{
    // Inherits all behavior from ProjectileBase
    // Add debug logging to confirm it uses updated base
    private void Start()
    {
        Debug.Log("[SimpleProjectile] Initialized with base behavior");
    }
}