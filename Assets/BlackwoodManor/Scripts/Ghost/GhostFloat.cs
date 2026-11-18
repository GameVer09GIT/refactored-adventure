using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Hiệu ứng con ma bay lơ lửng bồng bềnh và nghiêng mình khi lướt đi.
/// </summary>
public class GhostFloat : MonoBehaviour
{
    [Header("Cấu hình bay lơ lửng")]
    [Tooltip("Biên độ dao động lên xuống (mét)")]
    [SerializeField] private float floatAmplitude = 0.08f;

    [Tooltip("Tần số dao động (tốc độ bồng bềnh)")]
    [SerializeField] private float floatFrequency = 2.4f;

    [Tooltip("Góc nghiêng tối đa về phía trước khi di chuyển nhanh (độ)")]
    [SerializeField] private float maxTiltAngle = 6.0f;

    [Tooltip("Tốc độ chuyển đổi độ nghiêng")]
    [SerializeField] private float tiltSmoothSpeed = 5.0f;

    [Header("Transform trực quan")]
    [Tooltip("Transform chứa visual/mesh của con ma để không làm lệch tọa độ của NavMeshAgent")]
    [SerializeField] private Transform visualTransform;

    private Vector3 initialVisualLocalPos;
    private Quaternion initialVisualLocalRot;
    private NavMeshAgent agent;
    private float currentTilt = 0f;

    void Awake()
    {
        agent = GetComponentInParent<NavMeshAgent>();

        // Nếu chưa gán visualTransform thì mặc định là chính nó hoặc child đầu tiên
        if (visualTransform == null)
        {
            if (transform.childCount > 0)
            {
                visualTransform = transform.GetChild(0);
            }
            else
            {
                visualTransform = transform;
            }
        }

        if (visualTransform != null)
        {
            initialVisualLocalPos = visualTransform.localPosition;
            initialVisualLocalRot = visualTransform.localRotation;
        }
    }

    void Update()
    {
        if (visualTransform == null) return;

        // 1. Dao động lơ lửng theo trục Y (sóng Sin)
        float yOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        visualTransform.localPosition = initialVisualLocalPos + new Vector3(0f, yOffset, 0f);

        // 2. Nghiêng mình khi di chuyển
        float speedRatio = 0f;
        if (agent != null && agent.enabled)
        {
            speedRatio = Mathf.Clamp01(agent.velocity.magnitude / Mathf.Max(0.1f, agent.speed));
        }

        float targetTilt = speedRatio * maxTiltAngle;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSmoothSpeed);

        visualTransform.localRotation = initialVisualLocalRot * Quaternion.Euler(currentTilt, 0f, 0f);
    }
}
