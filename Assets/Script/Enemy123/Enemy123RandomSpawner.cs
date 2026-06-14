using System.Collections.Generic;
using UnityEngine;

public class Enemy123RandomSpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [Tooltip("Kéo 3 prefab Enemy1, Enemy2, Enemy3 vào đây.")]
    public GameObject[] enemyPrefabs;

    [Header("Spawn Points")]
    [Tooltip("Các điểm spawn trên map.")]
    public Transform[] spawnPoints;

    [Header("Story Control")]
    [Tooltip("Chỉ cho spawn khi Map4StoryManager gọi StartSpawn().")]
    public bool isSpawning = false;

    [Tooltip("Spawn ngay khi bắt đầu scene. Với Map 4 nên TẮT.")]
    public bool spawnOnStart = false;

    [Header("Spawn Limit")]
    [Tooltip("Số enemy tối đa được tồn tại cùng lúc trên map.")]
    public int maxAliveEnemies = 2;

    [Tooltip("Tổng số enemy tối đa được sinh ra trong cả màn. Đặt <= 0 nếu muốn vô hạn.")]
    public int maxTotalSpawnCount = 10;

    [Tooltip("Số enemy đã từng được sinh ra.")]
    public int totalSpawnedCount = 0;

    [Header("Respawn")]
    [Tooltip("Có tự sinh bù khi enemy chết không.")]
    public bool autoRespawn = true;

    [Tooltip("Thời gian chờ trước khi sinh bù enemy mới.")]
    public float respawnDelay = 1f;

    [Tooltip("Mỗi lần kiểm tra enemy chết cách nhau bao lâu.")]
    public float checkInterval = 0.5f;

    [Header("Target")]
    [Tooltip("Wukong.")]
    public Transform playerTarget;

    [Tooltip("Tự tìm Wukong bằng tag Player.")]
    public bool autoFindPlayer = true;

    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Header("Parent")]
    [Tooltip("Object cha chứa enemy spawn ra.")]
    public Transform spawnedParent;

    [Header("Spawn Option")]
    [Tooltip("Không spawn trùng cùng một spawn point nếu đang có enemy gần đó.")]
    public bool avoidOccupiedSpawnPoint = true;

    [Tooltip("Bán kính kiểm tra spawn point đã có enemy chưa.")]
    public float occupiedCheckRadius = 0.8f;

    [Tooltip("Nếu bật, enemy mới sẽ random trong các prefab.")]
    public bool randomEnemyPrefab = true;

    [Header("Debug")]
    public bool enableDebugLog = true;

    private float checkTimer;
    private float respawnTimer;
    private List<Enemy123Controller> aliveEnemies = new List<Enemy123Controller>();

    void Start()
    {
        FindPlayerIfNeeded();

        checkTimer = checkInterval;
        respawnTimer = respawnDelay;

        if (spawnOnStart)
        {
            StartSpawn();
        }
        else
        {
            isSpawning = false;
        }
    }

    void Update()
    {
        FindPlayerIfNeeded();

        if (!isSpawning)
        {
            return;
        }

        checkTimer -= Time.deltaTime;

        if (checkTimer <= 0f)
        {
            checkTimer = checkInterval;
            CleanupDeadEnemies();
        }

        if (!autoRespawn) return;
        if (!CanSpawnMoreByTotalLimit()) return;
        if (GetAliveEnemyCount() >= maxAliveEnemies) return;

        respawnTimer -= Time.deltaTime;

        if (respawnTimer <= 0f)
        {
            respawnTimer = respawnDelay;
            SpawnUntilFull();
        }
    }

    public void StartSpawn()
    {
        FindPlayerIfNeeded();

        isSpawning = true;
        respawnTimer = 0f;

        SpawnUntilFull();

        if (enableDebugLog)
        {
            Debug.Log("Enemy123RandomSpawner: bắt đầu spawn quái thường.");
        }
    }

    public void StopSpawn()
    {
        isSpawning = false;

        if (enableDebugLog)
        {
            Debug.Log("Enemy123RandomSpawner: dừng spawn quái thường.");
        }
    }

    public bool IsSpawnFinished()
    {
        if (maxTotalSpawnCount <= 0)
        {
            return false;
        }

        bool spawnedEnough = totalSpawnedCount >= maxTotalSpawnCount;
        bool noAliveEnemy = GetAliveEnemyCount() <= 0;

        return spawnedEnough && noAliveEnemy;
    }

    void FindPlayerIfNeeded()
    {
        if (!autoFindPlayer) return;
        if (playerTarget != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
    }

    void SpawnUntilFull()
    {
        CleanupDeadEnemies();

        while (GetAliveEnemyCount() < maxAliveEnemies && CanSpawnMoreByTotalLimit())
        {
            bool spawned = SpawnOneEnemy();

            if (!spawned)
            {
                break;
            }
        }
    }

    bool SpawnOneEnemy()
    {
        if (!CanSpawnMoreByTotalLimit())
        {
            if (enableDebugLog)
            {
                Debug.Log("Đã đạt giới hạn tổng số enemy spawn: " + totalSpawnedCount + "/" + maxTotalSpawnCount);
            }

            return false;
        }

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("Enemy123RandomSpawner chưa có enemy prefab.");
            return false;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Enemy123RandomSpawner chưa có spawn point.");
            return false;
        }

        GameObject prefab = GetRandomEnemyPrefab();

        if (prefab == null)
        {
            Debug.LogWarning("Enemy123RandomSpawner có prefab bị null.");
            return false;
        }

        Transform spawnPoint = GetRandomAvailableSpawnPoint();

        if (spawnPoint == null)
        {
            if (enableDebugLog)
            {
                Debug.Log("Không có spawn point trống để sinh enemy.");
            }

            return false;
        }

        GameObject enemyObject = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, spawnedParent);
        totalSpawnedCount++;

        Enemy123Controller controller = enemyObject.GetComponent<Enemy123Controller>();

        if (controller != null)
        {
            controller.target = playerTarget;
            controller.autoFindPlayer = true;
            aliveEnemies.Add(controller);
        }
        else
        {
            Debug.LogWarning("Prefab enemy thiếu Enemy123Controller: " + prefab.name);
        }

        if (enableDebugLog)
        {
            Debug.Log("Đã spawn enemy: " + prefab.name + " tại " + spawnPoint.name + " | Tổng đã spawn: " + totalSpawnedCount);
        }

        return true;
    }

    bool CanSpawnMoreByTotalLimit()
    {
        if (maxTotalSpawnCount <= 0) return true;

        return totalSpawnedCount < maxTotalSpawnCount;
    }

    GameObject GetRandomEnemyPrefab()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return null;

        if (!randomEnemyPrefab)
        {
            return enemyPrefabs[0];
        }

        int randomIndex = Random.Range(0, enemyPrefabs.Length);
        return enemyPrefabs[randomIndex];
    }

    Transform GetRandomAvailableSpawnPoint()
    {
        List<Transform> availablePoints = new List<Transform>();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[i];

            if (point == null) continue;

            if (avoidOccupiedSpawnPoint && IsSpawnPointOccupied(point))
            {
                continue;
            }

            availablePoints.Add(point);
        }

        if (availablePoints.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, availablePoints.Count);
        return availablePoints[randomIndex];
    }

    bool IsSpawnPointOccupied(Transform point)
    {
        if (point == null) return true;

        CleanupDeadEnemies();

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            Enemy123Controller enemy = aliveEnemies[i];

            if (enemy == null) continue;
            if (enemy.IsDead()) continue;

            float distance = Vector2.Distance(point.position, enemy.transform.position);

            if (distance <= occupiedCheckRadius)
            {
                return true;
            }
        }

        return false;
    }

    void CleanupDeadEnemies()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            Enemy123Controller enemy = aliveEnemies[i];

            if (enemy == null)
            {
                aliveEnemies.RemoveAt(i);
                continue;
            }

            if (enemy.IsDead())
            {
                aliveEnemies.RemoveAt(i);
            }
        }
    }

    int GetAliveEnemyCount()
    {
        CleanupDeadEnemies();

        int count = 0;

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            Enemy123Controller enemy = aliveEnemies[i];

            if (enemy == null) continue;
            if (enemy.IsDead()) continue;

            count++;
        }

        return count;
    }

    void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.green;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[i];

            if (point == null) continue;

            Gizmos.DrawWireSphere(point.position, occupiedCheckRadius);
            Gizmos.DrawSphere(point.position, 0.1f);
        }
    }
}