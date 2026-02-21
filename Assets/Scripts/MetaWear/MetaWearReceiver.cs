using UnityEngine;

public class MetaWearReceiver : MonoBehaviour
{
    [Header("Target Controllers")]
    public VirtualCaneController virtualCane;

    private Vector3 latestAccel;
    private Vector3 latestGyro;
    private bool hasAccel;
    private bool hasGyro;

    // Called from Swift: OnAccelData(JSON)
    public void OnAccelData(string json)
    {
        latestAccel = ParseJsonVector(json);
        hasAccel = true;
        ApplyIfReady();
    }

    // Called from Swift: OnGyroData(JSON)
    public void OnGyroData(string json)
    {
        latestGyro = ParseJsonVector(json);
        hasGyro = true;
        ApplyIfReady();
    }

    private void ApplyIfReady()
    {
        if (hasAccel && hasGyro && virtualCane != null)
        {
            virtualCane.OnIMUData(latestAccel, latestGyro);
            hasAccel = false;
            hasGyro = false;
        }
    }

    private Vector3 ParseJsonVector(string json)
    {
        // JSON format from Swift:
        // {"type":"accel","epoch":123,"x":0.1,"y":0.2,"z":0.3}
        try
        {
            JsonVector data = JsonUtility.FromJson<JsonVector>(json);
            return new Vector3(data.x, data.y, data.z);
        }
        catch
        {
            Debug.LogWarning("Failed to parse IMU JSON: " + json);
            return Vector3.zero;
        }
    }

    [System.Serializable]
    private class JsonVector
    {
        public float x;
        public float y;
        public float z;
    }
}
