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
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        BindUI();
    }

    void OnEnable()
    {
        BindUI();
        RefreshCachedHealth();
    }

    void Start()
    {
        RefreshCachedHealth();
    }

    void BindUI()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("Map4BossHUDController chưa được gán UI Document.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        boss4HealthFill = root.Q<VisualElement>(boss4HealthFillName);
        boss4HealthText = root.Q<Label>(boss4HealthTextName);

        boss5HealthFill = root.Q<VisualElement>(boss5HealthFillName);
        boss5HealthText = root.Q<Label>(boss5HealthTextName);

        if (boss4HealthFill == null)
        {
            Debug.LogWarning("Không tìm thấy Boss4 Health Fill: " + boss4HealthFillName);
        }
        else
        {
            PrepareFillElement(boss4HealthFill, boss4AnchorRight);
        }

        if (boss4HealthText == null)
        {
            Debug.LogWarning("Không tìm thấy Boss4 Health Text: " + boss4HealthTextName);
        }

        if (boss5HealthFill == null)
        {
            Debug.LogWarning("Không tìm thấy Boss5 Health Fill: " + boss5HealthFillName);
        }
        else
        {
            PrepareFillElement(boss5HealthFill, boss5AnchorRight);
        }

        if (boss5HealthText == null)
        {
            Debug.LogWarning("Không tìm thấy Boss5 Health Text: " + boss5HealthTextName);
        }
    }

    void PrepareFillElement(VisualElement fillElement, bool anchorRight)
    {
        if (fillElement == null)
            return;

        VisualElement parent = fillElement.parent;

        if (parent != null)
        {
            parent.style.position = Position.Relative;
            parent.style.overflow = Overflow.Hidden;
        }

        fillElement.style.position = Position.Absolute;
        fillElement.style.top = 0;
        fillElement.style.bottom = 0;
        fillElement.style.height = Length.Percent(100);

        if (anchorRight)
        {
            fillElement.style.right = 0;
            fillElement.style.left = StyleKeyword.Auto;
        }
        else
        {
            fillElement.style.left = 0;
            fillElement.style.right = StyleKeyword.Auto;
        }
    }

    public void SetBoss4Health(int currentHealth, int maxHealth)
    {
        cachedBoss4CurrentHealth = currentHealth;
        cachedBoss4MaxHealth = maxHealth;

        SetHealth(
            currentHealth,
            maxHealth,
            boss4HealthFill,
            boss4HealthText,
            boss4AnchorRight,
            "Boss4"
        );
    }

    public void SetBoss5Health(int currentHealth, int maxHealth)
    {
        cachedBoss5CurrentHealth = currentHealth;
        cachedBoss5MaxHealth = maxHealth;

        SetHealth(
            currentHealth,
            maxHealth,
            boss5HealthFill,
            boss5HealthText,
            boss5AnchorRight,
            "Boss5"
        );
    }

    void SetHealth(
        int currentHealth,
        int maxHealth,
        VisualElement fillElement,
        Label textElement,
        bool anchorRight,
        string debugName
    )
    {
        if (maxHealth <= 0)
        {
            maxHealth = 1;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        float percent = (float)currentHealth / maxHealth;
        percent = Mathf.Clamp01(percent);

        if (textElement != null)
        {
            textElement.text = currentHealth + " / " + maxHealth;
        }

        if (fillElement == null)
        {
            Debug.LogWarning(debugName + " thiếu fill element.");
            return;
        }

        PrepareFillElement(fillElement, anchorRight);

        fillElement.style.width = Length.Percent(percent * 100f);

        Debug.Log(
            debugName +
            " UI máu: " +
            currentHealth +
            " / " +
            maxHealth +
            " | Percent: " +
            percent +
            " | AnchorRight: " +
            anchorRight
        );
    }

    void RefreshCachedHealth()
    {
        RefreshBoss4CachedHealth();
        RefreshBoss5CachedHealth();
    }

    void RefreshBoss4CachedHealth()
    {
        if (cachedBoss4MaxHealth <= 0)
            return;

        SetBoss4Health(cachedBoss4CurrentHealth, cachedBoss4MaxHealth);
    }

    void RefreshBoss5CachedHealth()
    {
        if (cachedBoss5MaxHealth <= 0)
            return;

        SetBoss5Health(cachedBoss5CurrentHealth, cachedBoss5MaxHealth);
    }

    public void Show()
    {
        if (uiDocument != null)
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        }
    }

    public void Hide()
    {
        if (uiDocument != null)
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }
}