using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[Serializable]
public class Map25DialogueLine
{
    [Tooltip("Ảnh nhân vật đang nói.")]
    public Sprite avatar;

    [Tooltip("Tên nhân vật đang nói.")]
    public string speakerName;

    [TextArea(2, 5)]
    [Tooltip("Nội dung lời thoại.")]
    public string dialogueText;
}

/// <summary>
/// Dialogue controller riêng cho Map2.5.
/// Chỉ quản lý bật/tắt box thoại, hiện avatar, tên nhân vật, nội dung thoại,
/// và gọi callback khi thoại kết thúc để StoryManager chạy phase tiếp theo.
/// </summary>
public class Map25DialogueController : MonoBehaviour
{
    [Header("UI Document")]
    [Tooltip("UIDocument chứa box hội thoại.")]
    public UIDocument uiDocument;

    [Header("Element Names")]
    [Tooltip("Tên VisualElement cha của box hội thoại trong UI Builder.")]
    public string dialogueBoxName = "DialogueBox";

    [Tooltip("Tên VisualElement hiển thị avatar.")]
    public string avatarImageName = "AvatarImage";

    [Tooltip("Tên Label hiển thị tên nhân vật.")]
    public string speakerNameTextName = "SpeakerNameText";

    [Tooltip("Tên Label hiển thị lời thoại.")]
    public string dialogueTextName = "DialogueText";

    [Tooltip("Tên Label hiển thị gợi ý bấm phím.")]
    public string nextHintTextName = "NextHintText";

    [Header("Input")]
    [Tooltip("Dùng phím E để chuyển câu thoại.")]
    public bool useEKeyToNext = true;

    [Tooltip("Nội dung gợi ý chuyển thoại.")]
    public string nextHint = "Nhấn E để tiếp tục";

    [Header("Pause Control")]
    [Tooltip("Bật lên để khi Pause Game thì không cho bấm E chuyển thoại.")]
    public bool blockInputWhenGamePaused = true;

    [Tooltip("Sau khi Resume khỏi Pause, bắt người chơi nhả phím E rồi mới cho nhận E tiếp. Tránh vừa Resume đã nhảy thoại.")]
    public bool waitEKeyReleaseAfterResume = true;

    [Header("State")]
    public bool isDialoguePlaying;

    private VisualElement root;
    private VisualElement dialogueBox;
    private VisualElement avatarImage;
    private Label speakerNameText;
    private Label dialogueText;
    private Label nextHintText;

    private Map25DialogueLine[] currentLines;
    private int currentIndex;
    private Action onDialogueFinished;

    private bool wasPausedLastFrame;
    private bool waitingEKeyReleaseAfterPause;

    private void Awake()
    {
        SetupReferences();
        HideDialogue();
    }

    private void OnEnable()
    {
        SetupReferences();
        HideDialogue();
    }

    private void Update()
    {
        if (IsGamePausedAndBlocked())
        {
            HandlePausedState();
            return;
        }

        HandleResumeFromPauseState();

        if (!isDialoguePlaying)
        {
            return;
        }

        if (waitingEKeyReleaseAfterPause)
        {
            if (!IsEKeyPressed())
            {
                waitingEKeyReleaseAfterPause = false;
            }

            return;
        }

        if (useEKeyToNext && WasEKeyPressed())
        {
            ShowNextLine();
        }
    }

    private void HandlePausedState()
    {
        wasPausedLastFrame = true;
    }

    private void HandleResumeFromPauseState()
    {
        if (!wasPausedLastFrame)
        {
            return;
        }

        wasPausedLastFrame = false;

        if (waitEKeyReleaseAfterResume)
        {
            waitingEKeyReleaseAfterPause = IsEKeyPressed();
        }
        else
        {
            waitingEKeyReleaseAfterPause = false;
        }

        Debug.Log("Map25DialogueController: Resume khỏi Pause. Chờ nhả E = " + waitingEKeyReleaseAfterPause);
    }

    private void SetupReferences()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            uiDocument = GetComponentInParent<UIDocument>();
        }

#if UNITY_2023_1_OR_NEWER
        if (uiDocument == null)
        {
            UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);

            for (int i = 0; i < documents.Length; i++)
            {
                if (documents[i] != null && documents[i].rootVisualElement != null)
                {
                    uiDocument = documents[i];
                    break;
                }
            }
        }
#else
        if (uiDocument == null)
        {
            UIDocument[] documents = FindObjectsOfType<UIDocument>();

            for (int i = 0; i < documents.Length; i++)
            {
                if (documents[i] != null && documents[i].rootVisualElement != null)
                {
                    uiDocument = documents[i];
                    break;
                }
            }
        }
#endif

        if (uiDocument == null)
        {
            Debug.LogWarning("Map25DialogueController chưa có UIDocument.");
            return;
        }

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogWarning("Map25DialogueController không tìm thấy rootVisualElement.");
            return;
        }

        dialogueBox = root.Q<VisualElement>(dialogueBoxName);
        avatarImage = root.Q<VisualElement>(avatarImageName);
        speakerNameText = root.Q<Label>(speakerNameTextName);
        dialogueText = root.Q<Label>(dialogueTextName);
        nextHintText = root.Q<Label>(nextHintTextName);

        if (dialogueBox == null)
        {
            Debug.LogWarning("Không tìm thấy DialogueBox. Kiểm tra Name trong UI Builder: " + dialogueBoxName);
        }

        if (avatarImage == null)
        {
            Debug.LogWarning("Không tìm thấy AvatarImage. Kiểm tra Name trong UI Builder: " + avatarImageName);
        }

        if (speakerNameText == null)
        {
            Debug.LogWarning("Không tìm thấy SpeakerNameText. Kiểm tra Name trong UI Builder: " + speakerNameTextName);
        }

        if (dialogueText == null)
        {
            Debug.LogWarning("Không tìm thấy DialogueText. Kiểm tra Name trong UI Builder: " + dialogueTextName);
        }

        if (nextHintText == null)
        {
            Debug.LogWarning("Không tìm thấy NextHintText. Kiểm tra Name trong UI Builder: " + nextHintTextName);
        }
    }

    public void StartDialogue(Map25DialogueLine[] lines, Action onFinished = null)
    {
        SetupReferences();

        if (lines == null || lines.Length == 0)
        {
            onFinished?.Invoke();
            return;
        }

        currentLines = lines;
        currentIndex = 0;
        onDialogueFinished = onFinished;
        isDialoguePlaying = true;

        if (waitEKeyReleaseAfterResume)
        {
            waitingEKeyReleaseAfterPause = IsEKeyPressed();
        }
        else
        {
            waitingEKeyReleaseAfterPause = false;
        }

        ShowDialogue();
        ShowCurrentLine();
    }

    public void ShowNextLine()
    {
        if (IsGamePausedAndBlocked())
        {
            return;
        }

        if (!isDialoguePlaying)
        {
            return;
        }

        currentIndex++;

        if (currentLines == null || currentIndex >= currentLines.Length)
        {
            FinishDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (currentLines == null)
        {
            return;
        }

        if (currentIndex < 0 || currentIndex >= currentLines.Length)
        {
            return;
        }

        Map25DialogueLine line = currentLines[currentIndex];

        if (avatarImage != null)
        {
            if (line.avatar != null)
            {
                avatarImage.style.display = DisplayStyle.Flex;
                avatarImage.style.backgroundImage = new StyleBackground(line.avatar);
            }
            else
            {
                avatarImage.style.backgroundImage = null;
                avatarImage.style.display = DisplayStyle.None;
            }
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = line.speakerName;
        }

        if (dialogueText != null)
        {
            dialogueText.text = line.dialogueText;
        }

        if (nextHintText != null)
        {
            nextHintText.text = nextHint;
        }
    }

    private void FinishDialogue()
    {
        isDialoguePlaying = false;
        HideDialogue();

        Action callback = onDialogueFinished;

        onDialogueFinished = null;
        currentLines = null;
        currentIndex = 0;
        waitingEKeyReleaseAfterPause = false;

        callback?.Invoke();
    }

    public void ShowDialogue()
    {
        SetupReferences();

        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.Flex;
            dialogueBox.style.visibility = Visibility.Visible;
            dialogueBox.style.opacity = 1f;
            dialogueBox.pickingMode = PickingMode.Position;
        }
    }

    public void HideDialogue()
    {
        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.None;
            dialogueBox.style.visibility = Visibility.Hidden;
            dialogueBox.style.opacity = 0f;
            dialogueBox.pickingMode = PickingMode.Ignore;
        }
    }

    public void ForceStopDialogue()
    {
        isDialoguePlaying = false;
        onDialogueFinished = null;
        currentLines = null;
        currentIndex = 0;
        waitingEKeyReleaseAfterPause = false;

        HideDialogue();
    }

    private bool WasEKeyPressed()
    {
        if (IsGamePausedAndBlocked())
        {
            return false;
        }

        if (Keyboard.current == null)
        {
            return false;
        }

        return Keyboard.current.eKey.wasPressedThisFrame;
    }

    private bool IsEKeyPressed()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        return Keyboard.current.eKey.isPressed;
    }

    private bool IsGamePausedAndBlocked()
    {
        if (!blockInputWhenGamePaused)
        {
            return false;
        }

        return PauseMenuController.IsPausedGlobal;
    }
}