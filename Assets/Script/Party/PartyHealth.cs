using UnityEngine;
using UnityEngine.InputSystem;

public class PartyHealth : MonoBehaviour
{
    [Header("Party Health")]
    [Tooltip("Máu tối đa của đoàn.")]
    public int maxHealth = 1000;

    [Tooltip("Máu hiện tại của đoàn.")]
    public int currentHealth;

    [Header("UI")]
    [Tooltip("UI thanh máu của màn chơi.")]
    public MapHUDController hudController;

    [Header("Test Damage / Heal")]
    [Tooltip("Bật phím test mất máu và hồi máu.")]
    public bool enableTestKeys = true;

    [Tooltip("Sát thương test.")]
    public int testDamageAmount = 100;

    [Tooltip("Lượng máu hồi test.")]
    public int testHealAmount = 100;

    [Header("Game Over")]
    [Tooltip("Bật lên để khi đoàn hết máu sẽ báo MapStory bật GameOver.")]
    public bool gameOverWhenDead = true;

    [Header("Death Notify - Auto Set By MapStory")]
    [Tooltip("Không chỉnh ở Inspector PartyHealth. MapStoryManager sẽ tự gán bằng SetDeathNotifyTarget().")]
    [HideInInspector]
    public GameObject deathNotifyObject;

    [Tooltip("Tên hàm sẽ gọi trên MapStoryManager khi máu đoàn về 0.")]
    [HideInInspector]
    public string deathNotifyMessageName = "NotifyPartyDead";

    private bool isDead;
    private bool hasNotifiedBossDead;
    private bool hasNotifiedMapStoryDead;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        UpdateHealthUI();
    }

    void Update()
    {
        if (!enableTestKeys)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            TakeDamage(testDamageAmount);
        }

        if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            Heal(testHealAmount);
        }

        if (Keyboard.current.digit6Key.wasPressedThisFrame)
        {
            TakeDamage(maxHealth);
        }
    }

    // ================= DEATH NOTIFY TARGET =================

    /// <summary>
    /// MapStoryManager sẽ gọi hàm này khi vào map.
    /// Không cần kéo MapStory vào Inspector của PartyHealth.
    /// </summary>
    public void SetDeathNotifyTarget(GameObject target)
    {
        deathNotifyObject = target;
    }

    /// <summary>
    /// Nếu map nào muốn đổi tên hàm notify thì có thể gọi hàm này.
    /// Bình thường giữ mặc định: NotifyPartyDead.
    /// </summary>
    public void SetDeathNotifyMessageName(string messageName)
    {
        if (string.IsNullOrEmpty(messageName))
        {
            return;
        }

        deathNotifyMessageName = messageName;
    }

    private void NotifyMapStoryPartyDead()
    {
        if (hasNotifiedMapStoryDead)
        {
            return;
        }

        hasNotifiedMapStoryDead = true;

        if (!gameOverWhenDead)
        {
            return;
        }

        if (deathNotifyObject == null)
        {
            Debug.LogWarning("PartyHealth: Đoàn đã hết máu nhưng chưa có Death Notify Object. MapStoryManager chưa gán target.");
            return;
        }

        if (string.IsNullOrEmpty(deathNotifyMessageName))
        {
            Debug.LogWarning("PartyHealth: Death Notify Message Name đang trống.");
            return;
        }

        deathNotifyObject.SendMessage(
            deathNotifyMessageName,
            SendMessageOptions.DontRequireReceiver
        );

        Debug.Log("PartyHealth: Đã báo MapStory rằng đoàn thỉnh kinh đã hết máu.");
    }

    // ================= HEALTH =================

    public void TakeDamage(int damage)
    {
        if (isDead)
        {
            return;
        }

        if (damage <= 0)
        {
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthUI();

        Debug.Log("Đoàn thỉnh kinh mất máu: " + damage + " | Máu: " + currentHealth + " / " + maxHealth);

        if (currentHealth <= 0)
        {
            OnPartyHealthZero();
        }
    }

    void OnPartyHealthZero()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        currentHealth = 0;

        UpdateHealthUI();

        // Báo boss/enemy biết đoàn đã chết để dừng đánh hoặc đổi trạng thái.
        NotifyAllBossPartyDead();

        Debug.Log("Đoàn thỉnh kinh đã hết máu.");

        // Báo MapStoryManager bật GameOver.
        NotifyMapStoryPartyDead();
    }

    void NotifyAllBossPartyDead()
    {
        if (hasNotifiedBossDead)
        {
            return;
        }

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
                boss.NotifyPartyDead();
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
                boss5.NotifyPartyDead();
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
                enemy123.NotifyPartyDead();
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
                enemy4List[i].NotifyPartyDead();
            }
        }

#if UNITY_2023_1_OR_NEWER
        Boss1Controller[] boss1List = FindObjectsByType<Boss1Controller>(FindObjectsSortMode.None);
#else
        Boss1Controller[] boss1List = FindObjectsOfType<Boss1Controller>();
#endif

        foreach (Boss1Controller boss1 in boss1List)
        {
            if (boss1 != null)
            {
                boss1.NotifyPartyDead();
            }
        }
    }

    public void Heal(int amount)
    {
        if (isDead)
        {
            return;
        }

        if (amount <= 0)
        {
            return;
        }

        if (currentHealth >= maxHealth)
        {
            currentHealth = maxHealth;
            UpdateHealthUI();
            Debug.Log("Máu đoàn đã đầy, không thể hồi thêm.");
            return;
        }

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthUI();

        Debug.Log("Đoàn thỉnh kinh hồi máu: " + amount + " | Máu: " + currentHealth + " / " + maxHealth);
    }

    public void HealFull()
    {
        if (isDead)
        {
            return;
        }

        currentHealth = maxHealth;
        UpdateHealthUI();

        Debug.Log("Đoàn thỉnh kinh đã hồi đầy máu.");
    }

    public void HealToFull()
    {
        HealFull();
    }

    public void RestoreFullHealth()
    {
        HealFull();
    }

    void UpdateHealthUI()
    {
        if (hudController != null)
        {
            hudController.SetPartyHealth(currentHealth, maxHealth);
        }
        else
        {
            Debug.LogWarning("PartyHealth chưa gán MapHUDController.");
        }
    }

    public void UpdateUI()
    {
        UpdateHealthUI();
    }

    public void RefreshUI()
    {
        UpdateHealthUI();
    }

    public void UpdateHealthUIFromExternal()
    {
        UpdateHealthUI();
    }

    public bool IsDead()
    {
        return isDead;
    }

    public float GetHealthPercent()
    {
        if (maxHealth <= 0)
        {
            return 0f;
        }

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