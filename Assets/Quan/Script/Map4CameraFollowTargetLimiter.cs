using UnityEngine;

public class Map4CameraFollowTargetLimiter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Wukong/player thật.")]
    public Transform player;

    [Tooltip("Camera chính để tính nửa chiều rộng màn hình.")]
    public Camera targetCamera;

    [Tooltip("Map4StoryManager để biết phase hiện tại.")]
    public Map4StoryManager storyManager;

    [Header("Left Map Limit")]
    [Tooltip("Điểm này là mép trái màn hình ban đầu. Camera không bao giờ được nhìn vượt qua bên trái điểm này.")]
    public Transform mapLeftEdgePoint;

    [Tooltip("Luôn khóa mép trái map.")]
    public bool limitLeftAlways = true;

    [Tooltip("Offset mép trái ban đầu. Âm là cho nhìn thêm sang trái, dương là che nhiều hơn bên trái.")]
    public float leftEdgeOffset = 0f;

    [Header("Right Map Limit")]
    [Tooltip("Điểm này là mép phải cuối map. Camera không bao giờ được nhìn vượt qua bên phải điểm này.")]
    public Transform mapRightEdgePoint;

    [Tooltip("Luôn khóa mép phải map.")]
    public bool limitRightAlways = true;

    [Tooltip("Offset mép phải cuối map. Âm là che nhiều hơn bên phải, dương là cho nhìn thêm bên phải.")]
    public float mapRightEdgeOffset = 0f;

    [Header("Phase 2 Left Limit")]
    [Tooltip("Bật cơ chế đổi giới hạn trái khi Enemy123 bắt đầu sinh ra.")]
    public bool usePhase2LeftLimit = true;

    [Tooltip("Từ phase NormalEnemyWave trở đi, camera sẽ lấy điểm này làm mép trái mới.")]
    public Transform phase2LeftEdgePoint;

    [Tooltip("Offset riêng cho mép trái phase 2. Âm là cho nhìn thêm sang trái, dương là che nhiều hơn bên trái.")]
    public float phase2LeftEdgeOffset = 0f;

    [Tooltip("Collider chặn bên trái, chỉ bật khi Enemy123 bắt đầu sinh ra.")]
    public Collider2D phase2LeftBlockerCollider;

    [Tooltip("Object hình ảnh tường chặn bên trái. Có thể để trống nếu muốn tường vô hình.")]
    public GameObject phase2LeftBlockerVisual;

    [Tooltip("Sau khi bật giới hạn trái phase 2 thì giữ luôn đến hết map.")]
    public bool keepPhase2LeftLimitForever = true;

    [Header("Phase 1 Right Limit")]
    [Tooltip("Điểm này là mép phải màn hình khi Enemy4 chưa chết.")]
    public Transform phase1RightEdgePoint;

    [Tooltip("Giới hạn camera bên phải cho đến khi Enemy4 bị hạ.")]
    public bool limitRightUntilEnemy4Defeated = true;

    [Tooltip("Âm là che nhiều hơn bên phải, dương là cho nhìn thêm bên phải.")]
    public float rightEdgeOffset = -0.3f;

    [Header("Phase 2 Boss Right Limit")]
    [Tooltip("Bật giới hạn camera bên phải tại box trước Boss3/Boss4 khi đang đánh Enemy123 hoặc đang thoại trước boss.")]
    public bool usePhase2BossRightLimit = true;

    [Tooltip("Điểm này là mép phải màn hình trong phase Enemy123 / BeforeBossDialogue. Nên đặt trùng hoặc gần box chặn trước boss.")]
    public Transform phase2BossRightEdgePoint;

    [Tooltip("Offset riêng cho mép phải phase 2. Âm là che nhiều hơn bên phải, dương là cho nhìn thêm bên phải.")]
    public float phase2BossRightEdgeOffset = -0.3f;

    [Tooltip("Collider box chặn trước Boss3/Boss4. Có thể kéo BossPreview_Blocker vào đây.")]
    public Collider2D phase2BossBlockerCollider;

    [Tooltip("Object hình ảnh của box/tường chặn trước boss. Có thể để trống nếu tường vô hình.")]
    public GameObject phase2BossBlockerVisual;

    [Tooltip("Bật box chặn trước boss trong phase Enemy123 và BeforeBossDialogue.")]
    public bool controlPhase2BossBlocker = false;

    [Header("Follow")]
    [Tooltip("Có đi theo trục Y của Wukong không.")]
    public bool followPlayerY = false;

    [Tooltip("Y cố định của camera target nếu không follow Y.")]
    public float fixedY = 0f;

    [Tooltip("Z của CameraFollowTarget.")]
    public float fixedZ = 0f;

    [Header("Smooth Follow")]
    [Tooltip("Bật làm mượt CameraFollowTarget để tránh giật khi mở khóa phase.")]
    public bool useSmoothFollow = true;

    [Tooltip("Thời gian camera target trượt về vị trí mới. Càng nhỏ càng nhanh, càng lớn càng mượt.")]
    public float smoothTime = 0.25f;

    [Tooltip("Tốc độ tối đa khi camera target đuổi theo Wukong.")]
    public float maxSmoothSpeed = 80f;

    [Header("Debug")]
    public bool enableDebugLog = false;

    private Vector3 smoothVelocity;
    private bool initialized;

    private bool phase2LeftLimitActivated;
    private bool lastPhase2BossRightLimitActive;

    void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    void Start()
    {
        SetPhase2LeftBlockerActive(false);

        if (controlPhase2BossBlocker)
        {
            SetPhase2BossBlockerActive(false);
        }

        if (player != null)
        {
            Vector3 startPosition = GetClampedTargetPosition();
            transform.position = startPosition;
            initialized = true;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        CheckPhase2LeftLimitActivation();
        UpdatePhase2BossRightLimitBlocker();

        Vector3 targetPosition = GetClampedTargetPosition();

        if (!initialized)
        {
            transform.position = targetPosition;
            initialized = true;
            return;
        }

        if (useSmoothFollow)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref smoothVelocity,
                smoothTime,
                maxSmoothSpeed,
                Time.deltaTime
            );
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    void CheckPhase2LeftLimitActivation()
    {
        if (!usePhase2LeftLimit) return;
        if (storyManager == null) return;

        if (phase2LeftLimitActivated && keepPhase2LeftLimitForever) return;

        if (ShouldActivatePhase2LeftLimit())
        {
            ActivatePhase2LeftLimit();
        }
    }

    bool ShouldActivatePhase2LeftLimit()
    {
        if (storyManager == null) return false;

        return storyManager.currentPhase == Map4StoryManager.Map4Phase.NormalEnemyWave
            || storyManager.currentPhase == Map4StoryManager.Map4Phase.BeforeBossDialogue
            || storyManager.currentPhase == Map4StoryManager.Map4Phase.BossFight
            || storyManager.currentPhase == Map4StoryManager.Map4Phase.Boss5Appear
            || storyManager.currentPhase == Map4StoryManager.Map4Phase.Boss5StoryDialogue
            || storyManager.currentPhase == Map4StoryManager.Map4Phase.WukongTransform
            || storyManager.currentPhase == Map4StoryManager.Map4Phase.EndMap;
    }

    void ActivatePhase2LeftLimit()
    {
        phase2LeftLimitActivated = true;

        SetPhase2LeftBlockerActive(true);

        if (enableDebugLog)
        {
            Debug.Log("Map4CameraFollowTargetLimiter: Đã bật giới hạn trái phase 2.");
        }
    }

    void SetPhase2LeftBlockerActive(bool active)
    {
        if (phase2LeftBlockerCollider != null)
        {
            phase2LeftBlockerCollider.enabled = active;
        }

        if (phase2LeftBlockerVisual != null)
        {
            phase2LeftBlockerVisual.SetActive(active);
        }
    }

    void UpdatePhase2BossRightLimitBlocker()
    {
        bool shouldBeActive = ShouldLimitRightPhase2Boss();

        if (shouldBeActive == lastPhase2BossRightLimitActive) return;

        lastPhase2BossRightLimitActive = shouldBeActive;

        if (controlPhase2BossBlocker)
        {
            SetPhase2BossBlockerActive(shouldBeActive);
        }

        if (enableDebugLog)
        {
            Debug.Log("Map4CameraFollowTargetLimiter: Phase2 Boss Right Limit " + (shouldBeActive ? "ON" : "OFF"));
        }
    }

    void SetPhase2BossBlockerActive(bool active)
    {
        if (phase2BossBlockerCollider != null)
        {
            phase2BossBlockerCollider.enabled = active;
        }

        if (phase2BossBlockerVisual != null)
        {
            phase2BossBlockerVisual.SetActive(active);
        }
    }

    Vector3 GetClampedTargetPosition()
    {
        Vector3 targetPosition = player.position;

        if (!followPlayerY)
        {
            targetPosition.y = fixedY;
        }

        targetPosition.z = fixedZ;

        float halfCameraWidth = GetHalfCameraWidth();

        if (ShouldLimitLeft())
        {
            Transform leftEdgePoint = GetCurrentLeftEdgePoint();

            if (leftEdgePoint != null)
            {
                float currentLeftOffset = GetCurrentLeftEdgeOffset();
                float minTargetX = leftEdgePoint.position.x + halfCameraWidth + currentLeftOffset;

                if (targetPosition.x < minTargetX)
                {
                    targetPosition.x = minTargetX;
                }
            }
        }

        if (ShouldLimitRightPhase1())
        {
            float maxTargetX = phase1RightEdgePoint.position.x - halfCameraWidth + rightEdgeOffset;

            if (targetPosition.x > maxTargetX)
            {
                targetPosition.x = maxTargetX;
            }
        }

        if (ShouldLimitRightPhase2Boss())
        {
            float maxTargetX = phase2BossRightEdgePoint.position.x - halfCameraWidth + phase2BossRightEdgeOffset;

            if (targetPosition.x > maxTargetX)
            {
                targetPosition.x = maxTargetX;
            }
        }

        if (ShouldLimitRightMap())
        {
            float maxTargetX = mapRightEdgePoint.position.x - halfCameraWidth + mapRightEdgeOffset;

            if (targetPosition.x > maxTargetX)
            {
                targetPosition.x = maxTargetX;
            }
        }

        return targetPosition;
    }

    Transform GetCurrentLeftEdgePoint()
    {
        if (phase2LeftLimitActivated && phase2LeftEdgePoint != null)
        {
            return phase2LeftEdgePoint;
        }

        return mapLeftEdgePoint;
    }

    float GetCurrentLeftEdgeOffset()
    {
        if (phase2LeftLimitActivated && phase2LeftEdgePoint != null)
        {
            return phase2LeftEdgeOffset;
        }

        return leftEdgeOffset;
    }

    bool ShouldLimitLeft()
    {
        if (!limitLeftAlways) return false;

        Transform leftEdgePoint = GetCurrentLeftEdgePoint();

        if (leftEdgePoint == null) return false;

        return true;
    }

    bool ShouldLimitRightMap()
    {
        if (!limitRightAlways) return false;
        if (mapRightEdgePoint == null) return false;

        return true;
    }

    bool ShouldLimitRightPhase1()
    {
        if (!limitRightUntilEnemy4Defeated) return false;
        if (storyManager == null) return false;
        if (phase1RightEdgePoint == null) return false;

        return storyManager.currentPhase != Map4StoryManager.Map4Phase.Enemy4Defeated
            && storyManager.currentPhase != Map4StoryManager.Map4Phase.BossIntroDialogue
            && storyManager.currentPhase != Map4StoryManager.Map4Phase.NormalEnemyWave
            && storyManager.currentPhase != Map4StoryManager.Map4Phase.BeforeBossDialogue
            && storyManager.currentPhase != Map4StoryManager.Map4Phase.BossFight
            && storyManager.currentPhase != Map4StoryManager.Map4Phase.Boss5Appear
            && storyManager.currentPhase != Map4StoryManager.Map4Phase.Boss5StoryDialogue
            && storyManager.currentPhase != Map4StoryManager.Map4Phase.WukongTransform
            && storyManager.currentPhase != Map4StoryManager.Map4Phase.EndMap;
    }

    bool ShouldLimitRightPhase2Boss()
    {
        if (!usePhase2BossRightLimit) return false;
        if (storyManager == null) return false;
        if (phase2BossRightEdgePoint == null) return false;

        return storyManager.currentPhase == Map4StoryManager.Map4Phase.NormalEnemyWave
            || storyManager.currentPhase == Map4StoryManager.Map4Phase.BeforeBossDialogue;
    }

    float GetHalfCameraWidth()
    {
        if (targetCamera == null) return 8f;

        if (targetCamera.orthographic)
        {
            return targetCamera.orthographicSize * targetCamera.aspect;
        }

        float distance = Mathf.Abs(targetCamera.transform.position.z);
        float halfHeight = Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * distance;
        return halfHeight * targetCamera.aspect;
    }
}