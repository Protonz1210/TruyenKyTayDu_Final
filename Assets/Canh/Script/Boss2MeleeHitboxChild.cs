using UnityEngine;

public class Boss2MeleeHitboxChild : MonoBehaviour
{
    [Tooltip("Hitbox cha xử lý damage.")]
    public Boss2MeleeHitbox parentHitbox;

    void Awake()
    {
        if (parentHitbox == null)
        {
            parentHitbox = GetComponentInParent<Boss2MeleeHitbox>();
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