
using UnityEngine;
using UnityEngine.UIElements;

public class Boss2HUDController : MonoBehaviour
{
    [Header("UI Document")]
    [Tooltip("UI Document riêng của Boss2.")]
    public UIDocument uiDocument;

    [Header("UI Names")]
    [Tooltip("Root chứa toàn bộ UI máu Boss2. Có thể để trống nếu không dùng root.")]
    public string boss2RootName = "boss2-health-root";

    [Tooltip("Tên thanh fill máu Boss2.")]
    public string boss2HealthFillName = "boss2-health-fill";

    [Tooltip("Tên text máu Boss2.")]
    public string boss2HealthTextName = "boss2-health-text";

    [Tooltip("Thanh máu bám bên phải hay không.")]
    public bool anchorRight = false;

    VisualElement root;
    VisualElement boss2Root;
    VisualElement healthFill;
    Label healthText;

    int currentHealth;
    int maxHealth = 1;

    void Awake()
    {
        BindUI();
        SetVisible(false);
    }

    void OnEnable()
    {
        BindUI();
        Refresh();
    }

    void BindUI()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;
        if (root == null) return;

        if (!string.IsNullOrEmpty(boss2RootName))
        {
            boss2Root = root.Q<VisualElement>(boss2RootName);
        }

        healthFill = root.Q<VisualElement>(boss2HealthFillName);
        healthText = root.Q<Label>(boss2HealthTextName);
    }

    public void SetVisible(bool visible)
    {
        BindUI();

        DisplayStyle displayStyle = visible ? DisplayStyle.Flex : DisplayStyle.None;

        if (boss2Root != null)
        {
            boss2Root.style.display = displayStyle;
        }
        else if (root != null)
        {
            root.style.display = displayStyle;
        }
    }

    public void SetHealth(int current, int max)
    {
        currentHealth = Mathf.Max(0, current);
        maxHealth = Mathf.Max(1, max);

        Refresh();
    }
    void Refresh()
    {
        BindUI();

        float percent = Mathf.Clamp01((float)currentHealth / maxHealth);

        if (healthFill != null)
        {
            healthFill.style.width = Length.Percent(percent * 100f);

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