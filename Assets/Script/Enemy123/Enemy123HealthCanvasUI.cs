using UnityEngine;
using TMPro;

public class Enemy123HealthCanvasUI : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Enemy123 cần hiển thị máu.")]
    public Enemy123Controller enemy123;

    [Header("UI")]
    [Tooltip("Text hiển thị máu.")]
    public TextMeshProUGUI healthText;

    [Header("Position")]
    [Tooltip("Vị trí lệch so với Enemy123.")]
    public Vector3 offset = new Vector3(0f, 1.8f, 0f);

    [Header("Display")]
    [Tooltip("Ẩn UI khi Enemy123 chết.")]
    public bool hideWhenDead = true;

    [Tooltip("UI luôn quay về phía camera.")]
    public bool faceCamera = true;

    [Header("Fix Flip")]
    [Tooltip("Chống chữ bị lật khi Enemy123 quay trái.")]
    public bool fixTextFlip = true;

    private Camera mainCamera;
    private Vector3 originalLocalScale;

    void Awake()
    {
        if (enemy123 == null)
        {
            enemy123 = GetComponentInParent<Enemy123Controller>();
        }

        if (healthText == null)
        {
            healthText = GetComponentInChildren<TextMeshProUGUI>();
        }

        mainCamera = Camera.main;
        originalLocalScale = transform.localScale;
    }

    void LateUpdate()
    {
        if (enemy123 == null || healthText == null) return;

        int currentHealth = enemy123.GetCurrentHealth();
        int maxHealth = enemy123.GetMaxHealth();

        if (maxHealth <= 0)
        {
            maxHealth = 1;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        healthText.text = currentHealth + " / " + maxHealth;

        transform.position = enemy123.transform.position + offset;

        if (faceCamera && mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }

        if (fixTextFlip)
        {
            FixCanvasFlip();
        }

        if (hideWhenDead && currentHealth <= 0)
        {
            gameObject.SetActive(false);
        }
    }

    void FixCanvasFlip()
    {
        if (enemy123 == null) return;

        Vector3 fixedScale = originalLocalScale;

        float targetScaleX = enemy123.transform.lossyScale.x;

        if (targetScaleX < 0f)
        {
            fixedScale.x = -Mathf.Abs(originalLocalScale.x);
        }
        else
        {
            fixedScale.x = Mathf.Abs(originalLocalScale.x);
        }

        fixedScale.y = Mathf.Abs(originalLocalScale.y);
        fixedScale.z = Mathf.Abs(originalLocalScale.z);

        transform.localScale = fixedScale;
    }
}