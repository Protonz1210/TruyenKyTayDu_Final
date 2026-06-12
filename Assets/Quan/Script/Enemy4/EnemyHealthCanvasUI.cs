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

    [Header("Position")]
    [Tooltip("Vị trí lệch so với Enemy.")]
    public Vector3 offset = new Vector3(0f, 1.8f, 0f);

    [Header("Display")]
    [Tooltip("Ẩn UI khi Enemy chết.")]
    public bool hideWhenDead = true;

    [Tooltip("UI luôn quay về phía camera.")]
    public bool faceCamera = true;

    private Camera mainCamera;


void Awake()
    {
        if (enemy == null)
        {
            enemy = GetComponentInParent<Enemy4Controller>();
        }

        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (enemy == null || healthText == null)
            return;

        int currentHealth = enemy.GetCurrentHealth();
        int maxHealth = enemy.GetMaxHealth();

        if (maxHealth <= 0)
            maxHealth = 1;

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        healthText.text = currentHealth + " / " + maxHealth;

        transform.position = enemy.transform.position + offset;

        if (faceCamera && mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }

        if (hideWhenDead && currentHealth <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}