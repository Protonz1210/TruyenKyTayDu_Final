using UnityEngine;

public class Enemy123MeleeHitboxChild : MonoBehaviour
{
    [Tooltip("Hitbox cha xử lý damage.")]
    public Enemy123MeleeHitbox parentHitbox;

    void Awake()
    {
        if (parentHitbox == null)
        {
            parentHitbox = GetComponentInParent<Enemy123MeleeHitbox>();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (parentHitbox != null)
        {
            parentHitbox.ReceiveTrigger(other);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (parentHitbox != null)
        {
            parentHitbox.ReceiveTrigger(other);
        }
    }
}