using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [Tooltip("Máu tối đa của Wukong.")]
    public int maxHealth = 1000;

    [Tooltip("Máu hiện tại của Wukong.")]
    public int currentHealth;

    [Header("UI")]
    [Tooltip("UI thanh máu của màn chơi.")]
    public MapHUDController hudController;

    [Header("Test Damage / Heal")]
    [Tooltip("Bật phím test mất máu và hồi máu 1_2_3.")]
    public bool enableTestKeys = true;

    [Tooltip("Sát thương test.")]
    public int testDamageAmount = 100;

    [Tooltip("Lượng máu hồi test.")]
    public int testHealAmount = 100;

    private PlayerController playerController;
    private bool isDead;
    private bool hasNotifiedBossDead;

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

        if (Keyboard.current == null)
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
            NotifyAllBossWukongDead();
            Die();
        }
    }

    void NotifyAllBossWukongDead()
    {
        if (hasNotifiedBossDead)
            return;

        hasNotifiedBossDead = true;

#if UNITY_2023_1_OR_NEWER
        Map4BossController[] bosses = FindObjectsByType<Map4BossController>(FindObjectsSortMode.None);
#else
        Map4BossController[] bosses = FindObjectsOfType<Map4BossController>();
#endif

        foreach (Map4BossController boss in bosses)
        {
            if (boss != null)
            {
                boss.NotifyWukongDead();
            }
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
        else
        {
            Debug.LogWarning("PlayerHealth chưa gán MapHUDController.");
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