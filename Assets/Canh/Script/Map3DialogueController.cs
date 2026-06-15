using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[Serializable]
public class Map3DialogueLine
{
    [Tooltip("Ảnh nhân vật đang nói.")]
    public Sprite avatar;

    [Tooltip("Tên nhân vật đang nói.")]
    public string speakerName;

    [TextArea(2, 5)]
    [Tooltip("Nội dung lời thoại.")]
    public string dialogueText;
}

public class Map3DialogueController : MonoBehaviour
{
    [Header("UI Document")]
    [Tooltip("UIDocument riêng của map bạn.")]
    public UIDocument uiDocument;

    [Header("Element Names")]
    [Tooltip("Tên VisualElement cha của box thoại.")]
    public string dialogueBoxName = "dialogue-box";

    [Tooltip("Tên VisualElement avatar.")]
    public string avatarImageName = "dialogue-avatar";

    [Tooltip("Tên Label hiển thị tên nhân vật.")]
    public string speakerNameTextName = "dialogue-name";

    [Tooltip("Tên Label hiển thị nội dung thoại.")]
    public string dialogueTextName = "dialogue-text";

    [Tooltip("Tên Label hiển thị gợi ý phím.")]
    public string nextHintTextName = "dialogue-hint";

    [Header("Input")]
    [Tooltip("Dùng phím E để chuyển câu thoại.")]
    public bool useEKeyToNext = true;

    [Tooltip("Text gợi ý phím tiếp tục.")]
    public string nextHint = "E";

    [Header("State")]
    [Tooltip("Đang chạy thoại hay không.")]
    public bool isDialoguePlaying;

    private VisualElement root;
    private VisualElement dialogueBox;
    private VisualElement avatarImage;
    private Label speakerNameText;
    private Label dialogueText;
    private Label nextHintText;

    private Map3DialogueLine[] currentLines;
    private int currentIndex;
    private Action onDialogueFinished;

    void Awake()
    {
        SetupReferences();
        HideDialogue();
    }

    void OnEnable()
    {
        SetupReferences();
        HideDialogue();
    }

    void Update()
    {
        if (!isDialoguePlaying)
            return;

        if (useEKeyToNext && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ShowNextLine();
        }
    }

    void SetupReferences()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            Debug.LogWarning("Map3DialogueController chưa có UIDocument.");
            return;
        }

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogWarning("Map3DialogueController không tìm thấy rootVisualElement.");
            return;
        }

        dialogueBox = root.Q<VisualElement>(dialogueBoxName);
        avatarImage = root.Q<VisualElement>(avatarImageName);
        speakerNameText = root.Q<Label>(speakerNameTextName);
        dialogueText = root.Q<Label>(dialogueTextName);
        nextHintText = root.Q<Label>(nextHintTextName);

        if (dialogueBox == null)
            Debug.LogWarning("Map3 không tìm thấy dialogue-box: " + dialogueBoxName);

        if (avatarImage == null)
            Debug.LogWarning("Map3 không tìm thấy dialogue-avatar: " + avatarImageName);

        if (speakerNameText == null)
            Debug.LogWarning("Map3 không tìm thấy dialogue-name: " + speakerNameTextName);

        if (dialogueText == null)
            Debug.LogWarning("Map3 không tìm thấy dialogue-text: " + dialogueTextName);

        if (nextHintText == null)
            Debug.LogWarning("Map3 không tìm thấy dialogue-hint: " + nextHintTextName);
    }

    public void StartDialogue(Map3DialogueLine[] lines, Action onFinished = null)
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

        ShowDialogue();
        ShowCurrentLine();
    }

    public void ShowNextLine()
    {
        if (!isDialoguePlaying)
            return;

        currentIndex++;

        if (currentIndex >= currentLines.Length)
        {
            FinishDialogue();
            return;
        }

        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        if (currentLines == null)
            return;

        if (currentIndex < 0 || currentIndex >= currentLines.Length)
            return;

        Map3DialogueLine line = currentLines[currentIndex];

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

    void FinishDialogue()
    {
        isDialoguePlaying = false;
        HideDialogue();

        Action callback = onDialogueFinished;

        onDialogueFinished = null;
        currentLines = null;
        currentIndex = 0;

        callback?.Invoke();
    }

    public void ShowDialogue()
    {
        SetupReferences();

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
}