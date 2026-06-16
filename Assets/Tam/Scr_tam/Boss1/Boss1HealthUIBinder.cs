using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Binder UI máu Boss1 - Mãng Xà Tinh.
/// Script này đọc máu từ Boss1Controller và cập nhật lên Map2HUD.
/// Cách cập nhật thanh máu giống Map4BossHUDController:
/// - Text máu cập nhật theo current / max.
/// - Thanh đỏ boss1-health-fill đổi width theo phần trăm máu.
/// </summary>
public class Boss1HealthUIBinder : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Boss1Controller cần hiển thị máu.")]
    public Boss1Controller boss1Controller;

    [Tooltip("UIDocument đang dùng Map2HUD. Kéo object Map2HUD trong Hierarchy vào đây.")]
    public UIDocument uiDocument;

    [Header("UI Names")]
    [Tooltip("Group tổng của Boss1 UI.")]
    public string bossGroupName = "boss-1-group";

    [Tooltip("Tên Label hiển thị tên Boss1.")]
    public string bossNameTextName = "boss-1-name";

    [Tooltip("Tên thanh máu đỏ cần co giãn.")]
    public string bossHealthFillName = "boss1-health-fill";

    [Tooltip("Tên text hiển thị máu Boss1.")]
    public string bossHealthTextName = "boss-1-health-text";

    [Header("Display")]
    [Tooltip("Tên hiển thị của Boss1.")]
    public string bossDisplayName = "MÃNG XÀ TINH";

    [Tooltip("Thanh máu bám bên phải. Boss UI bên phải màn hình nên thường bật.")]
    public bool anchorRight = true;

    [Tooltip("Ẩn UI khi Boss chết.")]
    public bool hideWhenBossDead = false;

    [Header("Update")]
    [Tooltip("Tự cập nhật UI mỗi frame. Nên bật để chắc chắn thanh máu luôn đúng.")]
    public bool updateEveryFrame = true;

    [Header("Debug")]
    public bool enableDebugLog = false;

    private VisualElement bossGroup;
    private Label bossNameText;
    private VisualElement bossHealthFill;
    private Label bossHealthText;

    private int lastCurrentHealth = -1;
    private int lastMaxHealth = -1;
    private bool hasFoundUI;

    private void Awake()
    {
        AutoBindReferences();
        FindUIElements();
    }

    private void OnEnable()
    {
        AutoBindReferences();
        FindUIElements();
        ForceRefreshUI();
    }

    private void Start()
    {
        AutoBindReferences();
        FindUIElements();
        ForceRefreshUI();
    }

    private void Update()
    {
        if (!updateEveryFrame)
        {
            return;
        }

        RefreshUIIfNeeded();
    }

    private void AutoBindReferences()
    {
        if (boss1Controller == null)
        {
            boss1Controller = GetComponent<Boss1Controller>();
        }

        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            uiDocument = FindFirstObjectByType<UIDocument>();
        }
    }

    private void FindUIElements()
    {
        hasFoundUI = false;

        if (uiDocument == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("Boss1HealthUIBinder: Chưa có UIDocument.");
            }

            return;
        }

        if (uiDocument.rootVisualElement == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("Boss1HealthUIBinder: UIDocument chưa có rootVisualElement.");
            }

            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        bossGroup = root.Q<VisualElement>(bossGroupName);
        bossNameText = root.Q<Label>(bossNameTextName);
        bossHealthFill = root.Q<VisualElement>(bossHealthFillName);
        bossHealthText = root.Q<Label>(bossHealthTextName);

        if (bossNameText != null)
        {
            bossNameText.text = bossDisplayName;
        }

        if (bossGroup != null)
        {
            bossGroup.style.display = DisplayStyle.Flex;
        }

        if (bossHealthFill != null)
        {
            // Rất quan trọng:
            // Không để flexGrow tự kéo đầy thanh, vì như vậy width percent sẽ khó thấy thay đổi.
            bossHealthFill.style.flexGrow = 0;
            bossHealthFill.style.flexShrink = 0;
        }

        hasFoundUI = bossHealthFill != null || bossHealthText != null;

        if (enableDebugLog)
        {
            Debug.Log(
                "Boss1HealthUIBinder FindUIElements | Group: " + (bossGroup != null) +
                " | Name: " + (bossNameText != null) +
                " | Fill: " + (bossHealthFill != null) +
                " | Text: " + (bossHealthText != null)
            );
        }
    }

    private void RefreshUIIfNeeded()
    {
        if (boss1Controller == null)
        {
            return;
        }

        if (!hasFoundUI || bossHealthFill == null || bossHealthText == null)
        {
            FindUIElements();
        }

        int currentHealth = boss1Controller.GetCurrentHealth();
        int maxHealth = boss1Controller.GetMaxHealth();

        if (currentHealth == lastCurrentHealth && maxHealth == lastMaxHealth)
        {
            return;
        }

        SetBoss1Health(currentHealth, maxHealth);
    }

    public void ForceRefreshUI()
    {
        if (boss1Controller == null)
        {
            return;
        }

        int currentHealth = boss1Controller.GetCurrentHealth();
        int maxHealth = boss1Controller.GetMaxHealth();

        SetBoss1Health(currentHealth, maxHealth);
    }

    public void SetBoss1Health(int currentHealth, int maxHealth)
    {
        lastCurrentHealth = currentHealth;
        lastMaxHealth = maxHealth;

        UpdateBossHealthUI(currentHealth, maxHealth);
    }

    private void UpdateBossHealthUI(int currentHealth, int maxHealth)
    {
        if (maxHealth <= 0)
        {
            maxHealth = 1;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        float healthPercent = (float)currentHealth / maxHealth;
        float widthPercent = Mathf.Clamp01(healthPercent) * 100f;

        if (bossGroup != null)
        {
            if (hideWhenBossDead && currentHealth <= 0)
            {
                bossGroup.style.display = DisplayStyle.None;
            }
            else
            {
                bossGroup.style.display = DisplayStyle.Flex;
            }
        }

        if (bossHealthFill != null)
        {
            // Đây là phần fix chính:
            // Cập nhật trực tiếp vào boss1-health-fill giống Map4BossHUDController.
            bossHealthFill.style.width = Length.Percent(widthPercent);

            if (anchorRight)
            {
                bossHealthFill.style.marginLeft = StyleKeyword.Auto;
                bossHealthFill.style.marginRight = 0;
            }
            else
            {
                bossHealthFill.style.marginLeft = 0;
                bossHealthFill.style.marginRight = StyleKeyword.Auto;
            }
        }

        if (bossHealthText != null)
        {
            bossHealthText.text = currentHealth + " / " + maxHealth;
        }

        if (enableDebugLog)
        {
            Debug.Log("Boss1HealthUIBinder: UI cập nhật " + currentHealth + " / " + maxHealth + " | Fill = " + widthPercent + "%");
        }
    }
}