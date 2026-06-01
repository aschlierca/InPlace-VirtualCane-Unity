using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class CaneController : MonoBehaviour
{
    public HeightCalibration calibration;
    public Transform gripPoint;          // child Transform at the grip position; assign in Inspector

    private Quaternion targetRotation = Quaternion.identity;
    public float smoothSpeed = 10f;

    private bool hasData = false;   // stays false until real sensor data arrives (e.g. in editor with no device)

    void Awake()
    {
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

        Vector3 gyro = new Vector3(p.gx, -p.gy, -p.gz);
        float scale = calibration.GetMovementScale();
        Quaternion delta = Quaternion.Euler(gyro * scale * Time.deltaTime * Mathf.Rad2Deg);
        targetRotation = targetRotation * delta;
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
