using UnityEngine;
using TMPro;

public class EnemyHealthCanvasUI : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Enemy cần hiển thị máu.")]
    public Enemy4Controller enemy;

    [Header("UI")]
    [Tooltip("Text hiển thị máu.")]
    public TextMeshProUGUI healthText;

    [Tooltip("Canvas chứa UI máu. Nếu để trống sẽ tự tìm trên object này.")]
    public Canvas targetCanvas;

    [Header("Position")]
    [Tooltip("Vị trí lệch so với Enemy.")]
    public Vector3 offset = new Vector3(0f, 1.8f, 0f);

    [Header("Display")]
    [Tooltip("Chỉ hiện máu khi Enemy4 đã chuyển sang trạng thái tấn công.")]
    public bool showOnlyWhenCombatActivated = true;

    [Tooltip("Ẩn UI khi Enemy chết.")]
    public bool hideWhenDead = true;

    [Tooltip("UI luôn quay về phía camera.")]
    public bool faceCamera = true;

    [Header("Scale Fix")]
    [Tooltip("Sửa lỗi UI bị lật khi Enemy4 quay trái/phải bằng localScale âm.")]
    public bool fixFlipScale = true;

    [Tooltip("Giữ scale gốc của UI.")]
    public bool keepOriginalScale = true;

    private Camera mainCamera;
    private Vector3 originalLocalScale;

    void Awake()
    {
        originalLocalScale = transform.localScale;

        if (enemy == null)
        {
            enemy = GetComponentInParent<Enemy4Controller>();
        }

        if (healthText == null)
        {
            healthText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (targetCanvas == null)
        {
            targetCanvas = GetComponent<Canvas>();
        }

        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (enemy == null || healthText == null)
        {
            SetUIVisible(false);
            return;
        }

        bool shouldShow = ShouldShowHealthUI();

        SetUIVisible(shouldShow);

        if (!shouldShow)
        {
            return;
        }

        int currentHealth = enemy.GetCurrentHealth();
        int maxHealth = enemy.GetMaxHealth();

        if (maxHealth <= 0)
        {
            maxHealth = 1;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        healthText.text = currentHealth + " / " + maxHealth;

        transform.position = enemy.transform.position + offset;

        if (faceCamera && mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }

        FixScale();
    }

    bool ShouldShowHealthUI()
    {
        if (enemy == null) return false;

        if (hideWhenDead && enemy.IsDead())
        {
            return false;
        }

        if (showOnlyWhenCombatActivated)
        {
            if (!enemy.combatActivated)
            {
                return false;
            }

            if (enemy.combatStoppedByDeath)
            {
                return false;
            }
        }

        return true;
    }

    void SetUIVisible(bool visible)
    {
        if (targetCanvas != null)
        {
            targetCanvas.enabled = visible;
        }

        if (healthText != null && healthText.gameObject.activeSelf != visible)
        {
            healthText.gameObject.SetActive(visible);
        }
    }

    void FixScale()
    {
        if (!fixFlipScale) return;

        Vector3 fixedScale = keepOriginalScale ? originalLocalScale : transform.localScale;

        fixedScale.x = Mathf.Abs(fixedScale.x);
        fixedScale.y = Mathf.Abs(fixedScale.y);

        if (enemy != null && enemy.transform.lossyScale.x < 0f)
        {
            fixedScale.x = -Mathf.Abs(fixedScale.x);
        }

        transform.localScale = fixedScale;
    }
}