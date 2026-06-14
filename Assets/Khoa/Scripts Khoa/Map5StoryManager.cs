using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Map5StoryManager : MonoBehaviour
{
    public enum Map5StoryMode
    {
        Ground_5_0,
        Heaven_5_1
    }

    [Header("Map Mode")]
    [Tooltip("Ground_5_0: map dưới mặt đất, không có Phật Tổ, chạy xong thì fade đen, hiện chữ cinematic và chuyển sang MAP 5.1. Heaven_5_1: map thiên đình, có Phật Tổ và BuddhaBowl.")]
    public Map5StoryMode storyMode = Map5StoryMode.Ground_5_0;

    [Header("References")]
    [Tooltip("Kéo object có script Map5DialogueController vào đây.")]
    public Map5DialogueController dialogueController;

    [Tooltip("Kéo object FinalWukongZoneDuelTest vào đây.")]
    public FinalWukongZoneDuelTest duelTest;

    [Tooltip("Kéo object có script Map5SceneFadeController vào đây.")]
    public Map5SceneFadeController sceneFadeController;

    [Tooltip("Chỉ dùng ở Heaven_5_1. Kéo object có script Map5BuddhaInterventionController vào đây.")]
    public Map5BuddhaInterventionController buddhaInterventionController;

    [Header("Dialogue Beats")]
    [Tooltip("Hội thoại mở đầu của map hiện tại.")]
    public Map5DialogueLine[] introDialogueLines;

    [Tooltip("Hội thoại sau khi 2 Wukong đánh xong lần 1.")]
    public Map5DialogueLine[] afterBeat1DialogueLines;

    [Tooltip("Chỉ dùng ở MAP 5.1 nếu cần đánh lần 2.")]
    public Map5DialogueLine[] afterBeat2DialogueLines;

    [Tooltip("Chỉ dùng ở MAP 5.1 nếu cần đánh lần 3 hoặc đoạn chuẩn bị Phật Tổ can thiệp.")]
    public Map5DialogueLine[] afterBeat3DialogueLines;

    [Tooltip("Chỉ dùng ở Heaven_5_1, sau khi Phật Tổ dùng bát và FakeWukong Die.")]
    public Map5DialogueLine[] endDialogueLines;

    [Header("Ground 5.0 Settings")]
    [Tooltip("Tên scene thiên đình sẽ chuyển sang sau khi MAP 5.0 kết thúc.")]
    public string nextSceneName = "MAP 5.1";

    [Tooltip("Thời gian chờ sau hội thoại cuối MAP 5.0 rồi mới bắt đầu fade đen.")]
    public float delayBeforeLoadNextScene = 1f;

    [Tooltip("Bật: khi kết thúc MAP 5.0 sẽ hiện chữ cinematic trên nền đen trước khi load MAP 5.1.")]
    public bool useTransitionTextBeforeLoad = true;

    [Tooltip("Chữ hiện giữa màn hình đen khi chuyển từ MAP 5.0 sang MAP 5.1.")]
    public string transitionTextToHeaven = "Linh Sơn – Trước Phật Tổ";

    [Header("Heaven 5.1 Settings")]
    [Tooltip("Heaven_5_1 có dùng Phật Tổ can thiệp không.")]
    public bool useBuddhaIntervention = true;

    [Tooltip("Bật: ở MAP 5.1 sẽ chờ fade sáng xong rồi mới bắt đầu hội thoại.")]
    public bool waitFadeInBeforeStartStory = true;

    [Tooltip("Thời gian chờ nhẹ trước khi bắt đầu story, tránh việc StoryManager chạy trước FadeController.")]
    public float startStoryDelay = 0.1f;

    [Header("Final Ending")]
    [Tooltip("Bật: sau End Dialogue ở MAP 5.1 sẽ fade đen và hiện chữ kết thúc.")]
    public bool useFinalEndingFade = false;

    [Tooltip("Thời gian chờ sau khi End Dialogue kết thúc rồi mới fade đen.")]
    public float delayBeforeFinalFade = 0.6f;

    [Tooltip("Chữ cinematic hiện ở cuối MAP 5.1.")]
    public string finalEndingText = "Hành trình thỉnh kinh vẫn tiếp tục...";

    [Tooltip("Sau khi hiện chữ kết thúc, giữ màn hình đen luôn.")]
    public bool keepBlackScreenAfterEnding = true;

    [Header("Start Settings")]
    [Tooltip("Bật: vào Play là tự chạy flow cinematic.")]
    public bool autoStartOnPlay = true;

    [Tooltip("Bật: bấm phím K để test lại flow từ đầu.")]
    public bool enableTestKey = true;

    [Header("State")]
    [Tooltip("Đang chạy flow cinematic hay không. Khi bật thì không cho kích hoạt lại.")]
    public bool isStoryRunning;

    [Tooltip("Đang ở đoạn đánh nhau hay không.")]
    public bool isDuelBeatRunning;

    [Tooltip("Đang ở đoạn Phật Tổ can thiệp hay không.")]
    public bool isBuddhaInterventionRunning;

    [Tooltip("Đang ở đoạn ending cuối map hay không.")]
    public bool isEndingRunning;

    [Tooltip("Beat hiện tại đang chạy. 0 = dialogue, 1/2/3 = duel beat, 99 = Buddha intervention, 100 = end dialogue, 999 = ending.")]
    public int currentBeatIndex;

    private Coroutine storyRoutine;

    private void Start()
    {
        if (autoStartOnPlay)
        {
            StartCoroutine(AutoStartRoutine());
        }
    }

    private IEnumerator AutoStartRoutine()
    {
        yield return new WaitForSeconds(startStoryDelay);

        if (storyMode == Map5StoryMode.Heaven_5_1 && waitFadeInBeforeStartStory)
        {
            if (sceneFadeController != null)
            {
                yield return new WaitUntil(() => sceneFadeController.isFading == false);
            }
        }

        StartStoryFlow();
    }

    private void Update()
    {
        if (!enableTestKey)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            StartStoryFlow();
        }
    }

    public void StartStoryFlow()
    {
        if (isStoryRunning)
        {
            Debug.LogWarning("[Map5StoryManager] Story đang chạy, không chạy lại.");
            return;
        }

        if (storyMode == Map5StoryMode.Ground_5_0)
        {
            storyRoutine = StartCoroutine(GroundMapFlowRoutine());
        }
        else
        {
            storyRoutine = StartCoroutine(HeavenMapFlowRoutine());
        }
    }

    private IEnumerator GroundMapFlowRoutine()
    {
        isStoryRunning = true;
        isDuelBeatRunning = false;
        isBuddhaInterventionRunning = false;
        isEndingRunning = false;
        currentBeatIndex = 0;

        Debug.Log("[Map5StoryManager] Bắt đầu flow MAP 5.0 dưới mặt đất.");

        yield return StartCoroutine(PlayDialogueBeatRoutine(
            introDialogueLines,
            "Ground Intro Dialogue",
            0
        ));

        yield return StartCoroutine(PlayDuelBeatRoutine(
            1,
            "Ground Duel Beat 1"
        ));

        yield return StartCoroutine(PlayDialogueBeatRoutine(
            afterBeat1DialogueLines,
            "Ground After Beat 1 Dialogue",
            0
        ));

        Debug.Log("[Map5StoryManager] MAP 5.0 kết thúc. Chuẩn bị fade đen, hiện chữ và chuyển sang scene: " + nextSceneName);

        yield return new WaitForSeconds(delayBeforeLoadNextScene);

        if (sceneFadeController != null)
        {
            yield return StartCoroutine(sceneFadeController.FadeOutRoutine());

            if (useTransitionTextBeforeLoad)
            {
                sceneFadeController.useCinematicTextBeforeLoad = true;
                sceneFadeController.cinematicText = transitionTextToHeaven;

                yield return StartCoroutine(sceneFadeController.PlayCinematicTextRoutine());
            }

            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("[Map5StoryManager] Chưa gán Scene Fade Controller, sẽ chuyển scene không có fade.");
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private IEnumerator HeavenMapFlowRoutine()
    {
        isStoryRunning = true;
        isDuelBeatRunning = false;
        isBuddhaInterventionRunning = false;
        isEndingRunning = false;
        currentBeatIndex = 0;

        Debug.Log("[Map5StoryManager] Bắt đầu flow MAP 5.1 thiên đình.");

        yield return StartCoroutine(PlayDialogueBeatRoutine(
            introDialogueLines,
            "Heaven Intro Dialogue",
            0
        ));

        yield return StartCoroutine(PlayDuelBeatRoutine(
            1,
            "Heaven Duel Beat 1"
        ));

        yield return StartCoroutine(PlayDialogueBeatRoutine(
            afterBeat1DialogueLines,
            "Heaven After Beat 1 Dialogue",
            0
        ));

        yield return StartCoroutine(PlayDuelBeatRoutine(
            2,
            "Heaven Duel Beat 2"
        ));

        yield return StartCoroutine(PlayDialogueBeatRoutine(
            afterBeat2DialogueLines,
            "Heaven After Beat 2 Dialogue",
            0
        ));

        yield return StartCoroutine(PlayDuelBeatRoutine(
            3,
            "Heaven Duel Beat 3"
        ));

        yield return StartCoroutine(PlayDialogueBeatRoutine(
            afterBeat3DialogueLines,
            "Heaven After Beat 3 Dialogue",
            0
        ));

        if (useBuddhaIntervention)
        {
            yield return StartCoroutine(PlayBuddhaInterventionBeatRoutine());
        }

        yield return StartCoroutine(PlayDialogueBeatRoutine(
            endDialogueLines,
            "Heaven End Dialogue",
            100
        ));

        if (useFinalEndingFade)
        {
            yield return StartCoroutine(PlayFinalEndingRoutine());
        }

        Debug.Log("[Map5StoryManager] Flow MAP 5.1 đã chạy xong.");

        isStoryRunning = false;
        isDuelBeatRunning = false;
        isBuddhaInterventionRunning = false;
        isEndingRunning = false;
        currentBeatIndex = 0;
        storyRoutine = null;
    }

    private IEnumerator PlayDialogueBeatRoutine(Map5DialogueLine[] lines, string beatName, int beatIndex)
    {
        currentBeatIndex = beatIndex;

        Debug.Log("[Map5StoryManager] Bắt đầu " + beatName);

        if (dialogueController == null)
        {
            Debug.LogError("[Map5StoryManager] Chưa gán Dialogue Controller.");
            yield break;
        }

        bool dialogueFinished = false;

        dialogueController.StartDialogue(lines, () =>
        {
            dialogueFinished = true;
        });

        yield return new WaitUntil(() => dialogueFinished);

        Debug.Log("[Map5StoryManager] Kết thúc " + beatName);
    }

    private IEnumerator PlayDuelBeatRoutine(int beatIndex, string beatName)
    {
        Debug.Log("[Map5StoryManager] Bắt đầu " + beatName);

        if (duelTest == null)
        {
            Debug.LogError("[Map5StoryManager] Chưa gán Duel Test.");
            yield break;
        }

        currentBeatIndex = beatIndex;
        isDuelBeatRunning = true;

        bool duelFinished = false;

        duelTest.PlayDuelOnce(() =>
        {
            duelFinished = true;
        });

        yield return new WaitUntil(() => duelFinished);

        isDuelBeatRunning = false;

        Debug.Log("[Map5StoryManager] Kết thúc " + beatName);
    }

    private IEnumerator PlayBuddhaInterventionBeatRoutine()
    {
        Debug.Log("[Map5StoryManager] Bắt đầu Buddha Intervention.");

        if (buddhaInterventionController == null)
        {
            Debug.LogError("[Map5StoryManager] Chưa gán Buddha Intervention Controller.");
            yield break;
        }

        currentBeatIndex = 99;
        isBuddhaInterventionRunning = true;

        bool interventionFinished = false;

        buddhaInterventionController.PlayInterventionOnce(() =>
        {
            interventionFinished = true;
        });

        yield return new WaitUntil(() => interventionFinished);

        isBuddhaInterventionRunning = false;

        Debug.Log("[Map5StoryManager] Kết thúc Buddha Intervention.");
    }

    private IEnumerator PlayFinalEndingRoutine()
    {
        Debug.Log("[Map5StoryManager] Bắt đầu Final Ending.");

        currentBeatIndex = 999;
        isEndingRunning = true;

        yield return new WaitForSeconds(delayBeforeFinalFade);

        if (sceneFadeController == null)
        {
            Debug.LogWarning("[Map5StoryManager] Chưa gán Scene Fade Controller, không thể chạy ending fade.");
            isEndingRunning = false;
            yield break;
        }

        yield return StartCoroutine(sceneFadeController.FadeOutRoutine());

        sceneFadeController.useCinematicTextBeforeLoad = true;
        sceneFadeController.cinematicText = finalEndingText;

        yield return StartCoroutine(sceneFadeController.PlayCinematicTextRoutine());

        if (!keepBlackScreenAfterEnding)
        {
            yield return StartCoroutine(sceneFadeController.FadeInRoutine());
        }

        isEndingRunning = false;

        Debug.Log("[Map5StoryManager] Kết thúc Final Ending.");
    }
}