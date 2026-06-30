using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class CaneController : MonoBehaviour
{
    public HeightCalibration calibration;
    public Transform gripPoint;

    private Quaternion targetRotation = Quaternion.identity;
    private Quaternion initialRotation = Quaternion.identity;
    public float smoothSpeed = 10f;
    [Range(0.01f, 5f)]
    public float sensitivity = 50.0f;
    [Tooltip("Additional multiplier on vertical tilt (gy) — lower to reduce up/down sensitivity")]
    [Range(0.01f, 1f)]
    public float verticalSensitivity = 0.3f;

    [Header("Drift Correction")]
    [Tooltip("Proportional gain for yaw drift toward camera-forward while idle")]
    [Range(0f, 5f)]
    public float yawDriftRate = 0.5f;
    [Tooltip("Rate at which pitch springs back to initial downward angle while idle")]
    [Range(0f, 5f)]
    public float pitchDriftRate = 0.3f;
    [Tooltip("Gyro magnitude (deg/s) below which the cane is treated as stationary")]
    [Range(0.05f, 2f)]
    public float idleGyroThreshold = 0.15f;

    private bool hasData = false;
    private Camera headCamera;
    private float initialCameraYaw;

    void Awake()
    {
        headCamera = Camera.main;
        initialCameraYaw = headCamera != null ? headCamera.transform.eulerAngles.y : 0f;
        initialRotation = transform.rotation;
        targetRotation = transform.rotation;
    }

    public void ApplySensorData(SensorPacket p)
    {
        if (!hasData)
        {
            targetRotation = transform.rotation;
            hasData = true;
        }

        float scale = calibration.GetMovementScale() * sensitivity;
        float dt = Time.deltaTime;
        float gyroMag = new Vector3(p.gx, p.gy, p.gz).magnitude;

        if (gyroMag >= idleGyroThreshold)
        {
            // gy: vertical tilt with its own lower sensitivity
            targetRotation *= Quaternion.Euler(p.gy * scale * verticalSensitivity * dt, 0, 0);
            // gz: horizontal sweep — world-Y yaw centred at the grip point
            targetRotation = Quaternion.Euler(0, -p.gz * scale * dt, 0) * targetRotation;
        }
        else
        {
            // Yaw spring: drift toward camera-forward.
            // Uses forward-vector XZ projection — stable at large downward pitch angles.
            float cameraYaw = headCamera != null ? headCamera.transform.eulerAngles.y : 0f;
            Vector3 targetFwd = targetRotation * Vector3.forward;
            float currentYaw = Mathf.Atan2(targetFwd.x, targetFwd.z) * Mathf.Rad2Deg;
            Vector3 initialFwd = initialRotation * Vector3.forward;
            float initialYaw = Mathf.Atan2(initialFwd.x, initialFwd.z) * Mathf.Rad2Deg;
            float neutralYawDeg = initialYaw + (cameraYaw - initialCameraYaw);
            float yawError = Mathf.DeltaAngle(currentYaw, neutralYawDeg);
            targetRotation = Quaternion.Euler(0, yawError * yawDriftRate * dt, 0) * targetRotation;

            // Pitch spring: drift back toward the initial downward angle.
            // Decomposes targetRotation into yaw * pitch so both axes correct independently.
            Quaternion neutralYawQ = Quaternion.Euler(0, neutralYawDeg, 0);
            Quaternion initialYawQ = Quaternion.Euler(0, initialYaw, 0);
            Quaternion pitchCurrent = Quaternion.Inverse(neutralYawQ) * targetRotation;
            Quaternion pitchInitial = Quaternion.Inverse(initialYawQ) * initialRotation;
            targetRotation = neutralYawQ * Quaternion.Slerp(pitchCurrent, pitchInitial, pitchDriftRate * dt);
        }
    }

    void Update()
    {
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
