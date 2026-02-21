using UnityEngine;
using System.Collections.Generic;

public class DeviceManager : MonoBehaviour
{
    private List<string> devices = new List<string>();

    void Start()
    {
#if UNITY_IOS && !UNITY_EDITOR
        MetaWearNative.mw_start_scan();
#endif
    }

    public void OnDeviceFound(string uuid)
    {
        if (!devices.Contains(uuid))
        {
            devices.Add(uuid);
            Debug.Log("MetaWear Found: " + uuid);
        }
    }

    public void ConnectFirst()
    {
        if (devices.Count == 0) return;

#if UNITY_IOS && !UNITY_EDITOR
        MetaWearNative.mw_select_device(devices[0]);
        MetaWearNative.mw_connect_selected();
#endif
    }
}
