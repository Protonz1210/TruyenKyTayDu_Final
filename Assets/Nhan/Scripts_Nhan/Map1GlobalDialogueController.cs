using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class Map1GlobalDialogueLine
{
    [FormerlySerializedAs("characterAvatar")]
    [Tooltip("Ảnh nhân vật đang nói.")]
    public Sprite avatar;

    [FormerlySerializedAs("characterName")]
    [Tooltip("Tên nhân vật đang nói.")]
    public string speakerName;

    [FormerlySerializedAs("text")]
    [TextArea(2, 5)]
    [Tooltip("Nội dung lời thoại.")]
    public string dialogueText;
}

public class Map1GlobalDialogueController : MonoBehaviour
{
    public enum DialogueMode
    {
        MissionSingleLine,
        ConversationNextKey
    }

    [Header("UI Document")]
    [FormerlySerializedAs("globalHUDDocument")]
    [Tooltip("UIDocument của GlobalHUD. Không kéo UIDocument của Map1PoemDialogueUI.")]
    public UIDocument uiDocument;

    [Header("Element Names")]
    [FormerlySerializedAs("dialogueBoxElementName")]
    [Tooltip("Tên VisualElement cha của toàn bộ box hội thoại. Với UI hiện tại là dialogue-box.")]
    public string dialogueBoxName = "dialogue-box";

    [FormerlySerializedAs("characterAvatarElementName")]
    [Tooltip("Tên VisualElement hiển thị avatar.")]
    public string avatarImageName = "dialogue-avatar";

    [FormerlySerializedAs("characterNameElementName")]
    [Tooltip("Tên Label hiển thị tên nhân vật.")]
    public string speakerNameTextName = "dialogue-name";

    [FormerlySerializedAs("dialogueTextElementName")]
    [Tooltip("Tên Label hiển thị lời thoại.")]
    public string dialogueTextName = "dialogue-text";

    [FormerlySerializedAs("dialogueHintElementName")]
    [Tooltip("Tên Label hiển thị gợi ý bấm phím.")]
    public string nextHintTextName = "dialogue-hint";

    [Header("Mode")]
    [FormerlySerializedAs("playMode")]
    [Tooltip("MissionSingleLine = hiện 1 câu nhiệm vụ, không bấm E. ConversationNextKey = hội thoại nhiều câu, nhấn E để chuyển.")]
    public DialogueMode dialogueMode = DialogueMode.MissionSingleLine;

    [Header("Input")]
    [FormerlySerializedAs("nextKey")]
    [Tooltip("Phím chuyển câu khi dùng ConversationNextKey.")]
    public Key nextKey = Key.E;

    [Tooltip("Dùng phím tương tác để chuyển câu thoại.")]
    public bool useKeyToNext = true;

    [FormerlySerializedAs("nextHint")]
    [Tooltip("Text gợi ý chuyển thoại.")]
    public string nextHint = "E";

    [FormerlySerializedAs("showHintInConversation")]
    [Tooltip("Hiện hint khi đang ở chế độ ConversationNextKey.")]
    public bool showHintInConversation = true;

    [Tooltip("Chờ người chơi nhả phím E trước rồi mới cho nhận E tiếp, tránh vừa mở thoại đã skip luôn.")]
    public bool waitKeyReleaseBeforeConversationInput = true;

    [Header("Dialogue Lines")]
    [Tooltip("Danh sách thoại chỉnh trực tiếp trong Inspector.")]
    public Map1GlobalDialogueLine[] dialogueLines;

    [Header("Import TXT")]
    [Tooltip("Kéo file .txt vào đây rồi bấm Import TXT To Dialogue Lines. Định dạng khuyên dùng: TÊN|Nội dung thoại.")]
    public TextAsset dialogueTxtFile;

    [Tooltip("Khi import, tự lấy lại avatar từ Dialogue Lines cũ theo tên nhân vật. Nên bật để không phải gán avatar lại.")]
    public bool keepCurrentAvatarsWhenImport = true;

    [Header("UI Ready Wait")]
    [Tooltip("Thời gian tối đa chờ UIDocument GlobalHUD sẵn sàng khi PlayDialogue / StartDialogue được gọi.")]
    public float maxWaitForUIDocumentReady = 2f;

    [Header("State")]
    public bool isDialoguePlaying;

    [Header("Debug")]
    public bool enableDebugLog = false;

    private VisualElement root;
    private VisualElement dialogueBox;
    private VisualElement avatarImage;
    private Label speakerNameText;
    private Label dialogueText;
    private Label nextHintText;

    private Map1GlobalDialogueLine[] currentLines;
    private int currentIndex;
    private Action onDialogueFinished;

    private Coroutine startDialogueCoroutine;
    private bool waitingKeyRelease;

    private void Awake()
    {
        // Không bắt buộc bind UI ở Awake.
        // Vì GlobalHUD có thể đang bị tắt trong Intro/Tutorial.
        TrySetupReferences(false);
        HideDialogueIfReady();
    }

    private void OnEnable()
    {
        // Không Warning ở đây để tránh spam khi GlobalHUD chưa build rootVisualElement.
        TrySetupReferences(false);
    }

    private void Update()
    {
        if (!isDialoguePlaying)
        {
            return;
        }

        if (dialogueMode != DialogueMode.ConversationNextKey)
        {
            return;
        }

        if (!useKeyToNext)
        {
            return;
        }

        if (waitingKeyRelease)
        {
            if (!IsKeyPressed(nextKey))
            {
                waitingKeyRelease = false;
            }

            return;
        }

        if (WasKeyPressed(nextKey))
        {
            ShowNextLine();
        }
    }

    public void PlayDialogue()
    {
        StartDialogue(dialogueLines, null);
    }

    public void StartDialogue(Map1GlobalDialogueLine[] lines, Action onFinished = null)
    {
        if (startDialogueCoroutine != null)
        {
            StopCoroutine(startDialogueCoroutine);
            startDialogueCoroutine = null;
        }

        startDialogueCoroutine = StartCoroutine(StartDialogueWhenUIReadyRoutine(lines, onFinished));
    }

    private IEnumerator StartDialogueWhenUIReadyRoutine(Map1GlobalDialogueLine[] lines, Action onFinished)
    {
        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("Map1GlobalDialogueController: Chưa có Dialogue Lines.");
            onFinished?.Invoke();
            yield break;
        }

        float timer = 0f;

        while (!TrySetupReferences(false))
        {
            timer += Time.unscaledDeltaTime;

            if (timer >= maxWaitForUIDocumentReady)
            {
                Debug.LogWarning("Map1GlobalDialogueController: UIDocument GlobalHUD chưa sẵn sàng. Kiểm tra GlobalHUD có Active, có UIDocument, và đã gán đúng UI Document chưa.");
                onFinished?.Invoke();
                yield break;
            }

            yield return null;
        }

        currentLines = lines;
        currentIndex = 0;
        onDialogueFinished = onFinished;

        ShowDialogue();

        if (dialogueMode == DialogueMode.MissionSingleLine)
        {
            isDialoguePlaying = false;
            ShowCurrentLine();
            HideHint();

            startDialogueCoroutine = null;
            yield break;
        }

        isDialoguePlaying = true;

        if (waitKeyReleaseBeforeConversationInput)
        {
            waitingKeyRelease = IsKeyPressed(nextKey);
        }
        else
        {
            waitingKeyRelease = false;
        }

        ShowCurrentLine();

        startDialogueCoroutine = null;
    }

    public void ShowNextLine()
    {
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

        Map1GlobalDialogueLine line = currentLines[currentIndex];

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
            if (dialogueMode == DialogueMode.ConversationNextKey && showHintInConversation)
            {
                nextHintText.text = nextHint;
                nextHintText.style.display = DisplayStyle.Flex;
            }
            else
            {
                nextHintText.style.display = DisplayStyle.None;
            }
        }

        if (enableDebugLog)
        {
            Debug.Log("Map1GlobalDialogueController: " + line.speakerName + ": " + line.dialogueText);
        }
    }

    private void FinishDialogue()
    {
        isDialoguePlaying = false;
        waitingKeyRelease = false;

        Action callback = onDialogueFinished;

        onDialogueFinished = null;
        currentLines = null;
        currentIndex = 0;

        HideDialogue();

        callback?.Invoke();
    }

    public void ShowDialogue()
    {
        if (!TrySetupReferences(true))
        {
            return;
        }

        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.Flex;
            dialogueBox.style.visibility = Visibility.Visible;
        }
    }

    public void HideDialogue()
    {
        if (startDialogueCoroutine != null)
        {
            StopCoroutine(startDialogueCoroutine);
            startDialogueCoroutine = null;
        }

        isDialoguePlaying = false;
        waitingKeyRelease = false;

        if (!TrySetupReferences(false))
        {
            return;
        }

        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.None;
        }

        if (dialogueText != null)
        {
            dialogueText.text = "";
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = "";
        }

        if (avatarImage != null)
        {
            avatarImage.style.backgroundImage = null;
            avatarImage.style.display = DisplayStyle.None;
        }

        HideHint();
    }

    private void HideDialogueIfReady()
    {
        if (!TrySetupReferences(false))
        {
            return;
        }

        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.None;
        }

        HideHint();
    }

    private void HideHint()
    {
        if (nextHintText != null)
        {
            nextHintText.style.display = DisplayStyle.None;
        }
    }

    private bool TrySetupReferences(bool logWarning)
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            uiDocument = GetComponentInParent<UIDocument>();
        }

        if (uiDocument == null)
        {
            if (logWarning)
            {
                Debug.LogWarning("Map1GlobalDialogueController: Chưa gán UIDocument. Hãy kéo UIDocument của GlobalHUD vào UI Document.");
            }

            return false;
        }

        if (!uiDocument.gameObject.activeInHierarchy)
        {
            // GlobalHUD đang bị tắt trong intro/tutorial thì chưa bind.
            // Đây là trạng thái hợp lệ, không cần warning.
            return false;
        }

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            // UIDocument có thể chưa build rootVisualElement trong frame hiện tại.
            // Không warning ở Awake/OnEnable để tránh spam.
            if (logWarning)
            {
                Debug.LogWarning("Map1GlobalDialogueController: UIDocument chưa có rootVisualElement. Hãy kiểm tra GlobalHUD đang Active và UIDocument có Source Asset.");
            }

            return false;
        }

        dialogueBox = root.Q<VisualElement>(dialogueBoxName);
        avatarImage = root.Q<VisualElement>(avatarImageName);
        speakerNameText = root.Q<Label>(speakerNameTextName);
        dialogueText = root.Q<Label>(dialogueTextName);
        nextHintText = root.Q<Label>(nextHintTextName);

        bool hasRequiredUI = true;

        if (dialogueBox == null)
        {
            hasRequiredUI = false;

            if (logWarning)
            {
                Debug.LogWarning("Map1GlobalDialogueController: Không tìm thấy dialogue box. Kiểm tra tên trong UI Builder: " + dialogueBoxName);
            }
        }

        if (dialogueText == null)
        {
            hasRequiredUI = false;

            if (logWarning)
            {
                Debug.LogWarning("Map1GlobalDialogueController: Không tìm thấy dialogue text. Kiểm tra tên trong UI Builder: " + dialogueTextName);
            }
        }

        if (speakerNameText == null && logWarning)
        {
            Debug.LogWarning("Map1GlobalDialogueController: Không tìm thấy speaker name. Kiểm tra tên trong UI Builder: " + speakerNameTextName);
        }

        if (avatarImage == null && logWarning)
        {
            Debug.LogWarning("Map1GlobalDialogueController: Không tìm thấy avatar image. Kiểm tra tên trong UI Builder: " + avatarImageName);
        }

        if (nextHintText == null && logWarning)
        {
            Debug.LogWarning("Map1GlobalDialogueController: Không tìm thấy next hint. Kiểm tra tên trong UI Builder: " + nextHintTextName);
        }

        return hasRequiredUI;
    }

    private bool WasKeyPressed(Key key)
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        KeyControl keyControl = Keyboard.current[key];

        return keyControl != null && keyControl.wasPressedThisFrame;
    }

    private bool IsKeyPressed(Key key)
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        KeyControl keyControl = Keyboard.current[key];

        return keyControl != null && keyControl.isPressed;
    }

    public bool ImportDialogueFromTextAsset()
    {
        if (dialogueTxtFile == null)
        {
            Debug.LogWarning("Map1GlobalDialogueController: Chưa kéo file TXT vào Dialogue Txt File.");
            return false;
        }

        Map1GlobalDialogueLine[] importedLines = ParseDialogueText(dialogueTxtFile.text);

        if (importedLines == null || importedLines.Length == 0)
        {
            Debug.LogWarning("Map1GlobalDialogueController: File TXT không có dòng thoại hợp lệ. Dùng định dạng: TÊN|Nội dung thoại.");
            return false;
        }

        dialogueLines = importedLines;

        Debug.Log("Map1GlobalDialogueController: Đã import " + dialogueLines.Length + " dòng thoại từ file TXT: " + dialogueTxtFile.name);
        return true;
    }

    private Map1GlobalDialogueLine[] ParseDialogueText(string rawText)
    {
        List<Map1GlobalDialogueLine> result = new List<Map1GlobalDialogueLine>();

        if (string.IsNullOrWhiteSpace(rawText))
        {
            return result.ToArray();
        }

        Dictionary<string, Sprite> avatarLookup = BuildCurrentAvatarLookup();

        string normalizedText = rawText.Replace("\r\n", "\n").Replace("\r", "\n");
        string[] rawLines = normalizedText.Split('\n');

        Map1GlobalDialogueLine lastDialogueLine = null;

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

            if (TryParseImportedLine(line, out speaker, out text))
            {
                speaker = CleanSpeakerName(speaker);
                text = text.Trim();

                // Những dòng kiểu "Đà La Trang:" chỉ là tiêu đề địa điểm, không phải thoại.
                if (string.IsNullOrWhiteSpace(speaker) || string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                Map1GlobalDialogueLine dialogueLine = new Map1GlobalDialogueLine();
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
                // Nếu một câu thoại bị xuống dòng trong TXT mà dòng sau không có TÊN| hoặc TÊN:
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

    private bool TryParseImportedLine(string line, out string speaker, out string text)
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

    private Dictionary<string, Sprite> BuildCurrentAvatarLookup()
    {
        Dictionary<string, Sprite> avatarLookup = new Dictionary<string, Sprite>();

        if (dialogueLines == null)
        {
            return avatarLookup;
        }

        for (int i = 0; i < dialogueLines.Length; i++)
        {
            Map1GlobalDialogueLine line = dialogueLines[i];

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
[CustomEditor(typeof(Map1GlobalDialogueController))]
public class Map1GlobalDialogueControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Map1GlobalDialogueController controller = (Map1GlobalDialogueController)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("TXT Import Tool", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Kéo file .txt vào Dialogue Txt File rồi bấm nút import.\n\n" +
            "Định dạng tốt nhất:\n" +
            "LÃO NÔNG|Aaaa yêu quái, yêu quái....\n" +
            "TÔN NGỘ KHÔNG?|Xin thí chủ đừng hoảng sợ...\n\n" +
            "Cũng hỗ trợ dạng:\n" +
            "LÃO NÔNG: Aaaa yêu quái, yêu quái....\n\n" +
            "Dòng tiêu đề kiểu Đà La Trang: sẽ tự bị bỏ qua.",
            MessageType.Info
        );

        using (new EditorGUI.DisabledScope(controller.dialogueTxtFile == null))
        {
            if (GUILayout.Button("Import TXT To Dialogue Lines"))
            {
                Undo.RecordObject(controller, "Import Dialogue TXT");

                bool success = controller.ImportDialogueFromTextAsset();

                if (success)
                {
                    EditorUtility.SetDirty(controller);
                    serializedObject.Update();
                }
            }
        }
    }
}
#endif