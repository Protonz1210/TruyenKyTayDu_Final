using UnityEngine;
using UnityEngine.UIElements;

public class Map4BossHUDController : MonoBehaviour
{
    [Header("UI Document")]
    [Tooltip("UI Document của HUD Boss Map 4.")]
    public UIDocument uiDocument;

    [Header("Boss3 UI Names")]
    [Tooltip("Tên thanh máu Boss3.")]
    public string boss3HealthFillName = "boss3-health-fill";

    [Tooltip("Tên text máu Boss3.")]
    public string boss3HealthTextName = "boss3-health-text";

    [Tooltip("Thanh máu Boss3 bám bên phải.")]
    public bool boss3AnchorRight = true;

    [Header("Boss4 UI Names")]
    [Tooltip("Tên thanh máu Boss4.")]
    public string boss4HealthFillName = "boss4-health-fill";

    [Tooltip("Tên text máu Boss4.")]
    public string boss4HealthTextName = "boss4-health-text";

    [Tooltip("Thanh máu Boss4 bám bên phải.")]
    public bool boss4AnchorRight = true;


    private VisualElement boss3HealthFill;
    private Label boss3HealthText;

    private VisualElement boss4HealthFill;
    private Label boss4HealthText;

    private int cachedBoss3CurrentHealth = -1;
    private int cachedBoss3MaxHealth = -1;

    private int cachedBoss4CurrentHealth = -1;
    private int cachedBoss4MaxHealth = -1;

    void Awake()
    {
        FindUIElements();
    }

    void OnEnable()
    {
        FindUIElements();
        RefreshCachedHealth();
    }

    void Start()
    {
        FindUIElements();
        RefreshCachedHealth();
    }

    void FindUIElements()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null) return;
        if (uiDocument.rootVisualElement == null) return;

        VisualElement root = uiDocument.rootVisualElement;

        boss3HealthFill = root.Q<VisualElement>(boss3HealthFillName);
        boss3HealthText = root.Q<Label>(boss3HealthTextName);

        boss4HealthFill = root.Q<VisualElement>(boss4HealthFillName);
        boss4HealthText = root.Q<Label>(boss4HealthTextName);
    }

    void RefreshCachedHealth()
    {
        if (cachedBoss3CurrentHealth >= 0 && cachedBoss3MaxHealth > 0)
        {
            UpdateBossHealthUI(
                boss3HealthFill,
                boss3HealthText,
                cachedBoss3CurrentHealth,
                cachedBoss3MaxHealth,
                boss3AnchorRight
            );
        }

        if (cachedBoss4CurrentHealth >= 0 && cachedBoss4MaxHealth > 0)
        {
            UpdateBossHealthUI(
                boss4HealthFill,
                boss4HealthText,
                cachedBoss4CurrentHealth,
                cachedBoss4MaxHealth,
                boss4AnchorRight
            );
        }
    }

    public void SetBoss3Health(int currentHealth, int maxHealth)
    {
        cachedBoss3CurrentHealth = currentHealth;
        cachedBoss3MaxHealth = maxHealth;

        UpdateBossHealthUI(
            boss3HealthFill,
            boss3HealthText,
            currentHealth,
            maxHealth,
            boss3AnchorRight
        );
    }

    public void SetBoss4Health(int currentHealth, int maxHealth)
    {
        cachedBoss4CurrentHealth = currentHealth;
        cachedBoss4MaxHealth = maxHealth;

        UpdateBossHealthUI(
            boss4HealthFill,
            boss4HealthText,
            currentHealth,
            maxHealth,
            boss4AnchorRight
        );
    }

    public void SetBossHealth(int bossId, int currentHealth, int maxHealth)
    {
        if (bossId == 3)
        {
            SetBoss3Health(currentHealth, maxHealth);
        }
        else if (bossId == 4)
        {
            SetBoss4Health(currentHealth, maxHealth);
        }
    }

    void UpdateBossHealthUI(
        VisualElement healthFill,
        Label healthText,
        int currentHealth,
        int maxHealth,
        bool anchorRight
    )
    {
        if (maxHealth <= 0)
        {
            maxHealth = 1;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        float healthPercent = (float)currentHealth / maxHealth;
        float widthPercent = Mathf.Clamp01(healthPercent) * 100f;

        if (healthFill != null)
        {
            healthFill.style.width = Length.Percent(widthPercent);

            if (anchorRight)
            {
                healthFill.style.marginLeft = StyleKeyword.Auto;
                healthFill.style.marginRight = 0;
            }
            else
            {
                healthFill.style.marginLeft = 0;
                healthFill.style.marginRight = StyleKeyword.Auto;
            }
        }

        if (healthText != null)
        {
            healthText.text = currentHealth + " / " + maxHealth;
        }
    }
}