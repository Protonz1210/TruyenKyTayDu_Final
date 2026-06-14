
using UnityEngine;

public class WukongCinematicAttackEventReceiver : MonoBehaviour
{
    [Header("State")]
    public bool isAttackPlaying;
    public int currentAttackIndex = -1;

    [Header("Debug")]
    public bool enableDebugLog = true;

    public void BeginAttack(int attackIndex)
    {
        isAttackPlaying = true;
        currentAttackIndex = attackIndex;

        if (enableDebugLog)
        {
            Debug.Log(gameObject.name + " bắt đầu Attack" + attackIndex);
        }
    }

    // Hàm này sẽ được gọi bằng Animation Event ở frame cuối của mỗi animation Attack.
    public void OnCinematicAttackFinished()
    {
        if (enableDebugLog)
        {
            Debug.Log(gameObject.name + " kết thúc Attack" + currentAttackIndex);
        }

        isAttackPlaying = false;
    }

    public bool IsAttackFinished()
    {
        return !isAttackPlaying;
    }

    public void ForceFinishAttack()
    {
        isAttackPlaying = false;
    }
}