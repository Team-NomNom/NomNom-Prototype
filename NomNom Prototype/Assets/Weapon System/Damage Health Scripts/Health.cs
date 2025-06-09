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

    private Material cachedMaterial;

    public event System.Action<Health> OnDeath;

    private bool isDead = false;
    public bool IsAlive => !isDead;

    public float MaxHealth => maxHealth;

    private bool isInvincibleVisualActive = false;

    private void Awake()
    {
        currentHealth.Value = maxHealth;

        if (visualsRenderer != null)
        {
            cachedMaterial = visualsRenderer.material;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

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
        if (isInvincibleVisualActive && cachedMaterial != null)
        {
            float pulse = Mathf.PingPong(Time.time * 4f, 0.5f) + 0.5f;
            Color pulseColor = invincibleColor * pulse;
            pulseColor.a = 1f;

            cachedMaterial.color = pulseColor;
            cachedMaterial.SetColor("_EmissionColor", invincibleColor * pulse * 2f);
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;

        if (isDead) return;
        if (isInvincible.Value) return;

        currentHealth.Value -= damage;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0f, maxHealth);

        if (currentHealth.Value <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        OnDeath?.Invoke(this);

        if (visualsRoot != null)
            visualsRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void ResetHealth()
    {
        currentHealth.Value = maxHealth;
        isDead = false;

        if (visualsRoot != null)
            visualsRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        // Invincibility now handled by RespawnManager → correct architecture!
    }

    [ServerRpc]
    public void StartInvincibilityServerRpc()
    {
        StartCoroutine(InvincibilityCoroutine());
    }

    private IEnumerator InvincibilityCoroutine()
    {
        SetInvincibleServerRpc(true);

        yield return new WaitForSeconds(invincibilityDuration);

        SetInvincibleServerRpc(false);
    }

    [ServerRpc]
    private void SetInvincibleServerRpc(bool value)
    {
        isInvincible.Value = value;
    }

    private void OnInvincibleChanged(bool oldValue, bool newValue)
    {
        isInvincibleVisualActive = newValue;

        if (!newValue && cachedMaterial != null)
        {
            cachedMaterial.color = normalColor;
            cachedMaterial.SetColor("_EmissionColor", Color.black);
        }
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        if (healthText != null)
        {
            healthText.text = isDead ? "DEAD" : $"{currentHealth.Value}/{maxHealth}";
        }
    }

    public void SetHealthText(Text text)
    {
        healthText = text;
    }
}
