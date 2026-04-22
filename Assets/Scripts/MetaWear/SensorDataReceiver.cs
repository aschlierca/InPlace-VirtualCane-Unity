using UnityEngine;
using System.Runtime.InteropServices;
using TMPro;

public class SensorDataReceiver : MonoBehaviour
{
    public CaneController caneController;
    public SensorGraph graph;
    public DataLogger logger;
    public TMPro.TextMeshProUGUI statusText;

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void MetaWear_StartScan();

    [DllImport("__Internal")]
    private static extern void MetaWear_StopStreaming();

    [DllImport("__Internal")]
    private static extern void MetaWear_SetUserHeight(float heightCm);
#endif

    public void StartScanning()
    {
#if UNITY_IOS && !UNITY_EDITOR
        MetaWear_StartScan();
#endif
    }

    public void StopStreaming()
    {
#if UNITY_IOS && !UNITY_EDITOR
        MetaWear_StopStreaming();
#endif
    }

    public void SetUserHeight(float heightCm)
    {
#if UNITY_IOS && !UNITY_EDITOR
        MetaWear_SetUserHeight(heightCm);
#endif
    }

    public void OnSensorData(string json)
    {
        SensorPacket p = JsonUtility.FromJson<SensorPacket>(json);

        caneController.ApplySensorData(p);
        graph.AddPoint(p);
        logger.Log(p);
    }

    public void SetConnected()
    {
        statusText.text = "Connected";
        statusText.color = Color.green;
    }

    public void SetDisconnected()
    {
        statusText.text = "Disconnected";
        statusText.color = Color.red;
    }

    public void OnConnectionStatus(string status)
    {
        if (status == "connected")
            SetConnected();
        else
            SetDisconnected();
    }
}