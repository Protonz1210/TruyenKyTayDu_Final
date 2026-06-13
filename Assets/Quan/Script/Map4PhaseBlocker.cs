using UnityEngine;

public class Map4PhaseBlocker : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Manager tổng của Map 4.")]
    public Map4StoryManager storyManager;

    [Tooltip("Enemy4/yêu quái tuần núi. Nếu Enemy4 chết thì mở blocker.")]
    public Enemy4Controller enemy4;

    [Header("Blocker")]
    [Tooltip("Collider dùng để chặn đường.")]
    public Collider2D blockerCollider;

    [Tooltip("Object hiển thị tường/cổng chặn. Có thể để trống nếu muốn tường vô hình.")]
    public GameObject blockerVisual;

    [Header("Unlock Condition")]
    [Tooltip("Mở blocker khi Enemy4 chết.")]
    public bool unlockWhenEnemy4Dead = true;

    [Tooltip("Mở blocker khi Map4StoryManager chuyển sang phase Enemy4Defeated.")]
    public bool unlockWhenPhaseEnemy4Defeated = true;

    [Header("Debug")]
    public bool enableDebugLog = true;

    private bool isUnlocked;

    void Awake()
    {
        if (blockerCollider == null)
        {
            blockerCollider = GetComponent<Collider2D>();
        }

        SetBlockerActive(true);
    }

    void Start()
    {
        SetBlockerActive(true);
    }

    void Update()
    {
        if (isUnlocked) return;

        if (ShouldUnlock())
        {
            UnlockBlocker();
        }
    }

    bool ShouldUnlock()
    {
        if (unlockWhenEnemy4Dead && enemy4 != null && enemy4.IsDead())
        {
            return true;
        }

        if (unlockWhenPhaseEnemy4Defeated && storyManager != null)
        {
            if (storyManager.currentPhase == Map4StoryManager.Map4Phase.Enemy4Defeated)
            {
                return true;
            }
        }

        return false;
    }

    public void UnlockBlocker()
    {
        if (isUnlocked) return;

        isUnlocked = true;
        SetBlockerActive(false);

        if (enableDebugLog)
        {
            Debug.Log("Phase1_Blocker đã mở. Người chơi có thể đi tiếp sang phase 2.");
        }
    }

    public void LockBlocker()
    {
        isUnlocked = false;
        SetBlockerActive(true);

        if (enableDebugLog)
        {
            Debug.Log("Phase1_Blocker đang khóa đường sang phase 2.");
        }
    }

    void SetBlockerActive(bool active)
    {
        if (blockerCollider != null)
        {
            blockerCollider.enabled = active;
        }

        if (blockerVisual != null)
        {
            blockerVisual.SetActive(active);
        }
    }
}