using UnityEngine;

public class Map1CameraFollowTargetLimiter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Wukong/player thật.")]
    public Transform player;

    [Tooltip("Camera chính để tính nửa chiều rộng màn hình.")]
    public Camera targetCamera;

    [Tooltip("Map1StoryManager để biết phase hiện tại.")]
    public Map1StoryManager storyManager;

    [Header("Left Map Limit")]
    [Tooltip("Điểm này là mép trái map. Camera không bao giờ được nhìn vượt qua bên trái điểm này.")]
    public Transform mapLeftEdgePoint;

    [Tooltip("Luôn khóa mép trái map.")]
    public bool limitLeftAlways = true;

    [Tooltip("Offset mép trái. Âm là cho nhìn thêm sang trái, dương là che nhiều hơn bên trái.")]
    public float leftEdgeOffset = 0f;

    [Header("Final Right Map Limit")]
    [Tooltip("Điểm này là mép phải cuối map. Camera không bao giờ được nhìn vượt qua bên phải điểm này.")]
    public Transform mapRightEdgePoint;

    [Tooltip("Bật nếu muốn có giới hạn phải cuối map.")]
    public bool limitRightAlways = false;

    [Tooltip("Offset mép phải cuối map. Âm là che nhiều hơn bên phải, dương là cho nhìn thêm bên phải.")]
    public float mapRightEdgeOffset = 0f;

    [Header("Start Temporary Right Limit")]
    [Tooltip("Điểm này là mép phải màn hình ở khu intro/tutorial/post tutorial.")]
    public Transform startTemporaryRightEdgePoint;

    [Tooltip("Giới hạn camera bên phải cho đến khi hết Post Tutorial Dialogue.")]
    public bool limitRightUntilPostTutorialDone = true;

    [Tooltip("Âm là che nhiều hơn bên phải, dương là cho nhìn thêm bên phải.")]
    public float startTemporaryRightEdgeOffset = -0.3f;

    [Tooltip("Collider box chặn phải đầu map. Hết Post Tutorial Dialogue sẽ tắt.")]
    public Collider2D startTemporaryRightBlockerCollider;

    [Tooltip("Object hình ảnh box/tường chặn phải đầu map. Có thể để trống nếu tường vô hình.")]
    public GameObject startTemporaryRightBlockerVisual;

    [Tooltip("Bật lên để script tự bật/tắt box chặn phải đầu map theo phase.")]
    public bool controlStartTemporaryRightBlocker = true;

    [Header("Enemy Wave Right Limit")]
    [Tooltip("Điểm này là mép phải màn hình khi đang đánh Enemy123.")]
    public Transform enemyWaveRightEdgePoint;

    [Tooltip("Bật giới hạn camera bên phải khi đang phase EnemyWaveMission hoặc EnemyWaveFight.")]
    public bool useEnemyWaveRightLimit = true;

    [Tooltip("Offset riêng cho mép phải Enemy Wave. Âm là che nhiều hơn bên phải, dương là cho nhìn thêm bên phải.")]
    public float enemyWaveRightEdgeOffset = -0.3f;

    [Tooltip("Collider box chặn phải khu Enemy123. Chỉ tắt khi Enemy123 chết hết.")]
    public Collider2D enemyWaveRightBlockerCollider;

    [Tooltip("Object hình ảnh box/tường chặn phải khu Enemy123. Có thể để trống nếu tường vô hình.")]
    public GameObject enemyWaveRightBlockerVisual;

    [Tooltip("Bật lên để script tự bật/tắt box chặn Enemy Wave theo phase.")]
    public bool controlEnemyWaveRightBlocker = true;

    [Header("Follow")]
    [Tooltip("Có đi theo trục Y của Wukong không.")]
    public bool followPlayerY = false;

    [Tooltip("Y cố định của CameraFollowTarget nếu không follow Y.")]
    public float fixedY = 0f;

    [Tooltip("Z của CameraFollowTarget.")]
    public float fixedZ = 0f;

    [Header("Smooth Follow")]
    [Tooltip("Bật làm mượt CameraFollowTarget.")]
    public bool useSmoothFollow = true;

    [Tooltip("Thời gian camera target trượt về vị trí mới. Càng nhỏ càng nhanh.")]
    public float smoothTime = 0.25f;

    [Tooltip("Tốc độ tối đa khi CameraFollowTarget đuổi theo Wukong.")]
    public float maxSmoothSpeed = 80f;

    [Header("Debug")]
    public bool enableDebugLog = false;

    private Vector3 smoothVelocity;
    private bool initialized;

    private bool lastStartTemporaryBlockerActive;
    private bool lastEnemyWaveBlockerActive;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Start()
    {
        UpdateStartTemporaryBlocker();
        UpdateEnemyWaveBlocker();

        if (player != null)
        {
            Vector3 startPosition = GetClampedTargetPosition();
            transform.position = startPosition;
            initialized = true;
        }
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        UpdateStartTemporaryBlocker();
        UpdateEnemyWaveBlocker();

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

    private Vector3 GetClampedTargetPosition()
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

        if (ShouldLimitRightStartTemporary())
        {
            float maxTargetX = startTemporaryRightEdgePoint.position.x - halfCameraWidth + startTemporaryRightEdgeOffset;

            if (targetPosition.x > maxTargetX)
            {
                targetPosition.x = maxTargetX;
            }
        }

        if (ShouldLimitRightEnemyWave())
        {
            float maxTargetX = enemyWaveRightEdgePoint.position.x - halfCameraWidth + enemyWaveRightEdgeOffset;

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

    private bool ShouldLimitLeft()
    {
        if (!limitLeftAlways)
        {
            return false;
        }

        if (mapLeftEdgePoint == null)
        {
            return false;
        }

        return true;
    }

    private bool ShouldLimitRightMap()
    {
        if (!limitRightAlways)
        {
            return false;
        }

        if (mapRightEdgePoint == null)
        {
            return false;
        }

        return true;
    }

    private bool ShouldLimitRightStartTemporary()
    {
        if (!limitRightUntilPostTutorialDone)
        {
            return false;
        }

        if (startTemporaryRightEdgePoint == null)
        {
            return false;
        }

        if (storyManager == null)
        {
            return true;
        }

        return storyManager.currentPhase == Map1StoryManager.Map1Phase.Spawn
            || storyManager.currentPhase == Map1StoryManager.Map1Phase.IntroPoem
            || storyManager.currentPhase == Map1StoryManager.Map1Phase.Tutorial
            || storyManager.currentPhase == Map1StoryManager.Map1Phase.PostTutorialDialogue;
    }

    private bool ShouldLimitRightEnemyWave()
    {
        if (!useEnemyWaveRightLimit)
        {
            return false;
        }

        if (enemyWaveRightEdgePoint == null)
        {
            return false;
        }

        if (storyManager == null)
        {
            return false;
        }

        return storyManager.currentPhase == Map1StoryManager.Map1Phase.EnemyWaveMission
            || storyManager.currentPhase == Map1StoryManager.Map1Phase.EnemyWaveFight;
    }

    private void UpdateStartTemporaryBlocker()
    {
        if (!controlStartTemporaryRightBlocker)
        {
            return;
        }

        bool shouldBeActive = ShouldLimitRightStartTemporary();

        if (shouldBeActive == lastStartTemporaryBlockerActive)
        {
            return;
        }

        lastStartTemporaryBlockerActive = shouldBeActive;

        SetStartTemporaryBlockerActive(shouldBeActive);

        if (enableDebugLog)
        {
            Debug.Log("Map1CameraFollowTargetLimiter: Start Temporary Right Blocker " + (shouldBeActive ? "ON" : "OFF"));
        }
    }

    private void UpdateEnemyWaveBlocker()
    {
        if (!controlEnemyWaveRightBlocker)
        {
            return;
        }

        bool shouldBeActive = ShouldLimitRightEnemyWave();

        if (shouldBeActive == lastEnemyWaveBlockerActive)
        {
            return;
        }

        lastEnemyWaveBlockerActive = shouldBeActive;

        SetEnemyWaveBlockerActive(shouldBeActive);

        if (enableDebugLog)
        {
            Debug.Log("Map1CameraFollowTargetLimiter: Enemy Wave Right Blocker " + (shouldBeActive ? "ON" : "OFF"));
        }
    }

    private void SetStartTemporaryBlockerActive(bool active)
    {
        if (startTemporaryRightBlockerCollider != null)
        {
            startTemporaryRightBlockerCollider.enabled = active;
        }

        if (startTemporaryRightBlockerVisual != null)
        {
            startTemporaryRightBlockerVisual.SetActive(active);
        }
    }

    private void SetEnemyWaveBlockerActive(bool active)
    {
        if (enemyWaveRightBlockerCollider != null)
        {
            enemyWaveRightBlockerCollider.enabled = active;
        }

        if (enemyWaveRightBlockerVisual != null)
        {
            enemyWaveRightBlockerVisual.SetActive(active);
        }
    }

    public void ReleaseStartTemporaryRightLimit()
    {
        limitRightUntilPostTutorialDone = false;
        SetStartTemporaryBlockerActive(false);

        if (enableDebugLog)
        {
            Debug.Log("Map1CameraFollowTargetLimiter: Đã mở giới hạn phải đầu map.");
        }
    }

    public void ActivateEnemyWaveRightLimit()
    {
        useEnemyWaveRightLimit = true;
        SetEnemyWaveBlockerActive(true);

        if (enableDebugLog)
        {
            Debug.Log("Map1CameraFollowTargetLimiter: Đã bật giới hạn phải Enemy Wave.");
        }
    }

    public void ReleaseEnemyWaveRightLimit()
    {
        useEnemyWaveRightLimit = false;
        SetEnemyWaveBlockerActive(false);

        if (enableDebugLog)
        {
            Debug.Log("Map1CameraFollowTargetLimiter: Đã mở giới hạn phải Enemy Wave.");
        }
    }

    private float GetHalfCameraWidth()
    {
        if (targetCamera == null)
        {
            return 8f;
        }

        if (targetCamera.orthographic)
        {
            return targetCamera.orthographicSize * targetCamera.aspect;
        }

        float distance = Mathf.Abs(targetCamera.transform.position.z);
        float halfHeight = Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * distance;
        return halfHeight * targetCamera.aspect;
    }
}