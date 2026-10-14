using HearXR;
using UnityEngine;

/// <summary>
/// Drives the user's head rotation from AirPods head tracking (CMHeadphoneMotionManager via the HeadphoneMotion plugin).
/// The rotation is applied in the head's local space, so turning the whole User (e.g. HeadMovement touch-look) still composes.
/// Rotation only — this never moves the user's position.
/// </summary>
public class AirPodHeadTracking : MonoBehaviour
{
    [SerializeField] private Transform head;                        // User/Head — the camera (HeadVision) is its child
    [SerializeField] private bool recenterOnConnect = true;         // re-zero yaw whenever AirPods (re)connect, so "forward" = the way the user is facing

    private Quaternion baseLocalRotation;                           // head's authored local rotation
    private Quaternion yawOffset = Quaternion.identity;             // removes the AirPods' arbitrary starting yaw
    private Quaternion latestRotation = Quaternion.identity;        // most recent AirPods rotation
    private bool hasSample;
    private bool headphonesConnected;
    private bool recenterPending;
    private bool tracking;

    private void Start()
    {
        if (Application.isEditor)                                   // plugin is iOS device only
        {
            enabled = false;
            return;
        }

        if (head == null)
        {
            Debug.LogError("AirPodHeadTracking: head transform is not assigned.");
            enabled = false;
            return;
        }

        baseLocalRotation = head.localRotation;

        /* Init must be called before any other HeadphoneMotion call */
        HeadphoneMotion.Init();

        if (!HeadphoneMotion.IsHeadphoneMotionAvailable())
        {
            Debug.Log("AirPodHeadTracking: headphone motion is not available on this device.");
            enabled = false;
            return;
        }

        /* Subscribe before starting, or the initial "connected" callback is missed */
        HeadphoneMotion.OnHeadphoneConnectionChanged += HandleHeadphoneConnectionChange;
        HeadphoneMotion.OnHeadRotationQuaternion += HandleHeadRotationQuaternion;

        HeadphoneMotion.StartTracking();
        tracking = true;
    }

    private void OnDestroy()
    {
        if (!tracking) return;

        HeadphoneMotion.OnHeadphoneConnectionChanged -= HandleHeadphoneConnectionChange;
        HeadphoneMotion.OnHeadRotationQuaternion -= HandleHeadRotationQuaternion;
        HeadphoneMotion.StopTracking();
        tracking = false;
    }

    private void HandleHeadphoneConnectionChange(bool connected)
    {
        headphonesConnected = connected;
        hasSample = false;
        if (connected && recenterOnConnect) recenterPending = true;
    }

    private void HandleHeadRotationQuaternion(Quaternion rotation)
    {
        latestRotation = rotation;
        hasSample = true;
        if (recenterPending) Recenter();
    }

    /// <summary>
    /// Makes the direction the user is currently facing the head's "forward". Only yaw is reset; pitch and roll stay absolute.
    /// </summary>
    public void Recenter()
    {
        if (!hasSample) { recenterPending = true; return; }

        yawOffset = Quaternion.Inverse(Quaternion.Euler(0f, latestRotation.eulerAngles.y, 0f));
        recenterPending = false;
    }

    /* LateUpdate so this wins over anything that touched the head in Update */
    private void LateUpdate()
    {
        if (!headphonesConnected || !hasSample) return;

        head.localRotation = baseLocalRotation * yawOffset * latestRotation;
    }
}
