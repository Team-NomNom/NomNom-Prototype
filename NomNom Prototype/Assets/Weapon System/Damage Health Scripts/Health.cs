using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class Health : NetworkBehaviour, IDamagable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Optional UI")]
    [SerializeField] private Text healthText;

    [Header("Optional Visuals Root")]
    [SerializeField] private GameObject visualsRoot;

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
        if (isDead) return;

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

        Debug.Log($"Tank {OwnerClientId} died!");

        if (visualsRoot != null)
            visualsRoot.SetActive(false);
        else
            gameObject.SetActive(false);

        OnDeath?.Invoke(this);

        // Force UI refresh to show "DEAD"
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

        // Force UI refresh on respawn
        UpdateHealthUI();
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        Debug.Log($"[Health] OnHealthChanged → OwnerClientId: {OwnerClientId}, old: {oldValue}, new: {newValue}, isDead: {isDead}, IsOwner: {IsOwner}");
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (healthText != null)
        {
            if (isDead)
                healthText.text = "DEAD";
            else
                healthText.text = $"{currentHealth.Value}/{maxHealth}";
        }
    }

    // Allows assigning health text from scene / NetworkTankController
    public void SetHealthText(Text text)
    {
        healthText = text;
        UpdateHealthUI(); // refresh immediately
    }
}
