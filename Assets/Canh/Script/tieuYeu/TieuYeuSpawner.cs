
using UnityEngine;

public class TieuYeuSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Prefab Tiểu yêu cần spawn.")]
    public GameObject tieuYeuPrefab;

    [Header("Spawn Points")]
    [Tooltip("Các điểm spawn trên bản đồ.")]
    public Transform[] spawnPoints;

    [Header("Spawn Time")]
    [Tooltip("Thời gian giữa mỗi lần spawn.")]
    public float spawnInterval = 60f;

    [Tooltip("Spawn ngay khi bắt đầu.")]
    public bool spawnOnStart = false;

    [Header("Spawn Limit")]
    [Tooltip("Số Tiểu yêu tối đa tồn tại cùng lúc.")]
    public int maxAliveEnemies = 3;

    [Header("Target")]
    [Tooltip("Wukong.")]
    public Transform playerTarget;

    [Tooltip("Tự tìm Wukong.")]
    public bool autoFindPlayer = true;

    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Header("Parent")]
    [Tooltip("Object cha chứa Tiểu yêu spawn ra.")]
    public Transform spawnedParent;

    private float spawnTimer;

    void Start()
    {
        FindPlayerIfNeeded();

        spawnTimer = spawnInterval;

        if (spawnOnStart)
        {
            SpawnRandomTieuYeu();
        }
    }

    void Update()
    {
        FindPlayerIfNeeded();

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            spawnTimer = spawnInterval;
            SpawnRandomTieuYeu();
        }
    }

    void FindPlayerIfNeeded()
    {
        if (!autoFindPlayer)
            return;

        if (playerTarget != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
    }

    void SpawnRandomTieuYeu()
    {
        if (tieuYeuPrefab == null)
        {
            Debug.LogWarning("Chưa kéo prefab Tiểu yêu.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Chưa có spawn point.");
            return;
        }

        if (CountAliveTieuYeu() >= maxAliveEnemies)
        {
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject enemy = Instantiate(
            tieuYeuPrefab,
            spawnPoint.position,
            spawnPoint.rotation,
            spawnedParent
        );

        TieuYeuController controller = enemy.GetComponent<TieuYeuController>();

        if (controller != null)
        {
            controller.target = playerTarget;
            controller.autoFindPlayer = true;
        }
    }

    int CountAliveTieuYeu()
    {
        TieuYeuController[] enemies = FindObjectsByType<TieuYeuController>(FindObjectsSortMode.None);

        int count = 0;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null)
                continue;

            if (!enemies[i].IsDead())
            {
                count++;
            }
        }

        return count;
    }
}