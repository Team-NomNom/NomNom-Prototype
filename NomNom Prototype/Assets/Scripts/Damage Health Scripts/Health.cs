using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;

public class Health : NetworkBehaviour, IDamagable
{
    #region Inspector
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Who last hit me (server authoritative)
    private NetworkVariable<ulong> lastDamagerId = new(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);


    [Header("Regeneration")]
    [Tooltip("Tick health regeneration?")]
    [SerializeField] private bool enableRegen = true;
    [Tooltip("Health points regenerated per second once the delay has passed")]
    [SerializeField] private float regenRate = 5f;
    [Tooltip("Seconds after last damage taken before regeneration starts")]
    [SerializeField] private float regenDelay = 3f;

    [Header("Regen Visuals")]
    [Tooltip("Optional particle system that plays while the tank is regenerating health.")]
    [SerializeField] private ParticleSystem regenEffect;

    [Header("Optional UI")]
    [SerializeField] private Text healthText;
    [SerializeField] private Image radialHealthImage;

    [Header("Optional Visuals Root")]
    [SerializeField] private GameObject visualsRoot;

    [Header("Respawn Invincibility")]
    [SerializeField] private float invincibilityDuration = 1.5f;
    private NetworkVariable<bool> isInvincible = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    public bool IsInvincible => isInvincible.Value;
    public float InvincibilityDuration => invincibilityDuration;

    [Header("Invincibility Visuals")]
    [SerializeField] private Renderer visualsRenderer;
    [SerializeField] private Color invincibleColor = Color.cyan;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lowHealthPulseColor = Color.red;
    #endregion

    #region Private state
    private Material cachedMaterial;
    private bool isDead = false;
    private float lastDamageTime = -Mathf.Infinity;
    private NetworkTankController cachedTankController;
    #endregion

    #region Public API & Events
    public bool IsAlive => !isDead;
    public float MaxHealth => maxHealth;
    public event System.Action<Health> OnDeath;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        currentHealth.Value = maxHealth;
        if (visualsRenderer != null)
            cachedMaterial = visualsRenderer.material;
        // Ensure regen particles are off at start
        regenEffect?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        currentHealth.OnValueChanged += OnHealthChanged;
        isInvincible.OnValueChanged += OnInvincibleChanged;

        if (IsServer)
            cachedTankController = GetComponent<NetworkTankController>();

        OnHealthChanged(0f, currentHealth.Value);
    }

    private void OnDestroy()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        isInvincible.OnValueChanged -= OnInvincibleChanged;
    }

    private void Update()
    {
        HandleVisuals();
        UpdateRegenEffect();
        ServerRegenerationTick();
    }
    #endregion

    #region Damage / Healing
    // Legacy call – forward attackerId=unknown
    public void TakeDamage(float dmg) => TakeDamage(dmg, ulong.MaxValue);

    public void TakeDamage(float dmg, ulong attackerId = ulong.MaxValue)
    {
        if (!IsServer || isDead || IsInvincible || dmg <= 0f) return;

        lastDamagerId.Value = attackerId;      // NEW
        lastDamageTime = Time.time;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value - dmg, 0f, maxHealth);

        if (currentHealth.Value <= 0f) Die();
    }


    public void Heal(float amount)
    {
        if (!IsServer || isDead || amount <= 0f) return;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value + amount, 0f, maxHealth);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (IsServer)
            GameManagerNew.Instance?.RegisterKill(lastDamagerId.Value, OwnerClientId);


        OnDeath?.Invoke(this);

        regenEffect?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (visualsRoot != null)
            visualsRoot.SetActive(false);
        else
            gameObject.SetActive(false);

        // Trigger respawn countdown on owner client
        if (IsServer && cachedTankController != null)
        {
            float delay = RespawnManagerNew.Instance?.RespawnDelay ?? 3f;
            cachedTankController.ShowRespawnCountdownClientRpc(delay);
            // RespawnManagerNew.Instance?.RespawnTank(gameObject, OwnerClientId);

        }
    }
    #endregion

    #region Regeneration
    private void ServerRegenerationTick()
    {
        if (!IsServer) return;
        if (!enableRegen) return;
        if (isDead || currentHealth.Value >= maxHealth) return;
        if (Time.time - lastDamageTime < regenDelay) return;

        float healAmount = regenRate * Time.deltaTime;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value + healAmount, 0f, maxHealth);
    }

    private bool IsRegenerating()
    {
        return enableRegen && !isDead && !IsInvincible && currentHealth.Value < maxHealth && (Time.time - lastDamageTime) >= regenDelay;
    }

    private void UpdateRegenEffect()
    {
        if (regenEffect == null) return;

        bool shouldPlay = IsRegenerating();
        if (shouldPlay && !regenEffect.isPlaying)
            regenEffect.Play();
        else if (!shouldPlay && regenEffect.isPlaying)
            regenEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
    #endregion

    #region Invulnerability helpers
    public void ForceSetInvincible(bool value)
    {
        if (!IsServer) return;
        isInvincible.Value = value;
    }

    private void OnInvincibleChanged(bool oldValue, bool newValue)
    {
        // visuals handled in Update
    }
    #endregion

    #region Visuals & UI
    private void HandleVisuals()
    {
        if (cachedMaterial == null) return;

        if (IsInvincible)
        {
            float pulse = Mathf.PingPong(Time.time * 4f, 0.5f) + 0.5f;
            Color pulseColor = invincibleColor * pulse; pulseColor.a = 1f;
            cachedMaterial.color = pulseColor;
            cachedMaterial.SetColor("_EmissionColor", invincibleColor * pulse * 2f);
        }
        else
        {
            float healthPercent = currentHealth.Value / maxHealth;
            if (healthPercent < 0.3f)
            {
                float pulse = Mathf.PingPong(Time.time * 4f, 0.5f) + 0.5f;
                Color pulseColor = lowHealthPulseColor * pulse; pulseColor.a = 1f;
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

    private void OnHealthChanged(float oldValue, float newValue)
    {
        if (healthText != null)
        {
            int cur = Mathf.RoundToInt(currentHealth.Value);
            int max = Mathf.RoundToInt(maxHealth);
            healthText.text = isDead ? "DEAD" : $"{cur}/{max}";
        }

        if (radialHealthImage != null)
        {
            radialHealthImage.fillAmount = currentHealth.Value / maxHealth;
        }
    }

    public void SetHealthText(Text text)
    {
        healthText = text;
        OnHealthChanged(currentHealth.Value, currentHealth.Value);
    }

    public void SetRadialImage(Image image)
    {
        radialHealthImage = image;
        OnHealthChanged(currentHealth.Value, currentHealth.Value);
    }
    #endregion

    #region Utility
    public void ResetHealth()
    {
        currentHealth.Value = maxHealth;
        isDead = false;
        lastDamageTime = -Mathf.Infinity;

        if (visualsRoot != null)
            visualsRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        // Clear & stop regen effect at spawn
        regenEffect?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
    #endregion
}
