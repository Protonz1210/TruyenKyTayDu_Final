using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Map5BuddhaInterventionController : MonoBehaviour
{
    [Header("Buddha")]
    [Tooltip("Object Phật Tổ. Có thể là NPC5_Buddha.")]
    public GameObject buddhaObject;

    [Tooltip("Animator của Phật Tổ. Nếu chưa có animation đưa tay thì có thể để trống.")]
    public Animator buddhaAnimator;

    [Tooltip("Tên Trigger animation Phật Tổ đưa tay. Nếu Animator chưa có trigger này thì để trống.")]
    public string buddhaHandTriggerName = "Hand";

    [Tooltip("Thời gian chờ sau khi Phật Tổ bắt đầu đưa tay rồi mới hiện bát.")]
    public float delayBeforeBowlAppear = 0.8f;

    [Header("Buddha Bowl")]
    [Tooltip("Object bát Phật Tổ sẽ hiện ra.")]
    public GameObject buddhaBowl;

    [Tooltip("Điểm spawn/điểm đặt bát.")]
    public Transform buddhaBowlSpawnPoint;

    [Tooltip("Bật: khi hiện bát sẽ đặt bát đúng vị trí BuddhaBowlSpawnPoint.")]
    public bool moveBowlToSpawnPoint = true;

    [Tooltip("Bật: khi hiện bát sẽ xoay bát theo rotation của BuddhaBowlSpawnPoint.")]
    public bool useSpawnPointRotation = false;

    [Header("Buddha Bowl Auto Hide")]
    [Tooltip("Bật: sau khi bát hiện ra, chờ Buddha Bowl Visible Duration rồi tự tắt bát.")]
    public bool hideBuddhaBowlAfterDuration = true;

    [Tooltip("Thời gian bát được hiện trên màn hình tính từ lúc ShowBuddhaBowl chạy.")]
    public float buddhaBowlVisibleDuration = 2f;

    [Tooltip("Bật: sau khi FakeWukong chết xong và biến mất thì tắt bát. Nếu bật cùng Hide After Duration, điều kiện nào tới trước/tới sau đều có thể tắt bát, không ảnh hưởng flow.")]
    public bool hideBowlAfterFakeWukongDieFinished = false;

    [Tooltip("Chờ thêm sau khi FakeWukong biến mất rồi mới tắt bát.")]
    public float extraDelayBeforeHideBowlAfterFakeDie = 0.1f;

    [Header("Fake Wukong Die")]
    [Tooltip("Object FakeWukong.")]
    public GameObject fakeWukongObject;

    [Tooltip("Animator của FakeWukong.")]
    public Animator fakeWukongAnimator;

    [Tooltip("Tên Trigger Die trong Animator của FakeWukong.")]
    public string fakeDieTriggerName = "Die";

    [Tooltip("Tên state animation Die trong Animator của FakeWukong. Phải đúng tên state, ví dụ: Wukong2Die hoặc Die.")]
    public string fakeDieStateName = "Wukong2Die";

    [Tooltip("Thời gian chờ sau khi bát hiện ra rồi FakeWukong mới Die.")]
    public float delayBeforeFakeDie = 0.35f;

    [Header("Hide Fake Wukong After Die")]
    [Tooltip("Bật: FakeWukong chạy hết animation Die rồi biến mất.")]
    public bool hideFakeWukongAfterDie = true;

    [Tooltip("Bật: chờ Animator vào đúng state Die rồi normalizedTime chạy hết mới ẩn.")]
    public bool waitDieAnimationByStateName = true;

    [Tooltip("Nếu tên state Die không đúng hoặc Animation Event/Animator bị lỗi, sau thời gian này vẫn ép ẩn để tránh kẹt flow.")]
    public float maxWaitFakeDieAnimation = 4f;

    [Tooltip("Ngưỡng coi là animation Die đã chạy xong. 0.95 nghĩa là chạy khoảng 95% clip thì cho ẩn.")]
    [Range(0.5f, 1.2f)]
    public float fakeDieNormalizedEndThreshold = 0.98f;

    [Tooltip("Nếu không chờ bằng state name, code sẽ chờ thời gian này rồi ẩn FakeWukong.")]
    public float fakeDieFallbackWaitTime = 1.2f;

    [Tooltip("Chờ thêm một chút sau khi Die xong rồi mới ẩn. Để 0 nếu muốn biến mất ngay frame cuối.")]
    public float extraDelayAfterDieFinished = 0f;

    [Tooltip("Bật: SetActive(false) cả FakeWukong sau khi Die xong.")]
    public bool setFakeWukongInactiveAfterDie = true;

    [Tooltip("Bật: tắt Animator của FakeWukong trước khi ẩn để tránh bị kéo về Idle.")]
    public bool disableFakeAnimatorAfterDie = true;

    [Tooltip("Bật: tắt toàn bộ Collider2D của FakeWukong sau khi Die xong.")]
    public bool disableFakeCollidersAfterDie = true;

    [Tooltip("Bật: tắt toàn bộ Renderer của FakeWukong sau khi Die xong.")]
    public bool disableFakeRenderersAfterDie = true;

    [Header("Test")]
    [Tooltip("Bật để bấm phím B test riêng Phật Tổ can thiệp.")]
    public bool enableTestKey = true;

    [Header("State")]
    [Tooltip("Đang chạy đoạn Phật Tổ can thiệp hay không.")]
    public bool isInterventionRunning;

    private Action onFinishedCallback;
    private Coroutine bowlHideCoroutine;

    private void Start()
    {
        CacheReferences();
        HideBowlAtStart();
    }

    private void Update()
    {
        if (!enableTestKey)
        {
            return;
        }

        if (isInterventionRunning)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            PlayInterventionOnce(() =>
            {
                Debug.Log("[Map5BuddhaInterventionController] Test intervention finished.");
            });
        }
    }

    public void PlayInterventionOnce(Action onFinished = null)
    {
        if (isInterventionRunning)
        {
            Debug.LogWarning("[Map5BuddhaInterventionController] Intervention đang chạy, không gọi lại.");
            return;
        }

        onFinishedCallback = onFinished;
        StartCoroutine(InterventionRoutine());
    }

    private IEnumerator InterventionRoutine()
    {
        isInterventionRunning = true;

        PlayBuddhaHandAnimation();

        yield return new WaitForSeconds(delayBeforeBowlAppear);

        ShowBuddhaBowl();
        StartBowlAutoHideTimerIfNeeded();

        yield return new WaitForSeconds(delayBeforeFakeDie);

        PlayFakeWukongDie();

        if (hideFakeWukongAfterDie)
        {
            yield return StartCoroutine(WaitFakeDieThenHideRoutine());
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (hideBowlAfterFakeWukongDieFinished)
        {
            if (bowlHideCoroutine != null)
            {
                StopCoroutine(bowlHideCoroutine);
                bowlHideCoroutine = null;
            }

            if (extraDelayBeforeHideBowlAfterFakeDie > 0f)
            {
                yield return new WaitForSeconds(extraDelayBeforeHideBowlAfterFakeDie);
            }

            HideBuddhaBowl();
        }

        isInterventionRunning = false;

        Action callback = onFinishedCallback;
        onFinishedCallback = null;
        callback?.Invoke();
    }

    private void CacheReferences()
    {
        if (buddhaAnimator == null && buddhaObject != null)
        {
            buddhaAnimator = buddhaObject.GetComponent<Animator>();
        }

        if (fakeWukongAnimator == null && fakeWukongObject != null)
        {
            fakeWukongAnimator = fakeWukongObject.GetComponent<Animator>();
        }
    }

    private void HideBowlAtStart()
    {
        if (bowlHideCoroutine != null)
        {
            StopCoroutine(bowlHideCoroutine);
            bowlHideCoroutine = null;
        }

        if (buddhaBowl != null)
        {
            buddhaBowl.SetActive(false);
        }
    }

    private void PlayBuddhaHandAnimation()
    {
        if (buddhaAnimator == null)
        {
            Debug.LogWarning("[Map5BuddhaInterventionController] Chưa có Buddha Animator. Bỏ qua animation đưa tay.");
            return;
        }

        if (string.IsNullOrEmpty(buddhaHandTriggerName))
        {
            Debug.LogWarning("[Map5BuddhaInterventionController] Chưa nhập Buddha Hand Trigger Name. Bỏ qua animation đưa tay.");
            return;
        }

        buddhaAnimator.ResetTrigger(buddhaHandTriggerName);
        buddhaAnimator.SetTrigger(buddhaHandTriggerName);
    }

    private void ShowBuddhaBowl()
    {
        if (buddhaBowl == null)
        {
            Debug.LogError("[Map5BuddhaInterventionController] Chưa gán BuddhaBowl.");
            return;
        }

        if (moveBowlToSpawnPoint && buddhaBowlSpawnPoint != null)
        {
            buddhaBowl.transform.position = buddhaBowlSpawnPoint.position;

            if (useSpawnPointRotation)
            {
                buddhaBowl.transform.rotation = buddhaBowlSpawnPoint.rotation;
            }
        }

        buddhaBowl.SetActive(true);

        Debug.Log("[Map5BuddhaInterventionController] BuddhaBowl appeared.");
    }

    private void StartBowlAutoHideTimerIfNeeded()
    {
        if (!hideBuddhaBowlAfterDuration)
        {
            return;
        }

        if (buddhaBowl == null)
        {
            return;
        }

        if (bowlHideCoroutine != null)
        {
            StopCoroutine(bowlHideCoroutine);
            bowlHideCoroutine = null;
        }

        bowlHideCoroutine = StartCoroutine(HideBuddhaBowlAfterDurationRoutine());
    }

    private IEnumerator HideBuddhaBowlAfterDurationRoutine()
    {
        if (buddhaBowlVisibleDuration > 0f)
        {
            yield return new WaitForSeconds(buddhaBowlVisibleDuration);
        }

        HideBuddhaBowl();
        bowlHideCoroutine = null;
    }

    private void HideBuddhaBowl()
    {
        if (buddhaBowl != null)
        {
            buddhaBowl.SetActive(false);
            Debug.Log("[Map5BuddhaInterventionController] BuddhaBowl hidden.");
        }
    }

    private void PlayFakeWukongDie()
    {
        CacheReferences();

        if (fakeWukongAnimator == null)
        {
            Debug.LogError("[Map5BuddhaInterventionController] Chưa gán Animator của FakeWukong.");
            return;
        }

        if (string.IsNullOrEmpty(fakeDieTriggerName))
        {
            Debug.LogError("[Map5BuddhaInterventionController] Fake Die Trigger Name đang trống.");
            return;
        }

        fakeWukongAnimator.ResetTrigger(fakeDieTriggerName);
        fakeWukongAnimator.SetTrigger(fakeDieTriggerName);

        Debug.Log("[Map5BuddhaInterventionController] FakeWukong Die triggered.");
    }

    private IEnumerator WaitFakeDieThenHideRoutine()
    {
        if (fakeWukongObject == null)
        {
            Debug.LogWarning("[Map5BuddhaInterventionController] FakeWukong Object đang trống, không thể ẩn FakeWukong.");
            yield break;
        }

        if (fakeWukongAnimator == null)
        {
            HideFakeWukongAfterDieAnimation();
            yield break;
        }

        if (!waitDieAnimationByStateName)
        {
            if (fakeDieFallbackWaitTime > 0f)
            {
                yield return new WaitForSeconds(fakeDieFallbackWaitTime);
            }

            if (extraDelayAfterDieFinished > 0f)
            {
                yield return new WaitForSeconds(extraDelayAfterDieFinished);
            }

            HideFakeWukongAfterDieAnimation();
            yield break;
        }

        float timer = 0f;
        bool enteredDieState = false;

        // Chờ ít nhất 1 frame để Animator nhận Trigger Die.
        yield return null;

        while (timer < maxWaitFakeDieAnimation)
        {
            AnimatorStateInfo stateInfo = fakeWukongAnimator.GetCurrentAnimatorStateInfo(0);

            bool isInDieState = IsAnimatorInDieState(stateInfo);

            if (isInDieState)
            {
                enteredDieState = true;

                if (!fakeWukongAnimator.IsInTransition(0) &&
                    stateInfo.normalizedTime >= fakeDieNormalizedEndThreshold)
                {
                    break;
                }
            }
            else if (enteredDieState)
            {
                // Đã từng vào Die rồi rời state, coi như Die đã kết thúc.
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (timer >= maxWaitFakeDieAnimation)
        {
            Debug.LogWarning("[Map5BuddhaInterventionController] Chờ FakeWukong Die quá lâu. Kiểm tra Fake Die State Name hoặc animation Die có loop không.");
        }

        if (extraDelayAfterDieFinished > 0f)
        {
            yield return new WaitForSeconds(extraDelayAfterDieFinished);
        }

        HideFakeWukongAfterDieAnimation();
    }

    private bool IsAnimatorInDieState(AnimatorStateInfo stateInfo)
    {
        if (string.IsNullOrEmpty(fakeDieStateName))
        {
            return true;
        }

        if (stateInfo.IsName(fakeDieStateName))
        {
            return true;
        }

        int shortNameHash = Animator.StringToHash(fakeDieStateName);

        if (stateInfo.shortNameHash == shortNameHash)
        {
            return true;
        }

        return false;
    }

    private void HideFakeWukongAfterDieAnimation()
    {
        if (fakeWukongObject == null)
        {
            return;
        }

        Collider2D[] colliders = fakeWukongObject.GetComponentsInChildren<Collider2D>(true);
        Renderer[] renderers = fakeWukongObject.GetComponentsInChildren<Renderer>(true);

        if (disableFakeCollidersAfterDie)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = false;
                }
            }
        }

        if (disableFakeRenderersAfterDie)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = false;
                }
            }
        }

        if (disableFakeAnimatorAfterDie && fakeWukongAnimator != null)
        {
            fakeWukongAnimator.enabled = false;
        }

        if (setFakeWukongInactiveAfterDie)
        {
            fakeWukongObject.SetActive(false);
        }

        Debug.Log("[Map5BuddhaInterventionController] FakeWukong đã biến mất sau animation Die.");
    }
}
