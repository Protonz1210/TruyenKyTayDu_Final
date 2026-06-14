using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class FinalWukongZoneDuelTest : MonoBehaviour
{
    [Header("Wukong")]
    [Tooltip("Kéo object TrueWukong vào đây.")]
    public Transform trueWukong;

    [Tooltip("Kéo object FakeWukong vào đây.")]
    public Transform fakeWukong;

    [Tooltip("Animator của TrueWukong.")]
    public Animator trueAnimator;

    [Tooltip("Animator của FakeWukong.")]
    public Animator fakeAnimator;

    [Tooltip("Component nhận Animation Event của TrueWukong.")]
    public WukongCinematicAttackEventReceiver trueAttackEventReceiver;

    [Tooltip("Component nhận Animation Event của FakeWukong.")]
    public WukongCinematicAttackEventReceiver fakeAttackEventReceiver;

    [Header("Rest Zones")]
    [Tooltip("Vùng nghỉ bên trái. Chỉ dùng nếu Return To Original Start Position tắt.")]
    public Collider2D leftRestZone;

    [Tooltip("Vùng nghỉ bên phải. Chỉ dùng nếu Return To Original Start Position tắt.")]
    public Collider2D rightRestZone;

    [Header("Return")]
    [Tooltip("Bật: sau khi đánh xong, 2 Wukong chạy về đúng vị trí ban đầu lúc bấm Play. Tắt: chạy về LeftRestZone và RightRestZone.")]
    public bool returnToOriginalStartPosition = true;

    [Header("Attack Range")]
    [Tooltip("Khoảng cách bắt đầu đánh. Số càng lớn thì 2 Wukong đứng càng xa nhau đã bắt đầu đánh.")]
    public float attackStartDistance = 3.5f;

    [Tooltip("Bật: chỉ tính khoảng cách theo trục X. Nên bật cho game 2D platform ngang.")]
    public bool useOnlyXDistance = true;

    [Header("Animator Parameters")]
    [Tooltip("Tên parameter tốc độ trong Animator. Phải đúng là Speed nếu Animator đang dùng Speed.")]
    public string speedParameterName = "Speed";

    [Tooltip("Tên state Idle trong Animator.")]
    public string idleStateName = "Wukong2Idle";

    [Tooltip("Tên state Run trong Animator.")]
    public string runStateName = "Wukong2Run";

    [Tooltip("Tên state Jump trong Animator.")]
    public string jumpStateName = "Wukong2Jump";

    [Tooltip("Tên trigger Attack0 trong Animator.")]
    public string attack0TriggerName = "Attack0";

    [Tooltip("Tên trigger Attack1 trong Animator.")]
    public string attack1TriggerName = "Attack1";

    [Tooltip("Tên trigger Attack2 trong Animator.")]
    public string attack2TriggerName = "Attack2";

    [Tooltip("Tên trigger Attack3 trong Animator.")]
    public string attack3TriggerName = "Attack3";

    [Header("Move")]
    [Tooltip("Tốc độ chạy vào đánh và chạy về. Số càng lớn thì chạy càng nhanh.")]
    public float moveSpeed = 4f;

    [Tooltip("Bật: giữ nguyên độ cao Y ban đầu, chỉ di chuyển ngang theo X.")]
    public bool moveOnlyX = true;

    [Tooltip("Bật: khi vào tầm đánh thì dừng hẳn trước khi tung chiêu.")]
    public bool hardStopBeforeAttack = true;

    [Header("Jump Back Before Return")]
    [Tooltip("Bật: sau khi đánh xong, 2 Wukong cùng nhảy lùi ra sau rồi mới chạy về.")]
    public bool jumpBackBeforeReturn = true;

    [Tooltip("Khoảng cách nhảy lùi sau khi đánh xong. Số càng lớn thì bật ra càng xa.")]
    public float jumpBackDistance = 1.5f;

    [Tooltip("Độ cao cú nhảy lùi. Số càng lớn thì nhảy càng cao.")]
    public float jumpHeight = 1.2f;

    [Tooltip("Thời gian nhảy lùi. Số càng lớn thì cú nhảy càng chậm và mềm.")]
    public float jumpDuration = 0.45f;

    [Header("Fixed Combo")]
    [Tooltip("Chuỗi chiêu của TrueWukong. 0 = Attack0, 1 = Attack1, 2 = Attack2, 3 = Attack3.")]
    public int[] trueAttackCombo = new int[] { 0, 0, 1, 2, 0, 0, 3 };

    [Tooltip("Chuỗi chiêu của FakeWukong. 0 = Attack0, 1 = Attack1, 2 = Attack2, 3 = Attack3.")]
    public int[] fakeAttackCombo = new int[] { 0, 0, 2, 3, 0, 0, 1 };

    [Header("Combo Timing")]
    [Tooltip("Thời gian chờ trước khi bắt đầu combo sau khi 2 Wukong đã vào tầm đánh.")]
    public float attackPrepareTime = 0.15f;

    [Tooltip("Khoảng nghỉ riêng của TrueWukong sau mỗi chiêu. Để 0 nếu muốn nối chiêu nhanh.")]
    public float trueDelayBetweenAttacks = 0f;

    [Tooltip("Khoảng nghỉ riêng của FakeWukong sau mỗi chiêu. Để 0 nếu muốn nối chiêu nhanh.")]
    public float fakeDelayBetweenAttacks = 0f;

    [Tooltip("Thời gian chờ tối đa cho Animation Event báo kết thúc chiêu. Nếu vượt quá, code sẽ tự bỏ qua để tránh kẹt.")]
    public float maxWaitAttackEvent = 5f;

    [Tooltip("Sau khi cả hai đánh hết combo, đứng lại một chút rồi mới nhảy lùi/chạy về.")]
    public float afterComboDelay = 0.2f;

    [Tooltip("Sau khi chạy về vị trí đầu, đứng lại một chút rồi mới quay mặt nhìn nhau.")]
    public float afterReturnDelay = 0.15f;

    [Header("Facing")]
    [Tooltip("Bật nếu sprite gốc của Wukong đang nhìn sang phải. Nếu nhân vật quay ngược, thử tắt/bật dòng này.")]
    public bool spriteFacesRightByDefault = true;

    [Header("Test Input")]
    [Tooltip("Bật để nhấn phím 1 test đoạn 2 Wukong chạy vào đánh.")]
    public bool enableTestKey = true;

    [Tooltip("Bật để nhấn phím 2 test hàm PlayDuelOnce giống cách StoryManager sẽ gọi.")]
    public bool enablePublicCallTestKey = true;

    [Header("Debug Gizmos")]
    [Tooltip("Bật để hiện vùng khoảng cách đánh trong Scene view.")]
    public bool drawGizmos = true;

    [Tooltip("Độ cao vẽ gizmo so với chân nhân vật.")]
    public float gizmoHeightOffset = 1.2f;

    [Header("Debug")]
    [Tooltip("Bật để in log kiểm tra combo trong Console.")]
    public bool enableDebugLog = true;

    private bool isRunning;
    private float trueStartY;
    private float fakeStartY;

    private Vector3 trueOriginalStartPosition;
    private Vector3 fakeOriginalStartPosition;

    void Start()
    {
        CacheReferencesAndStartPositions();

        ForceBothIdle();
        FaceEachOther();
    }

    void Update()
    {
        if (isRunning) return;

        if (Keyboard.current == null) return;

        if (enableTestKey && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            StartCoroutine(DuelRoutine(null));
        }

        if (enablePublicCallTestKey && Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            PlayDuelOnce(() =>
            {
                Debug.Log("[FinalWukongZoneDuelTest] PlayDuelOnce test finished.");
            });
        }
    }

    public void PlayDuelOnce(Action onFinished = null)
    {
        if (isRunning)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("[FinalWukongZoneDuelTest] Duel đang chạy, không gọi lại.");
            }

            return;
        }

        StartCoroutine(DuelRoutine(onFinished));
    }

    public bool IsDuelRunning()
    {
        return isRunning;
    }

    IEnumerator DuelRoutine(Action onFinished)
    {
        isRunning = true;

        ForceBothIdle();
        FaceEachOther();

        yield return StartCoroutine(MoveUntilAttackDistanceRoutine());

        if (hardStopBeforeAttack)
        {
            StopBothRun();
        }

        FaceEachOther();

        yield return new WaitForSeconds(attackPrepareTime);

        Coroutine trueComboRoutine = StartCoroutine(PlayComboIndependentRoutine(
            trueAnimator,
            trueAttackEventReceiver,
            trueAttackCombo,
            trueDelayBetweenAttacks,
            "TrueWukong"
        ));

        Coroutine fakeComboRoutine = StartCoroutine(PlayComboIndependentRoutine(
            fakeAnimator,
            fakeAttackEventReceiver,
            fakeAttackCombo,
            fakeDelayBetweenAttacks,
            "FakeWukong"
        ));

        yield return trueComboRoutine;
        yield return fakeComboRoutine;

        yield return new WaitForSeconds(afterComboDelay);

        FaceAwayFromEachOther();

        if (jumpBackBeforeReturn)
        {
            yield return StartCoroutine(JumpBothBackRoutine());
        }

        Vector3 trueRestPos;
        Vector3 fakeRestPos;

        if (returnToOriginalStartPosition)
        {
            trueRestPos = trueOriginalStartPosition;
            fakeRestPos = fakeOriginalStartPosition;
        }
        else
        {
            trueRestPos = GetZoneCenter(leftRestZone, trueWukong.position);
            fakeRestPos = GetZoneCenter(rightRestZone, fakeWukong.position);
        }

        if (moveOnlyX)
        {
            trueRestPos.y = trueStartY;
            fakeRestPos.y = fakeStartY;
        }

        yield return StartCoroutine(MoveBothToRestRoutine(trueRestPos, fakeRestPos));

        ForceBothIdle();

        yield return new WaitForSeconds(afterReturnDelay);

        FaceEachOther();

        isRunning = false;

        if (enableDebugLog)
        {
            Debug.Log("[FinalWukongZoneDuelTest] Duel finished.");
        }

        onFinished?.Invoke();
    }

    void CacheReferencesAndStartPositions()
    {
        if (trueAnimator == null && trueWukong != null)
        {
            trueAnimator = trueWukong.GetComponent<Animator>();
        }

        if (fakeAnimator == null && fakeWukong != null)
        {
            fakeAnimator = fakeWukong.GetComponent<Animator>();
        }

        if (trueAttackEventReceiver == null && trueWukong != null)
        {
            trueAttackEventReceiver = trueWukong.GetComponent<WukongCinematicAttackEventReceiver>();
        }

        if (fakeAttackEventReceiver == null && fakeWukong != null)
        {
            fakeAttackEventReceiver = fakeWukong.GetComponent<WukongCinematicAttackEventReceiver>();
        }

        if (trueWukong != null)
        {
            trueOriginalStartPosition = trueWukong.position;
            trueStartY = trueWukong.position.y;
        }

        if (fakeWukong != null)
        {
            fakeOriginalStartPosition = fakeWukong.position;
            fakeStartY = fakeWukong.position.y;
        }
    }

    IEnumerator PlayComboIndependentRoutine(
        Animator animator,
        WukongCinematicAttackEventReceiver receiver,
        int[] combo,
        float delayBetweenAttacks,
        string actorName
    )
    {
        if (animator == null) yield break;
        if (combo == null || combo.Length == 0) yield break;

        for (int i = 0; i < combo.Length; i++)
        {
            int attackIndex = Mathf.Clamp(combo[i], 0, 3);

            if (enableDebugLog)
            {
                Debug.Log(actorName + " combo " + (i + 1) + " Attack" + attackIndex);
            }

            PlayAttack(animator, receiver, attackIndex);

            yield return StartCoroutine(WaitOneAttackEventFinishedRoutine(receiver, actorName));

            SetSpeed(animator, 0f);

            if (delayBetweenAttacks > 0f)
            {
                yield return new WaitForSeconds(delayBetweenAttacks);
            }
        }

        SetSpeed(animator, 0f);
    }

    IEnumerator WaitOneAttackEventFinishedRoutine(WukongCinematicAttackEventReceiver receiver, string actorName)
    {
        if (receiver == null)
        {
            yield break;
        }

        float timer = 0f;

        while (timer < maxWaitAttackEvent)
        {
            if (receiver.IsAttackFinished())
            {
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning(actorName + " chờ Animation Event quá lâu. Kiểm tra OnCinematicAttackFinished ở frame cuối animation Attack.");
        receiver.ForceFinishAttack();
    }

    void PlayAttack(Animator animator, WukongCinematicAttackEventReceiver receiver, int attackIndex)
    {
        if (animator == null) return;

        if (receiver != null)
        {
            receiver.BeginAttack(attackIndex);
        }

        ResetAttackTriggers(animator);
        SetSpeed(animator, 0f);

        if (attackIndex == 0)
        {
            animator.SetTrigger(attack0TriggerName);
        }
        else if (attackIndex == 1)
        {
            animator.SetTrigger(attack1TriggerName);
        }
        else if (attackIndex == 2)
        {
            animator.SetTrigger(attack2TriggerName);
        }
        else
        {
            animator.SetTrigger(attack3TriggerName);
        }
    }

    IEnumerator JumpBothBackRoutine()
    {
        Vector3 trueStart = trueWukong.position;
        Vector3 fakeStart = fakeWukong.position;

        float trueDirection = trueWukong.position.x < fakeWukong.position.x ? -1f : 1f;
        float fakeDirection = fakeWukong.position.x > trueWukong.position.x ? 1f : -1f;

        Vector3 trueEnd = trueStart + new Vector3(trueDirection * jumpBackDistance, 0f, 0f);
        Vector3 fakeEnd = fakeStart + new Vector3(fakeDirection * jumpBackDistance, 0f, 0f);

        trueEnd.y = trueStartY;
        fakeEnd.y = fakeStartY;

        FaceDirection(trueWukong, trueDirection);
        FaceDirection(fakeWukong, fakeDirection);

        PlayJumpState(trueAnimator);
        PlayJumpState(fakeAnimator);

        float timer = 0f;

        while (timer < jumpDuration)
        {
            float t = timer / jumpDuration;

            Vector3 truePos = Vector3.Lerp(trueStart, trueEnd, t);
            Vector3 fakePos = Vector3.Lerp(fakeStart, fakeEnd, t);
            truePos.y = trueStartY + Mathf.Sin(t * Mathf.PI) * jumpHeight;
            fakePos.y = fakeStartY + Mathf.Sin(t * Mathf.PI) * jumpHeight;

            trueWukong.position = truePos;
            fakeWukong.position = fakePos;

            timer += Time.deltaTime;
            yield return null;
        }

        trueWukong.position = trueEnd;
        fakeWukong.position = fakeEnd;
    }

    void PlayJumpState(Animator animator)
    {
        if (animator == null) return;

        SetSpeed(animator, 0f);

        if (!string.IsNullOrEmpty(jumpStateName))
        {
            animator.Play(jumpStateName, 0, 0f);
            animator.Update(0f);
        }
    }

    IEnumerator MoveUntilAttackDistanceRoutine()
    {
        while (!IsInAttackDistance())
        {
            MoveOneTowardOther(trueWukong, trueAnimator, fakeWukong, trueStartY);
            MoveOneTowardOther(fakeWukong, fakeAnimator, trueWukong, fakeStartY);

            FaceEachOther();

            yield return null;
        }

        StopBothRun();
    }

    void MoveOneTowardOther(Transform actor, Animator animator, Transform target, float lockedY)
    {
        if (actor == null || target == null) return;

        Vector3 currentPosition = actor.position;
        Vector3 targetPosition = target.position;

        if (moveOnlyX)
        {
            currentPosition.y = lockedY;
            targetPosition.y = lockedY;
        }

        float directionX = Mathf.Sign(targetPosition.x - currentPosition.x);

        if (Mathf.Abs(directionX) < 0.01f)
        {
            SetSpeed(animator, 0f);
            return;
        }

        Vector3 nextPosition = actor.position + new Vector3(directionX * moveSpeed * Time.deltaTime, 0f, 0f);

        if (moveOnlyX)
        {
            nextPosition.y = lockedY;
        }

        actor.position = nextPosition;
        SetSpeed(animator, 1f);
    }

    IEnumerator MoveBothToRestRoutine(Vector3 trueTarget, Vector3 fakeTarget)
    {
        bool trueArrived = false;
        bool fakeArrived = false;

        while (!trueArrived || !fakeArrived)
        {
            if (!trueArrived)
            {
                FaceMoveDirection(trueWukong, trueTarget);
                trueArrived = MoveOneToPosition(trueWukong, trueAnimator, trueTarget, trueStartY);
            }

            if (!fakeArrived)
            {
                FaceMoveDirection(fakeWukong, fakeTarget);
                fakeArrived = MoveOneToPosition(fakeWukong, fakeAnimator, fakeTarget, fakeStartY);
            }

            yield return null;
        }

        StopBothRun();
    }

    bool MoveOneToPosition(Transform actor, Animator animator, Vector3 targetPosition, float lockedY)
    {
        if (actor == null) return true;

        if (moveOnlyX)
        {
            targetPosition.y = lockedY;
        }

        float distanceX = Mathf.Abs(actor.position.x - targetPosition.x);

        if (distanceX <= 0.05f)
        {
            actor.position = new Vector3(targetPosition.x, targetPosition.y, actor.position.z);
            SetSpeed(animator, 0f);
            return true;
        }

        Vector3 nextPosition = Vector3.MoveTowards(actor.position, targetPosition, moveSpeed * Time.deltaTime);

        if (moveOnlyX)
        {
            nextPosition.y = lockedY;
        }

        actor.position = nextPosition;

        PlayRunState(animator);

        return false;
    }

    void PlayRunState(Animator animator)
    {
        if (animator == null) return;

        SetSpeed(animator, 1f);

        if (!string.IsNullOrEmpty(runStateName))
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (!stateInfo.IsName(runStateName))
            {
                animator.Play(runStateName, 0, 0f);
            }
        }
    }

    bool IsInAttackDistance()
    {
        if (trueWukong == null || fakeWukong == null) return false;

        if (useOnlyXDistance)
        {
            float distanceX = Mathf.Abs(trueWukong.position.x - fakeWukong.position.x);
            return distanceX <= attackStartDistance;
        }

        float distance = Vector2.Distance(trueWukong.position, fakeWukong.position);
        return distance <= attackStartDistance;
    }

    Vector3 GetZoneCenter(Collider2D zone, Vector3 fallback)
    {
        if (zone == null) return fallback;

        Vector3 center = zone.bounds.center;
        center.z = fallback.z;
        return center;
    }

    void ResetAttackTriggers(Animator animator)
    {
        if (animator == null) return;

        ResetTriggerIfExists(animator, attack0TriggerName);
        ResetTriggerIfExists(animator, attack1TriggerName);
        ResetTriggerIfExists(animator, attack2TriggerName);
        ResetTriggerIfExists(animator, attack3TriggerName);
    }

    void ResetTriggerIfExists(Animator animator, string triggerName)
    {
        if (animator == null) return;
        if (string.IsNullOrEmpty(triggerName)) return;

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == triggerName && parameters[i].type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(triggerName);
                return;
            }
        }
    }

    void ForceBothIdle()
    {
        ForceIdle(trueAnimator);
        ForceIdle(fakeAnimator);

        if (trueAttackEventReceiver != null)
        {
            trueAttackEventReceiver.ForceFinishAttack();
        }

        if (fakeAttackEventReceiver != null)
        {
            fakeAttackEventReceiver.ForceFinishAttack();
        }
    }

    void ForceIdle(Animator animator)
    {
        if (animator == null) return;

        SetSpeed(animator, 0f);

        if (!string.IsNullOrEmpty(idleStateName))
        {
            animator.Play(idleStateName, 0, 0f);
            animator.Update(0f);
        }
    }

    void StopBothRun()
    {
        SetSpeed(trueAnimator, 0f);
        SetSpeed(fakeAnimator, 0f);
    }

    void SetSpeed(Animator animator, float speed)
    {
        if (animator == null) return;
        if (string.IsNullOrEmpty(speedParameterName)) return;

        animator.SetFloat(speedParameterName, speed);
    }

    void FaceEachOther()
    {
        if (trueWukong == null || fakeWukong == null) return;

        FaceTarget(trueWukong, fakeWukong);
        FaceTarget(fakeWukong, trueWukong);
    }

    void FaceAwayFromEachOther()
    {
        if (trueWukong == null || fakeWukong == null) return;

        if (trueWukong.position.x < fakeWukong.position.x)
        {
            FaceDirection(trueWukong, -1f);
            FaceDirection(fakeWukong, 1f);
        }
        else
        {
            FaceDirection(trueWukong, 1f);
            FaceDirection(fakeWukong, -1f);
        }
    }

    void FaceMoveDirection(Transform actor, Vector3 targetPosition)
    {
        if (actor == null) return;

        float directionX = targetPosition.x - actor.position.x;
        FaceDirection(actor, directionX);
    }

    void FaceTarget(Transform actor, Transform target)
    {
        if (actor == null || target == null) return;

        float directionX = target.position.x - actor.position.x;
        FaceDirection(actor, directionX);
    }

    void FaceDirection(Transform actor, float directionX)
    {
        if (actor == null) return;
        if (Mathf.Abs(directionX) < 0.01f) return;

        bool shouldFaceRight = directionX > 0f;

        Vector3 scale = actor.localScale;
        float absX = Mathf.Abs(scale.x);

        if (spriteFacesRightByDefault)
        {
            scale.x = shouldFaceRight ? absX : -absX;
        }
        else
        {
            scale.x = shouldFaceRight ? -absX : absX;
        }

        actor.localScale = scale;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        if (trueWukong == null || fakeWukong == null) return;

        Vector3 truePos = trueWukong.position + Vector3.up * gizmoHeightOffset;
        Vector3 fakePos = fakeWukong.position + Vector3.up * gizmoHeightOffset;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(truePos, fakePos);

        Gizmos.DrawWireSphere(truePos, attackStartDistance * 0.5f);
        Gizmos.DrawWireSphere(fakePos, attackStartDistance * 0.5f);

        Vector3 middle = (truePos + fakePos) * 0.5f;
        Gizmos.DrawWireCube(middle, new Vector3(attackStartDistance, 0.25f, 0f));

        Gizmos.color = IsInAttackDistance() ? Color.green : Color.red;
        Gizmos.DrawSphere(middle, 0.15f);
    }
}