using UnityEngine;

public class VirtualCaneController : MonoBehaviour
{
    [Header("References")]
    public Transform gripPoint;
    public Transform caneTip;

    [Header("IMU Settings")]
    public float gyroScale = 90f;
    public float accScale = 20f;
    public float smoothing = 0.1f;

    private Vector3 rotationEuler;
    private Vector3 smoothedAccel;
    private Vector3 smoothedGyro;

    void LateUpdate()
    {
        if (gripPoint != null)
        {
            transform.position = gripPoint.position;
        }
    }

    // Called by MetaWearReceiver (NOT Swift directly)
    public void OnIMUData(Vector3 accel, Vector3 gyro)
    {
        // Smooth noisy MetaWear data
        smoothedAccel = Vector3.Lerp(smoothedAccel, accel, smoothing);
        smoothedGyro = Vector3.Lerp(smoothedGyro, gyro, smoothing);

        // Gyro integration (main orientation)
        rotationEuler += new Vector3(
            -smoothedGyro.y,
             smoothedGyro.x,
            -smoothedGyro.z
        ) * gyroScale * Time.deltaTime;

        // Gravity tilt stabilization
        Vector3 tilt = new Vector3(
            -smoothedAccel.y,
             smoothedAccel.x,
             0f
        ) * accScale;

        Quaternion imuRotation = Quaternion.Euler(rotationEuler + tilt);
        transform.rotation = imuRotation;

        // Keep cane tip physically aligned
        if (caneTip != null)
        {
            Vector3 localTip = caneTip.localPosition;
            Vector3 worldTip = transform.TransformPoint(localTip);
            Vector3 delta = worldTip - caneTip.position;
            transform.position -= delta;
        }
    }
}
