using UnityEngine;

public class CaneController : MonoBehaviour
{
    public HeightCalibration calibration;
    public Transform gripPoint;

    [Header("Smoothing")]
    public float smoothSpeed = 20f;

    [Header("Mounting")]
    public Vector3 mountOffsetEuler = Vector3.zero;
    private Quaternion mountOffset = Quaternion.identity;

    // --- Candidate sensor->Unity frame conversions. Cycle with the MAP button. ---
    private int mapIndex = 0;
    private static readonly string[] mapLabels = {
        "A (-y, z, x, -w)", "B (y, z, x, -w)", "C (y, -z, x, -w)", "D (-y, -z, x, w)",
        "E (x, z, y, -w)",  "F (x, y, z, w)",  "G (-x, -y, z, w)", "H (x, z, y, w)"
    };

    private Quaternion Convert(float w, float x, float y, float z)
    {
        switch (mapIndex)
        {
            case 0:  return new Quaternion(-y,  z,  x, -w);
            case 1:  return new Quaternion( y,  z,  x, -w);
            case 2:  return new Quaternion( y, -z,  x, -w);
            case 3:  return new Quaternion(-y, -z,  x,  w);
            case 4:  return new Quaternion( x,  z,  y, -w);
            case 5:  return new Quaternion( x,  y,  z,  w);
            case 6:  return new Quaternion(-x, -y,  z,  w);
            default: return new Quaternion( x,  z,  y,  w);
        }
    }

    // State
    private Quaternion sensorNow = Quaternion.identity;
    private Quaternion sensorRef = Quaternion.identity;
    private Quaternion caneRef   = Quaternion.identity;
    private float lw, lx, ly, lz;              // latest raw values, so re-zero works instantly
    private bool hasData = false;
    private bool calibrated = false;

    void Awake()
    {
        caneRef = transform.rotation;
        mountOffset = Quaternion.Euler(mountOffsetEuler);
    }

    public void ApplyQuaternion(float w, float x, float y, float z)
    {
        lw = w; lx = x; ly = y; lz = z;
        sensorNow = Convert(w, x, y, z);

        if (!hasData)
        {
            hasData = true;
            Recalibrate();
        }
    }

    public void Recalibrate()
    {
        sensorRef = sensorNow;
        caneRef = transform.rotation;
        calibrated = true;
        Debug.Log("[CaneController] Calibrated (mapping " + mapLabels[mapIndex] + ")");
    }

    private void NextMapping()
    {
        mapIndex = (mapIndex + 1) % mapLabels.Length;
        sensorNow = Convert(lw, lx, ly, lz);
        Recalibrate();   // fresh zero at the moment of switching
    }

    void Update()
    {
        if (!calibrated) return;

        Quaternion deltaBody = Quaternion.Inverse(sensorRef) * sensorNow;
        Quaternion target = caneRef * (Quaternion.Inverse(mountOffset) * deltaBody * mountOffset);

        Vector3 gripBefore = gripPoint.position;
        transform.rotation = Quaternion.Slerp(transform.rotation, target, smoothSpeed * Time.deltaTime);
        transform.position += gripBefore - gripPoint.position;
    }

    void OnGUI()
    {
        float bw = Screen.width * 0.42f, bh = Screen.height * 0.09f;
        GUI.skin.button.fontSize = (int)(bh * 0.3f);

        if (GUI.Button(new Rect(Screen.width * 0.04f, Screen.height - bh * 1.3f, bw, bh),
                       "MAP " + (mapIndex + 1) + "/8\n" + mapLabels[mapIndex]))
            NextMapping();

        if (GUI.Button(new Rect(Screen.width * 0.54f, Screen.height - bh * 1.3f, bw, bh), "ZERO"))
            Recalibrate();
    }
}