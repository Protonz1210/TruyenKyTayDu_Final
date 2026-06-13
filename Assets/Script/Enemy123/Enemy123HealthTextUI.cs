using UnityEngine;
using UnityEngine.UI;

public class Enemy123HealthTextUI : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Đối tượng Enemy123 cần đi theo.")]
    public Transform target;

    [Tooltip("Camera chính.")]
    public Camera mainCamera;

    [Header("UI")]
    [Tooltip("Text hiển thị máu.")]
    public Text healthText;

    [Header("Follow")]
    [Tooltip("Độ cao của text trên đầu.")]
    public Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

    [Tooltip("Luôn quay mặt về camera.")]
    public bool faceCamera = true;

    void Awake()
    {
        if (target == null)
        {
            target = transform.parent;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (healthText == null)
        {
            healthText = GetComponentInChildren<Text>();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + worldOffset;

        if (faceCamera && mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }

    public void UpdateHealthText(int currentHealth, int maxHealth)
    {
        if (healthText != null)
        {
            healthText.text = currentHealth + " / " + maxHealth;
        }
    }
}