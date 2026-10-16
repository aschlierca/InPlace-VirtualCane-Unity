using UnityEngine;
using System.Runtime.InteropServices;
using TMPro;

// Quaternion packet from the on-board sensor fusion module.
[System.Serializable]
public class QuaternionPacket
{
    public long epoch;
    public float qw;
    public float qx;
    public float qy;
    public float qz;
}

public class SensorDataReceiver : MonoBehaviour
{
    public CaneController caneController;
    public SensorGraph graph;
    public DataLogger logger;
    public TMPro.TextMeshProUGUI statusText;
    public TMPro.TextMeshProUGUI debugText;

    private bool isConnected = false;
    private float lastDataTime = 0f;
    private int packetCount = 0;
    private int quatCount = 0;

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void MetaWear_StartScan();

    [DllImport("__Internal")]
    private static extern void MetaWear_StopScan();

    [DllImport("__Internal")]
    private static extern void MetaWear_StartStreaming();

    [DllImport("__Internal")]
    private static extern void MetaWear_StopStreaming();

    [DllImport("__Internal")]
    private static extern void MetaWear_Disconnect();

    [DllImport("__Internal")]
    private static extern void MetaWear_SetUserHeight(float heightCm);
#endif

    void Start()
    {
        Debug.Log("SensorDataReceiver started");
        UpdateDebugText("Waiting to start scan...");

        Invoke("StartScanning", 1f);
    }

    void Update()
    {
        if (isConnected && Time.time - lastDataTime > 3f && lastDataTime > 0)
        {
            UpdateDebugText($"No data for {Time.time - lastDataTime:F1}s\nQuat packets: {quatCount}");
        }
    }

    public void StartScanning()
    {
        Debug.Log("Unity: Starting MetaWear scan...");
        UpdateDebugText("Scanning for devices...");

#if UNITY_IOS && !UNITY_EDITOR
        MetaWear_StartScan();
#else
        Debug.LogWarning("MetaWear scanning only works on iOS device");
        UpdateDebugText("Editor mode - scanning disabled");
#endif
    }

    public void StopScanning()
    {
#if UNITY_IOS && !UNITY_EDITOR
        MetaWear_StopScan();
#endif
    }

    public void StartStreaming()
    {
#if UNITY_IOS && !UNITY_EDITOR
        MetaWear_StartStreaming();
#endif
    }

    public void StopStreaming()
    {
#if UNITY_IOS && !UNITY_EDITOR
        MetaWear_StopStreaming();
#endif
    }

    public void DisconnectDevice()
    {
#if UNITY_IOS && !UNITY_EDITOR
        MetaWear_Disconnect();
#endif
    }

    public void SetUserHeight(float heightCm)
    {
#if UNITY_IOS && !UNITY_EDITOR
        MetaWear_SetUserHeight(heightCm);
#endif
    }

    // Called from iOS native code with the fused orientation quaternion.
    // THIS is what drives the cane now.
    public void OnQuaternionData(string json)
    {
        try
        {
            QuaternionPacket q = JsonUtility.FromJson<QuaternionPacket>(json);

            if (caneController != null)
                caneController.ApplyQuaternion(q.qw, q.qx, q.qy, q.qz);

            lastDataTime = Time.time;
            quatCount++;

            if (quatCount % 100 == 0)
            {
                UpdateDebugText($"Fusion streaming\nQuat packets: {quatCount}\nq: ({q.qw:F2}, {q.qx:F2}, {q.qy:F2}, {q.qz:F2})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing quaternion data: {e.Message}\nJSON: {json}");
        }
    }

    // Called from iOS native code with fusion-corrected accel/gyro.
    // Logger/graph only — the cane is NOT driven from this anymore.
    public void OnSensorData(string json)
    {
        try
        {
            SensorPacket p = JsonUtility.FromJson<SensorPacket>(json);

            if (logger != null)
                logger.Log(p);

            packetCount++;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing sensor data: {e.Message}\nJSON: {json}");
        }
    }

    // Called from iOS native code when connection status changes
    public void OnConnectionStatus(string status)
    {
        Debug.Log($"Unity: Connection status changed to '{status}'");

        if (status == "connected")
        {
            SetConnected();
        }
        else
        {
            SetDisconnected();
        }
    }

    public void SetConnected()
    {
        isConnected = true;
        packetCount = 0;
        quatCount = 0;
        lastDataTime = Time.time;

        if (statusText != null)
        {
            statusText.text = "Connected";
            statusText.color = Color.green;
        }

        UpdateDebugText("Connected!\nWaiting for data...");
    }

    public void SetDisconnected()
    {
        isConnected = false;

        if (statusText != null)
        {
            statusText.text = "Disconnected";
            statusText.color = Color.red;
        }

        UpdateDebugText($"Disconnected\nQuat packets: {quatCount}");
    }

    private void UpdateDebugText(string message)
    {
        if (debugText != null)
        {
            debugText.text = message;
        }
        Debug.Log($"[Debug] {message}");
    }

    // UI Button callbacks
    public void OnScanButtonPressed() => StartScanning();
    public void OnStopScanButtonPressed() => StopScanning();
    public void OnDisconnectButtonPressed() => DisconnectDevice();

    // Wire this to a "Zero Cane" UI button.
    public void OnRecalibrateButtonPressed()
    {
        if (caneController != null)
            caneController.Recalibrate();
    }
}
