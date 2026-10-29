using StarterAssets;
using UnityEngine;

public class AutoJumpOnBoxes : MonoBehaviour
{
    [SerializeField] private string featureFlagKey = "autoJumpOnBoxes";
    [SerializeField] private LayerMask boxLayer;
    [SerializeField] private float overlapRadius = 0.45f;
    [SerializeField] private float forwardOffset = 0.3f;
    [SerializeField] private float clearanceMargin = 0.15f;
    [SerializeField] private float retryInterval = 1f;

    private const int MaxOverlaps = 8;

    private ThirdPersonController thirdPersonController;
    private Collider lastCheckedBox;
    private float lastCheckTime = -999f;

    private readonly Collider[] overlapBuffer = new Collider[MaxOverlaps];

    private bool? lastLoggedFlagValue;

    private void Awake()
    {
        thirdPersonController = GetComponent<ThirdPersonController>();

        // The Third Person Controller (Script) must be attached to the Robot
        if (thirdPersonController == null)
        {
            Debug.LogError(
                "[AutoJumpOnBoxes] Attach this to the object that has " +
                "ThirdPersonController (Robot).",
                this
            );
            enabled = false;
        }
    }

    private bool IsAutoJumpEnabled()
    {
        // The snapshot is null until the ConfigCat client is ready.
        var snapshot = SingletonServices.ConfigSnapshot;

        if (snapshot == null)
        {
            return false;
        }
        bool featureFlagValue = snapshot.GetValue(featureFlagKey, false);

        if (lastLoggedFlagValue != featureFlagValue)
        {
            Debug.Log($"[AutoJumpOnBoxes] {featureFlagKey} = {featureFlagValue}");
            lastLoggedFlagValue = featureFlagValue;
        }

        return featureFlagValue;
    }

    private void Update()
    {
        if (!IsAutoJumpEnabled() || !thirdPersonController.Grounded)
        {
            return;
        }

        // Use an overlap radius to check boxes that are slightly ahead of the robot
        Vector3 overlapCenter =
            transform.position +
            Vector3.up * overlapRadius +
            transform.forward * forwardOffset;

        int overlapCount = Physics.OverlapSphereNonAlloc(
            overlapCenter,
            overlapRadius,
            overlapBuffer,
            boxLayer
        );

        for (int i = 0; i < overlapCount; i++)
        {
            Collider box = overlapBuffer[i];

            if (box == null)
            {
                continue;
            }

            float boxTop = box.bounds.max.y;

            // Skip boxes the robot is already standing on.
            if (transform.position.y >= boxTop - 0.05f)
            {
                continue;
            }

            if (box == lastCheckedBox &&
                Time.time - lastCheckTime < retryInterval)
            {
                continue;
            }

            float requiredHeight =
                boxTop - transform.position.y + clearanceMargin;

            if (requiredHeight <= 0f)
            {
                continue;
            }

            lastCheckedBox = box;
            lastCheckTime = Time.time;

            thirdPersonController.TriggerJump(requiredHeight);
            break;
        }
    }
}
