using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Quản lý cốt truyện Map1.
/// Cơ chế hiện tại:
/// - Vừa vào map sẽ chờ Wukong spawn xong và về Idle.
/// - Sau đó tắt UI tổng.
/// - Khóa điều khiển Wukong.
/// - Khóa đoàn thỉnh kinh đứng yên.
/// - Hiện một UI Document riêng chỉ có box chữ.
/// - Mỗi câu thoại / câu thơ có thể gán audio riêng.
/// - Chữ hiện dần theo thời lượng audio.
/// - Audio đọc xong câu hiện tại thì tự chuyển sang câu tiếp theo.
/// - Hết toàn bộ lời thoại thì ẩn box, bật lại UI tổng và mở lại điều khiển Wukong.
/// 
/// UI Document riêng chỉ cần có:
/// - VisualElement tên: DialogueBox
/// - Label tên: DialogueText
/// </summary>
public class Map1StoryManager : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 5)]
        [Tooltip("Nội dung câu thoại / câu thơ sẽ hiện trong box.")]
        public string text;

        [Tooltip("File âm thanh đọc câu này. Mỗi câu nên dùng 1 audio riêng để chữ và tiếng khớp nhau.")]
        public AudioClip voiceClip;
    }

    [Header("Auto Start")]
    [Tooltip("Bật lên để vừa vào Map1 là tự chạy đoạn thoại mở đầu.")]
    public bool autoStartIntroOnStart = true;

    [Tooltip("Thời gian chờ nhẹ sau khi Wukong đã sẵn sàng rồi mới hiện box thoại.")]
    public float startDelay = 0.3f;

    [Header("Intro Start Wait")]
    [Tooltip("Bật lên để chờ Wukong về Idle sau khi spawn rồi mới chạy intro.")]
    public bool waitWukongIdleBeforeIntro = true;

    [Tooltip("Tên state Idle trong Animator của Wukong.")]
    public string wukongIdleStateName = "Wukong Idle";

    [Tooltip("Thời gian chờ tối đa để Wukong tự về Idle. Hết thời gian này sẽ ép về Idle rồi chạy intro.")]
    public float maxWaitForIdleTime = 2f;

    [Tooltip("Chờ thêm một chút sau khi đã thấy Wukong Idle để tránh Animator chưa ổn định.")]
    public float extraDelayAfterIdle = 0.15f;

    [Header("Global HUD")]
    [Tooltip("Kéo object UI tổng GlobalHUD vào đây. Script sẽ tắt UI này khi intro bắt đầu và bật lại khi intro kết thúc.")]
    public GameObject globalHUDObject;

    [Tooltip("Bật lên nếu muốn tắt UI tổng trong lúc intro bài thơ.")]
    public bool hideGlobalHUDDuringIntro = true;

    [Header("Wukong Lock")]
    [Tooltip("Kéo object Wukong vào đây. Script sẽ tự tìm PlayerController, Rigidbody2D và Animator bên trong.")]
    public GameObject wukongObject;

    [Header("Party Lock")]
    [Tooltip("Kéo NPC1, NPC2, NPC3 vào đây để tắt di chuyển đoàn thỉnh kinh khi intro.")]
    public GameObject[] partyObjectsToStop;

    [Tooltip("Bật lên để đóng băng vật lý đoàn thỉnh kinh trong lúc intro.")]
    public bool freezePartyPhysicsDuringIntro = true;

    [Header("Dialogue UI")]
    [Tooltip("Kéo UIDocument của Map1PoemDialogueUI vào đây. Không kéo GlobalHUD.")]
    public UIDocument dialogueUIDocument;

    [Header("Dialogue Lines")]
    [Tooltip("Danh sách câu thoại / câu thơ. Mỗi Element là 1 câu. Có thể gán audio riêng cho từng câu.")]
    public DialogueLine[] dialogueLines;

    [Header("Text And Audio Sync")]
    [Tooltip("Nếu câu không có audio, mỗi ký tự sẽ hiện sau khoảng thời gian này.")]
    public float fallbackCharDelay = 0.04f;

    [Tooltip("Thời gian nghỉ giữa 2 câu sau khi audio đọc xong.")]
    public float delayBetweenLines = 0.35f;

    [Tooltip("Bật để phát audio đọc thoại.")]
    public bool playVoiceAudio = true;

    [Header("Audio")]
    [Tooltip("AudioSource dùng để phát giọng đọc. Nếu bỏ trống, script sẽ tự thêm AudioSource vào object này.")]
    public AudioSource voiceAudioSource;

    private Behaviour wukongController;
    private Rigidbody2D wukongRigidbody;
    private Animator wukongAnimator;

    private RigidbodyConstraints2D originalWukongConstraints;
    private bool cachedWukongConstraints;

    private VisualElement dialogueBox;
    private Label dialogueText;

    private bool introRunning;
    private Coroutine introCoroutine;

    private Behaviour[] cachedPartyMoveScripts;
    private Rigidbody2D[] cachedPartyRigidbodies;
    private Animator[] cachedPartyAnimators;

    private RigidbodyType2D[] cachedPartyBodyTypes;
    private float[] cachedPartyGravityScales;
    private RigidbodyConstraints2D[] cachedPartyConstraints;

    private void Awake()
    {
        AutoFindMissingReferences();
        CachePartyComponents();
        BindUIElements();

        // Tắt UI tổng ngay từ lúc scene bắt đầu,
        // không chờ đến khi intro chính thức chạy.
        if (autoStartIntroOnStart && hideGlobalHUDDuringIntro)
        {
            HideGlobalHUD();
        }

        // Ẩn box thoại riêng trước, chờ intro bắt đầu mới hiện.
        HideDialogueBox();
    }

    private void Start()
    {
        if (autoStartIntroOnStart)
        {
            StartCoroutine(StartIntroAfterWukongReadyRoutine());
        }
    }

    /// <summary>
    /// Chờ Wukong spawn ổn định và về Idle rồi mới chạy intro.
    /// </summary>
    private IEnumerator StartIntroAfterWukongReadyRoutine()
    {
        // Chờ 1 frame để Wukong, Animator, Rigidbody2D spawn ổn định trước.
        yield return null;

        // Tìm lại component nếu lúc Awake chưa kịp tìm đủ.
        FindWukongComponents();

        if (waitWukongIdleBeforeIntro)
        {
            float timer = 0f;

            while (timer < maxWaitForIdleTime)
            {
                if (IsWukongIdleAndStable())
                {
                    break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            // Nếu chờ quá lâu mà Wukong vẫn chưa Idle thì ép về Idle để tránh kẹt.
            if (!IsWukongIdleAndStable())
            {
                ForceWukongIdle();
            }

            if (extraDelayAfterIdle > 0f)
            {
                yield return new WaitForSeconds(extraDelayAfterIdle);
            }
        }

        StartMap1Intro();
    }

    /// <summary>
    /// Gọi hàm này nếu muốn bắt đầu intro thủ công từ script khác.
    /// </summary>
    public void StartMap1Intro()
    {
        if (introRunning)
        {
            return;
        }

        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
        }

        introCoroutine = StartCoroutine(PlayIntroRoutine());
    }

    private IEnumerator PlayIntroRoutine()
    {
        introRunning = true;

        LockPlayerAndParty();
        HideDialogueBox();

        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        ShowDialogueBox();

        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning("Map1StoryManager: Chưa có câu nào trong Dialogue Lines.");

            HideDialogueBox();
            ShowGlobalHUD();
            UnlockPlayerAndParty();

            introRunning = false;
            yield break;
        }

        for (int i = 0; i < dialogueLines.Length; i++)
        {
            DialogueLine line = dialogueLines[i];

            if (line == null)
            {
                continue;
            }

            yield return StartCoroutine(PlayOneLineRoutine(line));
        }

        HideDialogueBox();
        ShowGlobalHUD();
        UnlockPlayerAndParty();

        introRunning = false;

        Debug.Log("Map1StoryManager: Đã chạy xong đoạn thoại mở đầu Map1.");
    }

    private IEnumerator PlayOneLineRoutine(DialogueLine line)
    {
        if (dialogueText == null)
        {
            yield break;
        }

        string fullText = line.text;

        if (string.IsNullOrEmpty(fullText))
        {
            fullText = "";
        }

        dialogueText.text = "";

        float audioLength = 0f;

        if (line.voiceClip != null)
        {
            audioLength = line.voiceClip.length;
        }

        if (playVoiceAudio && voiceAudioSource != null && line.voiceClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = line.voiceClip;
            voiceAudioSource.Play();
        }

        float charDelay = fallbackCharDelay;

        if (audioLength > 0f && fullText.Length > 0)
        {
            // Chia thời lượng audio cho số ký tự để chữ hiện khớp tương đối với giọng đọc.
            charDelay = audioLength / fullText.Length;
        }

        for (int i = 0; i < fullText.Length; i++)
        {
            dialogueText.text += fullText[i];
            yield return new WaitForSeconds(charDelay);
        }

        // Đảm bảo hiện đủ câu sau khi chạy hiệu ứng chữ.
        dialogueText.text = fullText;

        // Nếu audio còn đang đọc thì chờ đọc xong mới qua câu sau.
        if (playVoiceAudio && voiceAudioSource != null)
        {
            while (voiceAudioSource.isPlaying)
            {
                yield return null;
            }
        }
        else if (audioLength > 0f)
        {
            yield return new WaitForSeconds(audioLength);
        }

        if (delayBetweenLines > 0f)
        {
            yield return new WaitForSeconds(delayBetweenLines);
        }
    }

    private void AutoFindMissingReferences()
    {
        FindWukongComponents();

        if (dialogueUIDocument == null)
        {
            // Chỉ là dự phòng.
            // Tốt nhất vẫn nên kéo tay UIDocument riêng của Map1PoemDialogueUI vào Inspector.
            dialogueUIDocument = Object.FindFirstObjectByType<UIDocument>();
        }

        if (voiceAudioSource == null)
        {
            voiceAudioSource = GetComponent<AudioSource>();

            if (voiceAudioSource == null)
            {
                voiceAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (voiceAudioSource != null)
        {
            voiceAudioSource.playOnAwake = false;
            voiceAudioSource.loop = false;
            voiceAudioSource.spatialBlend = 0f;
        }
    }

    private void FindWukongComponents()
    {
        if (wukongObject == null)
        {
            Debug.LogWarning("Map1StoryManager: Chưa gán Wukong Object.");
            return;
        }

        // Tìm PlayerController trên chính object Wukong.
        Behaviour[] behaviours = wukongObject.GetComponents<Behaviour>();

        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour != null && behaviour.GetType().Name == "PlayerController")
            {
                wukongController = behaviour;
                break;
            }
        }

        // Nếu root không có PlayerController thì tìm trong object con.
        if (wukongController == null)
        {
            Behaviour[] childBehaviours = wukongObject.GetComponentsInChildren<Behaviour>(true);

            foreach (Behaviour behaviour in childBehaviours)
            {
                if (behaviour != null && behaviour.GetType().Name == "PlayerController")
                {
                    wukongController = behaviour;
                    break;
                }
            }
        }

        if (wukongController == null)
        {
            Debug.LogWarning(
                "Map1StoryManager: Không tìm thấy PlayerController trên Wukong hoặc object con. " +
                "Intro vẫn chạy, nhưng có thể không khóa được điều khiển Wukong."
            );
        }

        // Tìm Rigidbody2D trên root Wukong.
        wukongRigidbody = wukongObject.GetComponent<Rigidbody2D>();

        // Nếu root không có Rigidbody2D thì tìm trong object con.
        if (wukongRigidbody == null)
        {
            wukongRigidbody = wukongObject.GetComponentInChildren<Rigidbody2D>(true);
        }

        if (wukongRigidbody == null)
        {
            Debug.LogWarning(
                "Map1StoryManager: Không tìm thấy Rigidbody2D trên Wukong hoặc object con. " +
                "Intro vẫn chạy, nhưng không thể dừng vận tốc Wukong."
            );
        }

        // Tìm Animator trên root Wukong.
        wukongAnimator = wukongObject.GetComponent<Animator>();

        // Nếu root không có Animator thì tìm trong object con.
        if (wukongAnimator == null)
        {
            wukongAnimator = wukongObject.GetComponentInChildren<Animator>(true);
        }

        if (wukongAnimator == null)
        {
            Debug.LogWarning(
                "Map1StoryManager: Không tìm thấy Animator trên Wukong hoặc object con. " +
                "Intro vẫn chạy, nhưng không thể ép Wukong về Idle."
            );
        }
    }

    private bool IsWukongIdleAndStable()
    {
        if (wukongAnimator == null)
        {
            // Không có Animator thì coi như ổn để tránh kẹt intro.
            return true;
        }

        if (wukongAnimator.IsInTransition(0))
        {
            return false;
        }

        AnimatorStateInfo stateInfo = wukongAnimator.GetCurrentAnimatorStateInfo(0);

        bool isIdleState = stateInfo.IsName(wukongIdleStateName);

        bool isRigidbodyStable = true;

        if (wukongRigidbody != null)
        {
            isRigidbodyStable =
                Mathf.Abs(wukongRigidbody.linearVelocity.x) < 0.05f &&
                Mathf.Abs(wukongRigidbody.linearVelocity.y) < 0.05f;
        }

        return isIdleState && isRigidbodyStable;
    }

    private void ForceWukongIdle()
    {
        if (wukongAnimator == null)
        {
            return;
        }

        if (HasAnimatorParameter(wukongAnimator, "Speed"))
        {
            wukongAnimator.SetFloat("Speed", 0f);
        }

        if (HasAnimatorParameter(wukongAnimator, "VerticalVelocity"))
        {
            wukongAnimator.SetFloat("VerticalVelocity", 0f);
        }

        wukongAnimator.Play(wukongIdleStateName, 0, 0f);
    }

    private bool HasAnimatorParameter(Animator animator, string parameterName)
    {
        if (animator == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private void BindUIElements()
    {
        if (dialogueUIDocument == null)
        {
            Debug.LogWarning("Map1StoryManager: Chưa gán Dialogue UIDocument. Hãy kéo UIDocument của Map1PoemDialogueUI vào Inspector.");
            return;
        }

        VisualElement root = dialogueUIDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogWarning("Map1StoryManager: UIDocument chưa có rootVisualElement.");
            return;
        }

        dialogueBox = root.Q<VisualElement>("DialogueBox");
        dialogueText = root.Q<Label>("DialogueText");

        if (dialogueBox == null)
        {
            Debug.LogWarning("Map1StoryManager: Không tìm thấy UI element tên DialogueBox trong UXML.");
        }

        if (dialogueText == null)
        {
            Debug.LogWarning("Map1StoryManager: Không tìm thấy UI element tên DialogueText trong UXML.");
        }
    }

    private void ShowDialogueBox()
    {
        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.Flex;
        }
    }

    private void HideDialogueBox()
    {
        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.None;
        }

        if (dialogueText != null)
        {
            dialogueText.text = "";
        }
    }

    private void HideGlobalHUD()
    {
        if (!hideGlobalHUDDuringIntro)
        {
            return;
        }

        if (globalHUDObject != null)
        {
            globalHUDObject.SetActive(false);
        }
    }

    private void ShowGlobalHUD()
    {
        if (!hideGlobalHUDDuringIntro)
        {
            return;
        }

        if (globalHUDObject != null)
        {
            globalHUDObject.SetActive(true);
        }
    }

    private void LockPlayerAndParty()
    {
        // Tắt điều khiển Wukong.
        if (wukongController != null)
        {
            wukongController.enabled = false;
        }

        // Dừng vật lý Wukong để không bị trôi lúc intro.
        if (wukongRigidbody != null)
        {
            wukongRigidbody.linearVelocity = Vector2.zero;
            wukongRigidbody.angularVelocity = 0f;

            if (!cachedWukongConstraints)
            {
                originalWukongConstraints = wukongRigidbody.constraints;
                cachedWukongConstraints = true;
            }

            // Khóa X và Rotation để Wukong không trôi ngang.
            // Không khóa Y để tránh kẹt nếu nhân vật vừa spawn hơi lệch mặt đất.
            wukongRigidbody.constraints =
                originalWukongConstraints |
                RigidbodyConstraints2D.FreezePositionX |
                RigidbodyConstraints2D.FreezeRotation;
        }

        ForceWukongIdle();

        StopPartyMovement();
    }

    private void UnlockPlayerAndParty()
    {
        // Trả lại constraint gốc cho Wukong.
        if (wukongRigidbody != null && cachedWukongConstraints)
        {
            wukongRigidbody.constraints = originalWukongConstraints;
            wukongRigidbody.linearVelocity = Vector2.zero;
            wukongRigidbody.angularVelocity = 0f;
        }

        // Mở lại điều khiển Wukong.
        if (wukongController != null)
        {
            wukongController.enabled = true;
        }

        RestorePartyMovement();
    }

    private void CachePartyComponents()
    {
        if (partyObjectsToStop == null)
        {
            return;
        }

        cachedPartyMoveScripts = new Behaviour[partyObjectsToStop.Length];
        cachedPartyRigidbodies = new Rigidbody2D[partyObjectsToStop.Length];
        cachedPartyAnimators = new Animator[partyObjectsToStop.Length];

        cachedPartyBodyTypes = new RigidbodyType2D[partyObjectsToStop.Length];
        cachedPartyGravityScales = new float[partyObjectsToStop.Length];
        cachedPartyConstraints = new RigidbodyConstraints2D[partyObjectsToStop.Length];

        for (int i = 0; i < partyObjectsToStop.Length; i++)
        {
            GameObject partyObject = partyObjectsToStop[i];

            if (partyObject == null)
            {
                continue;
            }

            Behaviour[] behaviours = partyObject.GetComponents<Behaviour>();

            foreach (Behaviour behaviour in behaviours)
            {
                if (behaviour != null && behaviour.GetType().Name == "FollowerController")
                {
                    cachedPartyMoveScripts[i] = behaviour;
                    break;
                }
            }

            cachedPartyRigidbodies[i] = partyObject.GetComponent<Rigidbody2D>();
            cachedPartyAnimators[i] = partyObject.GetComponent<Animator>();

            if (cachedPartyRigidbodies[i] != null)
            {
                cachedPartyBodyTypes[i] = cachedPartyRigidbodies[i].bodyType;
                cachedPartyGravityScales[i] = cachedPartyRigidbodies[i].gravityScale;
                cachedPartyConstraints[i] = cachedPartyRigidbodies[i].constraints;
            }
        }
    }

    private void StopPartyMovement()
    {
        if (partyObjectsToStop == null)
        {
            return;
        }

        for (int i = 0; i < partyObjectsToStop.Length; i++)
        {
            if (cachedPartyMoveScripts != null && i < cachedPartyMoveScripts.Length && cachedPartyMoveScripts[i] != null)
            {
                cachedPartyMoveScripts[i].enabled = false;
            }

            if (cachedPartyRigidbodies != null && i < cachedPartyRigidbodies.Length && cachedPartyRigidbodies[i] != null)
            {
                Rigidbody2D rb = cachedPartyRigidbodies[i];

                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;

                if (freezePartyPhysicsDuringIntro)
                {
                    rb.gravityScale = 0f;
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.constraints = RigidbodyConstraints2D.FreezeAll;
                }
            }

            if (cachedPartyAnimators != null && i < cachedPartyAnimators.Length && cachedPartyAnimators[i] != null)
            {
                cachedPartyAnimators[i].SetFloat("Speed", 0f);
            }
        }
    }

    private void RestorePartyMovement()
    {
        if (partyObjectsToStop == null)
        {
            return;
        }

        for (int i = 0; i < partyObjectsToStop.Length; i++)
        {
            if (cachedPartyRigidbodies != null && i < cachedPartyRigidbodies.Length && cachedPartyRigidbodies[i] != null)
            {
                Rigidbody2D rb = cachedPartyRigidbodies[i];

                if (freezePartyPhysicsDuringIntro)
                {
                    rb.bodyType = cachedPartyBodyTypes[i];
                    rb.gravityScale = cachedPartyGravityScales[i];
                    rb.constraints = cachedPartyConstraints[i];
                }

                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            if (cachedPartyMoveScripts != null && i < cachedPartyMoveScripts.Length && cachedPartyMoveScripts[i] != null)
            {
                cachedPartyMoveScripts[i].enabled = true;
            }

            if (cachedPartyAnimators != null && i < cachedPartyAnimators.Length && cachedPartyAnimators[i] != null)
            {
                cachedPartyAnimators[i].SetFloat("Speed", 0f);
            }
        }
    }
}