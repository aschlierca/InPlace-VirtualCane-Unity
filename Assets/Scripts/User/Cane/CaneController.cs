using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class CaneController : MonoBehaviour
{
    public HeightCalibration calibration;
    public Transform gripPoint;          // child Transform at the grip position; assign in Inspector

    private Quaternion targetRotation = Quaternion.identity;
    private Quaternion initialRotation = Quaternion.identity;
    public float smoothSpeed = 10f;
    [Range(0.01f, 1f)]
    public float sensitivity = 0.1f;

    private bool hasData = false;   // stays false until real sensor data arrives (e.g. in editor with no device)
    private Camera headCamera;
    private float initialCameraYaw;

    void Awake()
    {
        headCamera       = Camera.main;
        initialCameraYaw = headCamera != null ? headCamera.transform.eulerAngles.y : 0f;
        initialRotation  = transform.rotation;
        targetRotation   = transform.rotation;
    }

    public void ApplySensorData(SensorPacket p)
    {
        if (!hasData)
        {
            // seed from the cane's current pose so the first packet doesn't snap
            targetRotation = transform.rotation;
            hasData = true;
        }

        float scale = calibration.GetMovementScale() * sensitivity;
        float dt    = Time.deltaTime;

        // gx: vertical tilt — cane tip raises/lowers from the grip point
        Quaternion verticalTilt = Quaternion.Euler(-p.gx * scale * dt, 0, 0);
        targetRotation = targetRotation * verticalTilt;

        // gy: local tilt in the cane's own frame (side-lean)
        Quaternion localDelta = Quaternion.Euler(0, -p.gy * scale * dt, 0);
        targetRotation = targetRotation * localDelta;

        // gz: horizontal sweep — world-Y yaw centred at the grip point.
        // The grip-point compensation in Update() keeps the pivot pinned there,
        // so this makes the cane tip arc left/right instead of spinning in place.
        Quaternion worldYaw = Quaternion.Euler(0, -p.gz * scale * dt, 0);
        targetRotation = worldYaw * targetRotation;
    }

    // Align the cane to the user's current head direction.
    // Because the sensor only tracks relative gyro deltas, the cane drifts away
    // from the head's forward over time. ResetCane() computes how much the camera
    // has yawed since the scene started and applies that same offset to the initial
    // cane pose, so the cane re-centres to wherever the user is looking.
    public void ResetCane()
    {
        float yawOffset = headCamera != null
            ? headCamera.transform.eulerAngles.y - initialCameraYaw
            : 0f;
        targetRotation = Quaternion.Euler(0, yawOffset, 0) * initialRotation;
        // leave hasData unchanged — Update() keeps slerping smoothly to the centred pose
    }

    void Update()
    {
        // Don't touch the transform until sensor data is streaming; otherwise we
        // fight whatever movement script is driving the cane (UserMovement_Editor, etc.)
        if (!hasData)
            return;

        Vector3 gripBefore = gripPoint.position;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            smoothSpeed * Time.deltaTime
        );

        transform.position += gripBefore - gripPoint.position;
    }
}
