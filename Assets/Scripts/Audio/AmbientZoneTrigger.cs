using UnityEngine;

/// <summary>
/// Dat trigger vao tung khu vuc (Forest/Sea/City) de dieu khien ambient soundscape.
/// Player vao trigger => fade in region nay.
/// Player ra trigger  => fade out ve None (neu dang o region do).
/// </summary>
[RequireComponent(typeof(Collider))]
public class AmbientZoneTrigger : MonoBehaviour
{
    public AmbientSoundscapeManager.AmbientRegion region = AmbientSoundscapeManager.AmbientRegion.None;

    [Tooltip("Neu rong thi dung tag Player")]
    public Transform playerOverride;

    private Transform _playerTransform;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
            col.isTrigger = true;
    }

    private void Start()
    {
        if (playerOverride != null)
        {
            _playerTransform = playerOverride;
            return;
        }

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
            _playerTransform = playerObject.transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        if (AmbientSoundscapeManager.Instance != null)
            AmbientSoundscapeManager.Instance.EnterRegion(region);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        if (AmbientSoundscapeManager.Instance != null)
            AmbientSoundscapeManager.Instance.LeaveRegion(region);
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null)
            return false;

        if (_playerTransform != null)
            return other.transform == _playerTransform || other.transform.IsChildOf(_playerTransform);

        return other.CompareTag("Player");
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        if (col is BoxCollider box)
        {
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = previous;
        }
        else if (col is SphereCollider sphere)
        {
            Vector3 worldCenter = transform.TransformPoint(sphere.center);
            float worldRadius = sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            Gizmos.DrawWireSphere(worldCenter, worldRadius);
        }
    }
#endif
}
