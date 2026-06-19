using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Map4StoryManager : MonoBehaviour
{
    public enum Map4Phase
    {
        StartMap,
        Enemy4IntroDialogue,
        Enemy4Combat,
        Enemy4Defeated,
        BossIntroDialogue,
        NormalEnemyWave,
        BeforeBossDialogue,
        BossFight,
        Boss5Appear,
        Boss5StoryDialogue,
        WukongTransform,
        EndMap
    }

    [Header("Current Phase")]
    public Map4Phase currentPhase = Map4Phase.StartMap;

    [Header("Player / Party")]
    public PlayerController wukongController;
    public Rigidbody2D wukongRigidbody;
    public Animator wukongAnimator;

    [Tooltip("Các script đi theo của Đường Tăng / Trư Bát Giới / Sa Tăng.")]
    public Behaviour[] partyFollowScripts;

    [Tooltip("Animator của Đường Tăng / Trư Bát Giới / Sa Tăng.")]
    public Animator[] partyAnimators;

    [Header("Game Over")]
    [Tooltip("UI GameOver riêng của Map4. Kéo object OVER có GameOverMenuController vào đây.")]
    public GameOverMenuController gameOverMenuController;

    [Tooltip("Bật lên để khi Wukong chết xong animation ở phase thường thì hiện GameOver.")]
    public bool gameOverWhenWukongDead = true;

    [Tooltip("Bật lên để khi máu đoàn thỉnh kinh về 0 thì hiện GameOver.")]
    public bool gameOverWhenPartyDead = true;

    [Tooltip("Nếu Wukong chết trong phase Boss5Appear / Boss5StoryDialogue thì không GameOver, vì đó là cốt truyện.")]
    public bool ignoreWukongGameOverDuringBoss5Story = true;

    [Header("Party Health Notify")]
    [Tooltip("Script máu tổng của đoàn thỉnh kinh. Kéo PartyManager có PartyHealth vào đây.")]
    public PartyHealth partyHealth;

    [Tooltip("Nếu chưa gán PartyHealth, tự tìm PartyHealth trong scene.")]
    public bool autoFindPartyHealthIfMissing = true;

    private bool gameOverStarted;

    [Header("Gameplay UI Elements To Hide")]
    [Tooltip("UIDocument chứa UI tổng Map4. Không kéo object box thoại riêng vào đây.")]
    public UIDocument gameplayUIDocument;

    [Tooltip("Tên các VisualElement cần ẩn khi vào đoạn Boss5 story. Không nhập tên box thoại.")]
    public string[] gameplayElementNamesToHide =
    {
        "wukong-health-group",
        "party-health-group",
        "skill-cooldown-group"
    };

    [Tooltip("Tắt các element máu/kỹ năng khi Wukong hết máu ở phase Boss5.")]
    public bool hideGameplayElementsWhenBoss5StoryStart = true;

    [Header("Wukong Animator")]
    [Tooltip("Idle cũ của Wukong. Chỉ dùng cho các đoạn khóa bình thường, không dùng ở đoạn transform cuối map.")]
    public string wukongIdleStateName = "Wukong1Idle";

    public string wukongSpeedParameterName = "Speed";
    public string[] wukongBoolParametersToFalse;
    public string[] wukongTriggersToReset;

    [Header("Wukong Transform Animator")]
    [Tooltip("Tên trigger trong Animator để chạy transition biến hình. Theo Animator của bạn là Transform.")]
    public string wukongTransformTriggerName = "Transform";

    [Tooltip("Tên state animation transition biến hình.")]
    public string wukongTransformStateName = "Wukong1_transition";

    [Tooltip("Tên state Idle mới sau khi biến hình. Theo Animator của bạn là WukongIdle2.")]
    public string wukongTransformIdleStateName = "WukongIdle2";

    [Tooltip("Thời gian chờ tối đa để Wukong vào Idle2. Quá thời gian sẽ ép sang Idle2 để tránh kẹt.")]
    public float maxWaitWukongTransformIdleTime = 5f;

    [Tooltip("Khi Wukong vừa vào Idle2 thì chuyển map luôn.")]
    public bool loadSceneImmediatelyWhenIdle2Reached = true;

    [Header("Party Animator")]
    public string partyIdleStateName = "Idle";
    public string partySpeedParameterName = "Speed";
    public string[] partyBoolParametersToFalse;
    public string[] partyTriggersToReset;

    [Header("Start Location Intro")]
    [Tooltip("Bật intro địa danh khi mới vào Map 4.")]
    public bool playLocationIntroOnStart = true;

    [Tooltip("Chờ một khoảng ngắn sau khi load map rồi mới khóa. Làm giống cơ chế ổn định của Map2.")]
    public float waitBeforeLockTime = 0.25f;

    [Tooltip("Sau khi khóa di chuyển, chờ thêm một chút cho animation về Idle.")]
    public float waitIdleAfterLockTime = 0.25f;

    [Tooltip("Sau intro có mở lại di chuyển không.")]
    public bool restoreMovementAfterIntro = true;

    [Header("Location Title UI")]
    [Tooltip("HUD tổng của Map 4. Kéo object chứa Map4BossHUDController vào đây.")]
    public Map4BossHUDController mapHUDController;

    [Tooltip("Tên box chứa bảng địa danh trong UI Builder.")]
    public string locationBoxName = "Box_mask";

    [Tooltip("Tên text địa danh trong UI Builder.")]
    public string locationTextName = "Box_text";

    [TextArea(2, 5)]
    [Tooltip("Nội dung địa danh đầu map. Có thể xuống dòng.")]
    public string locationTitleText = "SƯ\nĐÀ\nLĨNH";

    [Tooltip("Thời gian fade in bảng địa danh.")]
    public float locationFadeInTime = 1f;

    [Tooltip("Thời gian giữ bảng địa danh.")]
    public float locationHoldTime = 2f;

    [Tooltip("Thời gian fade out bảng địa danh.")]
    public float locationFadeOutTime = 1f;

    [Tooltip("Nếu bật, Map4StoryManager sẽ ghi đè setting Location Title UI sang Map4BossHUDController khi chạy intro.")]
    public bool overrideHUDLocationSettings = true;

    private bool hasPlayedStartLocationIntro;

    [Header("Dialogue")]
    public DialogueController dialogueController;

    [Tooltip("Hội thoại khi gặp Enemy4 / Tiểu Tuần Phong.")]
    public DialogueLine[] enemy4IntroLines;

    [Tooltip("Hội thoại khi gặp Boss3/Boss4.")]
    public DialogueLine[] bossIntroLines;

    [Tooltip("Hội thoại sau khi diệt hết quái thường, trước khi đánh Boss3/Boss4.")]
    public DialogueLine[] beforeBossFightLines;

    [Tooltip("Hội thoại khi Boss5 xuất hiện.")]
    public DialogueLine[] boss5StoryLines;

    [Header("Dialogue TXT Import")]
    [Tooltip("TXT import vào Enemy 4 Intro Lines. Định dạng: TÊN|Nội dung thoại hoặc TÊN: Nội dung thoại.")]
    public TextAsset enemy4IntroTxtFile;

    [Tooltip("TXT import vào Boss Intro Lines. Định dạng: TÊN|Nội dung thoại hoặc TÊN: Nội dung thoại.")]
    public TextAsset bossIntroTxtFile;

    [Tooltip("TXT import vào Before Boss Fight Lines. Định dạng: TÊN|Nội dung thoại hoặc TÊN: Nội dung thoại.")]
    public TextAsset beforeBossFightTxtFile;

    [Tooltip("TXT import vào Boss 5 Story Lines. Định dạng: TÊN|Nội dung thoại hoặc TÊN: Nội dung thoại.")]
    public TextAsset boss5StoryTxtFile;

    [Tooltip("Khi import TXT, tự giữ avatar cũ theo tên nhân vật trong từng nhóm thoại.")]
    public bool keepCurrentAvatarsWhenImport = true;

    [Header("Enemy4")]
    [Tooltip("Script Enemy4Controller / Enemy4 chính trong scene.")]
    public MonoBehaviour enemy4;

    [Tooltip("Object gốc của Enemy4. Nếu để trống, hệ thống sẽ lấy từ enemy4.")]
    public GameObject enemy4Object;

    [Tooltip("Tự chuyển phase Enemy4Defeated khi Enemy4 bị Destroy hoặc SetActive(false).")]
    public bool autoDetectEnemy4Dead = true;

    [Header("Enemy123 Wave")]
    public Enemy123RandomSpawner enemy123Spawner;

    [Header("Before Boss Dialogue Wait Idle")]
    [Tooltip("Sau khi diệt hết Enemy123, đợi Wukong tự đánh hết chiêu và về Idle rồi mới mở thoại.")]
    public bool waitWukongIdleBeforeBossDialogue = true;

    [Tooltip("Tên state Idle thật của Wukong trong Animator.")]
    public string wukongIdleStateNameForDialogueWait = "Wukong1Idle";

    [Tooltip("Tốc độ Rigidbody nhỏ hơn số này thì coi như Wukong đã đứng yên.")]
    public float wukongIdleVelocityThreshold = 0.05f;

    [Tooltip("Wukong phải đứng Idle liên tục bao lâu mới mở thoại.")]
    public float wukongIdleStableTime = 0.25f;

    [Tooltip("Thời gian chờ tối đa. Nếu quá thời gian này vẫn chưa Idle thì mới ép mở thoại để tránh kẹt phase.")]
    public float maxWaitWukongIdleTime = 5f;

    private bool waitingBeforeBossDialogue;

    [Header("Boss 3 / Boss 4")]
    public Map4BossController boss3;
    public Map4BossController boss4;

    [Header("Boss5")]
    public GameObject boss5Object;
    public Transform boss5SpawnPoint;

    [Header("Boss5 Story Trigger")]
    [Tooltip("Sau khi Boss5 xuất hiện, chờ Wukong chết rồi mới hiện thoại Boss5.")]
    public bool waitWukongDeathBeforeBoss5Story = true;

    [Tooltip("Delay nhỏ sau khi Wukong chết rồi mới mở thoại Boss5.")]
    public float boss5StoryDelayAfterWukongDeath = 0.6f;

    private bool waitingForWukongDeathAfterBoss5Appear;
    private bool boss5StoryStarted;

    [Range(0f, 1f)]
    public float boss5AppearHealthPercent = 0.33f;

    [Header("Debug")]
    public bool enableDebugLog = true;

    [Header("Scene Transition")]
    [Tooltip("Controller fade đen chuyển cảnh của Map4.")]
    public Map4SceneFadeController sceneFadeController;

    [Tooltip("Tên scene tiếp theo sau Map4. Ví dụ: Map5 hoặc MAP 5.1.")]
    public string nextSceneName = "Map5";

    [Tooltip("Tự chuyển scene khi gọi StartEndMapByTrigger hoặc FinishWukongTransformAndEndMap.")]
    public bool autoLoadNextSceneWhenEndMap = true;

    [Tooltip("Thời gian chờ ngắn trước khi bắt đầu fade chuyển map. Đoạn transform cuối map sẽ bỏ qua delay này.")]
    public float delayBeforeLoadNextScene = 0.2f;

    private bool isLoadingNextScene;

    private bool enemy4IntroStarted;
    private bool bossIntroStarted;
    private bool normalWaveStarted;
    private bool beforeBossDialogueStarted;
    private bool bossFightStarted;
    private bool boss5Appeared;
    private bool endMapStarted;

    private bool wukongTransformStarted;
    private bool wukongReachedIdle2;
    private bool keepWukongIdle2UntilSceneLoad;
    private Coroutine wukongTransformCoroutine;

    void Start()
    {
        currentPhase = Map4Phase.StartMap;

        wukongTransformStarted = false;
        wukongReachedIdle2 = false;
        keepWukongIdle2UntilSceneLoad = false;
        SetupDeathNotifyTargets();

        if (enemy4Object == null && enemy4 != null)
        {
            enemy4Object = enemy4.gameObject;
        }

        if (boss5Object != null)
        {
            boss5Object.SetActive(false);
        }

        if (enemy123Spawner != null)
        {
            enemy123Spawner.isSpawning = false;
            enemy123Spawner.spawnOnStart = false;
        }

        if (boss3 != null)
        {
            boss3.DeactivateCombat();
        }

        if (boss4 != null)
        {
            boss4.DeactivateCombat();
        }

        PrepareLocationTitleStartState();

        LogPhase("Map 4 bắt đầu.");

        if (playLocationIntroOnStart)
        {
            StartCoroutine(PlayStartLocationIntroRoutine());
        }
    }

    void Update()
    {
        if (currentPhase == Map4Phase.Enemy4Combat)
        {
            CheckEnemy4DeadAuto();
        }

        if (currentPhase == Map4Phase.NormalEnemyWave)
        {
            CheckNormalEnemyWaveFinished();
        }

        if (currentPhase == Map4Phase.BossFight)
        {
            CheckBoss5AppearCondition();
        }

        // Sau khi đã vào Idle2, giữ Wukong ở Idle2 cho tới khi scene mới load.
        // Không gọi ForceWukongIdle(), không ép về Idle1 nữa.
        if (keepWukongIdle2UntilSceneLoad && wukongReachedIdle2)
        {
            MaintainWukongIdle2UntilSceneLoad();
        }
    }

    void SetupDeathNotifyTargets()
    {
        SetupWukongDeathNotifyTarget();
        SetupPartyDeathNotifyTargets();
    }

    void SetupWukongDeathNotifyTarget()
    {
        if (wukongController == null)
        {
#if UNITY_2023_1_OR_NEWER
            wukongController = FindFirstObjectByType<PlayerController>();
#else
            wukongController = FindObjectOfType<PlayerController>();
#endif
        }

        if (wukongController == null)
        {
            Debug.LogWarning("Map4StoryManager: Không tìm thấy PlayerController để setup Death Notify cho Wukong.");
            return;
        }

        wukongController.SetDeathNotifyTarget(gameObject);
        wukongController.SetDeathNotifyMessageName("NotifyWukongDeathFinished");

        if (enableDebugLog)
        {
            Debug.Log("Map4StoryManager: Đã setup Death Notify Target cho Wukong.");
        }
    }

    void SetupPartyDeathNotifyTargets()
    {
        bool hasSetupAnyPartyHealth = false;

        // Ưu tiên 1: kéo PartyManager vào Inspector.
        if (partyHealth != null)
        {
            SetupOnePartyHealthDeathNotify(partyHealth);
            hasSetupAnyPartyHealth = true;
        }

        // Ưu tiên 2: tìm trong party follow scripts.
        if (partyFollowScripts != null && partyFollowScripts.Length > 0)
        {
            for (int i = 0; i < partyFollowScripts.Length; i++)
            {
                Behaviour followScript = partyFollowScripts[i];

                if (followScript == null)
                {
                    continue;
                }

                PartyHealth foundPartyHealth = followScript.GetComponent<PartyHealth>();

                if (foundPartyHealth == null)
                {
                    foundPartyHealth = followScript.GetComponentInChildren<PartyHealth>(true);
                }

                if (foundPartyHealth == null)
                {
                    foundPartyHealth = followScript.GetComponentInParent<PartyHealth>();
                }

                if (foundPartyHealth == null)
                {
                    continue;
                }

                SetupOnePartyHealthDeathNotify(foundPartyHealth);
                hasSetupAnyPartyHealth = true;
            }
        }

        // Ưu tiên 3: tự tìm trong scene.
        if (!hasSetupAnyPartyHealth && autoFindPartyHealthIfMissing)
        {
#if UNITY_2023_1_OR_NEWER
            PartyHealth[] allPartyHealth = FindObjectsByType<PartyHealth>(FindObjectsSortMode.None);
#else
            PartyHealth[] allPartyHealth = FindObjectsOfType<PartyHealth>();
#endif

            for (int i = 0; i < allPartyHealth.Length; i++)
            {
                if (allPartyHealth[i] == null)
                {
                    continue;
                }

                SetupOnePartyHealthDeathNotify(allPartyHealth[i]);
                hasSetupAnyPartyHealth = true;
            }
        }

        if (!hasSetupAnyPartyHealth)
        {
            Debug.LogWarning("Map4StoryManager: Không tìm thấy PartyHealth nào để setup Death Notify cho đoàn. Hãy kéo PartyManager vào field Party Health.");
        }
    }

    void SetupOnePartyHealthDeathNotify(PartyHealth targetPartyHealth)
    {
        if (targetPartyHealth == null)
        {
            return;
        }

        targetPartyHealth.SetDeathNotifyTarget(gameObject);
        targetPartyHealth.SetDeathNotifyMessageName("NotifyPartyDead");

        if (enableDebugLog)
        {
            Debug.Log("Map4StoryManager: Đã setup Death Notify Target cho PartyHealth trên object: " + targetPartyHealth.gameObject.name);
        }
    }

    void PrepareLocationTitleStartState()
    {
        if (mapHUDController == null)
        {
            return;
        }

        ApplyLocationTitleSettingsToHUD();
        mapHUDController.HideLocationTitleImmediate();
    }

    IEnumerator PlayStartLocationIntroRoutine()
    {
        if (hasPlayedStartLocationIntro)
        {
            yield break;
        }

        hasPlayedStartLocationIntro = true;

        if (waitBeforeLockTime > 0f)
        {
            yield return new WaitForSeconds(waitBeforeLockTime);
        }

        LockWukongAndParty();

        if (waitIdleAfterLockTime > 0f)
        {
            yield return new WaitForSeconds(waitIdleAfterLockTime);
        }

        if (mapHUDController != null)
        {
            ApplyLocationTitleSettingsToHUD();

            mapHUDController.SetLocationTitleText(locationTitleText);
            yield return StartCoroutine(mapHUDController.PlayLocationTitleRoutine(locationTitleText));
        }
        else
        {
            Debug.LogWarning("Map4StoryManager chưa gán Map4BossHUDController cho Location Title UI.");
        }

        if (restoreMovementAfterIntro)
        {
            UnlockWukongAndParty();
        }

        LogPhase("Hoàn thành intro địa danh đầu Map 4.");
    }

    void ApplyLocationTitleSettingsToHUD()
    {
        if (mapHUDController == null) return;
        if (!overrideHUDLocationSettings) return;

        mapHUDController.locationBoxName = locationBoxName;
        mapHUDController.locationTextName = locationTextName;
        mapHUDController.locationTitleText = locationTitleText;
        mapHUDController.locationFadeInTime = locationFadeInTime;
        mapHUDController.locationHoldTime = locationHoldTime;
        mapHUDController.locationFadeOutTime = locationFadeOutTime;
    }

    public void StartEnemy4Intro()
    {
        if (enemy4IntroStarted) return;

        enemy4IntroStarted = true;
        currentPhase = Map4Phase.Enemy4IntroDialogue;

        LockWukongAndParty();

        if (dialogueController != null && enemy4IntroLines != null && enemy4IntroLines.Length > 0)
        {
            dialogueController.StartDialogue(enemy4IntroLines, OnEnemy4IntroFinished);
        }
        else
        {
            OnEnemy4IntroFinished();
        }

        LogPhase("Bắt đầu hội thoại Enemy4.");
    }

    void OnEnemy4IntroFinished()
    {
        currentPhase = Map4Phase.Enemy4Combat;

        UnlockWukongAndParty();

        if (enemy4 != null)
        {
            enemy4.SendMessage("ActivateCombat", SendMessageOptions.DontRequireReceiver);
        }

        LogPhase("Enemy4 bắt đầu combat.");
    }

    void CheckEnemy4DeadAuto()
    {
        if (!autoDetectEnemy4Dead) return;

        if (enemy4Object == null && enemy4 == null)
        {
            NotifyEnemy4Dead();
            return;
        }

        if (enemy4Object != null && !enemy4Object.activeInHierarchy)
        {
            NotifyEnemy4Dead();
            return;
        }
    }

    public void NotifyEnemy4Dead()
    {
        if (currentPhase == Map4Phase.Enemy4Defeated) return;

        currentPhase = Map4Phase.Enemy4Defeated;
        LogPhase("Enemy4 đã chết. Mở khóa camera phase 1.");
    }

    public void StartBossIntro()
    {
        if (bossIntroStarted) return;

        bossIntroStarted = true;
        currentPhase = Map4Phase.BossIntroDialogue;

        LockWukongAndParty();

        if (boss3 != null)
        {
            boss3.DeactivateCombat();
        }

        if (boss4 != null)
        {
            boss4.DeactivateCombat();
        }

        if (dialogueController != null && bossIntroLines != null && bossIntroLines.Length > 0)
        {
            dialogueController.StartDialogue(bossIntroLines, StartNormalEnemyWave);
        }
        else
        {
            StartNormalEnemyWave();
        }

        LogPhase("Bắt đầu hội thoại BossIntro.");
    }

    void StartNormalEnemyWave()
    {
        if (normalWaveStarted) return;

        normalWaveStarted = true;
        currentPhase = Map4Phase.NormalEnemyWave;

        UnlockWukongAndParty();

        if (boss3 != null)
        {
            boss3.DeactivateCombat();
        }

        if (boss4 != null)
        {
            boss4.DeactivateCombat();
        }

        if (enemy123Spawner != null)
        {
            enemy123Spawner.StartSpawn();
        }

        LogPhase("Bắt đầu wave Enemy123.");
    }

    void CheckNormalEnemyWaveFinished()
    {
        if (beforeBossDialogueStarted) return;
        if (waitingBeforeBossDialogue) return;
        if (enemy123Spawner == null) return;

        if (enemy123Spawner.IsSpawnFinished())
        {
            StartCoroutine(WaitWukongIdleThenStartBeforeBossDialogue());
        }
    }

    IEnumerator WaitWukongIdleThenStartBeforeBossDialogue()
    {
        waitingBeforeBossDialogue = true;

        if (enemy123Spawner != null)
        {
            enemy123Spawner.StopSpawn();
        }

        float waitTimer = 0f;
        float idleTimer = 0f;

        while (waitWukongIdleBeforeBossDialogue)
        {
            waitTimer += Time.deltaTime;

            bool isIdleReady = IsWukongIdleReadyForDialogue();

            if (isIdleReady)
            {
                idleTimer += Time.deltaTime;

                if (idleTimer >= wukongIdleStableTime)
                {
                    break;
                }
            }
            else
            {
                idleTimer = 0f;
            }

            if (waitTimer >= maxWaitWukongIdleTime)
            {
                Debug.LogWarning("Chờ Wukong về Idle quá lâu. Tự mở thoại trước Boss để tránh kẹt phase.");
                break;
            }

            yield return null;
        }

        LockWukongAndParty();

        StartBeforeBossDialogue();
    }

    bool IsWukongIdleReadyForDialogue()
    {
        if (wukongAnimator == null)
        {
            return true;
        }

        if (wukongAnimator.IsInTransition(0))
        {
            return false;
        }

        AnimatorStateInfo stateInfo = wukongAnimator.GetCurrentAnimatorStateInfo(0);

        bool isIdleState = false;

        if (!string.IsNullOrEmpty(wukongIdleStateNameForDialogueWait))
        {
            isIdleState = stateInfo.IsName(wukongIdleStateNameForDialogueWait);
        }

        if (!isIdleState)
        {
            return false;
        }

        if (wukongRigidbody != null)
        {
            float velocityX = Mathf.Abs(wukongRigidbody.linearVelocity.x);
            float velocityY = Mathf.Abs(wukongRigidbody.linearVelocity.y);

            if (velocityX > wukongIdleVelocityThreshold) return false;
            if (velocityY > wukongIdleVelocityThreshold) return false;
        }

        return true;
    }

    void StartBeforeBossDialogue()
    {
        if (beforeBossDialogueStarted) return;

        beforeBossDialogueStarted = true;
        currentPhase = Map4Phase.BeforeBossDialogue;

        LockWukongAndParty();

        if (enemy123Spawner != null)
        {
            enemy123Spawner.StopSpawn();
        }

        if (boss3 != null)
        {
            boss3.DeactivateCombat();
        }

        if (boss4 != null)
        {
            boss4.DeactivateCombat();
        }

        if (dialogueController != null && beforeBossFightLines != null && beforeBossFightLines.Length > 0)
        {
            dialogueController.StartDialogue(beforeBossFightLines, OnBeforeBossFightDialogueFinished);
        }
        else
        {
            StartBossFight();
        }

        LogPhase("Bắt đầu hội thoại trước BossFight.");
    }

    void OnBeforeBossFightDialogueFinished()
    {
        StartBossFight();
    }

    public void StartBossFight()
    {
        if (bossFightStarted) return;

        bossFightStarted = true;
        currentPhase = Map4Phase.BossFight;

        UnlockWukongAndParty();

        if (wukongController != null)
        {
            wukongController.enabled = true;
        }

        if (wukongRigidbody != null)
        {
            wukongRigidbody.linearVelocity = Vector2.zero;
        }

        if (partyFollowScripts != null)
        {
            for (int i = 0; i < partyFollowScripts.Length; i++)
            {
                if (partyFollowScripts[i] != null)
                {
                    partyFollowScripts[i].enabled = true;
                }
            }
        }

        if (boss3 != null)
        {
            boss3.ActivateCombat();
        }
        else
        {
            Debug.LogWarning("Map4StoryManager chưa gán Boss3.");
        }

        if (boss4 != null)
        {
            boss4.ActivateCombat();
        }
        else
        {
            Debug.LogWarning("Map4StoryManager chưa gán Boss4.");
        }

        LogPhase("Hết thoại trước boss. Boss3/Boss4 bắt đầu tấn công, Wukong được mở khóa.");
    }

    void CheckBoss5AppearCondition()
    {
        if (boss5Appeared) return;

        bool boss3Low = IsBossLowHealth(boss3);
        bool boss4Low = IsBossLowHealth(boss4);

        if (boss3Low || boss4Low)
        {
            StartBoss5Appear();
        }
    }

    bool IsBossLowHealth(Map4BossController boss)
    {
        if (boss == null) return false;

        return boss.GetHealthPercent() <= boss5AppearHealthPercent;
    }

    void StartBoss5Appear()
    {
        if (boss5Appeared) return;

        boss5Appeared = true;
        currentPhase = Map4Phase.Boss5Appear;

        if (boss5Object != null)
        {
            if (boss5SpawnPoint != null)
            {
                boss5Object.transform.position = boss5SpawnPoint.position;
            }

            boss5Object.SetActive(true);
        }

        if (waitWukongDeathBeforeBoss5Story)
        {
            waitingForWukongDeathAfterBoss5Appear = true;

            LogPhase("Boss5 xuất hiện. Đang chờ Wukong hết máu rồi mới mở thoại Boss5.");
            return;
        }

        StartBoss5StoryDialogue();
    }

    public void NotifyWukongDeadForBoss5Story()
    {
        Debug.Log("Map4StoryManager đã nhận tín hiệu Wukong chết ở phase Boss5.");

        if (!waitingForWukongDeathAfterBoss5Appear)
        {
            Debug.LogWarning("Chưa bật waitingForWukongDeathAfterBoss5Appear nên không mở thoại Boss5.");
            return;
        }

        if (boss5StoryStarted)
        {
            Debug.LogWarning("Boss5 story đã chạy rồi.");
            return;
        }

        if (currentPhase != Map4Phase.Boss5Appear)
        {
            Debug.LogWarning("Phase hiện tại không phải Boss5Appear. Current Phase = " + currentPhase);
            return;
        }

        StartCoroutine(StartBoss5StoryAfterWukongDeadRoutine());
    }

    IEnumerator StartBoss5StoryAfterWukongDeadRoutine()
    {
        boss5StoryStarted = true;
        waitingForWukongDeathAfterBoss5Appear = false;

        yield return new WaitForSeconds(boss5StoryDelayAfterWukongDeath);

        StartBoss5StoryDialogue();
    }

    void StartBoss5StoryDialogue()
    {
        currentPhase = Map4Phase.Boss5StoryDialogue;

        if (hideGameplayElementsWhenBoss5StoryStart)
        {
            HideGameplayElementsOnly();
        }

        LockWukongAndParty();

        if (boss3 != null)
        {
            boss3.StopCombatAndReturnIdle();
        }

        if (boss4 != null)
        {
            boss4.StopCombatAndReturnIdle();
        }

        if (boss5StoryLines != null && boss5StoryLines.Length > 0 && dialogueController != null)
        {
            dialogueController.StartDialogue(boss5StoryLines, StartWukongTransform);
        }
        else
        {
            StartWukongTransform();
        }

        LogPhase("Wukong đã hết máu. Ẩn UI máu/kỹ năng và bắt đầu hội thoại Boss5.");
    }

    void StartWukongTransform()
    {
        if (wukongTransformStarted)
        {
            return;
        }

        wukongTransformStarted = true;
        wukongReachedIdle2 = false;
        keepWukongIdle2UntilSceneLoad = false;

        currentPhase = Map4Phase.WukongTransform;

        // Không UnlockWukongAndParty ở đây.
        // Nếu Unlock, PlayerController sẽ kéo Animator về state cũ.
        LockWukongForTransformWithoutPlayingIdle1();
        LockParty();

        StartWukongTransformAnimation();

        if (wukongTransformCoroutine != null)
        {
            StopCoroutine(wukongTransformCoroutine);
        }

        wukongTransformCoroutine = StartCoroutine(WaitWukongIdle2ThenEndMapRoutine());

        LogPhase("Wukong bắt đầu transition bằng trigger Transform.");
    }

    void StartWukongTransformAnimation()
    {
        if (wukongAnimator == null)
        {
            return;
        }

        wukongAnimator.enabled = true;

        SetAnimatorFloatIfExists(wukongAnimator, wukongSpeedParameterName, 0f);

        if (wukongBoolParametersToFalse != null)
        {
            for (int i = 0; i < wukongBoolParametersToFalse.Length; i++)
            {
                SetAnimatorBoolIfExists(wukongAnimator, wukongBoolParametersToFalse[i], false);
            }
        }

        ResetWukongTriggersExceptTransform();

        SetAnimatorTriggerIfExists(wukongAnimator, wukongTransformTriggerName);
        wukongAnimator.Update(0f);
    }

    IEnumerator WaitWukongIdle2ThenEndMapRoutine()
    {
        float timer = 0f;

        // Chờ ít nhất 1 frame để Animator nhận trigger Transform.
        yield return null;

        while (timer < maxWaitWukongTransformIdleTime)
        {
            LockWukongForTransformWithoutPlayingIdle1();

            if (wukongAnimator == null)
            {
                break;
            }

            bool isIdle2 =
                IsAnimatorInState(wukongAnimator, wukongTransformIdleStateName) &&
                !wukongAnimator.IsInTransition(0);

            if (isIdle2)
            {
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        wukongReachedIdle2 = true;
        keepWukongIdle2UntilSceneLoad = true;

        LockWukongForTransformWithoutPlayingIdle1();

        if (wukongAnimator != null)
        {
            ResetAnimatorTriggerIfExists(wukongAnimator, wukongTransformTriggerName);

            if (!IsAnimatorInState(wukongAnimator, wukongTransformIdleStateName))
            {
                PlayAnimatorStateIfExists(wukongAnimator, wukongTransformIdleStateName);
            }
        }

        if (loadSceneImmediatelyWhenIdle2Reached)
        {
            FinishWukongTransformAndEndMap();
        }
    }

    public void StartEndMapByTrigger()
    {
        if (endMapStarted) return;

        endMapStarted = true;
        currentPhase = Map4Phase.EndMap;

        if (keepWukongIdle2UntilSceneLoad)
        {
            LockWukongForTransformWithoutPlayingIdle1();
            LockParty();
        }
        else
        {
            LockWukongAndParty();
        }

        LogPhase("Kết thúc Map 4.");

        if (autoLoadNextSceneWhenEndMap)
        {
            LoadNextSceneWithFade();
        }
    }

    void LockWukongAndParty()
    {
        // Nếu đã vào flow transform thì tuyệt đối không gọi LockWukong() nữa,
        // vì LockWukong() sẽ ForceWukongIdle() và kéo về Idle1.
        if (keepWukongIdle2UntilSceneLoad || currentPhase == Map4Phase.WukongTransform || currentPhase == Map4Phase.EndMap)
        {
            LockWukongForTransformWithoutPlayingIdle1();
        }
        else
        {
            LockWukong();
        }

        LockParty();
    }

    void UnlockWukongAndParty()
    {
        UnlockWukong();
        UnlockParty();
    }

    void LockWukong()
    {
        if (wukongRigidbody != null)
        {
            wukongRigidbody.linearVelocity = Vector2.zero;
            wukongRigidbody.angularVelocity = 0f;
        }

        if (wukongController != null)
        {
            wukongController.enabled = false;
        }

        ForceWukongIdle();
    }

    void LockWukongForTransformWithoutPlayingIdle1()
    {
        if (wukongRigidbody != null)
        {
            wukongRigidbody.linearVelocity = Vector2.zero;
            wukongRigidbody.angularVelocity = 0f;
        }

        if (wukongController != null)
        {
            wukongController.enabled = false;
        }

        // Không gọi ForceWukongIdle ở đây.
        // Hàm này chỉ khóa vật lý/controller, không ép về Idle1.
    }

    void MaintainWukongIdle2UntilSceneLoad()
    {
        LockWukongForTransformWithoutPlayingIdle1();

        if (wukongAnimator == null)
        {
            return;
        }

        SetAnimatorFloatIfExists(wukongAnimator, wukongSpeedParameterName, 0f);

        if (wukongBoolParametersToFalse != null)
        {
            for (int i = 0; i < wukongBoolParametersToFalse.Length; i++)
            {
                SetAnimatorBoolIfExists(wukongAnimator, wukongBoolParametersToFalse[i], false);
            }
        }

        ResetWukongTriggersExceptTransform();
        ResetAnimatorTriggerIfExists(wukongAnimator, wukongTransformTriggerName);

        // Nếu đang ở Idle2 thì không Play lại, để Idle2 vẫn loop mượt.
        // Nếu vì lý do nào đó bị kéo về Idle1, kéo lại Idle2 ngay.
        if (!IsAnimatorInOrGoingToState(wukongAnimator, wukongTransformIdleStateName))
        {
            PlayAnimatorStateIfExists(wukongAnimator, wukongTransformIdleStateName);
        }
    }

    void UnlockWukong()
    {
        if (wukongRigidbody != null)
        {
            wukongRigidbody.linearVelocity = Vector2.zero;
            wukongRigidbody.angularVelocity = 0f;
        }

        if (wukongController != null)
        {
            wukongController.enabled = true;
        }
    }

    void LockParty()
    {
        if (partyFollowScripts != null)
        {
            for (int i = 0; i < partyFollowScripts.Length; i++)
            {
                if (partyFollowScripts[i] != null)
                {
                    partyFollowScripts[i].enabled = false;
                }
            }
        }

        ForcePartyIdle();
    }

    void UnlockParty()
    {
        if (partyFollowScripts != null)
        {
            for (int i = 0; i < partyFollowScripts.Length; i++)
            {
                if (partyFollowScripts[i] != null)
                {
                    partyFollowScripts[i].enabled = true;
                }
            }
        }
    }

    void ForceWukongIdle()
    {
        if (wukongAnimator == null) return;

        SetAnimatorFloatIfExists(wukongAnimator, wukongSpeedParameterName, 0f);

        if (wukongBoolParametersToFalse != null)
        {
            for (int i = 0; i < wukongBoolParametersToFalse.Length; i++)
            {
                SetAnimatorBoolIfExists(wukongAnimator, wukongBoolParametersToFalse[i], false);
            }
        }

        if (wukongTriggersToReset != null)
        {
            for (int i = 0; i < wukongTriggersToReset.Length; i++)
            {
                ResetAnimatorTriggerIfExists(wukongAnimator, wukongTriggersToReset[i]);
            }
        }

        PlayAnimatorStateIfExists(wukongAnimator, wukongIdleStateName);
    }

    void ForcePartyIdle()
    {
        if (partyAnimators == null) return;

        for (int i = 0; i < partyAnimators.Length; i++)
        {
            Animator targetAnimator = partyAnimators[i];

            if (targetAnimator == null) continue;

            SetAnimatorFloatIfExists(targetAnimator, partySpeedParameterName, 0f);

            if (partyBoolParametersToFalse != null)
            {
                for (int j = 0; j < partyBoolParametersToFalse.Length; j++)
                {
                    SetAnimatorBoolIfExists(targetAnimator, partyBoolParametersToFalse[j], false);
                }
            }

            if (partyTriggersToReset != null)
            {
                for (int j = 0; j < partyTriggersToReset.Length; j++)
                {
                    ResetAnimatorTriggerIfExists(targetAnimator, partyTriggersToReset[j]);
                }
            }

            PlayAnimatorStateIfExists(targetAnimator, partyIdleStateName);
        }
    }

    void PlayAnimatorStateIfExists(Animator targetAnimator, string stateName)
    {
        if (targetAnimator == null) return;
        if (string.IsNullOrEmpty(stateName)) return;

        int stateHash = Animator.StringToHash(stateName);

        if (!targetAnimator.HasState(0, stateHash))
        {
            if (enableDebugLog)
            {
                Debug.LogWarning(
                    "Map4StoryManager: Animator " +
                    targetAnimator.gameObject.name +
                    " không có state '" +
                    stateName +
                    "'. Bỏ qua Play để tránh lỗi GotoState."
                );
            }

            return;
        }

        targetAnimator.Play(stateName, 0, 0f);
        targetAnimator.Update(0f);
    }

    bool IsAnimatorInState(Animator targetAnimator, string stateName)
    {
        if (targetAnimator == null) return false;
        if (string.IsNullOrEmpty(stateName)) return false;

        AnimatorStateInfo currentState = targetAnimator.GetCurrentAnimatorStateInfo(0);
        return currentState.IsName(stateName);
    }

    bool IsAnimatorInOrGoingToState(Animator targetAnimator, string stateName)
    {
        if (targetAnimator == null) return false;
        if (string.IsNullOrEmpty(stateName)) return false;

        AnimatorStateInfo currentState = targetAnimator.GetCurrentAnimatorStateInfo(0);

        if (currentState.IsName(stateName))
        {
            return true;
        }

        if (targetAnimator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = targetAnimator.GetNextAnimatorStateInfo(0);

            if (nextState.IsName(stateName))
            {
                return true;
            }
        }

        return false;
    }

    void HideGameplayElementsOnly()
    {
        if (gameplayUIDocument == null)
        {
            Debug.LogWarning("Map4StoryManager chưa gán Gameplay UI Document.");
            return;
        }

        VisualElement root = gameplayUIDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogWarning("Map4StoryManager không lấy được rootVisualElement của Gameplay UI.");
            return;
        }

        if (gameplayElementNamesToHide == null)
        {
            return;
        }

        for (int i = 0; i < gameplayElementNamesToHide.Length; i++)
        {
            string elementName = gameplayElementNamesToHide[i];

            if (string.IsNullOrEmpty(elementName))
            {
                continue;
            }

            VisualElement element = root.Q<VisualElement>(elementName);

            if (element != null)
            {
                element.style.display = DisplayStyle.None;
            }
            else
            {
                Debug.LogWarning("Không tìm thấy UI element cần ẩn: " + elementName);
            }
        }
    }

    void SetAnimatorFloatIfExists(Animator targetAnimator, string parameterName, float value)
    {
        if (targetAnimator == null) return;
        if (string.IsNullOrEmpty(parameterName)) return;

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == AnimatorControllerParameterType.Float)
            {
                targetAnimator.SetFloat(parameterName, value);
                return;
            }
        }
    }

    void SetAnimatorBoolIfExists(Animator targetAnimator, string parameterName, bool value)
    {
        if (targetAnimator == null) return;
        if (string.IsNullOrEmpty(parameterName)) return;

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == AnimatorControllerParameterType.Bool)
            {
                targetAnimator.SetBool(parameterName, value);
                return;
            }
        }
    }

    void SetAnimatorTriggerIfExists(Animator targetAnimator, string parameterName)
    {
        if (targetAnimator == null) return;
        if (string.IsNullOrEmpty(parameterName)) return;

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == AnimatorControllerParameterType.Trigger)
            {
                targetAnimator.SetTrigger(parameterName);
                return;
            }
        }

        Debug.LogWarning("Map4StoryManager: Animator không có Trigger '" + parameterName + "'.");
    }

    void ResetAnimatorTriggerIfExists(Animator targetAnimator, string parameterName)
    {
        if (targetAnimator == null) return;
        if (string.IsNullOrEmpty(parameterName)) return;

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == AnimatorControllerParameterType.Trigger)
            {
                targetAnimator.ResetTrigger(parameterName);
                return;
            }
        }
    }

    void ResetWukongTriggersExceptTransform()
    {
        if (wukongAnimator == null) return;
        if (wukongTriggersToReset == null) return;

        for (int i = 0; i < wukongTriggersToReset.Length; i++)
        {
            string triggerName = wukongTriggersToReset[i];

            if (string.IsNullOrEmpty(triggerName))
            {
                continue;
            }

            if (triggerName == wukongTransformTriggerName)
            {
                continue;
            }

            ResetAnimatorTriggerIfExists(wukongAnimator, triggerName);
        }
    }

    public void NotifyWukongDeathFinished()
    {
        if (!gameOverWhenWukongDead)
        {
            return;
        }

        if (ShouldIgnoreWukongGameOverForBoss5Story())
        {
            Debug.Log("Map4StoryManager: Wukong chết trong phase Boss5 story nên không bật GameOver.");
            return;
        }

        Debug.Log("Map4StoryManager: Đã nhận báo Wukong chết xong animation.");
        ShowGameOver("Wukong chết xong animation.");
    }

    public void NotifyPartyDead()
    {
        if (!gameOverWhenPartyDead)
        {
            return;
        }

        Debug.Log("Map4StoryManager: Đã nhận báo đoàn thỉnh kinh hết máu.");
        ShowGameOver("Đoàn thỉnh kinh hết máu.");
    }

    bool ShouldIgnoreWukongGameOverForBoss5Story()
    {
        if (!ignoreWukongGameOverDuringBoss5Story)
        {
            return false;
        }

        return currentPhase == Map4Phase.Boss5Appear
            || currentPhase == Map4Phase.Boss5StoryDialogue
            || currentPhase == Map4Phase.WukongTransform
            || currentPhase == Map4Phase.EndMap;
    }

    void ShowGameOver(string reason)
    {
        if (gameOverStarted)
        {
            return;
        }

        gameOverStarted = true;

        Debug.Log("Map4StoryManager: GAME OVER. Lý do: " + reason);

        StopAllStoryCombatForGameOver();
        HideUIBeforeGameOver();

        if (gameOverMenuController != null)
        {
            gameOverMenuController.ShowGameOver();
        }
        else
        {
            Debug.LogWarning("Map4StoryManager: Chưa gán GameOverMenuController.");
        }
    }

    void StopAllStoryCombatForGameOver()
    {
        if (enemy123Spawner != null)
        {
            enemy123Spawner.StopSpawn();
            enemy123Spawner.isSpawning = false;
        }

        if (enemy4 != null)
        {
            enemy4.SendMessage("NotifyWukongDead", SendMessageOptions.DontRequireReceiver);
            enemy4.SendMessage("NotifyPartyDead", SendMessageOptions.DontRequireReceiver);
            enemy4.SendMessage("DeactivateCombat", SendMessageOptions.DontRequireReceiver);
        }

        if (boss3 != null)
        {
            boss3.StopCombatAndReturnIdle();
        }

        if (boss4 != null)
        {
            boss4.StopCombatAndReturnIdle();
        }

        if (boss5Object != null)
        {
            boss5Object.SendMessage("NotifyWukongDead", SendMessageOptions.DontRequireReceiver);
            boss5Object.SendMessage("NotifyPartyDead", SendMessageOptions.DontRequireReceiver);
        }

        LockWukongAndParty();
    }

    void HideUIBeforeGameOver()
    {
        if (dialogueController != null)
        {
            dialogueController.HideDialogue();
        }

        if (mapHUDController != null)
        {
            mapHUDController.HideLocationTitleImmediate();
        }

        if (hideGameplayElementsWhenBoss5StoryStart)
        {
            HideGameplayElementsOnly();
        }
    }

    void LogPhase(string message)
    {
        if (!enableDebugLog) return;

        Debug.Log("[Map4StoryManager] " + message + " Current Phase = " + currentPhase);
    }

    public void LoadNextSceneWithFade()
    {
        if (isLoadingNextScene)
            return;

        StartCoroutine(LoadNextSceneWithFadeRoutine());
    }

    public void LoadSceneWithFade(string sceneName)
    {
        if (isLoadingNextScene)
            return;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Map4StoryManager: Scene Name đang trống, không thể chuyển map.");
            return;
        }

        StartCoroutine(LoadSceneWithFadeRoutine(sceneName));
    }

    IEnumerator LoadNextSceneWithFadeRoutine()
    {
        yield return StartCoroutine(LoadSceneWithFadeRoutine(nextSceneName));
    }

    IEnumerator LoadSceneWithFadeRoutine(string sceneName)
    {
        if (isLoadingNextScene)
            yield break;

        isLoadingNextScene = true;

        currentPhase = Map4Phase.EndMap;

        if (keepWukongIdle2UntilSceneLoad)
        {
            LockWukongForTransformWithoutPlayingIdle1();
            LockParty();
            MaintainWukongIdle2UntilSceneLoad();
        }
        else
        {
            LockWukongAndParty();

            if (delayBeforeLoadNextScene > 0f)
            {
                yield return new WaitForSeconds(delayBeforeLoadNextScene);
            }
        }

        if (sceneFadeController != null)
        {
            sceneFadeController.FadeOutThenLoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Map4StoryManager chưa gán Map4SceneFadeController. Chuyển scene trực tiếp.");
            SceneManager.LoadScene(sceneName);
        }
    }

    public void FinishWukongTransformAndEndMap()
    {
        if (endMapStarted) return;

        endMapStarted = true;
        currentPhase = Map4Phase.EndMap;

        keepWukongIdle2UntilSceneLoad = true;

        // Chỉ khóa vật lý/controller, không Play Idle1.
        LockWukongForTransformWithoutPlayingIdle1();
        LockParty();

        MaintainWukongIdle2UntilSceneLoad();

        Debug.Log("Map4StoryManager: Wukong đã vào WukongIdle2. Chuyển map ngay, không quay về WukongIdle.");

        if (autoLoadNextSceneWhenEndMap)
        {
            LoadNextSceneWithFade();
        }
    }

    // ======================================================
    // TXT IMPORT - CHỈ ĐỔ DỮ LIỆU VÀO CÁC MẢNG THOẠI
    // Không ảnh hưởng phase, combat, boss, UI, chuyển scene.
    // ======================================================

    public bool ImportEnemy4IntroLinesFromTextAsset()
    {
        return ImportDialogueLinesFromTextAsset(enemy4IntroTxtFile, ref enemy4IntroLines, "Enemy4 Intro Lines");
    }

    public bool ImportBossIntroLinesFromTextAsset()
    {
        return ImportDialogueLinesFromTextAsset(bossIntroTxtFile, ref bossIntroLines, "Boss Intro Lines");
    }

    public bool ImportBeforeBossFightLinesFromTextAsset()
    {
        return ImportDialogueLinesFromTextAsset(beforeBossFightTxtFile, ref beforeBossFightLines, "Before Boss Fight Lines");
    }

    public bool ImportBoss5StoryLinesFromTextAsset()
    {
        return ImportDialogueLinesFromTextAsset(boss5StoryTxtFile, ref boss5StoryLines, "Boss 5 Story Lines");
    }

    private bool ImportDialogueLinesFromTextAsset(TextAsset txtFile, ref DialogueLine[] targetLines, string sectionName)
    {
        if (txtFile == null)
        {
            Debug.LogWarning("Map4StoryManager: Chưa kéo file TXT cho phần " + sectionName + ".");
            return false;
        }

        DialogueLine[] importedLines = ParseDialogueText(txtFile.text, targetLines);

        if (importedLines == null || importedLines.Length == 0)
        {
            Debug.LogWarning("Map4StoryManager: File TXT của phần " + sectionName + " không có dòng thoại hợp lệ. Dùng định dạng: TÊN|Nội dung thoại.");
            return false;
        }

        targetLines = importedLines;

        Debug.Log("Map4StoryManager: Đã import " + targetLines.Length + " dòng thoại vào " + sectionName + " từ file TXT: " + txtFile.name);
        return true;
    }

    private DialogueLine[] ParseDialogueText(string rawText, DialogueLine[] currentLinesForAvatar)
    {
        List<DialogueLine> result = new List<DialogueLine>();

        if (string.IsNullOrWhiteSpace(rawText))
        {
            return result.ToArray();
        }

        Dictionary<string, Sprite> avatarLookup = BuildCurrentAvatarLookup(currentLinesForAvatar);

        string normalizedText = rawText.Replace("\r\n", "\n").Replace("\r", "\n");
        string[] rawLines = normalizedText.Split('\n');

        DialogueLine lastDialogueLine = null;

        for (int i = 0; i < rawLines.Length; i++)
        {
            string line = rawLines[i];

            if (line == null)
            {
                continue;
            }

            line = line.Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string speaker;
            string text;

            if (TryParseImportedDialogueLine(line, out speaker, out text))
            {
                speaker = CleanSpeakerName(speaker);
                text = text.Trim();

                // Những dòng kiểu "Sư Đà Lĩnh:" hoặc "Đà La Trang:" chỉ là tiêu đề, không phải thoại.
                if (string.IsNullOrWhiteSpace(speaker) || string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                DialogueLine dialogueLine = new DialogueLine();
                dialogueLine.speakerName = speaker;
                dialogueLine.dialogueText = text;

                if (keepCurrentAvatarsWhenImport)
                {
                    Sprite avatar;
                    string key = NormalizeSpeakerKey(speaker);

                    if (avatarLookup.TryGetValue(key, out avatar))
                    {
                        dialogueLine.avatar = avatar;
                    }
                }

                result.Add(dialogueLine);
                lastDialogueLine = dialogueLine;
            }
            else
            {
                // Nếu một câu bị xuống dòng trong TXT mà dòng sau không có TÊN| hoặc TÊN:
                // thì nối dòng đó vào câu thoại ngay trước nó.
                if (lastDialogueLine != null)
                {
                    if (!string.IsNullOrWhiteSpace(lastDialogueLine.dialogueText))
                    {
                        lastDialogueLine.dialogueText += "\n";
                    }

                    lastDialogueLine.dialogueText += line;
                }
            }
        }

        return result.ToArray();
    }

    private bool TryParseImportedDialogueLine(string line, out string speaker, out string text)
    {
        speaker = "";
        text = "";

        int separatorIndex = line.IndexOf('|');

        if (separatorIndex >= 0)
        {
            speaker = line.Substring(0, separatorIndex);
            text = line.Substring(separatorIndex + 1);
            return !string.IsNullOrWhiteSpace(speaker);
        }

        int colonIndex = line.IndexOf(':');

        if (colonIndex >= 0)
        {
            speaker = line.Substring(0, colonIndex);
            text = line.Substring(colonIndex + 1);
            return !string.IsNullOrWhiteSpace(speaker);
        }

        return false;
    }

    private Dictionary<string, Sprite> BuildCurrentAvatarLookup(DialogueLine[] sourceLines)
    {
        Dictionary<string, Sprite> avatarLookup = new Dictionary<string, Sprite>();

        if (sourceLines == null)
        {
            return avatarLookup;
        }

        for (int i = 0; i < sourceLines.Length; i++)
        {
            DialogueLine line = sourceLines[i];

            if (line == null)
            {
                continue;
            }

            if (line.avatar == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(line.speakerName))
            {
                continue;
            }

            string key = NormalizeSpeakerKey(line.speakerName);

            if (!avatarLookup.ContainsKey(key))
            {
                avatarLookup.Add(key, line.avatar);
            }
        }

        return avatarLookup;
    }

    private string CleanSpeakerName(string speaker)
    {
        if (speaker == null)
        {
            return "";
        }

        speaker = speaker.Trim();

        while (speaker.EndsWith(":"))
        {
            speaker = speaker.Substring(0, speaker.Length - 1).Trim();
        }

        return speaker;
    }

    private string NormalizeSpeakerKey(string speaker)
    {
        speaker = CleanSpeakerName(speaker);
        return speaker.ToUpperInvariant();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Map4StoryManager))]
public class Map4StoryManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Map4StoryManager manager = (Map4StoryManager)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Dialogue TXT Import", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Kéo file .txt vào đúng ô TXT của từng phần rồi bấm nút import tương ứng.\n\n" +
            "Định dạng tốt nhất:\n" +
            "TIỂU TUẦN PHONG|Ta được đại vương sai đi tuần núi...\n" +
            "TÔN NGỘ KHÔNG?|Kẻ nào đang ở phía trước?\n\n" +
            "Cũng hỗ trợ dạng:\n" +
            "TIỂU TUẦN PHONG: Ta được đại vương sai đi tuần núi...\n\n" +
            "Dòng tiêu đề không có nội dung sau dấu : sẽ tự bị bỏ qua.",
            MessageType.Info
        );

        DrawImportButton(
            "Import TXT To Enemy 4 Intro Lines",
            manager.enemy4IntroTxtFile,
            manager.ImportEnemy4IntroLinesFromTextAsset,
            manager
        );

        DrawImportButton(
            "Import TXT To Boss Intro Lines",
            manager.bossIntroTxtFile,
            manager.ImportBossIntroLinesFromTextAsset,
            manager
        );

        DrawImportButton(
            "Import TXT To Before Boss Fight Lines",
            manager.beforeBossFightTxtFile,
            manager.ImportBeforeBossFightLinesFromTextAsset,
            manager
        );

        DrawImportButton(
            "Import TXT To Boss 5 Story Lines",
            manager.boss5StoryTxtFile,
            manager.ImportBoss5StoryLinesFromTextAsset,
            manager
        );
    }

    private void DrawImportButton(string buttonText, TextAsset txtFile, System.Func<bool> importAction, Map4StoryManager manager)
    {
        using (new EditorGUI.DisabledScope(txtFile == null))
        {
            if (GUILayout.Button(buttonText))
            {
                Undo.RecordObject(manager, buttonText);

                bool success = importAction.Invoke();

                if (success)
                {
                    EditorUtility.SetDirty(manager);
                    serializedObject.Update();
                }
            }
        }
    }
}
#endif