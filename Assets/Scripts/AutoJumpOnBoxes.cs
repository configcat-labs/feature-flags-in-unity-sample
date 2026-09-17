using System.Threading.Tasks;
using StarterAssets;
using UnityEngine;

[RequireComponent(typeof(ThirdPersonController))]
public class AutoJumpOnBoxes : MonoBehaviour
{
    [SerializeField] private string featureFlagKey = "autoJumpOnBoxes";
    [SerializeField] private LayerMask boxLayer;
    [SerializeField] private float overlapRadius = 0.3f;
    [SerializeField] private float clearanceMargin = 0.15f;
    [SerializeField] private float retryInterval = 1f;

    private const int MaxOverlaps = 8;

    private ThirdPersonController controller;
    private Collider lastCheckedBox;
    private float lastCheckTime = -999f;
    private readonly Collider[] overlapBuffer = new Collider[MaxOverlaps];

    private void Awake()
    {
        controller = GetComponent<ThirdPersonController>();
    }

    private void Update()
    {
        if (!controller.Grounded)
        {
            return;
        }

        Vector3 overlapCenter = transform.position + Vector3.up * overlapRadius;
        int overlapCount = Physics.OverlapSphereNonAlloc(overlapCenter, overlapRadius, overlapBuffer, boxLayer);

        for (int i = 0; i < overlapCount; i++)
        {
            Collider box = overlapBuffer[i];
            float boxTop = box.bounds.max.y;

            // Robot is already standing on box - continue...
            if (transform.position.y >= boxTop - 0.05f)
            {
                continue;
            }

            if (box == lastCheckedBox && Time.time - lastCheckTime < retryInterval)
            {
                continue;
            }

            lastCheckedBox = box;
            lastCheckTime = Time.time;

            float requiredHeight = boxTop - transform.position.y + clearanceMargin;
            _ = TryAutoJumpAsync(requiredHeight);
            break;
        }
    }

    private async Task TryAutoJumpAsync(float requiredHeight)
    {
        var configCatClient = SingletonServices.Instance.ConfigCatClient;
        var isAutoJumpEnabled = await configCatClient.GetValueAsync(featureFlagKey, false);

        Debug.Log($"[AutoJumpOnBoxes] {featureFlagKey} = {isAutoJumpEnabled}");

        if (isAutoJumpEnabled)
        {
            controller.TriggerJump(requiredHeight);
        }
    }
}
