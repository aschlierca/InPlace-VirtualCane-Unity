using UnityEngine;

// Drives the virtual cane from the MetaWear on-board sensor-fusion
// quaternion (absolute orientation), instead of integrating raw gyro rates.
//
// Design:
//   target = caneRefRotation * mountOffset⁻¹ * (sensorRef⁻¹ * sensorNow) * mountOffset
//
// i.e. the sensor's rotation *since calibration*, expressed in the cane's
// body frame, applied to the cane's calibrated pose. This makes physical
// pointing map 1:1 to virtual pointing, with no gain constants, no dt, and
// no drift springs.
//
// Calibration: hold the physical cane in the normal grip pose pointing
// straight ahead, then call Recalibrate() (wire it to a UI button and/or a
// voice command). Auto-calibrates on the first packet as a fallback.
public class CaneController : MonoBehaviour
{
    public HeightCalibration calibration;
    public Transform gripPoint;

    [Header("Smoothing")]
    [Tooltip("Slerp rate toward the sensor pose. 15-25 is responsive; lower adds lag but hides BLE jitter.")]
    public float smoothSpeed = 20f;

    [Header("Mounting")]
    [Tooltip("Fixed rotation from the sensor's body axes to the cane's body axes. " +
             "Leave zero if the board is mounted flat on top of the shaft, X along the shaft. " +
             "If sweep/dip axes come out swapped or tilted, adjust this once (multiples of 90 usually).")]
    public Vector3 mountOffsetEuler = Vector3.zero;

    private Quaternion mountOffset = Quaternion.identity;

    // State
    private Quaternion sensorNow = Quaternion.identity;  // sensor orientation in Unity axes
    private Quaternion sensorRef = Quaternion.identity;  // sensor orientation at calibration
    private Quaternion caneRef   = Quaternion.identity;  // cane world rotation at calibration
    private bool hasData = false;
    private bool calibrated = false;

    void Awake()
    {
        caneRef = transform.rotation;
        mountOffset = Quaternion.Euler(mountOffsetEuler);
    }

    void OnValidate()
    {
        mountOffset = Quaternion.Euler(mountOffsetEuler);
    }

    // Called by SensorDataReceiver with the fused quaternion from the board.
    // MetaWear fusion output is right-handed (Z-up Earth frame); Unity is
    // left-handed Y-up. Swap the Y/Z components and negate w.
    //
    // If, during the axis check (see Recalibrate docs), a physical rotation
    // produces the *reverse* virtual rotation on one axis, try negating that
    // single component below (e.g. -x) rather than changing mountOffset.
    public void ApplyQuaternion(float w, float x, float y, float z)
    {
        sensorNow = new Quaternion(x, z, y, -w);

        if (!hasData)
        {
            hasData = true;
            Recalibrate(); // fallback; a deliberate button press is better
        }
    }

    // Zero the mapping: the cane's CURRENT physical pose is declared to be
    // its current virtual pose. Have the user hold the cane in the standard
    // grip, pointing straight ahead, then trigger this.
    //
    // In IMU_PLUS fusion mode yaw drifts slowly (order of degrees per
    // minute), so expose this on a button and recalibrate between trials.
    public void Recalibrate()
    {
        sensorRef = sensorNow;
        caneRef = transform.rotation;
        calibrated = true;
        Debug.Log("[CaneController] Calibrated: sensor pose latched as reference.");
    }

    void Update()
    {
        if (!calibrated)
            return;

        // Rotation since calibration, expressed in the cane's body frame.
        Quaternion deltaBody = Quaternion.Inverse(sensorRef) * sensorNow;
        Quaternion target = caneRef * (Quaternion.Inverse(mountOffset) * deltaBody * mountOffset);

        // Rotate about the grip point rather than the transform origin.
        Vector3 gripBefore = gripPoint.position;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            target,
            smoothSpeed * Time.deltaTime
        );

        transform.position += gripBefore - gripPoint.position;
    }
}
