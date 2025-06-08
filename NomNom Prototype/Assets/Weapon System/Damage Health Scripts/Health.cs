using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;

public class Health : NetworkBehaviour, IDamagable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Optional UI")]
    [SerializeField] private Text healthText;

    [Header("Optional Visuals Root")]
    [SerializeField] private GameObject visualsRoot;

    [Header("Respawn Invincibility")]
    [SerializeField] private float invincibilityDuration = 1.5f;
    private bool isInvincible = false;
    public bool IsInvincible => isInvincible;

    public event System.Action<Health> OnDeath;

    private bool isDead = false;
    public bool IsAlive => !isDead;

    public float MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth.Value = maxHealth;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[Health] OnNetworkSpawn → OwnerClientId: {OwnerClientId}, currentHealth: {currentHealth.Value}");

        currentHealth.OnValueChanged += OnHealthChanged;
        OnHealthChanged(0f, currentHealth.Value); // force refresh to current value
    }

    private void OnDestroy()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;

        Debug.Log($"[Health] TakeDamage({damage}) called → isDead: {isDead}, isInvincible: {isInvincible}, currentHealth BEFORE: {currentHealth.Value}");

        if (isDead || isInvincible) return;

        currentHealth.Value -= damage;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0f, maxHealth);

        Debug.Log($"[Health] TakeDamage → currentHealth AFTER: {currentHealth.Value}");

        if (currentHealth.Value <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[Health] Tank {OwnerClientId} died!");

        if (visualsRoot != null)
            visualsRoot.SetActive(false);
        else
            gameObject.SetActive(false);

        OnDeath?.Invoke(this);

        UpdateHealthUI();
    }

    public void ResetHealth()
    {
        currentHealth.Value = maxHealth;
        isDead = false;

        if (visualsRoot != null)
            visualsRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        Debug.Log($"[Health] ResetHealth called → currentHealth reset to: {currentHealth.Value}");

        UpdateHealthUI();

        // Start invincibility window
        StartCoroutine(InvincibilityCoroutine());
    }

    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        Debug.Log($"[Health] Invincibility started for {invincibilityDuration} seconds.");
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
        Debug.Log("[Health] Invincibility ended.");
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        Debug.Log($"[Health] OnHealthChanged → OwnerClientId: {OwnerClientId}, old: {oldValue}, new: {newValue}, isDead: {isDead}, IsOwner: {IsOwner}");
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        string healthPart = isDead ? "DEAD" : $"{currentHealth.Value}/{maxHealth}";

        if (healthText != null)
        {
            Debug.Log($"[Health] UpdateHealthUI → updating healthText to: {healthPart}");
            healthText.text = healthPart;
        }
        else
        {
            Debug.LogWarning($"[Health] UpdateHealthUI → healthText is null! Intended text would be: {healthPart}");
        }
    }

    public void SetHealthText(Text text)
    {
        healthText = text;
        Debug.Log($"[Health] SetHealthText called → assigned to: {healthText?.gameObject.name ?? "NULL"}");
        UpdateHealthUI();
    }
}
