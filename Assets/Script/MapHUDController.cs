using UnityEngine;
using UnityEngine.UIElements;

public class MapHUDController : MonoBehaviour
{
    [Header("UI Document")]
    [Tooltip("UI Document của HUD trong màn chơi.")]
    public UIDocument uiDocument;

    private VisualElement wukongHealthFill;
    private Label wukongHealthText;

    private VisualElement partyHealthFill;
    private Label partyHealthText;

    private VisualElement[] skillCooldownFills = new VisualElement[4];

    private void Awake()
    {
        BindUI();
    }

    private void OnEnable()
    {
        BindUI();
        ResetSkillCooldownVisuals();
    }

    private void BindUI()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            Debug.LogError("MapHUDController: Không tìm thấy UIDocument.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogError("MapHUDController: UIDocument chưa có rootVisualElement.");
            return;
        }

        // Wukong health
        wukongHealthFill = root.Q<VisualElement>("wukong-health-fill");
        wukongHealthText = root.Q<Label>("wukong-health-text");

        // Party health
        partyHealthFill = root.Q<VisualElement>("party-health-fill");
        partyHealthText = root.Q<Label>("party-health-text");

        // Skill cooldown fills
        // Không dùng skill 0 vì Attack0 là đánh thường, không cần UI hồi chiêu.
        skillCooldownFills[1] = root.Q<VisualElement>("skill-cd-bar-fill-1");
        skillCooldownFills[2] = root.Q<VisualElement>("skill-cd-bar-fill-2");
        skillCooldownFills[3] = root.Q<VisualElement>("skill-cd-bar-fill-3");

        CheckMissingUI();
        PrepareSkillFill(1);
        PrepareSkillFill(2);
        PrepareSkillFill(3);
    }

    private void CheckMissingUI()
    {
        if (wukongHealthFill == null)
        {
            Debug.LogError("Không tìm thấy UI: wukong-health-fill");
        }

        if (wukongHealthText == null)
        {
            Debug.LogError("Không tìm thấy UI: wukong-health-text");
        }

        if (partyHealthFill == null)
        {
            Debug.LogError("Không tìm thấy UI: party-health-fill");
        }

        if (partyHealthText == null)
        {
            Debug.LogError("Không tìm thấy UI: party-health-text");
        }

        if (skillCooldownFills[1] == null)
        {
            Debug.LogError("Không tìm thấy UI: skill-cd-bar-fill-1");
        }

        if (skillCooldownFills[2] == null)
        {
            Debug.LogError("Không tìm thấy UI: skill-cd-bar-fill-2");
        }

        if (skillCooldownFills[3] == null)
        {
            Debug.LogError("Không tìm thấy UI: skill-cd-bar-fill-3");
        }
    }

    private void PrepareSkillFill(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skillCooldownFills.Length)
        {
            return;
        }

        VisualElement fill = skillCooldownFills[skillIndex];

        if (fill == null)
        {
            return;
        }

        // Giữ layout đơn giản như bản Map4 cũ.
        // Chỉ cho fill tự co theo width, không đụng mask/frame.
        fill.style.flexGrow = 0;
        fill.style.flexShrink = 0;
        fill.style.minWidth = 0;
        fill.style.display = DisplayStyle.Flex;
        fill.style.visibility = Visibility.Visible;
    }

    private void ResetSkillCooldownVisuals()
    {
        SetSkillCooldownFill(1, 1f);
        SetSkillCooldownFill(2, 1f);
        SetSkillCooldownFill(3, 1f);
    }

    public void SetWukongHealth(int currentHealth, int maxHealth)
    {
        SetHealthBar(wukongHealthFill, wukongHealthText, currentHealth, maxHealth);
    }

    public void SetPartyHealth(int currentHealth, int maxHealth)
    {
        SetHealthBar(partyHealthFill, partyHealthText, currentHealth, maxHealth);
    }

    private void SetHealthBar(
        VisualElement fillElement,
        Label textElement,
        int currentHealth,
        int maxHealth
    )
    {
        if (maxHealth <= 0)
        {
            maxHealth = 1;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        float percent = (float)currentHealth / maxHealth;

        if (fillElement != null)
        {
            fillElement.style.width = Length.Percent(percent * 100f);
        }

        if (textElement != null)
        {
            textElement.text = currentHealth + " / " + maxHealth;
        }
    }

    public void SetSkillCooldownFill(int skillIndex, float percent)
    {
        if (skillIndex < 0 || skillIndex >= skillCooldownFills.Length)
        {
            return;
        }

        VisualElement fill = skillCooldownFills[skillIndex];

        if (fill == null)
        {
            return;
        }

        percent = Mathf.Clamp01(percent);

        // Cơ chế ổn định: co trực tiếp fill.
        // Không co mask, không co frame để tránh tràn UI sang skill khác.
        fill.style.flexGrow = 0;
        fill.style.flexShrink = 0;
        fill.style.minWidth = 0;
        fill.style.width = Length.Percent(percent * 100f);
        fill.style.display = DisplayStyle.Flex;
        fill.style.visibility = Visibility.Visible;
    }
}