using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI; // only if you want to update a UI element (optional)

public class Health : NetworkBehaviour, IDamagable
{
    [Header("Health Settings")]
    [Tooltip("Maximum health value.")]
    [SerializeField] private float maxHealth = 100f;

    // This NetworkVariable synchronizes health across clients.
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Optional UI")]
    [Tooltip("Drag a UI Slider or Text here to show current health (for debugging).")]
    [SerializeField] private Slider healthBar; // or Text healthText;
    [SerializeField] private Text healthText;

    private void Awake()
    {
        currentHealth.Value = maxHealth;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        currentHealth.OnValueChanged += OnHealthChanged;

    }

    private void OnDestroy()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }


    // Subtracts from currentHealth.
    public void TakeDamage(float amount)
    {
        // Only the server should ever modify health.
        if (!IsServer) return;

        currentHealth.Value -= amount;
        Debug.Log($"[Health] {gameObject.name} took {amount} damage, now at {currentHealth.Value}/{maxHealth}");

        if (currentHealth.Value <= 0f)
        {
            Die();
        }
    }

    private void OnHealthChanged(float previousValue, float newValue)
    {
        if (healthBar != null)
        {
            healthBar.value = newValue / maxHealth;
        }
        // If you had a Text field:
        if (healthText != null) healthText.text = $"{newValue:0}/{maxHealth:0}";
    }


    private void Die()
    {
        Debug.Log($"[Health] {gameObject.name} died.");


        GetComponent<NetworkObject>().Despawn();
    }

    // Example of a ClientRpc to play a death VFX on everyone:
    /*
    [ClientRpc]
    private void SpawnDeathVFXClientRpc()
    {
        // Instantiate your particle prefab at transform.position, etc.
    }
    */
}
