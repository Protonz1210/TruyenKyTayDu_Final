
using UnityEngine;

public class Map2CameraFollowTargetLimiter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kéo Player/Wukong thật vào đây.")]
    public Transform player;

    [Tooltip("Kéo Main Camera vào đây để tính nửa chiều rộng màn hình.")]
    public Camera targetCamera;

    [Header("Map Left Limit")]
    [Tooltip("Điểm mép trái map. Camera không được nhìn vượt qua điểm này.")]
    public Transform mapLeftEdgePoint;

    [Tooltip("Bật giới hạn mép trái map.")]
    public bool limitLeft = true;

    [Tooltip("Offset mép trái. Âm = cho nhìn thêm sang trái, dương = che bớt bên trái.")]
    public float leftEdgeOffset = 0f;

    [Header("Map Right Limit")]
    [Tooltip("Điểm mép phải map. Camera không được nhìn vượt qua điểm này.")]
    public Transform mapRightEdgePoint;

    [Tooltip("Bật giới hạn mép phải map.")]
    public bool limitRight = true;

    [Tooltip("Offset mép phải. Âm = che bớt bên phải, dương = cho nhìn thêm bên phải.")]
    public float rightEdgeOffset = 0f;

    [Header("Follow")]
    [Tooltip("Bật nếu muốn CameraFollowTarget đi theo Y của player. Map đi ngang thường nên tắt.")]
    public bool followPlayerY = false;

    [Tooltip("Y cố định của CameraFollowTarget nếu không follow Y.")]
    public float fixedY = 0f;

    [Tooltip("Z của CameraFollowTarget. Thường để 0.")]
    public float fixedZ = 0f;

    [Header("Smooth Follow")]
    [Tooltip("Bật để CameraFollowTarget di chuyển mượt hơn, không giật.")]
    public bool useSmoothFollow = true;

    [Tooltip("Thời gian làm mượt. Số càng nhỏ camera càng bám nhanh.")]
    public float smoothTime = 0.18f;

    [Tooltip("Tốc độ tối đa khi CameraFollowTarget đuổi theo player.")]
    public float maxSmoothSpeed = 80f;

    [Header("Debug")]
    [Tooltip("Bật để in log kiểm tra vị trí camera target.")]
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

        if (enableDebugLog)
        {
            Debug.Log("Map2 CameraFollowTarget Position: " + transform.position);
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

        if (limitLeft && mapLeftEdgePoint != null)
        {
            float minTargetX = mapLeftEdgePoint.position.x + halfCameraWidth + leftEdgeOffset;

            if (targetPosition.x < minTargetX)
            {
                targetPosition.x = minTargetX;
            }
        }

        if (limitRight && mapRightEdgePoint != null)
        {
            float maxTargetX = mapRightEdgePoint.position.x - halfCameraWidth + rightEdgeOffset;

            if (targetPosition.x > maxTargetX)
            {
                targetPosition.x = maxTargetX;
            }
        }

        return targetPosition;
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

    void OnDrawGizmos()
    {
        if (mapLeftEdgePoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                new Vector3(mapLeftEdgePoint.position.x, mapLeftEdgePoint.position.y - 10f, 0f),
                new Vector3(mapLeftEdgePoint.position.x, mapLeftEdgePoint.position.y + 10f, 0f)
            );
        }

        if (mapRightEdgePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                new Vector3(mapRightEdgePoint.position.x, mapRightEdgePoint.position.y - 10f, 0f),
                new Vector3(mapRightEdgePoint.position.x, mapRightEdgePoint.position.y + 10f, 0f)
            );
        }
    }
}