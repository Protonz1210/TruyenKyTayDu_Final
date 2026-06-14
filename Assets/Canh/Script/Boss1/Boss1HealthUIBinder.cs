using UnityEngine;
using UnityEngine.UIElements;

public class Boss1HealthUIBinder : MonoBehaviour
{
    [Header("Boss")]
    [Tooltip("Controller của Boss1 Mãng Xà Tinh.")]
    public Boss1Controller boss1Controller;

    [Header("UI Document")]
    [Tooltip("UI Document chứa HUD của map.")]
    public UIDocument uiDocument;

    [Header("UI Element Names")]
    [Tooltip("Tên nhóm máu Boss1.")]
    public string bossGroupName = "boss-1-group";

    [Tooltip("Tên text tên Boss1.")]
    public string bossNameTextName = "boss-1-name";

    [Tooltip("Tên mask chứa thanh máu Boss1.")]
    public string bossHealthMaskName = "boss-1-health-mask";

    [Tooltip("Tên fill máu Boss1.")]
    public string bossHealthFillName = "boss-1-health-fill";

    [Tooltip("Tên text máu Boss1.")]
    public string bossHealthTextName = "boss-1-health-text";

    [Header("Display")]
    [Tooltip("Tên hiển thị của Boss1.")]
    public string bossDisplayName = "MÃNG XÀ TINH";

    [Tooltip("Ẩn UI Boss1 khi Boss chết.")]
    public bool hideWhenDead = false;

    [Header("Direction")]
    [Tooltip("Bật để máu tụt từ trái qua phải, phần đỏ bám mép phải giống Boss2.")]
    public bool drainFromLeftToRight = true;

    [Header("Auto Find")]
    [Tooltip("Tự tìm Boss1Controller nếu chưa kéo.")]
    public bool autoFindBossController = true;

    [Tooltip("Tự tìm UIDocument nếu chưa kéo.")]
    public bool autoFindUIDocument = true;

    [Header("Debug")]
    [Tooltip("Bật log debug.")]
    public bool enableDebugLog = true;

    private VisualElement root;
    private VisualElement bossGroup;
    private VisualElement bossHealthMask;
    private VisualElement bossHealthFill;
    private Label bossNameText;
    private Label bossHealthText;

    private int lastCurrentHealth = -999;
    private int lastMaxHealth = -999;

    void Awake()
    {
        FindReferencesIfNeeded();
        CacheUI();
        UpdateBossHealthUI(true);
    }

    void OnEnable()
    {
        FindReferencesIfNeeded();
        CacheUI();
        UpdateBossHealthUI(true);
    }

    void Start()
    {
        FindReferencesIfNeeded();
        CacheUI();
        UpdateBossHealthUI(true);
    }

    void Update()
    {
        FindReferencesIfNeeded();

        if (boss1Controller == null)
            return;

        if (boss1Controller.GetCurrentHealth() != lastCurrentHealth || boss1Controller.GetMaxHealth() != lastMaxHealth)
        {
            UpdateBossHealthUI(false);
        }

        if (hideWhenDead && bossGroup != null && boss1Controller.GetCurrentHealth() <= 0)
        {
            bossGroup.style.display = DisplayStyle.None;
        }
    }

    void FindReferencesIfNeeded()
    {
        if (autoFindBossController && boss1Controller == null)
        {
            boss1Controller = GetComponent<Boss1Controller>();
        }

        if (autoFindUIDocument && uiDocument == null)
        {
            UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);

            for (int i = 0; i < documents.Length; i++)
            {
                if (documents[i] == null)
                    continue;

                VisualElement docRoot = documents[i].rootVisualElement;

                if (docRoot == null)
                    continue;

                VisualElement foundFill = docRoot.Q<VisualElement>(bossHealthFillName);

                if (foundFill != null)
                {
                    uiDocument = documents[i];
                    break;
                }
            }

            if (uiDocument == null)
            {
                uiDocument = FindFirstObjectByType<UIDocument>();
            }
        }
    }

    void CacheUI()
    {
        if (uiDocument == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("Boss1HealthUIBinder chưa có UIDocument.");
            }

            return;
        }

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("Boss1HealthUIBinder không tìm thấy rootVisualElement.");
            }

            return;
        }

        bossGroup = root.Q<VisualElement>(bossGroupName);
        bossNameText = root.Q<Label>(bossNameTextName);
        bossHealthMask = root.Q<VisualElement>(bossHealthMaskName);
        bossHealthFill = root.Q<VisualElement>(bossHealthFillName);
        bossHealthText = root.Q<Label>(bossHealthTextName);

        if (bossNameText != null)
        {
            bossNameText.text = bossDisplayName;
        }

        SetupHealthFillDirection();

        if (enableDebugLog)
        {
            if (bossGroup == null)
            {
                Debug.LogWarning("Không tìm thấy Boss1 Group: " + bossGroupName);
            }

            if (bossHealthMask == null)
            {
                Debug.LogWarning("Không tìm thấy Boss1 Health Mask: " + bossHealthMaskName);
            }

            if (bossHealthFill == null)
            {
                Debug.LogWarning("Không tìm thấy Boss1 Health Fill: " + bossHealthFillName);
            }

            if (bossHealthText == null)
            {
                Debug.LogWarning("Không tìm thấy Boss1 Health Text: " + bossHealthTextName);
            }

            if (bossNameText == null)
            {
                Debug.LogWarning("Không tìm thấy Boss1 Name Text: " + bossNameTextName);
            }
        }
    }

    void SetupHealthFillDirection()
    {
        if (bossHealthMask != null)
        {
            bossHealthMask.style.overflow = Overflow.Hidden;
            bossHealthMask.style.position = Position.Relative;
        }

        if (bossHealthFill == null)
            return;

        bossHealthFill.style.position = Position.Absolute;
        bossHealthFill.style.top = 0;
        bossHealthFill.style.bottom = 0;
        bossHealthFill.style.height = Length.Percent(100f);

        if (drainFromLeftToRight)
        {
            bossHealthFill.style.right = 0;
            bossHealthFill.style.left = StyleKeyword.Auto;
        }
        else
        {
            bossHealthFill.style.left = 0;
            bossHealthFill.style.right = StyleKeyword.Auto;
        }
    }

    public void UpdateBossHealthUI(bool forceRecache)
    {
        if (forceRecache)
        {
            CacheUI();
        }

        if (boss1Controller == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("Boss1HealthUIBinder chưa có Boss1Controller.");
            }

            return;
        }

        int currentHealth = boss1Controller.GetCurrentHealth();
        int maxHealth = boss1Controller.GetMaxHealth();

        if (maxHealth <= 0)
        {
            maxHealth = 1;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        float percent = (float)currentHealth / maxHealth;

        if (bossGroup != null)
        {
            bossGroup.style.display = DisplayStyle.Flex;
        }

        SetupHealthFillDirection();

        if (bossHealthFill != null)
        {
            bossHealthFill.style.width = Length.Percent(percent * 100f);
        }

        if (bossHealthText != null)
        {
            bossHealthText.text = currentHealth + " / " + maxHealth;
        }

        if (bossNameText != null)
        {
            bossNameText.text = bossDisplayName;
        }

        lastCurrentHealth = currentHealth;
        lastMaxHealth = maxHealth;

        if (enableDebugLog)
        {
            Debug.Log("Boss1 UI cập nhật: " + currentHealth + "/" + maxHealth);
        }
    }
}