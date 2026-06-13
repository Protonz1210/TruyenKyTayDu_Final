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

    [Header("Boss5 Special Death Rule")]
    [Tooltip("Nếu có Boss5 trong scene, Wukong hết máu sẽ chuyển về Idle thay vì Die.")]
    public bool useIdleInsteadOfDeathWhenBoss5Exists = true;

    [Tooltip("Animator của Wukong. Nếu để trống sẽ tự lấy Animator trên object.")]
    public Animator playerAnimator;

    [Tooltip("Rigidbody2D của Wukong. Nếu để trống sẽ tự lấy Rigidbody2D trên object.")]
    public Rigidbody2D rb;

    [Tooltip("Tên state Idle thật trong Animator của Wukong.")]
    public string idleStateName = "Idle";

    [Tooltip("Khi Wukong hết máu trong màn có Boss5, có tắt PlayerController không.")]
    public bool disablePlayerControllerWhenBoss5IdleDeath = true;

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
        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<Animator>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

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
#if UNITY_2023_1_OR_NEWER
        Boss5Controller[] boss5List = FindObjectsByType<Boss5Controller>(FindObjectsSortMode.None);
#else
        Boss5Controller[] boss5List = FindObjectsOfType<Boss5Controller>();
#endif

        foreach (Boss5Controller boss5 in boss5List)
        {
            if (boss5 != null)
            {
                boss5.NotifyWukongDead();
            }
        }
#if UNITY_2023_1_OR_NEWER
Enemy123Controller[] enemy123List = FindObjectsByType<Enemy123Controller>(FindObjectsSortMode.None);
#else
        Enemy123Controller[] enemy123List = FindObjectsOfType<Enemy123Controller>();
#endif

        foreach (Enemy123Controller enemy123 in enemy123List)
        {
            if (enemy123 != null)
            {
                enemy123.NotifyWukongDead();
            }
        }
      
#if UNITY_2023_1_OR_NEWER
    Enemy4Controller[] enemy4List = FindObjectsByType<Enemy4Controller>(FindObjectsSortMode.None);
#else
            Enemy4Controller[] enemy4List = FindObjectsOfType<Enemy4Controller>();
#endif

            for (int i = 0; i < enemy4List.Length; i++)
            {
                if (enemy4List[i] != null)
                {
                    enemy4List[i].NotifyWukongDead();
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
        if (isDead) return;

        isDead = true;
        currentHealth = 0;
        UpdateHealthUI();

        if (ShouldUseBoss5IdleDeathMode())
        {
            Debug.Log("Wukong hết máu khi Boss5 xuất hiện: chuyển về Idle, không chạy animation chết.");
            ForceWukongIdleByBoss5Rule();
            return;
        }

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
    bool ShouldUseBoss5IdleDeathMode()
    {
        if (!useIdleInsteadOfDeathWhenBoss5Exists) return false;

#if UNITY_2023_1_OR_NEWER
    Boss5Controller[] boss5List = FindObjectsByType<Boss5Controller>(FindObjectsSortMode.None);
#else
        Boss5Controller[] boss5List = FindObjectsOfType<Boss5Controller>();
#endif

        for (int i = 0; i < boss5List.Length; i++)
        {
            Boss5Controller boss5 = boss5List[i];

            if (boss5 == null) continue;
            if (!boss5.gameObject.activeInHierarchy) continue;

            return true;
        }

        return false;
    }
    void ForceWukongIdleByBoss5Rule()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (playerController != null && disablePlayerControllerWhenBoss5IdleDeath)
        {
            playerController.enabled = false;
        }

        if (playerAnimator != null)
        {
            if (!string.IsNullOrEmpty(idleStateName))
            {
                playerAnimator.Play(idleStateName, 0, 0f);
                playerAnimator.Update(0f);
            }
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
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
    public int GetMaxHealth()
    {
        return maxHealth;
    }
}