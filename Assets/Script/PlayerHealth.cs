using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 1000;
    public int currentHealth;

    [Header("UI")]
    public MapHUDController hudController;

    [Header("Test Damage / Heal")]
    public bool enableTestKeys = true;
    public int testDamageAmount = 100;
    public int testHealAmount = 100;

    private PlayerController playerController;
    private bool isDead;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        currentHealth = maxHealth;
    }

    void Start()
    {
        UpdateHealthUI();
    }

    void Update()
    {
        if (!enableTestKeys)
            return;

        // Test trừ máu bằng phím 1
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            TakeDamage(testDamageAmount);
        }

        // Test hồi máu bằng phím 2
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            Heal(testHealAmount);
        }

        // Test chết ngay bằng phím 3
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            TakeDamage(maxHealth);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        if (damage <= 0)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthUI();

        Debug.Log("Ngộ Không mất máu: " + damage + " | Máu: " + currentHealth + " / " + maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead)
            return;

        if (amount <= 0)
            return;

        if (currentHealth >= maxHealth)
        {
            currentHealth = maxHealth;
            UpdateHealthUI();
            Debug.Log("Máu đã đầy, không thể hồi thêm.");
            return;
        }

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthUI();

        Debug.Log("Ngộ Không hồi máu: " + amount + " | Máu: " + currentHealth + " / " + maxHealth);
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;
        currentHealth = 0;

        UpdateHealthUI();

        Debug.Log("Ngộ Không đã bị hạ gục.");

        if (playerController != null)
        {
            playerController.Die();
        }
        else
        {
            Debug.LogWarning("Không tìm thấy PlayerController trên Ngộ Không.");
        }
    }

    void UpdateHealthUI()
    {
        if (hudController != null)
        {
            hudController.SetWukongHealth(currentHealth, maxHealth);
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    public float GetHealthPercent()
    {
        if (maxHealth <= 0)
            return 0f;

        return (float)currentHealth / maxHealth;
    }
}