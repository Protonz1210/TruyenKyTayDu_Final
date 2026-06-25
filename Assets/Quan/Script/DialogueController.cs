using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[Serializable]
public class DialogueLine
{
    [Tooltip("Ảnh nhân vật đang nói.")]
    public Sprite avatar;

    [Tooltip("Tên nhân vật đang nói.")]
    public string speakerName;

    [TextArea(2, 5)]
    [Tooltip("Nội dung lời thoại.")]
    public string dialogueText;
}

public class DialogueController : MonoBehaviour
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

    private DialogueLine[] currentLines;
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

        Debug.Log("DialogueController: Resume khỏi Pause. Chờ nhả E = " + waitingEKeyReleaseAfterPause);
    }

    private void SetupReferences()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            Debug.LogWarning("DialogueController chưa có UIDocument.");
            return;
        }

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogWarning("DialogueController không tìm thấy rootVisualElement.");
            return;
        }

        dialogueBox = root.Q<VisualElement>(dialogueBoxName);
        avatarImage = root.Q<VisualElement>(avatarImageName);
        speakerNameText = root.Q<Label>(speakerNameTextName);
        dialogueText = root.Q<Label>(dialogueTextName);
        nextHintText = root.Q<Label>(nextHintTextName);

        if (dialogueBox == null)
        {
            Debug.LogWarning("Không tìm thấy DialogueBox trong UI Document. Kiểm tra Name trong UI Builder: " + dialogueBoxName);
        }

        if (avatarImage == null)
        {
            Debug.LogWarning("Không tìm thấy AvatarImage trong UI Document. Kiểm tra Name trong UI Builder: " + avatarImageName);
        }

        if (speakerNameText == null)
        {
            Debug.LogWarning("Không tìm thấy SpeakerNameText trong UI Document. Kiểm tra Name trong UI Builder: " + speakerNameTextName);
        }

        if (dialogueText == null)
        {
            Debug.LogWarning("Không tìm thấy DialogueText trong UI Document. Kiểm tra Name trong UI Builder: " + dialogueTextName);
        }

        if (nextHintText == null)
        {
            Debug.LogWarning("Không tìm thấy NextHintText trong UI Document. Kiểm tra Name trong UI Builder: " + nextHintTextName);
        }
    }

    public void StartDialogue(DialogueLine[] lines, Action onFinished = null)
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

        DialogueLine line = currentLines[currentIndex];

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
        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.Flex;
        }
    }

    public void HideDialogue()
    {
        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.None;
        }
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