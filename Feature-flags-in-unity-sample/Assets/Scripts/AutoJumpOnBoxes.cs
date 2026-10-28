
using System;
using System.Collections;
using System.Threading.Tasks;
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
    [SerializeField] private float flagRefreshInterval = 2f;

    private const int MaxOverlaps = 8;

    private ThirdPersonController thirdPersonController;
    private Collider lastCheckedBox;
    private float lastCheckTime = -999f;

    private readonly Collider[] overlapBuffer = new Collider[MaxOverlaps];

    private bool isAutoJumpEnabled;
    private bool isFlagReady;
    private bool isRefreshingFlag;

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

    private void Start()
    {
        StartCoroutine(RefreshFeatureFlagPeriodically());
    }

    private IEnumerator RefreshFeatureFlagPeriodically()
    {
        while (true)
        {
            if (!isRefreshingFlag)
            {
                _ = RefreshFeatureFlagAsync();
            }

            yield return new WaitForSeconds(
                Mathf.Max(0.1f, flagRefreshInterval)
            );
        }
    }

    private async Task RefreshFeatureFlagAsync()
    {
        isRefreshingFlag = true;

        try
        {
            var services = SingletonServices.Instance;

            if (services == null || services.ConfigCatClient == null)
            {
                return;
            }

            bool featureFlagValue = await services.ConfigCatClient.GetValueAsync(
                featureFlagKey,
                false
            );

            // The component may have been destroyed or disabled
            // while awaiting the result.
            if (this == null || !isActiveAndEnabled)
            {
                return;
            }

            if (!isFlagReady || isAutoJumpEnabled != featureFlagValue)
            {
                Debug.Log(
                    $"[AutoJumpOnBoxes] {featureFlagKey} = {featureFlagValue}"
                );
            }

            isAutoJumpEnabled = featureFlagValue;
            isFlagReady = true;
        }
        catch (Exception exception)
        {
            if (this != null)
            {
                Debug.LogException(exception);
            }
        }
        finally
        {
            isRefreshingFlag = false;
        }
    }

    private void Update()
    {
        if (!isFlagReady ||
            !isAutoJumpEnabled ||
            !thirdPersonController.Grounded)
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
