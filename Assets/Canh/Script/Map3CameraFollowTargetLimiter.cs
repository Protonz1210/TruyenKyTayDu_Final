using UnityEngine;

public class Map3CameraFollowTargetLimiter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Wukong/player thật.")]
    public Transform player;

    [Tooltip("Camera chính để tính nửa chiều rộng màn hình.")]
    public Camera targetCamera;

    [Header("Left Map Limit")]
    [Tooltip("Điểm này là mép trái map. Camera không bao giờ được nhìn vượt qua bên trái điểm này.")]
    public Transform mapLeftEdgePoint;

    [Tooltip("Bật giới hạn bên trái map.")]
    public bool limitLeftAlways = true;

    [Tooltip("Offset mép trái. Âm là cho nhìn thêm sang trái, dương là che nhiều hơn bên trái.")]
    public float leftEdgeOffset = 0f;

    [Header("Right Map Limit")]
    [Tooltip("Điểm này là mép phải map. Camera không bao giờ được nhìn vượt qua bên phải điểm này.")]
    public Transform mapRightEdgePoint;

    [Tooltip("Bật giới hạn bên phải map.")]
    public bool limitRightAlways = true;

    [Tooltip("Offset mép phải. Âm là che nhiều hơn bên phải, dương là cho nhìn thêm bên phải.")]
    public float rightEdgeOffset = 0f;

    [Header("Follow")]
    [Tooltip("Có đi theo trục Y của Wukong không.")]
    public bool followPlayerY = false;

    [Tooltip("Y cố định của CameraFollowTarget nếu không follow Y.")]
    public float fixedY = 0f;

    [Tooltip("Z của CameraFollowTarget.")]
    public float fixedZ = 0f;

    [Header("Smooth Follow")]
    [Tooltip("Bật làm mượt CameraFollowTarget để tránh camera giật.")]
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

        if (ShouldLimitRight())
        {
            float maxTargetX = mapRightEdgePoint.position.x - halfCameraWidth + rightEdgeOffset;

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

    bool ShouldLimitRight()
    {
        if (!limitRightAlways) return false;
        if (mapRightEdgePoint == null) return false;

        return true;
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

    void OnDrawGizmosSelected()
    {
        if (!enableDebugLog) return;

        Gizmos.color = Color.green;

        if (mapLeftEdgePoint != null)
        {
            Gizmos.DrawLine(
                new Vector3(mapLeftEdgePoint.position.x, -100f, 0f),
                new Vector3(mapLeftEdgePoint.position.x, 100f, 0f)
            );
        }

        Gizmos.color = Color.red;

        if (mapRightEdgePoint != null)
        {
            Gizmos.DrawLine(
                new Vector3(mapRightEdgePoint.position.x, -100f, 0f),
                new Vector3(mapRightEdgePoint.position.x, 100f, 0f)
            );
        }
    }
}