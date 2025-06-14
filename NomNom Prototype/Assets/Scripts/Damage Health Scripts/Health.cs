using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;

public class Health : NetworkBehaviour, IDamagable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Optional UI")]
    [SerializeField] private Text healthText;

    [Header("Optional Visuals Root")]
    [SerializeField] private GameObject visualsRoot;

    [Header("Respawn Invincibility")]
    [SerializeField] private float invincibilityDuration = 1.5f;
    private NetworkVariable<bool> isInvincible = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public bool IsInvincible => isInvincible.Value;
    public float InvincibilityDuration => invincibilityDuration;

    [Header("Invincibility Visuals")]
    [SerializeField] private Renderer visualsRenderer;
    [SerializeField] private Color invincibleColor = Color.cyan;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lowHealthPulseColor = Color.red;

    private Material cachedMaterial;

    public event System.Action<Health> OnDeath;

    private bool isDead = false;
    public bool IsAlive => !isDead;
    public float MaxHealth => maxHealth;

    private NetworkTankController cachedTankController;

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

        if (IsServer)
        {
            cachedTankController = GetComponent<NetworkTankController>();
        }

        OnHealthChanged(0f, currentHealth.Value);

        Debug.Log($"[Health] OnNetworkSpawn → isInvincible initial value = {isInvincible.Value}, OwnerClientId = {OwnerClientId}");
    }

    private void OnDestroy()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        isInvincible.OnValueChanged -= OnInvincibleChanged;
    }

    private void Update()
    {
        if (cachedMaterial != null)
        {
            if (IsInvincible)
            {
                float pulse = Mathf.PingPong(Time.time * 4f, 0.5f) + 0.5f;
                Color pulseColor = invincibleColor * pulse;
                pulseColor.a = 1f;

                cachedMaterial.color = pulseColor;
                cachedMaterial.SetColor("_EmissionColor", invincibleColor * pulse * 2f);
            }
            else
            {
                float healthPercent = currentHealth.Value / maxHealth;
                if (healthPercent < 0.3f)
                {
                    float pulse = Mathf.PingPong(Time.time * 4f, 0.5f) + 0.5f;
                    Color pulseColor = lowHealthPulseColor * pulse;
                    pulseColor.a = 1f;

                    cachedMaterial.color = pulseColor;
                    cachedMaterial.SetColor("_EmissionColor", lowHealthPulseColor * pulse * 2f);
                }
                else
                {
                    cachedMaterial.color = normalColor;
                    cachedMaterial.SetColor("_EmissionColor", Color.black);
                }
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer)
        {
            Debug.LogWarning($"[Health] TakeDamage ignored → not server! OwnerClientId={OwnerClientId}");
            return;
        }

        if (isDead || isInvincible.Value) return;

        Debug.Log($"[Health] TakeDamage({damage}) on tank {gameObject.name}, currentHealth={currentHealth.Value}");

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

        Debug.Log($"[Health] Die() called for tank {gameObject.name}, OwnerClientId={OwnerClientId}");

        OnDeath?.Invoke(this);

        if (visualsRoot != null)
            visualsRoot.SetActive(false);
        else
            gameObject.SetActive(false);

        // Trigger respawn countdown on client
        if (IsServer && cachedTankController != null)
        {
            float delay = FindObjectOfType<RespawnManager>()?.RespawnDelay ?? 3f;
            cachedTankController.ShowRespawnCountdownClientRpc(delay);
        }
    }

    public void ResetHealth()
    {
        currentHealth.Value = maxHealth;
        isDead = false;

        if (visualsRoot != null)
            visualsRoot.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void ForceSetInvincible(bool value)
    {
        isInvincible.Value = value;
    }

    private void OnInvincibleChanged(bool oldValue, bool newValue)
    {
        // Visual feedback is handled in Update
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
