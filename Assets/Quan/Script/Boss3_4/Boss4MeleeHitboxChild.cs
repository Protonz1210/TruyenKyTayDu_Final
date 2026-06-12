using UnityEngine;

public class Boss4MeleeHitboxChild : MonoBehaviour
{
    public Boss4MeleeHitbox parentHitbox;

    void Awake()
    {
        if (parentHitbox == null)
        {
            parentHitbox = GetComponentInParent<Boss4MeleeHitbox>();
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