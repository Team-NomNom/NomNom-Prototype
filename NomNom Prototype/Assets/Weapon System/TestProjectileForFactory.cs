using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class TestProjectileForFactory : NetworkBehaviour
{
    [SerializeField]
    public float bulletSpeed = 10.0f;

    [SerializeField]
    private float lifetimeSeconds = 5f;


    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        // Only the server (or host) should give this bullet its initial velocity
        // (so we stay authoritative). On clients, we just let NetworkTransform move it.
        if (IsServer)
        {
            // Set velocity exactly once when this bullet spawns
            rb.linearVelocity = transform.forward * bulletSpeed;

            // Start the self-destruct coroutine only on the server
            StartCoroutine(KillMeWhenTimedOut());
        }
    }

    void FixedUpdate()
    {
    }
    // Prevents too many bullets from being loaded
    private IEnumerator KillMeWhenTimedOut()
    {
        yield return new WaitForSeconds(lifetimeSeconds);

        // Only the server should destroy / despawn the networked object
        if (IsServer)
        {
            // Despawn on the network and destroy the GameObject
            GetComponent<NetworkObject>().Despawn();
            Destroy(gameObject);
        }
    }
}
