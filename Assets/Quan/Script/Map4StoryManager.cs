using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Wukong")]
    [Tooltip("PlayerController của Wukong.")]
    public PlayerController wukongController;

    [Tooltip("Máu của Wukong.")]
    public PlayerHealth wukongHealth;

    [Tooltip("Rigidbody2D của Wukong.")]
    public Rigidbody2D wukongRb;

    [Tooltip("Animator của Wukong.")]
    public Animator wukongAnimator;

    [Tooltip("Tên state Idle của Wukong.")]
    public string wukongIdleStateName = "Idle";

    [Tooltip("Tên trigger/state transition biến hình của Wukong.")]
    public string wukongTransformTriggerName = "Transition";

    [Tooltip("Tên parameter tốc độ của Wukong.")]
    public string wukongSpeedParameterName = "Speed";


    [Tooltip("Các bool Animator cần tắt khi vào hội thoại, ví dụ IsRunning, IsJumping, IsAttacking.")]
    public string[] wukongBoolParametersToFalse;

    [Tooltip("Các trigger Animator cần reset khi vào hội thoại.")]
    public string[] wukongTriggersToReset;

    [Header("Party")]
    [Tooltip("Máu chung của đoàn thỉnh kinh.")]
    public PartyHealth partyHealth;

    [Tooltip("Các script điều khiển đoàn thỉnh kinh cần tắt khi hội thoại/cinematic.")]
    public Behaviour[] partyControlScripts;

    [Tooltip("Animator của Đường Tăng, Trư Bát Giới, Sa Tăng.")]
    public Animator[] partyAnimators;

    [Tooltip("Tên state Idle của đoàn. Nếu mỗi nhân vật khác tên idle, tạm thời để trống và không ép idle.")]
    public string partyIdleStateName = "Idle";

    [Header("Enemy4 / Yêu quái tuần núi")]
    public Enemy4Controller enemy4;

    [Header("Normal Enemy Wave")]
    [Tooltip("Spawner của Enemy1/2/3.")]
    public Enemy123RandomSpawner enemy123Spawner;

    [Tooltip("Object cha chứa các Enemy123 được spawn ra. Dùng để kiểm tra đã diệt hết quái chưa.")]
    public Transform enemy123SpawnedParent;

    [Tooltip("Tự bắt đầu wave sau khi boss intro dialogue kết thúc.")]
    public bool autoStartNormalEnemyWave = true;

    [Header("Boss3 / Boss4")]
    [Tooltip("Thanh Sư Tinh.")]
    public Map4BossController boss3;

    [Tooltip("Bạch Tượng Tinh.")]
    public Map4BossController boss4;

    [Tooltip("Ngưỡng máu để Boss5 xuất hiện. 0.333 = 1/3 máu.")]
    [Range(0.05f, 1f)]
    public float boss5AppearHealthPercent = 0.333f;

    [Header("Boss5 / Kim Sí Điểu")]
    [Tooltip("Object Boss5. Ban đầu nên để inactive.")]
    public GameObject boss5Object;

    [Tooltip("Controller Boss5 nếu có.")]
    public Boss5Controller boss5Controller;

    [Tooltip("Khi Boss5 xuất hiện thì khóa điều khiển để tạo cinematic ngắn.")]
    public bool lockControlWhenBoss5Appear = false;

    [Tooltip("Sau khi Boss5 xuất hiện, nếu Wukong máu về 0 thì không xử lý chết bình thường mà chuyển sang hội thoại.")]
    public bool useStoryDeathWhenBoss5Appear = true;

    [Header("Dialogue Controller")]
    public DialogueController dialogueController;

    [Header("Dialogues")]
    [Tooltip("Hội thoại khi gặp Enemy4.")]
    public DialogueLine[] enemy4IntroLines;

    [Tooltip("Hội thoại khi gặp Thanh Sư Tinh và Bạch Tượng Tinh.")]
    public DialogueLine[] bossIntroLines;

    [Tooltip("Hội thoại sau khi diệt hết quái thường, trước khi đánh Boss3/Boss4.")]
    public DialogueLine[] beforeBossFightLines;

    [Tooltip("Hội thoại khi Kim Sí Điểu xuất hiện / Wukong bị áp đảo.")]
    public DialogueLine[] boss5StoryLines;

    [Header("End Map")]
    [Tooltip("Có tự chuyển scene khi kết thúc map không.")]
    public bool loadNextSceneWhenEnd = false;

    [Tooltip("Tên scene map cuối.")]
    public string nextSceneName = "Map5_Final";

    [Tooltip("Thời gian chờ sau khi Wukong transition trước khi kết thúc map.")]
    public float endMapDelay = 1.5f;

    [Header("Debug")]
    public bool enableDebugLog = true;

    private bool enemy4IntroStarted;
    private bool enemy4DefeatedHandled;

    private bool bossIntroStarted;
    private bool normalWaveStarted;
    private bool normalWaveFinishedHandled;

    private bool bossFightStarted;
    private bool boss5Appeared;
    private bool boss5StoryStarted;

    private bool wukongTransformStarted;
    private float endMapTimer;
    private bool endMapCounting;

    void Start()
    {
        SetupInitialState();
    }

    void Update()
    {
        switch (currentPhase)
        {
            case Map4Phase.Enemy4Combat:
                CheckEnemy4Defeated();
                break;

            case Map4Phase.NormalEnemyWave:
                CheckNormalEnemyWaveFinished();
                break;

            case Map4Phase.BossFight:
                CheckBoss5AppearCondition();
                break;

            case Map4Phase.Boss5Appear:
                CheckWukongStoryDeath();
                break;

            case Map4Phase.EndMap:
                UpdateEndMapTimer();
                break;
        }
    }

    void SetupInitialState()
    {
        currentPhase = Map4Phase.StartMap;

        if (enemy4 != null)
        {
            enemy4.combatActivated = false;
        }

        if (boss5Object != null)
        {
            boss5Object.SetActive(false);
        }

        if (boss3 != null)
        {
            boss3.SendMessage("DeactivateCombat", SendMessageOptions.DontRequireReceiver);
        }

        if (boss4 != null)
        {
            boss4.SendMessage("DeactivateCombat", SendMessageOptions.DontRequireReceiver);
        }

        if (enableDebugLog)
        {
            Debug.Log("Map4StoryManager: khởi tạo Map 4.");
        }
    }

    // =========================
    // ENEMY4 INTRO
    // =========================

    public void StartEnemy4Intro()
    {
        if (enemy4IntroStarted) return;
        if (currentPhase != Map4Phase.StartMap) return;

        enemy4IntroStarted = true;
        currentPhase = Map4Phase.Enemy4IntroDialogue;

        LockWukongAndParty();

        if (enemy4 != null)
        {
            enemy4.combatActivated = false;
        }

        PlayDialogue(enemy4IntroLines, OnEnemy4IntroFinished);

        LogPhase("Bắt đầu hội thoại Enemy4.");
    }

    void OnEnemy4IntroFinished()
    {
        currentPhase = Map4Phase.Enemy4Combat;

        UnlockWukongAndParty();

        if (enemy4 != null)
        {
            enemy4.ActivateCombat();
        }

        LogPhase("Hết hội thoại Enemy4. Enemy4 bắt đầu combat.");
    }

    void CheckEnemy4Defeated()
    {
        if (enemy4DefeatedHandled) return;
        if (enemy4 == null) return;

        if (enemy4.IsDead())
        {
            enemy4DefeatedHandled = true;
            currentPhase = Map4Phase.Enemy4Defeated;

            UnlockWukongAndParty();

            LogPhase("Enemy4 đã bị hạ gục. Người chơi đi tiếp.");
        }
    }

    // =========================
    // BOSS INTRO
    // =========================

    public void StartBossIntro()
    {
        if (bossIntroStarted) return;
        if (currentPhase != Map4Phase.Enemy4Defeated && currentPhase != Map4Phase.StartMap) return;

        bossIntroStarted = true;
        currentPhase = Map4Phase.BossIntroDialogue;

        LockWukongAndParty();

        PlayDialogue(bossIntroLines, OnBossIntroFinished);

        LogPhase("Bắt đầu hội thoại Boss3/Boss4.");
    }

    void OnBossIntroFinished()
    {
        if (autoStartNormalEnemyWave)
        {
            StartNormalEnemyWave();
        }
        else
        {
            currentPhase = Map4Phase.NormalEnemyWave;
        }
    }

    // =========================
    // NORMAL ENEMY WAVE
    // =========================

    public void StartNormalEnemyWave()
    {
        if (normalWaveStarted) return;

        normalWaveStarted = true;
        currentPhase = Map4Phase.NormalEnemyWave;

        UnlockWukongAndParty();

        if (enemy123Spawner != null)
        {
            enemy123Spawner.SendMessage("StartSpawn", SendMessageOptions.DontRequireReceiver);
            enemy123Spawner.enabled = true;
        }

        LogPhase("Bắt đầu wave quái thường Enemy1/2/3.");
    }

    void CheckNormalEnemyWaveFinished()
    {
        if (normalWaveFinishedHandled) return;
        if (!normalWaveStarted) return;

        if (IsEnemy123WaveFinished())
        {
            normalWaveFinishedHandled = true;
            currentPhase = Map4Phase.BeforeBossDialogue;

            LockWukongAndParty();

            PlayDialogue(beforeBossFightLines, OnBeforeBossFightDialogueFinished);

            LogPhase("Đã diệt hết quái thường. Bắt đầu hội thoại trước boss.");
        }
    }

    bool IsEnemy123WaveFinished()
    {
        if (enemy123Spawner == null) return false;

        if (enemy123Spawner.maxTotalSpawnCount > 0)
        {
            if (enemy123Spawner.totalSpawnedCount < enemy123Spawner.maxTotalSpawnCount)
            {
                return false;
            }
        }

        int aliveCount = CountAliveEnemy123();

        return aliveCount <= 0;
    }

    int CountAliveEnemy123()
    {
        int count = 0;

        if (enemy123SpawnedParent != null)
        {
            Enemy123Controller[] enemiesInParent = enemy123SpawnedParent.GetComponentsInChildren<Enemy123Controller>(true);

            for (int i = 0; i < enemiesInParent.Length; i++)
            {
                if (enemiesInParent[i] == null) continue;
                if (enemiesInParent[i].IsDead()) continue;
                if (!enemiesInParent[i].gameObject.activeInHierarchy) continue;

                count++;
            }

            return count;
        }

#if UNITY_2023_1_OR_NEWER
        Enemy123Controller[] enemies = FindObjectsByType<Enemy123Controller>(FindObjectsSortMode.None);
#else
        Enemy123Controller[] enemies = FindObjectsOfType<Enemy123Controller>();
#endif

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null) continue;
            if (enemies[i].IsDead()) continue;
            if (!enemies[i].gameObject.activeInHierarchy) continue;

            count++;
        }

        return count;
    }

    // =========================
    // BOSS FIGHT
    // =========================

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

        if (boss3 != null)
        {
            boss3.SendMessage("ActivateCombat", SendMessageOptions.DontRequireReceiver);
        }

        if (boss4 != null)
        {
            boss4.SendMessage("ActivateCombat", SendMessageOptions.DontRequireReceiver);
        }

        LogPhase("Boss3/Boss4 bắt đầu combat.");
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
        if (boss.IsDead()) return false;

        int maxHealth = boss.GetMaxHealth();
        int currentHealth = boss.GetCurrentHealth();

        if (maxHealth <= 0) return false;

        float percent = (float)currentHealth / maxHealth;
        return percent <= boss5AppearHealthPercent;
    }

    // =========================
    // BOSS5 / STORY DEATH
    // =========================

    public void StartBoss5Appear()
    {
        if (boss5Appeared) return;

        boss5Appeared = true;
        currentPhase = Map4Phase.Boss5Appear;

        if (boss5Object != null)
        {
            boss5Object.SetActive(true);
        }

        if (boss5Controller != null)
        {
            boss5Controller.SendMessage("ActivateCombat", SendMessageOptions.DontRequireReceiver);
        }

        if (lockControlWhenBoss5Appear)
        {
            LockWukongAndParty();
        }

        LogPhase("Boss5 Kim Sí Điểu xuất hiện.");
    }

    void CheckWukongStoryDeath()
    {
        if (!useStoryDeathWhenBoss5Appear) return;
        if (boss5StoryStarted) return;
        if (wukongHealth == null) return;

        if (wukongHealth.GetCurrentHealth() <= 0)
        {
            StartBoss5StoryDialogue();
        }
    }

    public void StartBoss5StoryDialogue()
    {
        if (boss5StoryStarted) return;

        boss5StoryStarted = true;
        currentPhase = Map4Phase.Boss5StoryDialogue;

        LockWukongAndParty();

        StopAllMapEnemiesForStory();

        ForceAnimatorIdle(wukongAnimator, wukongIdleStateName);

        PlayDialogue(boss5StoryLines, OnBoss5StoryDialogueFinished);

        LogPhase("Wukong bị áp đảo. Bắt đầu hội thoại Boss5.");
    }

    void OnBoss5StoryDialogueFinished()
    {
        StartWukongTransform();
    }

    // =========================
    // WUKONG TRANSFORM / END MAP
    // =========================

    public void StartWukongTransform()
    {
        if (wukongTransformStarted) return;

        wukongTransformStarted = true;
        currentPhase = Map4Phase.WukongTransform;

        LockWukongAndParty();

        if (wukongAnimator != null)
        {
            if (!string.IsNullOrEmpty(wukongTransformTriggerName))
            {
                wukongAnimator.ResetTrigger(wukongTransformTriggerName);
                wukongAnimator.SetTrigger(wukongTransformTriggerName);
            }
        }

        LogPhase("Wukong bắt đầu transition đổi trang phục.");
    }

    public void OnWukongTransformFinished()
    {
        EndMap4();
    }

    public void StartEndMapByTrigger()
    {
        EndMap4();
    }

    public void EndMap4()
    {
        currentPhase = Map4Phase.EndMap;
        endMapTimer = endMapDelay;
        endMapCounting = true;

        LockWukongAndParty();
        StopAllMapEnemiesForStory();

        LogPhase("Map 4 kết thúc.");
    }

    void UpdateEndMapTimer()
    {
        if (!endMapCounting) return;

        endMapTimer -= Time.deltaTime;

        if (endMapTimer <= 0f)
        {
            endMapCounting = false;

            if (loadNextSceneWhenEnd && !string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                Debug.Log("Map4StoryManager: đã kết thúc map, chưa bật load scene.");
            }
        }
    }

    // =========================
    // CONTROL
    // =========================

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
        if (wukongRb != null)
        {
            wukongRb.linearVelocity = Vector2.zero;
            wukongRb.angularVelocity = 0f;
        }

        if (wukongController != null)
        {
            wukongController.enabled = false;
        }

        ForceWukongIdle();
    }
    void ForceWukongIdle()
    {
        if (wukongAnimator == null) return;

        if (!string.IsNullOrEmpty(wukongSpeedParameterName))
        {
            SetAnimatorFloatIfExists(wukongAnimator, wukongSpeedParameterName, 0f);
        }

        

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
    void UnlockWukong()
    {
        if (wukongController != null)
        {
            wukongController.enabled = true;
        }
    }

    void LockParty()
    {
        if (partyControlScripts != null)
        {
            for (int i = 0; i < partyControlScripts.Length; i++)
            {
                if (partyControlScripts[i] != null)
                {
                    partyControlScripts[i].enabled = false;
                }
            }
        }

        if (partyAnimators != null)
        {
            for (int i = 0; i < partyAnimators.Length; i++)
            {
                ForceAnimatorIdle(partyAnimators[i], partyIdleStateName);
            }
        }
    }

    void UnlockParty()
    {
        if (partyControlScripts != null)
        {
            for (int i = 0; i < partyControlScripts.Length; i++)
            {
                if (partyControlScripts[i] != null)
                {
                    partyControlScripts[i].enabled = true;
                }
            }
        }
    }

    void ForceAnimatorIdle(Animator targetAnimator, string idleStateName)
    {
        if (targetAnimator == null) return;
        if (string.IsNullOrEmpty(idleStateName)) return;

        targetAnimator.Play(idleStateName, 0, 0f);
        targetAnimator.Update(0f);
    }

    // =========================
    // DIALOGUE
    // =========================

    void PlayDialogue(DialogueLine[] lines, System.Action onFinished)
    {
        if (dialogueController == null)
        {
            Debug.LogWarning("Map4StoryManager chưa gán DialogueController.");
            onFinished?.Invoke();
            return;
        }

        dialogueController.StartDialogue(lines, onFinished);
    }

    // =========================
    // STOP ENEMIES FOR STORY
    // =========================

    void StopAllMapEnemiesForStory()
    {
        if (enemy4 != null)
        {
            enemy4.SendMessage("StopCombatAndReturnIdle", SendMessageOptions.DontRequireReceiver);
        }

        if (boss3 != null)
        {
            boss3.SendMessage("StopCombatAndReturnIdle", SendMessageOptions.DontRequireReceiver);
            boss3.SendMessage("DeactivateCombat", SendMessageOptions.DontRequireReceiver);
        }

        if (boss4 != null)
        {
            boss4.SendMessage("StopCombatAndReturnIdle", SendMessageOptions.DontRequireReceiver);
            boss4.SendMessage("DeactivateCombat", SendMessageOptions.DontRequireReceiver);
        }

        if (boss5Controller != null)
        {
            boss5Controller.SendMessage("StopCombatAndReturnIdle", SendMessageOptions.DontRequireReceiver);
            boss5Controller.SendMessage("DeactivateCombat", SendMessageOptions.DontRequireReceiver);
        }

        if (enemy123Spawner != null)
        {
            enemy123Spawner.SendMessage("StopSpawn", SendMessageOptions.DontRequireReceiver);
        }
    }

    void LogPhase(string message)
    {
        if (!enableDebugLog) return;
        Debug.Log("[Map4StoryManager] " + message + " | Phase: " + currentPhase);
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
}