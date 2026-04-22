using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class CaneController : MonoBehaviour
{
    public HeightCalibration calibration;
    public Transform gripPoint;          // child Transform at the grip position; assign in Inspector

    private Quaternion targetRotation = Quaternion.identity;
    public float smoothSpeed = 10f;

    public void ApplySensorData(SensorPacket p)
    {
        Vector3 gyro = new Vector3(p.gx, -p.gy, -p.gz);
        float scale = calibration.GetMovementScale();
        Quaternion delta = Quaternion.Euler(gyro * scale * Time.deltaTime * Mathf.Rad2Deg);
        targetRotation = targetRotation * delta;
    }

    void Update()
    {
        Vector3 gripBefore = gripPoint.position;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            smoothSpeed * Time.deltaTime
        );

        transform.position += gripBefore - gripPoint.position;
    }
}
