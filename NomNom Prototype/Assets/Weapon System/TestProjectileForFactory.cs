using System;
using System.Collections;
using UnityEngine;

public class TestProjectileForFactory : MonoBehaviour
{
    [SerializeField]
    public float bulletSpeed = 10.0f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * bulletSpeed;
        StartCoroutine(killMe());
    }

    void FixedUpdate()
    {
    }
    // Prevents too many bullets from being loaded
    IEnumerator killMe()
    {
        yield return new WaitForSeconds(5);
        Destroy(gameObject);
    }
}
