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

    void Awake()
    {
        initialRotation = transform.rotation;
        targetRotation = transform.rotation;
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
        float dt = Time.deltaTime;

        // gy: local tilt of the cane (pitch/roll in the cane's own frame)
        Quaternion localDelta = Quaternion.Euler(0, -p.gy * scale * dt, 0);
        targetRotation = targetRotation * localDelta;

        // gz: horizontal sweep — world-Y yaw centred at the grip point.
        // The grip-point compensation in Update() keeps the pivot pinned there,
        // so this makes the cane tip arc left/right instead of spinning in place.
        Quaternion worldYaw = Quaternion.Euler(0, -p.gz * scale * dt, 0);
        targetRotation = worldYaw * targetRotation;
    }

    public void ResetCane()
    {
        targetRotation = initialRotation;
        hasData = false;
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
