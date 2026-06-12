using UnityEngine;
using UnityEngine.UIElements;

public class Map4BossHUDController : MonoBehaviour
{
    [Header("UI Document")]
    [Tooltip("UI Document của HUD Boss Map 4.")]
    public UIDocument uiDocument;

    [Header("Boss4 UI Names")]
    [Tooltip("Tên thanh máu Boss4.")]
    public string boss4HealthFillName = "boss4-health-fill";

    [Tooltip("Tên text máu Boss4.")]
    public string boss4HealthTextName = "boss4-health-text";

    [Tooltip("Thanh máu Boss4 bám bên phải.")]
    public bool boss4AnchorRight = true;

    [Header("Boss5 UI Names")]
    [Tooltip("Tên thanh máu Boss5.")]
    public string boss5HealthFillName = "boss5-health-fill";

    [Tooltip("Tên text máu Boss5.")]
    public string boss5HealthTextName = "boss5-health-text";

    [Tooltip("Thanh máu Boss5 bám bên phải.")]
    public bool boss5AnchorRight = true;

    private VisualElement boss4HealthFill;
    private Label boss4HealthText;

    private VisualElement boss5HealthFill;
    private Label boss5HealthText;

    private int cachedBoss4CurrentHealth = -1;
    private int cachedBoss4MaxHealth = -1;

    private int cachedBoss5CurrentHealth = -1;
    private int cachedBoss5MaxHealth = -1;

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

        boss4HealthFill = root.Q<VisualElement>(boss4HealthFillName);
        boss4HealthText = root.Q<Label>(boss4HealthTextName);

        boss5HealthFill = root.Q<VisualElement>(boss5HealthFillName);
        boss5HealthText = root.Q<Label>(boss5HealthTextName);
    }

    void RefreshCachedHealth()
    {
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

        if (cachedBoss5CurrentHealth >= 0 && cachedBoss5MaxHealth > 0)
        {
            UpdateBossHealthUI(
                boss5HealthFill,
                boss5HealthText,
                cachedBoss5CurrentHealth,
                cachedBoss5MaxHealth,
                boss5AnchorRight
            );
        }
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

    public void SetBoss5Health(int currentHealth, int maxHealth)
    {
        cachedBoss5CurrentHealth = currentHealth;
        cachedBoss5MaxHealth = maxHealth;

        UpdateBossHealthUI(
            boss5HealthFill,
            boss5HealthText,
            currentHealth,
            maxHealth,
            boss5AnchorRight
        );
    }

    public void SetBossHealth(int bossId, int currentHealth, int maxHealth)
    {
        if (bossId == 4)
        {
            SetBoss4Health(currentHealth, maxHealth);
        }
        else
        {
            SetBoss5Health(currentHealth, maxHealth);
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
        float widthPercent = healthPercent * 100f;

        if (healthFill != null)
        {
            healthFill.style.width = Length.Percent(widthPercent);

            if (anchorRight)
            {
                healthFill.style.alignSelf = Align.FlexEnd;
            }
            else
            {
                healthFill.style.alignSelf = Align.FlexStart;
            }
        }

        if (healthText != null)
        {
            healthText.text = currentHealth + " / " + maxHealth;
        }
    }
}