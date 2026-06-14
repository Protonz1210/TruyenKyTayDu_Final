using System.Collections;
using UnityEngine;

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

    [Header("Wukong Animator")]
    public string wukongIdleStateName = "Idle";
    public string wukongSpeedParameterName = "Speed";
    public string[] wukongBoolParametersToFalse;
    public string[] wukongTriggersToReset;

    [Header("Party Animator")]
    public string partyIdleStateName = "Idle";
    public string partySpeedParameterName = "Speed";
    public string[] partyBoolParametersToFalse;
    public string[] partyTriggersToReset;

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

    [Header("Enemy4")]
    [Tooltip("Script Enemy4Controller / Enemy4 chính trong scene.")]
    public MonoBehaviour enemy4;

    [Tooltip("Object gốc của Enemy4. Nếu để trống, hệ thống sẽ lấy từ enemy4.")]
    public GameObject enemy4Object;

    [Tooltip("Tự chuyển phase Enemy4Defeated khi Enemy4 bị Destroy hoặc SetActive(false).")]
    public bool autoDetectEnemy4Dead = true;

    [Header("Enemy123 Wave")]
    public Enemy123RandomSpawner enemy123Spawner;

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

    private bool enemy4IntroStarted;
    private bool bossIntroStarted;
    private bool normalWaveStarted;
    private bool beforeBossDialogueStarted;
    private bool bossFightStarted;
    private bool boss5Appeared;
    private bool endMapStarted;

    void Start()
    {
        currentPhase = Map4Phase.StartMap;

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

        LogPhase("Map 4 bắt đầu.");
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
        if (enemy123Spawner == null) return;

        if (enemy123Spawner.IsSpawnFinished())
        {
            StartBeforeBossDialogue();
        }
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

        // Mở khóa Wukong và đoàn thỉnh kinh.
        UnlockWukongAndParty();

        // Đảm bảo Wukong được bật lại.
        if (wukongController != null)
        {
            wukongController.enabled = true;
        }

        // Đảm bảo Rigidbody không bị đứng yên do khóa hội thoại.
        if (wukongRigidbody != null)
        {
            wukongRigidbody.linearVelocity = Vector2.zero;
        }

        // Bật lại các script đi theo của đoàn.
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

        // Kích hoạt Boss3.
        if (boss3 != null)
        {
            boss3.ActivateCombat();
        }
        else
        {
            Debug.LogWarning("Map4StoryManager chưa gán Boss3.");
        }

        // Kích hoạt Boss4.
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

        // Boss5 xuất hiện nhưng KHÔNG hiện thoại ngay.
        // Vẫn để Wukong ở gameplay để Boss5/code map làm Wukong hết máu.
        if (waitWukongDeathBeforeBoss5Story)
        {
            waitingForWukongDeathAfterBoss5Appear = true;

            LogPhase("Boss5 xuất hiện. Đang chờ Wukong hết máu rồi mới mở thoại Boss5.");
            return;
        }

        // Nếu không muốn chờ Wukong chết thì mới dùng nhánh cũ này.
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

        // Chờ một chút để animation / code chết của Wukong kịp đưa về Idle.
        yield return new WaitForSeconds(boss5StoryDelayAfterWukongDeath);

        StartBoss5StoryDialogue();
    }

    void StartBoss5StoryDialogue()
    {
        currentPhase = Map4Phase.Boss5StoryDialogue;

        // Lúc này Wukong đã chết / về idle rồi mới khóa toàn đội để nói chuyện.
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

        LogPhase("Wukong đã hết máu. Bắt đầu hội thoại Boss5.");
    }
    void StartWukongTransform()
    {
        currentPhase = Map4Phase.WukongTransform;

        UnlockWukongAndParty();

        LogPhase("Wukong chuyển trạng thái / transition.");
    }

    public void StartEndMapByTrigger()
    {
        if (endMapStarted) return;

        endMapStarted = true;
        currentPhase = Map4Phase.EndMap;

        LockWukongAndParty();

        LogPhase("Kết thúc Map 4.");
    }

    void LockWukongAndParty()
    {
        LockWukong();
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

        if (!string.IsNullOrEmpty(wukongIdleStateName))
        {
            wukongAnimator.Play(wukongIdleStateName, 0, 0f);
            wukongAnimator.Update(0f);
        }
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

            if (!string.IsNullOrEmpty(partyIdleStateName))
            {
                targetAnimator.Play(partyIdleStateName, 0, 0f);
                targetAnimator.Update(0f);
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

    void LogPhase(string message)
    {
        if (!enableDebugLog) return;

        Debug.Log("[Map4StoryManager] " + message + " Current Phase = " + currentPhase);
    }
}