using UnityEngine;

public class Map1CameraFollowTarget : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Kéo object Wukong vào đây.")]
    public Transform wukong;

    [Header("Follow Settings")]
    [Tooltip("Tốc độ CameraFollowTarget bám theo Wukong. Số càng cao camera càng bám sát.")]
    public float followSmoothSpeed = 10f;

    [Tooltip("Offset X so với Wukong. Dương = camera lệch sang phải, âm = lệch sang trái.")]
    public float xOffset = 0f;

    [Tooltip("Offset Y so với Wukong. Chỉ dùng khi Lock Y Position tắt.")]
    public float yOffset = 0f;

    [Header("Lock Axis")]
    [Tooltip("Bật lên để giữ nguyên trục Y ban đầu, camera không nhảy lên xuống theo Wukong.")]
    public bool lockYPosition = true;

    [Tooltip("Bật lên để giữ nguyên trục Z ban đầu.")]
    public bool lockZPosition = true;

    private float fixedY;
    private float fixedZ;

    private void Start()
    {
        // Lưu lại vị trí Y ban đầu của CameraFollowTarget.
        // Nếu lockYPosition bật, camera sẽ luôn giữ Y này.
        fixedY = transform.position.y;

        // Lưu lại vị trí Z ban đầu.
        fixedZ = transform.position.z;
    }

    private void LateUpdate()
    {
        if (wukong == null)
        {
            return;
        }

        // X luôn đi theo Wukong.
        float targetX = wukong.position.x + xOffset;

        // Nếu khóa Y thì giữ Y ban đầu.
        // Nếu không khóa Y thì đi theo Y của Wukong.
        float targetY = lockYPosition
            ? fixedY
            : wukong.position.y + yOffset;

        // Nếu khóa Z thì giữ Z ban đầu.
        // Thường object CameraFollowTarget để Z = 0 là được.
        float targetZ = lockZPosition
            ? fixedZ
            : transform.position.z;

        Vector3 targetPosition = new Vector3(targetX, targetY, targetZ);

        // Di chuyển mượt về vị trí Wukong.
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSmoothSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// Hàm này dùng khi cần ép CameraFollowTarget nhảy ngay về Wukong,
    /// ví dụ lúc bắt đầu map hoặc sau khi teleport.
    /// </summary>
    public void SnapToWukong()
    {
        if (wukong == null)
        {
            return;
        }

        float targetX = wukong.position.x + xOffset;
        float targetY = lockYPosition ? fixedY : wukong.position.y + yOffset;
        float targetZ = lockZPosition ? fixedZ : transform.position.z;

        transform.position = new Vector3(targetX, targetY, targetZ);
    }
}
