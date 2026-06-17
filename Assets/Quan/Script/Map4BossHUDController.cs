using System.Collections;
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

    [Header("Location Title UI")]
    [Tooltip("Tên box chứa bảng địa danh.")]
    public string locationBoxName = "Box_mask";

    [Tooltip("Tên text địa danh.")]
    public string locationTextName = "Box_text";

    [TextArea(2, 5)]
    [Tooltip("Nội dung địa danh.")]
    public string locationTitleText = "SƯ\nĐÀ\nLĨNH";

    [Tooltip("Thời gian fade in.")]
    public float locationFadeInTime = 1f;

    [Tooltip("Thời gian giữ bảng địa danh.")]
    public float locationHoldTime = 2f;

    [Tooltip("Thời gian fade out.")]
    public float locationFadeOutTime = 1f;

    [Header("Debug")]
    public bool enableDebugLog = true;

    private VisualElement boss3HealthFill;
    private Label boss3HealthText;

    private VisualElement boss4HealthFill;
    private Label boss4HealthText;

    private VisualElement locationBox;
    private Label locationTitleLabel;

    private int cachedBoss3CurrentHealth = -1;
    private int cachedBoss3MaxHealth = -1;

    private int cachedBoss4CurrentHealth = -1;
    private int cachedBoss4MaxHealth = -1;

    void Awake()
    {
        FindUIElements();
        HideLocationTitleImmediate();
    }

    void OnEnable()
    {
        FindUIElements();
        RefreshCachedHealth();
        HideLocationTitleImmediate();
    }

    void Start()
    {
        FindUIElements();
        RefreshCachedHealth();
        HideLocationTitleImmediate();
    }

    void FindUIElements()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("Map4BossHUDController chưa có UIDocument.");
            }

            return;
        }

        if (uiDocument.rootVisualElement == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("Map4BossHUDController chưa lấy được rootVisualElement.");
            }

            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        boss3HealthFill = root.Q<VisualElement>(boss3HealthFillName);
        boss3HealthText = root.Q<Label>(boss3HealthTextName);

        boss4HealthFill = root.Q<VisualElement>(boss4HealthFillName);
        boss4HealthText = root.Q<Label>(boss4HealthTextName);

        locationBox = root.Q<VisualElement>(locationBoxName);
        locationTitleLabel = root.Q<Label>(locationTextName);

        if (enableDebugLog)
        {
            if (locationBox == null)
            {
                Debug.LogWarning("Không tìm thấy Location Box: " + locationBoxName);
            }

            if (locationTitleLabel == null)
            {
                Debug.LogWarning("Không tìm thấy Location Text: " + locationTextName);
            }
        }
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

    public void SetLocationTitleText(string text)
    {
        locationTitleText = text;

        if (locationTitleLabel == null)
        {
            FindUIElements();
        }

        if (locationTitleLabel != null)
        {
            locationTitleLabel.text = text;
        }
    }

    public void HideLocationTitleImmediate()
    {
        if (locationBox == null)
        {
            return;
        }

        locationBox.style.opacity = 0f;
        locationBox.style.display = DisplayStyle.None;
    }

    public IEnumerator PlayLocationTitleRoutine(string customText = null)
    {
        if (locationBox == null || locationTitleLabel == null)
        {
            FindUIElements();
        }

        if (locationBox == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("Không thể hiện Location Title vì chưa tìm thấy: " + locationBoxName);
            }

            yield break;
        }

        if (locationTitleLabel != null)
        {
            if (!string.IsNullOrEmpty(customText))
            {
                locationTitleLabel.text = customText;
            }
            else
            {
                locationTitleLabel.text = locationTitleText;
            }
        }

        locationBox.style.display = DisplayStyle.Flex;
        locationBox.style.opacity = 0f;

        float timer = 0f;

        while (timer < locationFadeInTime)
        {
            timer += Time.deltaTime;

            float alpha = locationFadeInTime > 0f
                ? Mathf.Clamp01(timer / locationFadeInTime)
                : 1f;

            locationBox.style.opacity = alpha;

            yield return null;
        }

        locationBox.style.opacity = 1f;

        if (locationHoldTime > 0f)
        {
            yield return new WaitForSeconds(locationHoldTime);
        }

        timer = 0f;

        while (timer < locationFadeOutTime)
        {
            timer += Time.deltaTime;

            float alpha = locationFadeOutTime > 0f
                ? 1f - Mathf.Clamp01(timer / locationFadeOutTime)
                : 0f;

            locationBox.style.opacity = alpha;

            yield return null;
        }

        locationBox.style.opacity = 0f;
        locationBox.style.display = DisplayStyle.None;
    }
}