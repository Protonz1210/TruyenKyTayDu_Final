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
    [Tooltip("Game Over khi đoàn chết.")]
    public bool gameOverWhenDead = true;

    private bool isDead;


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
            return;

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

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        if (damage <= 0)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthUI();

        Debug.Log("Đoàn thỉnh kinh mất máu: " + damage + " | Máu: " + currentHealth + " / " + maxHealth);

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
            Debug.Log("Máu đoàn đã đầy, không thể hồi thêm.");
            return;
        }

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthUI();

        Debug.Log("Đoàn thỉnh kinh hồi máu: " + amount + " | Máu: " + currentHealth + " / " + maxHealth);
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;
        currentHealth = 0;

        UpdateHealthUI();

        Debug.Log("Đoàn thỉnh kinh đã bị hạ gục. Game Over.");

        if (gameOverWhenDead)
        {
            Debug.Log("GAME OVER: Máu đoàn thỉnh kinh về 0.");
        }
    }
  

    void UpdateHealthUI()
    {
        if (hudController != null)
        {
            hudController.SetPartyHealth(currentHealth, maxHealth);
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
