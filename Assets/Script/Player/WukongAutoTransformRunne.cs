using System.Collections;
using UnityEngine;

public class WukongAutoTransformRunner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Transform của Wukong.")]
    public Transform wukong;

    [Tooltip("Rigidbody2D của Wukong.")]
    public Rigidbody2D wukongRigidbody;

    [Tooltip("Animator của Wukong.")]
    public Animator wukongAnimator;

    [Tooltip("PlayerController của Wukong. Script này sẽ tắt khi cinematic bắt đầu.")]
    public PlayerController wukongController;

    [Tooltip("Map4StoryManager để đọc phase và báo kết thúc transition.")]
    public Map4StoryManager storyManager;

    [Header("Auto Start")]
    [Tooltip("Nếu bật, khi Map4StoryManager chuyển sang phase WukongTransform thì script tự chạy.")]
    public bool autoStartWhenWukongTransformPhase = true;

    [Header("Extra Scripts To Disable")]
    [Tooltip("Các script điều khiển khác của Wukong nếu có. Ví dụ input/move script phụ.")]
    public Behaviour[] extraScriptsToDisableDuringCinematic;

    [Header("Party Disable During Cinematic")]
    [Tooltip("Kéo thẳng object Đường Tăng, Bát Giới, Sa Tăng vào đây. Script sẽ tự tìm FollowerController, Rigidbody2D, Animator.")]
    public GameObject[] partyObjectsToStop;

    [Tooltip("Có đóng băng Rigidbody2D của đoàn thỉnh kinh không.")]
    public bool freezePartyPhysicsDuringCinematic = true;

    [Tooltip("Tên parameter Speed của animator đoàn thỉnh kinh.")]
    public string partySpeedParameterName = "Speed";

    [Tooltip("Tên state Idle của đoàn thỉnh kinh. Nếu mỗi nhân vật tên Idle khác nhau thì có thể để trống.")]
    public string partyIdleStateName = "Idle";

    [Header("Boss References")]
    [Tooltip("Boss3 / Thanh Sư Tinh.")]
    public Map4BossController boss3;

    [Tooltip("Boss4 / Bạch Tượng Tinh.")]
    public Map4BossController boss4;

    [Tooltip("Boss5 / Kim Sí Điểu. Nếu Boss5 không dùng Map4BossController thì có thể để trống.")]
    public MonoBehaviour boss5Controller;

    [Tooltip("Object Boss5 để quét vị trí và lấy collider.")]
    public GameObject boss5Object;

    [Header("Boss Scan")]
    [Tooltip("Layer chứa Boss3/Boss4/Boss5. Nên tạo layer Boss rồi gán boss vào layer này.")]
    public LayerMask bossLayer;

    [Tooltip("Bề ngang vùng quét quanh Wukong. Càng lớn thì Wukong chạy xa boss hơn mới transition.")]
    public float scanBoxWidth = 10f;

    [Tooltip("Chiều cao vùng quét quanh Wukong.")]
    public float scanBoxHeight = 6f;

    [Tooltip("Vị trí lệch của vùng quét so với Wukong.")]
    public Vector2 scanBoxOffset = new Vector2(0f, 1f);

    [Tooltip("Vùng quét phải sạch boss liên tục từng này giây thì mới được transition.")]
    public float safeStableTime = 0.3f;

    [Header("Auto Run")]
    [Tooltip("Tốc độ Wukong tự chạy khi tách khỏi boss.")]
    public float autoRunSpeed = 4f;

    [Tooltip("Nếu bị kẹt hoặc vẫn còn boss gần quá lâu thì ép dừng để tránh kẹt phase.")]
    public float maxRunTime = 6f;

    [Tooltip("Nếu Wukong đã chạy quá khoảng cách này thì ép dừng để tránh chạy quá xa.")]
    public float maxRunDistance = 14f;

    [Tooltip("Nếu vận tốc X quá nhỏ trong lúc đang chạy thì coi như bị chặn tường.")]
    public float stuckVelocityThreshold = 0.05f;

    [Tooltip("Thời gian bị chặn tường trước khi đổi hướng.")]
    public float stuckSwitchDirectionTime = 0.35f;

    [Header("Cinematic Collision")]
    [Tooltip("Tắt collider của boss trong lúc cinematic để Wukong không bị kẹt.")]
    public bool disableBossCollidersDuringCinematic = true;

    [Tooltip("Collider của Boss3 cần tắt. Có thể kéo collider thân boss vào đây.")]
    public Collider2D[] boss3Colliders;

    [Tooltip("Collider của Boss4 cần tắt. Có thể kéo collider thân boss vào đây.")]
    public Collider2D[] boss4Colliders;

    [Tooltip("Collider của Boss5 cần tắt. Có thể kéo collider thân boss vào đây.")]
    public Collider2D[] boss5Colliders;

    [Header("Boss Physics Freeze")]
    [Tooltip("Đóng băng vật lý Boss3/Boss4 trong cinematic để boss không bị rơi khi tắt collider.")]
    public bool freezeBossPhysicsDuringCinematic = true;

    [Tooltip("Rigidbody2D của Boss3. Có thể để trống, script sẽ tự tìm.")]
    public Rigidbody2D[] boss3Rigidbodies;

    [Tooltip("Rigidbody2D của Boss4. Có thể để trống, script sẽ tự tìm.")]
    public Rigidbody2D[] boss4Rigidbodies;

    [Header("Animator Parameters")]
    [Tooltip("Tên parameter Speed trong Animator của Wukong.")]
    public string speedParameterName = "Speed";

    [Tooltip("Tên state Idle thường của Wukong. Phải đúng 100% tên state trong Animator.")]
    public string idleStateName = "Wukong Idle";

    [Tooltip("Trigger chạy animation biến hình / mặc giáp.")]
    public string transitionTriggerName = "Transform";

    [Tooltip("Tên state Idle sau khi mặc giáp. Phải đúng 100% tên state trong Animator.")]
    public string armoredIdleStateName = "WukongIdle2";

    [Tooltip("Thời gian chờ sau khi bắn trigger Transition trước khi chuyển sang ArmoredIdle.")]
    public float transitionDuration = 2.5f;

    [Tooltip("Thời gian đứng Idle thường trước khi bắt đầu Transition.")]
    public float idleBeforeTransitionTime = 0.35f;

    [Tooltip("Thời gian đứng ArmoredIdle trước khi báo kết thúc map.")]
    public float armoredIdleHoldTime = 1.2f;

    [Header("Flip")]
    [Tooltip("Nếu dùng localScale để lật Wukong, bật cái này.")]
    public bool useLocalScaleFlip = true;

    [Tooltip("Nếu sprite gốc đang nhìn sang phải thì để true.")]
    public bool spriteFacesRightByDefault = true;

    [Header("Debug")]
    public bool enableDebugLog = true;
    public bool drawScanGizmo = true;

    private bool isRunning;
    private bool hasFinished;
    private bool hasAutoStartedFromPhase;

    private int moveDirection = -1;

    private float startX;
    private float runTimer;
    private float safeTimer;
    private float stuckTimer;

    private Collider2D[] cachedBoss3Colliders;
    private Collider2D[] cachedBoss4Colliders;
    private Collider2D[] cachedBoss5Colliders;

    private Rigidbody2D[] cachedBoss3Rigidbodies;
    private Rigidbody2D[] cachedBoss4Rigidbodies;

    void Awake()
    {
        AutoFindReferencesIfMissing();
        CacheBossColliders();
        CacheBossRigidbodies();
    }

    void Update()
    {
        CheckAutoStartByPhase();

        if (!isRunning) return;

        RunCinematicUpdate();
    }

    void CheckAutoStartByPhase()
    {
        if (!autoStartWhenWukongTransformPhase) return;
        if (hasAutoStartedFromPhase) return;
        if (isRunning) return;
        if (hasFinished) return;
        if (storyManager == null) return;

        if (storyManager.currentPhase == Map4StoryManager.Map4Phase.WukongTransform)
        {
            hasAutoStartedFromPhase = true;

            if (enableDebugLog)
            {
                Debug.Log("WukongAutoTransformRunner: Tự phát hiện phase WukongTransform, bắt đầu cinematic.");
            }

            StartAutoTransform();
        }
    }

    void AutoFindReferencesIfMissing()
    {
        if (wukong == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                wukong = playerObject.transform;
            }
            else
            {
                wukong = transform;
            }
        }

        if (wukongRigidbody == null && wukong != null)
        {
            wukongRigidbody = wukong.GetComponent<Rigidbody2D>();
        }

        if (wukongAnimator == null && wukong != null)
        {
            wukongAnimator = wukong.GetComponent<Animator>();

            if (wukongAnimator == null)
            {
                wukongAnimator = wukong.GetComponentInChildren<Animator>();
            }
        }

        if (wukongController == null && wukong != null)
        {
            wukongController = wukong.GetComponent<PlayerController>();

            if (wukongController == null)
            {
                wukongController = wukong.GetComponentInChildren<PlayerController>();
            }

            if (wukongController == null)
            {
                wukongController = wukong.GetComponentInParent<PlayerController>();
            }
        }

        if (storyManager == null)
        {
            storyManager = FindFirstObjectByType<Map4StoryManager>();
        }
    }

    void CacheBossColliders()
    {
        cachedBoss3Colliders = boss3Colliders;
        cachedBoss4Colliders = boss4Colliders;
        cachedBoss5Colliders = boss5Colliders;

        if ((cachedBoss3Colliders == null || cachedBoss3Colliders.Length == 0) && boss3 != null)
        {
            cachedBoss3Colliders = boss3.GetComponentsInChildren<Collider2D>(true);
        }

        if ((cachedBoss4Colliders == null || cachedBoss4Colliders.Length == 0) && boss4 != null)
        {
            cachedBoss4Colliders = boss4.GetComponentsInChildren<Collider2D>(true);
        }

        if ((cachedBoss5Colliders == null || cachedBoss5Colliders.Length == 0) && boss5Object != null)
        {
            cachedBoss5Colliders = boss5Object.GetComponentsInChildren<Collider2D>(true);
        }
    }

    void CacheBossRigidbodies()
    {
        cachedBoss3Rigidbodies = boss3Rigidbodies;
        cachedBoss4Rigidbodies = boss4Rigidbodies;

        if ((cachedBoss3Rigidbodies == null || cachedBoss3Rigidbodies.Length == 0) && boss3 != null)
        {
            cachedBoss3Rigidbodies = boss3.GetComponentsInChildren<Rigidbody2D>(true);
        }

        if ((cachedBoss4Rigidbodies == null || cachedBoss4Rigidbodies.Length == 0) && boss4 != null)
        {
            cachedBoss4Rigidbodies = boss4.GetComponentsInChildren<Rigidbody2D>(true);
        }
    }

    public void StartAutoTransform()
    {
        if (isRunning || hasFinished) return;

        AutoFindReferencesIfMissing();
        CacheBossColliders();
        CacheBossRigidbodies();

        if (wukong == null || wukongRigidbody == null)
        {
            Debug.LogWarning("WukongAutoTransformRunner thiếu Wukong hoặc Rigidbody2D.");
            return;
        }

        hasFinished = false;
        isRunning = true;

        startX = wukong.position.x;
        runTimer = 0f;
        safeTimer = 0f;
        stuckTimer = 0f;

        PrepareCinematicMode();

        moveDirection = ChooseDirectionAwayFromBosses();
        FaceMoveDirection();

        if (enableDebugLog)
        {
            Debug.Log("WukongAutoTransformRunner: Bắt đầu auto tách khỏi boss. Direction = " + moveDirection);
        }
    }

    void PrepareCinematicMode()
    {
        DisableWukongControl();
        DisablePartyMovement();

        if (boss3 != null)
        {
            boss3.StopCombatAndReturnIdle();
        }

        if (boss4 != null)
        {
            boss4.StopCombatAndReturnIdle();
        }

        if (freezeBossPhysicsDuringCinematic)
        {
            FreezeBossRigidbodies(cachedBoss3Rigidbodies);
            FreezeBossRigidbodies(cachedBoss4Rigidbodies);
        }

        if (disableBossCollidersDuringCinematic)
        {
            SetCollidersEnabled(cachedBoss3Colliders, false);
            SetCollidersEnabled(cachedBoss4Colliders, false);
            SetCollidersEnabled(cachedBoss5Colliders, false);
        }

        ForceWukongRun();
    }

    void DisableWukongControl()
    {
        if (wukongController != null)
        {
            wukongController.enabled = false;

            if (enableDebugLog)
            {
                Debug.Log("WukongAutoTransformRunner: Đã tắt PlayerController của Wukong.");
            }
        }
        else
        {
            Debug.LogWarning("WukongAutoTransformRunner: Chưa gán được PlayerController nên Wukong vẫn có thể nhận input.");
        }

        if (extraScriptsToDisableDuringCinematic != null)
        {
            for (int i = 0; i < extraScriptsToDisableDuringCinematic.Length; i++)
            {
                if (extraScriptsToDisableDuringCinematic[i] != null)
                {
                    extraScriptsToDisableDuringCinematic[i].enabled = false;
                }
            }
        }
    }

    void DisablePartyMovement()
    {
        if (partyObjectsToStop == null) return;

        for (int i = 0; i < partyObjectsToStop.Length; i++)
        {
            GameObject partyObject = partyObjectsToStop[i];

            if (partyObject == null) continue;

            FollowerController followerController = partyObject.GetComponent<FollowerController>();

            if (followerController != null)
            {
                followerController.enabled = false;
            }

            Rigidbody2D rb = partyObject.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;

                if (freezePartyPhysicsDuringCinematic)
                {
                    rb.gravityScale = 0f;
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.constraints = RigidbodyConstraints2D.FreezeAll;
                }
            }

            Animator animator = partyObject.GetComponent<Animator>();

            if (animator == null)
            {
                animator = partyObject.GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                if (!string.IsNullOrEmpty(partySpeedParameterName))
                {
                    animator.SetFloat(partySpeedParameterName, 0f);
                }

                if (!string.IsNullOrEmpty(partyIdleStateName))
                {
                    animator.Play(partyIdleStateName, 0, 0f);
                }
            }
        }

        if (enableDebugLog)
        {
            Debug.Log("WukongAutoTransformRunner: Đã tắt di chuyển đoàn thỉnh kinh trong cinematic.");
        }
    }

    void RunCinematicUpdate()
    {
        runTimer += Time.deltaTime;

        bool hasBossNearby = HasBossNearby();

        if (!hasBossNearby)
        {
            safeTimer += Time.deltaTime;

            if (safeTimer >= safeStableTime)
            {
                StartCoroutine(StopAndTransformRoutine());
                return;
            }
        }
        else
        {
            safeTimer = 0f;

            int newDirection = ChooseDirectionAwayFromBosses();

            if (newDirection != moveDirection)
            {
                moveDirection = newDirection;
                FaceMoveDirection();
            }
        }

        CheckStuckAndMaybeSwitchDirection();

        MoveWukong();

        float movedDistance = Mathf.Abs(wukong.position.x - startX);

        if (runTimer >= maxRunTime || movedDistance >= maxRunDistance)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("WukongAutoTransformRunner: Đạt giới hạn an toàn, ép dừng để transition.");
            }

            StartCoroutine(StopAndTransformRoutine());
        }
    }

    void MoveWukong()
    {
        if (wukongRigidbody == null) return;

        Vector2 velocity = wukongRigidbody.linearVelocity;
        velocity.x = moveDirection * autoRunSpeed;
        wukongRigidbody.linearVelocity = velocity;

        ForceWukongRun();
    }

    void CheckStuckAndMaybeSwitchDirection()
    {
        if (wukongRigidbody == null) return;

        float velocityX = Mathf.Abs(wukongRigidbody.linearVelocity.x);

        if (velocityX <= stuckVelocityThreshold)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer >= stuckSwitchDirectionTime)
            {
                moveDirection *= -1;
                stuckTimer = 0f;
                FaceMoveDirection();

                if (enableDebugLog)
                {
                    Debug.LogWarning("WukongAutoTransformRunner: Có vẻ bị chặn mép map, đổi hướng chạy.");
                }
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    int ChooseDirectionAwayFromBosses()
    {
        float bossLeftScore = 0f;
        float bossRightScore = 0f;

        AddBossSideScore(boss3 != null ? boss3.transform : null, ref bossLeftScore, ref bossRightScore);
        AddBossSideScore(boss4 != null ? boss4.transform : null, ref bossLeftScore, ref bossRightScore);

        if (boss5Object != null)
        {
            AddBossSideScore(boss5Object.transform, ref bossLeftScore, ref bossRightScore);
        }
        else if (boss5Controller != null)
        {
            AddBossSideScore(boss5Controller.transform, ref bossLeftScore, ref bossRightScore);
        }

        if (bossRightScore > bossLeftScore)
        {
            return -1;
        }

        if (bossLeftScore > bossRightScore)
        {
            return 1;
        }

        return -1;
    }

    void AddBossSideScore(Transform bossTransform, ref float bossLeftScore, ref float bossRightScore)
    {
        if (bossTransform == null || wukong == null) return;

        float deltaX = bossTransform.position.x - wukong.position.x;
        float distance = Mathf.Abs(deltaX);

        float score = 1f / Mathf.Max(distance, 0.2f);

        if (deltaX >= 0f)
        {
            bossRightScore += score;
        }
        else
        {
            bossLeftScore += score;
        }
    }

    bool HasBossNearby()
    {
        Vector2 center = GetScanCenter();
        Vector2 size = new Vector2(scanBoxWidth, scanBoxHeight);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, bossLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null) continue;
            if (!hit.gameObject.activeInHierarchy) continue;

            return true;
        }

        return false;
    }

    Vector2 GetScanCenter()
    {
        if (wukong == null)
        {
            return transform.position;
        }

        return new Vector2(
            wukong.position.x + scanBoxOffset.x,
            wukong.position.y + scanBoxOffset.y
        );
    }

    IEnumerator StopAndTransformRoutine()
    {
        if (!isRunning) yield break;

        isRunning = false;
        hasFinished = true;

        StopWukongMovement();
        ForceWukongIdle();

        if (enableDebugLog)
        {
            Debug.Log("WukongAutoTransformRunner: Đã tách khỏi boss, chuẩn bị Transition.");
        }

        yield return new WaitForSeconds(idleBeforeTransitionTime);

        PlayTransitionAnimation();

        yield return new WaitForSeconds(transitionDuration);

        PlayArmoredIdle();

        yield return new WaitForSeconds(armoredIdleHoldTime);

        FinishTransform();
    }

    void StopWukongMovement()
    {
        if (wukongRigidbody != null)
        {
            wukongRigidbody.linearVelocity = Vector2.zero;
        }

        if (wukongAnimator != null && !string.IsNullOrEmpty(speedParameterName))
        {
            wukongAnimator.SetFloat(speedParameterName, 0f);
        }
    }

    void ForceWukongRun()
    {
        if (wukongAnimator == null) return;

        if (!string.IsNullOrEmpty(speedParameterName))
        {
            wukongAnimator.SetFloat(speedParameterName, Mathf.Abs(autoRunSpeed));
        }
    }

    void ForceWukongIdle()
    {
        if (wukongAnimator == null) return;

        if (!string.IsNullOrEmpty(speedParameterName))
        {
            wukongAnimator.SetFloat(speedParameterName, 0f);
        }

        if (!string.IsNullOrEmpty(idleStateName))
        {
            wukongAnimator.Play(idleStateName, 0, 0f);
        }
    }

    void PlayTransitionAnimation()
    {
        if (wukongAnimator == null) return;

        if (!string.IsNullOrEmpty(transitionTriggerName))
        {
            wukongAnimator.ResetTrigger(transitionTriggerName);
            wukongAnimator.SetTrigger(transitionTriggerName);
        }
    }

    void PlayArmoredIdle()
    {
        if (wukongAnimator == null) return;

        if (!string.IsNullOrEmpty(speedParameterName))
        {
            wukongAnimator.SetFloat(speedParameterName, 0f);
        }

        if (!string.IsNullOrEmpty(armoredIdleStateName))
        {
            wukongAnimator.Play(armoredIdleStateName, 0, 0f);
        }
    }

    void FinishTransform()
    {
        if (enableDebugLog)
        {
            Debug.Log("WukongAutoTransformRunner: Hoàn tất transition Wukong.");
        }

        if (storyManager != null)
        {
            storyManager.FinishWukongTransformAndEndMap();
        }
        else
        {
            Debug.LogWarning("WukongAutoTransformRunner chưa gán StoryManager nên không thể báo EndMap.");
        }
    }

    void FaceMoveDirection()
    {
        if (!useLocalScaleFlip) return;
        if (wukong == null) return;

        Vector3 scale = wukong.localScale;
        float absX = Mathf.Abs(scale.x);

        if (spriteFacesRightByDefault)
        {
            scale.x = moveDirection > 0 ? absX : -absX;
        }
        else
        {
            scale.x = moveDirection > 0 ? -absX : absX;
        }

        wukong.localScale = scale;
    }

    void SetCollidersEnabled(Collider2D[] colliders, bool enabled)
    {
        if (colliders == null) return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = enabled;
            }
        }
    }

    void FreezeBossRigidbodies(Rigidbody2D[] rigidbodies)
    {
        if (rigidbodies == null) return;

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody2D rb = rigidbodies[i];

            if (rb == null) continue;

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawScanGizmo) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(GetScanCenter(), new Vector3(scanBoxWidth, scanBoxHeight, 0f));
    }
}