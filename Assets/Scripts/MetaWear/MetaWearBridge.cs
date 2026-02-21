using UnityEngine;
using TMPro.Examples; // For CameraController namespace
using System.Collections;

public class MetaWearBridge : MonoBehaviour
{
    [Header("References")]
    public VirtualCaneController virtualCane;
    public CameraController cameraController;

    [Header("Editor Simulation")]
    public bool simulateIMUInEditor = true;
    public Vector3 simulatedAccel = new Vector3(0f, 0f, -1f); // gravity down
    public Vector3 simulatedGyro = Vector3.zero; // no rotation
    public float simulationUpdateRate = 0.02f; // 50 Hz

#if UNITY_IOS && !UNITY_EDITOR
    // This ensures we only call MetaWearNative on iOS builds
    void Start()
    {
        MetaWearNative.mw_start_scan();
    }
#else
    // In editor: start simulation if enabled
    void Start()
    {
        if (simulateIMUInEditor)
            StartCoroutine(SimulateIMU());
    }
#endif

    /// <summary>
    /// Called from Swift via UnitySendMessage
    /// Format: "ax,ay,az,gx,gy,gz"
    /// </summary>
    /// <param name="data"></param>
    public void OnIMUData(string data)
    {
        string[] p = data.Split(',');
        if (p.Length != 6) return;

        // Parse accelerometer
        Vector3 accel = new Vector3(
            float.Parse(p[0]),
            float.Parse(p[1]),
            float.Parse(p[2])
        );

        // Parse gyroscope
        Vector3 gyro = new Vector3(
            float.Parse(p[3]),
            float.Parse(p[4]),
            float.Parse(p[5])
        );

        // Send data to cane and camera
        virtualCane?.OnIMUData(accel, gyro);
        cameraController?.OnIMUData(accel, gyro);
    }

#if UNITY_EDITOR
    // Simulate IMU data in editor
    private IEnumerator SimulateIMU()
    {
        while (true)
        {
            OnIMUData($"{simulatedAccel.x},{simulatedAccel.y},{simulatedAccel.z},{simulatedGyro.x},{simulatedGyro.y},{simulatedGyro.z}");
            yield return new WaitForSeconds(simulationUpdateRate);
        }
    }
#endif

    /// <summary>
    /// Call these externally from Unity UI buttons
    /// </summary>
    public void StartScan()
    {
#if UNITY_IOS && !UNITY_EDITOR
        MetaWearNative.mw_start_scan();
#endif
    }

    public void StopScan()
    {
#if UNITY_IOS && !UNITY_EDITOR
        MetaWearNative.mw_stop_scan();
#endif
    }

    public void ConnectFirstDevice(string uuid)
    {
#if UNITY_IOS && !UNITY_EDITOR
        MetaWearNative.mw_select_device(uuid);
        MetaWearNative.mw_connect_selected();
#endif
    }
}
