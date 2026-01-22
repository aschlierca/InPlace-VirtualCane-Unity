using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class MetaWearManager : MonoBehaviour
{
    public IntPtr boardPtr;
    public IntPtr quaternionSignalPtr;

    // Quaternion struct to match MetaWear
    [StructLayout(LayoutKind.Sequential)]
    public struct QuaternionData
    {
        public float w;
        public float x;
        public float y;
        public float z;
    }

    // Delegate type for callbacks from native code
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void QuaternionCallback(IntPtr context, ref QuaternionData data);

    // DLL Imports
    [DllImport("__Internal")]
    private static extern void mw_init_board(IntPtr board, IntPtr btleConnection);

    [DllImport("__Internal")]
    private static extern void mw_start_sensor_fusion(IntPtr board);

    [DllImport("__Internal")]
    private static extern void mw_stop_sensor_fusion(IntPtr board);

    [DllImport("__Internal")]
    private static extern IntPtr mw_get_quaternion_signal(IntPtr board);

    [DllImport("__Internal")]
    private static extern void mw_subscribe_quaternion(IntPtr signal, IntPtr context, QuaternionCallback callback);

    [DllImport("__Internal")]
    private static extern void mw_unsubscribe(IntPtr signal);

    [DllImport("__Internal")]
    private static extern void mw_free_board(IntPtr board);

    // Example callback
    private void OnQuaternionData(IntPtr context, ref QuaternionData data)
    {
        Debug.Log($"Quaternion: w={data.w}, x={data.x}, y={data.y}, z={data.z}");
        // Here you can use data to rotate an object in Unity
    }

    // Start sensor fusion and subscribe
    public void StartFusion()
    {
        mw_start_sensor_fusion(boardPtr);
        quaternionSignalPtr = mw_get_quaternion_signal(boardPtr);
        mw_subscribe_quaternion(quaternionSignalPtr, IntPtr.Zero, OnQuaternionData);
    }

    // Stop sensor fusion and unsubscribe
    public void StopFusion()
    {
        if (quaternionSignalPtr != IntPtr.Zero)
        {
            mw_unsubscribe(quaternionSignalPtr);
            quaternionSignalPtr = IntPtr.Zero;
        }
        mw_stop_sensor_fusion(boardPtr);
    }

    // Cleanup
    private void OnDestroy()
    {
        StopFusion();
        mw_free_board(boardPtr);
    }
}
