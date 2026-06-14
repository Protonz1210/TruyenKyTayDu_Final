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
    [Tooltip("Điểm này là mép trái màn hình. Camera không bao giờ được nhìn vượt qua bên trái điểm này.")]
    public Transform mapLeftEdgePoint;

    [Tooltip("Luôn khóa mép trái map.")]
    public bool limitLeftAlways = true;

    [Tooltip("Âm là cho nhìn thêm sang trái, dương là che nhiều hơn bên trái.")]
    public float leftEdgeOffset = 0f;

    [Header("Phase 1 Right Limit")]
    [Tooltip("Điểm này là mép phải màn hình khi Enemy4 chưa chết.")]
    public Transform phase1RightEdgePoint;

    [Tooltip("Giới hạn camera bên phải cho đến khi Enemy4 bị hạ.")]
    public bool limitRightUntilEnemy4Defeated = true;

    [Tooltip("Âm là che nhiều hơn bên phải, dương là cho nhìn thêm bên phải.")]
    public float rightEdgeOffset = -0.3f;

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

    void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    void Start()
    {
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
            float minTargetX = mapLeftEdgePoint.position.x + halfCameraWidth + leftEdgeOffset;

            if (targetPosition.x < minTargetX)
            {
                targetPosition.x = minTargetX;
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

        return targetPosition;
    }

    bool ShouldLimitLeft()
    {
        if (!limitLeftAlways) return false;
        if (mapLeftEdgePoint == null) return false;

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