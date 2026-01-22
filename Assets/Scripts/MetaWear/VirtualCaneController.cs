using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class VirtualCaneController : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern IntPtr mw_get_quaternion(IntPtr board);

    [DllImport("__Internal")]
    private static extern void mw_start_sensor_fusion(IntPtr board);

    [DllImport("__Internal")]
    private static extern void mw_stop_sensor_fusion(IntPtr board);

    private IntPtr boardPtr; // Should be assigned to your MetaWearBoard instance

    // Latest quaternion data from MetaWear
    private Quaternion sensorRotation = Quaternion.identity;

    // Example: Update quaternion from sensor fusion callback
    // Normally you set this in a callback from your MetaWearBridge
    public void OnQuaternionReceived(float w, float x, float y, float z)
    {
        // MetaWear quaternion: w + xi + yj + zk
        // Unity quaternion: x, y, z, w
        sensorRotation = new Quaternion(x, y, z, w);
    }

    void Start()
    {
        mw_start_sensor_fusion(boardPtr);
    }

    void OnDestroy()
    {
        mw_stop_sensor_fusion(boardPtr);
    }

    void Update()
    {
        // Apply rotation to the cane
        transform.rotation = sensorRotation;
    }
}
