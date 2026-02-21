using System.Runtime.InteropServices;

public static class MetaWearNative
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] public static extern void mw_start_scan();
    [DllImport("__Internal")] public static extern void mw_stop_scan();
    [DllImport("__Internal")] public static extern void mw_select_device(string uuid);
    [DllImport("__Internal")] public static extern void mw_connect_selected();
#else
    public static void mw_start_scan() { }
    public static void mw_stop_scan() { }
    public static void mw_select_device(string uuid) { }
    public static void mw_connect_selected() { }
#endif
}
