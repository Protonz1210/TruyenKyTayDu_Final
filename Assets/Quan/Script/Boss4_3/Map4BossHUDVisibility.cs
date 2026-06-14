using UnityEngine;
using UnityEngine.UIElements;

public class Map4BossHUDVisibility : MonoBehaviour
{
    [Header("References")]
    [Tooltip("UIDocument chứa Map4BossHUD.")]
    public UIDocument uiDocument;

    [Tooltip("Map4StoryManager để đọc phase hiện tại.")]
    public Map4StoryManager storyManager;

    [Header("UI Element Name")]
    [Tooltip("Tên VisualElement chứa UI máu boss trong UI Builder.")]
    public string bossHudElementName = "Map4BossHUD";

    [Header("Display Rule")]
    [Tooltip("Hiện UI boss khi vào BossFight.")]
    public bool showInBossFight = true;

    [Tooltip("Tiếp tục hiện UI boss khi Boss5 xuất hiện.")]
    public bool showInBoss5Appear = true;

    [Tooltip("Ẩn UI boss khi vào đoạn hội thoại Boss5.")]
    public bool hideInBoss5StoryDialogue = false;

    [Tooltip("Ẩn UI boss khi Wukong transition / kết thúc map.")]
    public bool hideWhenMapEnding = true;

    [Header("Debug")]
    public bool enableDebugLog = false;

    private VisualElement root;
    private VisualElement bossHudElement;
    private bool lastVisibleState;

    void Awake()
    {
        SetupReferences();
        UpdateHUDVisibility(true);
    }

    void OnEnable()
    {
        SetupReferences();
        UpdateHUDVisibility(true);
    }

    void Update()
    {
        UpdateHUDVisibility(false);
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

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogWarning("Map4BossHUDVisibility không tìm thấy rootVisualElement.");
            return;
        }

        bossHudElement = root.Q<VisualElement>(bossHudElementName);

        if (bossHudElement == null)
        {
            Debug.LogWarning("Không tìm thấy UI boss HUD: " + bossHudElementName + ". Kiểm tra Name trong UI Builder.");
        }
    }

    void UpdateHUDVisibility(bool forceUpdate)
    {
        if (bossHudElement == null)
        {
            SetupReferences();
        }

        if (bossHudElement == null) return;

        bool shouldShow = ShouldShowBossHUD();

        if (!forceUpdate && shouldShow == lastVisibleState) return;

        bossHudElement.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        lastVisibleState = shouldShow;

        if (enableDebugLog)
        {
            Debug.Log("Map4BossHUDVisibility: " + (shouldShow ? "Hiện" : "Ẩn") + " Map4BossHUD");
        }
    }

    bool ShouldShowBossHUD()
    {
        if (storyManager == null)
        {
            return false;
        }

        switch (storyManager.currentPhase)
        {
            case Map4StoryManager.Map4Phase.BossFight:
                return showInBossFight;

            case Map4StoryManager.Map4Phase.Boss5Appear:
                return showInBoss5Appear;

            case Map4StoryManager.Map4Phase.Boss5StoryDialogue:
                return !hideInBoss5StoryDialogue;

            case Map4StoryManager.Map4Phase.WukongTransform:
                return !hideWhenMapEnding;

            case Map4StoryManager.Map4Phase.EndMap:
                return !hideWhenMapEnding;

            default:
                return false;
        }
    }

    public void ShowBossHUD()
    {
        if (bossHudElement == null)
        {
            SetupReferences();
        }

        if (bossHudElement != null)
        {
            bossHudElement.style.display = DisplayStyle.Flex;
            lastVisibleState = true;
        }
    }

    public void HideBossHUD()
    {
        if (bossHudElement == null)
        {
            SetupReferences();
        }

        if (bossHudElement != null)
        {
            bossHudElement.style.display = DisplayStyle.None;
            lastVisibleState = false;
        }
    }
}
