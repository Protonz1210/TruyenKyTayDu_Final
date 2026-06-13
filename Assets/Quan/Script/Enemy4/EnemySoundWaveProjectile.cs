using UnityEngine;

public class EnemySoundWaveProjectile : MonoBehaviour
{
    [Header("Move")]
    [Tooltip("Tốc độ bay.")]
    public float moveSpeed = 7f;

    [Tooltip("Thời gian tự hủy.")]
    public float lifeTime = 3f;

    [Header("Hit")]
    [Tooltip("Sát thương.")]
    public int damage = 100;

    [Tooltip("Tự hủy khi va chạm.")]
    public bool destroyOnHit = true;

    [Header("Tags")]
    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Tag của đoàn thỉnh kinh.")]
    public string partyTag = "Party";

    [Tooltip("Tag của Enemy.")]
    public string enemyTag = "Enemy";

    [Tooltip("Tag của mặt đất.")]
    public string groundTag = "Ground";

    [Header("Visual Direction")]
    [Tooltip("Lật hình theo hướng bay.")]
    public bool flipByDirection = true;

    [Tooltip("Xoay hình theo hướng bay.")]
    public bool rotateByDirection = false;

    private Vector2 moveDirection;
    private bool initialized;
    private bool isDestroyed;


public void Init(Vector2 direction, int projectileDamage)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = Vector2.right;
        }

        moveDirection = direction.normalized;
        damage = projectileDamage;
        initialized = true;

        ApplyVisualDirection(moveDirection);

        // Dự phòng: nếu animation không gọi DestroyProjectile thì vẫn tự mất.
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (!initialized)
            return;

        transform.position += (Vector3)(moveDirection * moveSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed)
            return;

        // Không xử lý enemy. Layer Matrix cũng nên tắt EnemyProjectile x Enemy.
        if (other.CompareTag(enemyTag))
        {
            return;
        }

        // Trúng Ngộ Không
        if (other.CompareTag(playerTag))
        {
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }

            DestroySelfIfNeeded();
            return;
        }

        // Trúng đoàn thỉnh kinh
        if (other.CompareTag(partyTag))
        {
            PartyMemberHitReceiver partyMember =
                other.GetComponentInParent<PartyMemberHitReceiver>();

            if (partyMember != null)
            {
                partyMember.TakeDamage(damage);
            }

            DestroySelfIfNeeded();
            return;
        }

        // Trúng đất
        if (other.CompareTag(groundTag))
        {
            DestroySelfIfNeeded();
            return;
        }
    }

    void DestroySelfIfNeeded()
    {
        if (!destroyOnHit)
            return;

        DestroyProjectile();
    }

    void ApplyVisualDirection(Vector2 direction)
    {
        if (flipByDirection)
        {
            Vector3 scale = transform.localScale;

            if (direction.x < 0f)
            {
                scale.x = -Mathf.Abs(scale.x);
            }
            else
            {
                scale.x = Mathf.Abs(scale.x);
            }

            transform.localScale = scale;
        }

        if (rotateByDirection)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    // Gắn hàm này bằng Animation Event ở frame cuối animation sóng âm.
    public void DestroyProjectile()
    {
        if (isDestroyed)
            return;

        isDestroyed = true;
        Destroy(gameObject);
    }
}