using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[Serializable]
public class Map5DialogueLine
{
    [Tooltip("Ảnh avatar nhân vật đang nói. Có thể để trống nếu chưa cần.")]
    public Sprite avatar;

    [Tooltip("Tên nhân vật đang nói.")]
    public string speakerName;

    [TextArea(2, 5)]
    [Tooltip("Nội dung lời thoại.")]
    public string dialogueText;
}

public class Map5DialogueController : MonoBehaviour
{
    [Header("UI Document")]
    [Tooltip("UIDocument chứa UXML hội thoại riêng của Map 5.")]
    public UIDocument uiDocument;

    [Header("Element Names")]
    [Tooltip("Tên VisualElement cha bao toàn bộ UI hội thoại.")]
    public string dialogueBoxName = "dialogue-box";

    [Tooltip("Tên khung thoại chính.")]
    public string dialogueFrameName = "dialogue-frame";

    [Tooltip("Tên vùng hiển thị avatar.")]
    public string avatarImageName = "dialogue-avatar";

    [Tooltip("Tên Label hiển thị tên nhân vật.")]
    public string speakerNameTextName = "dialogue-name";

    [Tooltip("Tên Label hiển thị nội dung thoại.")]
    public string dialogueTextName = "dialogue-text";

    [Tooltip("Tên Label hiển thị phím gợi ý.")]
    public string nextHintTextName = "dialogue-hint";

    [Header("Input")]
    [Tooltip("Dùng phím E để chuyển sang câu thoại tiếp theo.")]
    public bool useEKeyToNext = true;

    [Tooltip("Nội dung hiển thị ở ô gợi ý phím.")]
    public string nextHint = "E";

    [Header("Test Dialogue")]
    [Tooltip("Bật để vào Play là tự chạy hội thoại test.")]
    public bool autoStartTestDialogueOnPlay = true;

    [Tooltip("Bật để bấm T chạy lại hội thoại test.")]
    public bool useTKeyToStartTestDialogue = true;

    [Tooltip("Danh sách câu thoại test để kiểm tra riêng UI Map 5.")]
    public Map5DialogueLine[] testDialogueLines;

    [Header("State")]
    [Tooltip("Chỉ bật khi hội thoại đang chạy. Khi tắt thì bấm E không có tác dụng.")]
    public bool isDialoguePlaying;

    private VisualElement root;
    private VisualElement dialogueBox;
    private VisualElement dialogueFrame;
    private VisualElement avatarImage;
    private Label speakerNameText;
    private Label dialogueText;
    private Label nextHintText;

    private Map5DialogueLine[] currentLines;
    private int currentIndex;
    private Action onDialogueFinished;

    private void Awake()
    {
        SetupReferences();
        HideDialogue();
    }

    private void Start()
    {
        if (autoStartTestDialogueOnPlay)
        {
            StartDialogue(testDialogueLines, () =>
            {
                Debug.Log("[Map5DialogueController] Test dialogue finished.");
            });
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (useTKeyToStartTestDialogue && Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (!isDialoguePlaying)
            {
                StartDialogue(testDialogueLines, () =>
                {
                    Debug.Log("[Map5DialogueController] Test dialogue finished.");
                });
            }
        }

        if (!isDialoguePlaying)
        {
            return;
        }

        if (useEKeyToNext && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ShowNextLine();
        }
    }

    private void SetupReferences()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            Debug.LogError("[Map5DialogueController] Chưa có UIDocument trên object này.");
            return;
        }

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogError("[Map5DialogueController] Không tìm thấy rootVisualElement.");
            return;
        }

        dialogueBox = root.Q<VisualElement>(dialogueBoxName);
        dialogueFrame = root.Q<VisualElement>(dialogueFrameName);
        avatarImage = root.Q<VisualElement>(avatarImageName);
        speakerNameText = root.Q<Label>(speakerNameTextName);
        dialogueText = root.Q<Label>(dialogueTextName);
        nextHintText = root.Q<Label>(nextHintTextName);

        if (dialogueBox == null)
        {
            Debug.LogError("[Map5DialogueController] Không tìm thấy element: " + dialogueBoxName);
        }

        if (dialogueFrame == null)
        {
            Debug.LogWarning("[Map5DialogueController] Không tìm thấy element: " + dialogueFrameName);
        }

        if (avatarImage == null)
        {
            Debug.LogWarning("[Map5DialogueController] Không tìm thấy element: " + avatarImageName);
        }

        if (speakerNameText == null)
        {
            Debug.LogError("[Map5DialogueController] Không tìm thấy Label: " + speakerNameTextName);
        }

        if (dialogueText == null)
        {
            Debug.LogError("[Map5DialogueController] Không tìm thấy Label: " + dialogueTextName);
        }

        if (nextHintText == null)
        {
            Debug.LogWarning("[Map5DialogueController] Không tìm thấy Label: " + nextHintTextName);
        }
    }

    public void StartDialogue(Map5DialogueLine[] lines, Action onFinished = null)
    {
        SetupReferences();

        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("[Map5DialogueController] Không có dòng thoại nào để chạy.");
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
        {
            return;
        }

        currentIndex++;

        if (currentIndex >= currentLines.Length)
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

        Map5DialogueLine line = currentLines[currentIndex];

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

        Debug.Log("[Map5DialogueController] Đang hiện câu " + currentIndex + " - " + line.speakerName);
    }

    private void FinishDialogue()
    {
        isDialoguePlaying = false;
        HideDialogue();

        Action callback = onDialogueFinished;

        onDialogueFinished = null;
        currentLines = null;
        currentIndex = 0;

        Debug.Log("[Map5DialogueController] Hội thoại kết thúc.");

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
}