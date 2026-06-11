using UnityEngine;
using UnityEngine.UIElements;

public class Map4BossHUDController : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument uiDocument;

    [Header("Thanh Su Tinh UI Names")]
    public string thanhSuHealthFillName = "thanh-su-health-fill";
    public string thanhSuHealthTextName = "thanh-su-health-text";

    [Header("Bach Tuong Tinh UI Names")]
    public string bachTuongHealthFillName = "bach-tuong-health-fill";
    public string bachTuongHealthTextName = "bach-tuong-health-text";

    private VisualElement thanhSuHealthFill;
    private Label thanhSuHealthText;

    private VisualElement bachTuongHealthFill;
    private Label bachTuongHealthText;

    private float thanhSuFullWidth = -1f;
    private float bachTuongFullWidth = -1f;

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
    }

    void BindUI()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("Map4BossHUDController chưa được gán UI Document.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        thanhSuHealthFill = root.Q<VisualElement>(thanhSuHealthFillName);
        thanhSuHealthText = root.Q<Label>(thanhSuHealthTextName);

        bachTuongHealthFill = root.Q<VisualElement>(bachTuongHealthFillName);
        bachTuongHealthText = root.Q<Label>(bachTuongHealthTextName);

        if (thanhSuHealthFill == null)
            Debug.LogWarning("Không tìm thấy Thanh Sư Tinh health fill: " + thanhSuHealthFillName);

        if (thanhSuHealthText == null)
            Debug.LogWarning("Không tìm thấy Thanh Sư Tinh health text: " + thanhSuHealthTextName);

        if (bachTuongHealthFill == null)
            Debug.LogWarning("Không tìm thấy Bạch Tượng Tinh health fill: " + bachTuongHealthFillName);

        if (bachTuongHealthText == null)
            Debug.LogWarning("Không tìm thấy Bạch Tượng Tinh health text: " + bachTuongHealthTextName);

        if (thanhSuHealthFill != null)
        {
            thanhSuHealthFill.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (thanhSuFullWidth <= 0f)
                    thanhSuFullWidth = thanhSuHealthFill.resolvedStyle.width;
            });
        }

        if (bachTuongHealthFill != null)
        {
            bachTuongHealthFill.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (bachTuongFullWidth <= 0f)
                    bachTuongFullWidth = bachTuongHealthFill.resolvedStyle.width;
            });
        }
    }

    public void SetThanhSuTinhHealth(int currentHealth, int maxHealth)
    {
        SetHealth(
            currentHealth,
            maxHealth,
            thanhSuHealthFill,
            thanhSuHealthText,
            ref thanhSuFullWidth
        );
    }

    public void SetBachTuongTinhHealth(int currentHealth, int maxHealth)
    {
        SetHealth(
            currentHealth,
            maxHealth,
            bachTuongHealthFill,
            bachTuongHealthText,
            ref bachTuongFullWidth
        );
    }

    void SetHealth(
        int currentHealth,
        int maxHealth,
        VisualElement fillElement,
        Label textElement,
        ref float fullWidth
    )
    {
        if (maxHealth <= 0)
            maxHealth = 1;

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        float percent = (float)currentHealth / maxHealth;

        if (textElement != null)
        {
            textElement.text = currentHealth + " / " + maxHealth;
        }

        if (fillElement != null)
        {
            if (fullWidth <= 0f)
            {
                fullWidth = fillElement.resolvedStyle.width;
            }

            if (fullWidth > 0f)
            {
                fillElement.style.width = fullWidth * percent;
            }
            else
            {
                fillElement.style.width = Length.Percent(percent * 100f);
            }
        }
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