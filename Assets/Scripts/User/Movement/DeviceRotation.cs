using UnityEngine;
using System.Runtime.InteropServices;

public class DeviceRotation : MonoBehaviour
{
    [DllImport("__Internal")] private static extern void StartDeviceMotion();
    [DllImport("__Internal")] private static extern double GetPitch();
    [DllImport("__Internal")] private static extern double GetRoll();
    [DllImport("__Internal")] private static extern double GetYaw();

    void Start()
    {
#if UNITY_IOS && !UNITY_EDITOR
        StartDeviceMotion();
#endif
    }

    void Update()
    {
#if UNITY_IOS && !UNITY_EDITOR
        double pitch = GetPitch();
        double roll = GetRoll();
        double yaw = GetYaw();
#else
        double pitch = 0;
        double roll = 0;
        double yaw = 0;
#endif

        // Convert radians → degrees
        float p = (float)(pitch * Mathf.Rad2Deg);
        float r = (float)(roll * Mathf.Rad2Deg);
        float y = (float)(yaw * Mathf.Rad2Deg);

        // Apply rotation to cane (tweak as needed)
        transform.localRotation = Quaternion.Euler(p, -y, -r);
    }
}
