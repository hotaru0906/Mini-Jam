using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object pooling cho xe.
/// Mỗi lane có SpawnPoint (điểm xuất phát) và hướng di chuyển.
/// Xe di chuyển đến khi vượt quá travelDistance rồi trả về pool.
/// </summary>
public class TrafficVehicleSpawner : MonoBehaviour
{
    // -------------------------------------------------------
    // Data classes
    // -------------------------------------------------------

    [System.Serializable]
    public class VehicleLane
    {
        [Tooltip("Tên lane để debug")]
        public string laneName = "Lane";

        [Tooltip("Điểm spawn của xe")]
        public Transform spawnPoint;

        [Tooltip("Hướng di chuyển (sẽ được normalize tự động)")]
        public Vector3 moveDirection = Vector3.right;

        [Tooltip("Tốc độ xe (units/giây)")]
        public float speed = 5f;

        [Tooltip("Khoảng cách di chuyển trước khi deactivate")]
        public float travelDistance = 20f;

        [Tooltip("Khoảng thời gian giữa các lần spawn (giây)")]
        public float spawnInterval = 1.5f;

        [HideInInspector] public Coroutine spawnCoroutine;
    }

    // -------------------------------------------------------
    // Inspector
    // -------------------------------------------------------

    [Header("Pool Setup")]
    [Tooltip("Prefab xe (dùng chung cho tất cả lane)")]
    public GameObject vehiclePrefab;

    [Tooltip("Số xe tối đa trong pool")]
    public int poolSize = 10;

    [Header("Lanes")]
    public VehicleLane[] lanes;

    [Header("Debug")]
    public bool logSpawn = false;

    // -------------------------------------------------------
    // Pool
    // -------------------------------------------------------

    private readonly Queue<GameObject> _pool = new Queue<GameObject>();
    private bool _spawning;

    // -------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------

    private void Awake()
    {
        BuildPool();
    }

    private void BuildPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(vehiclePrefab, transform);
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }
    }

    // -------------------------------------------------------
    // Public API — gọi bởi TrafficTimerManager
    // -------------------------------------------------------

    public void StartSpawning()
    {
        if (_spawning) return;
        _spawning = true;

        if (lanes == null) return;
        foreach (var lane in lanes)
        {
            if (lane.spawnPoint == null) continue;
            lane.spawnCoroutine = StartCoroutine(SpawnLoop(lane));
        }

        if (logSpawn) Debug.Log("[VehicleSpawner] Bắt đầu spawn xe.");
    }

    public void StopSpawning()
    {
        _spawning = false;

        if (lanes == null) return;
        foreach (var lane in lanes)
        {
            if (lane.spawnCoroutine != null)
            {
                StopCoroutine(lane.spawnCoroutine);
                lane.spawnCoroutine = null;
            }
        }

        if (logSpawn) Debug.Log("[VehicleSpawner] Dừng spawn xe.");
    }

    // -------------------------------------------------------
    // Spawn loop per lane
    // -------------------------------------------------------

    private IEnumerator SpawnLoop(VehicleLane lane)
    {
        while (_spawning)
        {
            SpawnVehicle(lane);
            yield return new WaitForSeconds(lane.spawnInterval);
        }
    }

    private void SpawnVehicle(VehicleLane lane)
    {
        GameObject vehicle = GetFromPool();
        if (vehicle == null)
        {
            if (logSpawn) Debug.LogWarning("[VehicleSpawner] Pool rỗng!");
            return;
        }

        vehicle.transform.position = lane.spawnPoint.position;
        vehicle.transform.rotation = Quaternion.LookRotation(lane.moveDirection.normalized);
        vehicle.SetActive(true);

        StartCoroutine(MoveVehicle(vehicle, lane));

        if (logSpawn) Debug.Log($"[VehicleSpawner] Spawn xe tại lane [{lane.laneName}].");
    }

    // -------------------------------------------------------
    // Vehicle movement
    // -------------------------------------------------------

    private IEnumerator MoveVehicle(GameObject vehicle, VehicleLane lane)
    {
        Vector3 startPos  = vehicle.transform.position;
        Vector3 direction = lane.moveDirection.normalized;
        float   traveled  = 0f;

        while (traveled < lane.travelDistance)
        {
            if (vehicle == null) yield break;

            float step = lane.speed * Time.deltaTime;
            vehicle.transform.position += direction * step;
            traveled += step;

            yield return null;
        }

        ReturnToPool(vehicle);
    }

    // -------------------------------------------------------
    // Pool helpers
    // -------------------------------------------------------

    private GameObject GetFromPool()
    {
        // Tìm object đang inactive trong pool
        int count = _pool.Count;
        for (int i = 0; i < count; i++)
        {
            GameObject obj = _pool.Dequeue();
            if (!obj.activeInHierarchy)
                return obj;          // Trả ra dùng, không Enqueue lại
            _pool.Enqueue(obj);      // Đang dùng → bỏ lại cuối queue
        }
        return null; // Pool rỗng
    }

    private void ReturnToPool(GameObject vehicle)
    {
        if (vehicle == null) return;
        vehicle.SetActive(false);
        _pool.Enqueue(vehicle);
        if (logSpawn) Debug.Log("[VehicleSpawner] Xe trả về pool.");
    }

    // -------------------------------------------------------
    // Editor helper
    // -------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (lanes == null) return;
        foreach (var lane in lanes)
        {
            if (lane.spawnPoint == null) continue;
            Gizmos.color = Color.yellow;
            Vector3 end = lane.spawnPoint.position + lane.moveDirection.normalized * lane.travelDistance;
            Gizmos.DrawLine(lane.spawnPoint.position, end);
            Gizmos.DrawWireSphere(end, 0.3f);
        }
    }
#endif
}