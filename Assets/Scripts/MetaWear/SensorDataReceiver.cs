using UnityEngine;
using System.Runtime.InteropServices;
using TMPro;

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

        // Auto-start scanning after a short delay
        Invoke("StartScanning", 1f);
    }

    void Update()
    {
        // Check for data timeout (no data received for 3 seconds while connected)
        if (isConnected && Time.time - lastDataTime > 3f && lastDataTime > 0)
        {
            UpdateDebugText($"No data for {Time.time - lastDataTime:F1}s\nPackets: {packetCount}");
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
        Debug.Log("Unity: Stopping MetaWear scan...");

#if UNITY_IOS && !UNITY_EDITOR
        MetaWear_StopScan();
#endif
    }

    public void StartStreaming()
    {
        Debug.Log("Unity: Starting streaming...");

#if UNITY_IOS && !UNITY_EDITOR
        MetaWear_StartStreaming();
#endif
    }

    public void StopStreaming()
    {
        Debug.Log("Unity: Stopping streaming...");

#if UNITY_IOS && !UNITY_EDITOR
        MetaWear_StopStreaming();
#endif
    }

    public void DisconnectDevice()
    {
        Debug.Log("Unity: Disconnecting device...");

#if UNITY_IOS && !UNITY_EDITOR
        MetaWear_Disconnect();
#endif
    }

    public void SetUserHeight(float heightCm)
    {
        Debug.Log($"Unity: Setting user height to {heightCm}cm");

#if UNITY_IOS && !UNITY_EDITOR
        MetaWear_SetUserHeight(heightCm);
#endif
    }

    /// <summary>
    /// Called from iOS native code when sensor data is received
    /// </summary>
    public void OnSensorData(string json)
    {
        try
        {
            SensorPacket p = JsonUtility.FromJson<SensorPacket>(json);

            // Update components
            if (caneController != null)
                caneController.ApplySensorData(p);

            if (logger != null)
                logger.Log(p);

            // Track data reception
            lastDataTime = Time.time;
            packetCount++;

            // Update debug info periodically
            if (packetCount % 100 == 0)
            {
                UpdateDebugText($"Receiving data\nPackets: {packetCount}\nAccel: ({p.ax:F2}, {p.ay:F2}, {p.az:F2})\nGyro: ({p.gx:F2}, {p.gy:F2}, {p.gz:F2})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing sensor data: {e.Message}\nJSON: {json}");
            UpdateDebugText($"Error parsing data:\n{e.Message}");
        }
    }

    /// <summary>
    /// Called from iOS native code when connection status changes
    /// </summary>
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
        lastDataTime = Time.time;

        if (statusText != null)
        {
            statusText.text = "Connected";
            statusText.color = Color.green;
        }

        UpdateDebugText("Connected!\nWaiting for data...");
        Debug.Log("Unity: MetaWear connected");
    }

    public void SetDisconnected()
    {
        isConnected = false;

        if (statusText != null)
        {
            statusText.text = "Disconnected";
            statusText.color = Color.red;
        }

        UpdateDebugText($"Disconnected\nLast packet count: {packetCount}");
        Debug.Log("Unity: MetaWear disconnected");
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
    public void OnScanButtonPressed()
    {
        StartScanning();
    }

    public void OnStopScanButtonPressed()
    {
        StopScanning();
    }

    public void OnDisconnectButtonPressed()
    {
        DisconnectDevice();
    }
}
