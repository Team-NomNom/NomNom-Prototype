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
    private NetworkVariable<bool> isInvincible = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public bool IsInvincible => isInvincible.Value;
    public float InvincibilityDuration => invincibilityDuration;

    [Header("Invincibility Visuals")]
    [SerializeField] private Renderer visualsRenderer;
    [SerializeField] private Color invincibleColor = Color.cyan;
    [SerializeField] private Color normalColor = Color.white;

    [Header("Low Health Flicker")]
    [SerializeField] private float lowHealthThresholdPercent = 25f;
    [SerializeField] private Color lowHealthFlickerColor = Color.red;
    [SerializeField] private float flickerSpeed = 8f;

    private Coroutine lowHealthFlickerCoroutine;
    private bool isFlickering = false;

    private bool isInvincibleVisualActive = false;

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
        isInvincible.OnValueChanged += OnInvincibleChanged;

        OnHealthChanged(0f, currentHealth.Value);
    }

    private void OnDestroy()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        isInvincible.OnValueChanged -= OnInvincibleChanged;
    }

    private void Update()
    {
        if (isInvincibleVisualActive && visualsRenderer != null)
        {
            float pulse = Mathf.PingPong(Time.time * 4f, 0.5f) + 0.5f;
            Color pulseColor = invincibleColor * pulse;
            pulseColor.a = 1f;

            visualsRenderer.material.color = pulseColor;
            visualsRenderer.material.SetColor("_EmissionColor", invincibleColor * pulse * 2f);
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;

        if (isDead)
        {
            Debug.Log($"[Health] TakeDamage({damage}) blocked → already dead.");
            return;
        }

        if (isInvincible.Value)
        {
            Debug.Log($"[Health] TakeDamage({damage}) blocked → invincible!");
            return;
        }

        Debug.Log($"[Health] TakeDamage({damage}) accepted → currentHealth BEFORE: {currentHealth.Value}");

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

        StartCoroutine(InvincibilityCoroutine());
    }

    private IEnumerator InvincibilityCoroutine()
    {
        SetInvincibleServerRpc(true);
        Debug.Log($"[Health] Invincibility started for {invincibilityDuration} seconds.");

        yield return new WaitForSeconds(invincibilityDuration);

        SetInvincibleServerRpc(false);
        Debug.Log("[Health] Invincibility ended.");
    }

    [ServerRpc]
    private void SetInvincibleServerRpc(bool value)
    {
        isInvincible.Value = value;
    }

    private void OnInvincibleChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[Health] OnInvincibleChanged → old: {oldValue}, new: {newValue}");

        isInvincibleVisualActive = newValue;

        if (!newValue)
        {
            if (visualsRenderer != null)
            {
                visualsRenderer.material.color = normalColor;
                visualsRenderer.material.SetColor("_EmissionColor", Color.black);
            }

            CheckLowHealthFlicker();
        }
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        Debug.Log($"[Health] OnHealthChanged → OwnerClientId: {OwnerClientId}, old: {oldValue}, new: {newValue}, isDead: {isDead}, IsOwner: {IsOwner}");
        UpdateHealthUI();
        CheckLowHealthFlicker();
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

    private void CheckLowHealthFlicker()
    {
        float healthPercent = (currentHealth.Value / maxHealth) * 100f;

        if (healthPercent <= lowHealthThresholdPercent && !isFlickering && IsAlive && !isInvincible.Value)
        {
            lowHealthFlickerCoroutine = StartCoroutine(LowHealthFlickerCoroutine());
        }
        else if ((healthPercent > lowHealthThresholdPercent || !IsAlive || isInvincible.Value) && isFlickering)
        {
            StopCoroutine(lowHealthFlickerCoroutine);
            isFlickering = false;

            if (visualsRenderer != null)
            {
                visualsRenderer.material.color = normalColor;
                visualsRenderer.material.SetColor("_EmissionColor", Color.black);
            }

            Debug.Log("[Health] Low health flicker stopped.");
        }
    }

    private IEnumerator LowHealthFlickerCoroutine()
    {
        isFlickering = true;
        Debug.Log("[Health] Low health flicker started.");

        while (true)
        {
            float pulse = Mathf.PingPong(Time.time * flickerSpeed, 1f);

            if (visualsRenderer != null)
            {
                Color flickerColor = Color.Lerp(normalColor, lowHealthFlickerColor, pulse);
                flickerColor.a = 1f;

                visualsRenderer.material.color = flickerColor;
                visualsRenderer.material.SetColor("_EmissionColor", lowHealthFlickerColor * pulse * 2f);
            }

            yield return null;
        }
    }
}
