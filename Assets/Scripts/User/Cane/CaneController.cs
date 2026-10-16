using UnityEngine;

// Cane orientation from MetaWear fused quaternion.
// Axis map and chirality come from a previously saved calibration in
// PlayerPrefs; without one, the identity mapping is used.
public class CaneController : MonoBehaviour
{
    public HeightCalibration calibration;
    public Transform gripPoint;

    [Header("Smoothing")]
    public float smoothSpeed = 20f;

    // calibration state (persisted)
    private Quaternion axisMap = Quaternion.identity;
    private int chirality = 1;

    // runtime state
    private Quaternion sensorNow = Quaternion.identity;
    private Quaternion sensorRef = Quaternion.identity;
    private Quaternion caneRef = Quaternion.identity;
    private bool hasData = false;
    private bool calibrated = false;

    void Awake()
    {
        caneRef = transform.rotation;
        if (PlayerPrefs.HasKey("cane_map_w"))
        {
            axisMap = new Quaternion(
                PlayerPrefs.GetFloat("cane_map_x"), PlayerPrefs.GetFloat("cane_map_y"),
                PlayerPrefs.GetFloat("cane_map_z"), PlayerPrefs.GetFloat("cane_map_w"));
            chirality = PlayerPrefs.GetInt("cane_chir", 1);
        }
    }

    public void ApplyQuaternion(float w, float x, float y, float z)
    {
        sensorNow = new Quaternion(-x, -y, z, w);
        if (!hasData) { hasData = true; Recalibrate(); }
    }

    public void Recalibrate()
    {
        sensorRef = sensorNow;
        caneRef = transform.rotation;
        calibrated = true;
    }

    void Update()
    {
        if (!calibrated) return;

        Quaternion delta = Quaternion.Inverse(sensorRef) * sensorNow;
        if (chirality < 0) delta = Quaternion.Inverse(delta);
        Quaternion corrected = axisMap * delta * Quaternion.Inverse(axisMap);
        Quaternion target = caneRef * corrected;

        Vector3 gripBefore = gripPoint.position;
        transform.rotation = Quaternion.Slerp(transform.rotation, target, smoothSpeed * Time.deltaTime);
        transform.position += gripBefore - gripPoint.position;
    }
}
