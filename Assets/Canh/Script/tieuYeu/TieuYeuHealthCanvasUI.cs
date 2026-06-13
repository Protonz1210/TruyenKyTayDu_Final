using UnityEngine;
using TMPro;

public class TieuYeuHealthCanvasUI : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Tiểu yêu cần hiển thị máu.")]
    public TieuYeuController tieuYeu;

    [Header("UI")]
    [Tooltip("Text hiển thị máu.")]
    public TextMeshProUGUI healthText;

    [Header("Position")]
    [Tooltip("Vị trí lệch so với Tiểu yêu.")]
    public Vector3 offset = new Vector3(0f, 1.8f, 0f);

    [Header("Display")]
    [Tooltip("Ẩn UI khi Tiểu yêu chết.")]
    public bool hideWhenDead = true;

    [Tooltip("UI luôn quay về phía camera.")]
    public bool faceCamera = true;

    [Header("Fix Flip")]
    [Tooltip("Chống chữ bị lật khi Tiểu yêu quay trái.")]
    public bool fixTextFlip = true;

    private Camera mainCamera;
    private Vector3 originalLocalScale;

    void Awake()
    {
        if (tieuYeu == null)
        {
            tieuYeu = GetComponentInParent<TieuYeuController>();
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
        if (tieuYeu == null || healthText == null)
            return;

        int currentHealth = tieuYeu.GetCurrentHealth();
        int maxHealth = tieuYeu.GetMaxHealth();

        if (maxHealth <= 0)
            maxHealth = 1;

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        healthText.text = currentHealth + " / " + maxHealth;

        transform.position = tieuYeu.transform.position + offset;

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
        if (tieuYeu == null)
            return;

        Vector3 fixedScale = originalLocalScale;

        float targetScaleX = tieuYeu.transform.lossyScale.x;

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