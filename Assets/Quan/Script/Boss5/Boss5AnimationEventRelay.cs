using UnityEngine;

public class Boss5AnimationEventRelay : MonoBehaviour
{
    [Tooltip("Kéo Boss5Controller ở object cha vào đây.")]
    public Boss5Controller boss5Controller;

    void Awake()
    {
        if (boss5Controller == null)
        {
            boss5Controller = GetComponentInParent<Boss5Controller>();
        }
    }

    public void Boss5_AttackFireEvent()
    {
        if (boss5Controller == null) return;

        boss5Controller.Boss5_AttackFireEvent();
    }
}