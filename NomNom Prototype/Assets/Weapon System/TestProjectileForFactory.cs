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

        if (IsServer)
        {
            rb.linearVelocity = transform.forward * bulletSpeed;

            // Start the self-destruct coroutine only on the server
            StartCoroutine(KillMeWhenTimedOut());
        }
    }

    private IEnumerator KillMeWhenTimedOut()
    {
        yield return new WaitForSeconds(lifetimeSeconds);

        if (IsServer)
        {
            // Despawn on the network and destroy the GameObject
            GetComponent<NetworkObject>().Despawn();
            Destroy(gameObject);
        }
    }
}
