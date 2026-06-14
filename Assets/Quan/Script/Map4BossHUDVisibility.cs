using UnityEngine;
using UnityEngine.UIElements;

public class Map4BossHUDVisibility : MonoBehaviour
{
    [Header("References")]
    [Tooltip("UIDocument chứa Map4HUD.uxml.")]
    public UIDocument uiDocument;

    [Tooltip("Boss3 / Thanh Sư Tinh.")]
    public Map4BossController boss3;

    [Tooltip("Boss4 / Bạch Tượng Tinh.")]
    public Map4BossController boss4;

    [Header("UI Element")]
    [Tooltip("Tên element chứa toàn bộ UI máu Boss3/Boss4 trong UI Builder.")]
    public string bossHudElementName = "boss-panel";

    [Header("Display Rule")]
    [Tooltip("Bật nếu chỉ cần 1 trong 2 boss vào combat là hiện UI.")]
    public bool showWhenAnyBossCombat = true;

    [Header("Debug")]
    public bool enableDebugLog = false;

    private VisualElement bossHudElement;
    private bool lastVisibleState;

    void Awake()
    {
        SetupReferences();
        HideBossHUD();
    }

    void OnEnable()
    {
        SetupReferences();
        HideBossHUD();
    }

    void Update()
    {
        UpdateVisibility();
    }

    void SetupReferences()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            Debug.LogWarning("Map4BossHUDVisibility chưa gán UIDocument.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogWarning("Map4BossHUDVisibility không tìm thấy rootVisualElement.");
            return;
        }

        bossHudElement = root.Q<VisualElement>(bossHudElementName);

        if (bossHudElement == null)
        {
            Debug.LogWarning("Không tìm thấy boss HUD element: " + bossHudElementName);
        }
    }

    void UpdateVisibility()
    {
        if (bossHudElement == null)
        {
            SetupReferences();
        }

        if (bossHudElement == null) return;

        bool shouldShow = ShouldShowBossHUD();

        if (shouldShow == lastVisibleState) return;

        bossHudElement.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        lastVisibleState = shouldShow;

        if (enableDebugLog)
        {
            Debug.Log("Map4BossHUDVisibility: " + (shouldShow ? "Hiện boss HUD" : "Ẩn boss HUD"));
        }
    }

    bool ShouldShowBossHUD()
    {
        bool boss3CanShow = boss3 != null && boss3.CanShowBossUI();
        bool boss4CanShow = boss4 != null && boss4.CanShowBossUI();

        if (showWhenAnyBossCombat)
        {
            return boss3CanShow || boss4CanShow;
        }

        return boss3CanShow && boss4CanShow;
    }

    public void ShowBossHUD()
    {
        if (bossHudElement == null)
        {
            SetupReferences();
        }

        if (bossHudElement == null) return;

        bossHudElement.style.display = DisplayStyle.Flex;
        lastVisibleState = true;
    }

    public void HideBossHUD()
    {
        if (bossHudElement == null)
        {
            SetupReferences();
        }

        if (bossHudElement == null) return;

        bossHudElement.style.display = DisplayStyle.None;
        lastVisibleState = false;
    }
}