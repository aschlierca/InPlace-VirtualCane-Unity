using UnityEngine;

// Cane orientation from MetaWear fused quaternion.
// Working configuration (validated 2026-07): guided CAL wizard with ~180-degree
// motions, chirality FLIP toggle, persisted via PlayerPrefs.
// This build adds READ: logs cane yaw/pitch relative to the last ZERO for the
// floor validation protocol.
public class CaneController : MonoBehaviour
{
    public HeightCalibration calibration;
    public Transform gripPoint;

    [Header("Smoothing")]
    public float smoothSpeed = 20f;

    // ---- calibration state (persisted) ----
    private Quaternion axisMap = Quaternion.identity;
    private int chirality = 1;

    // ---- runtime state ----
    private Quaternion sensorNow = Quaternion.identity;
    private Quaternion sensorRef = Quaternion.identity;
    private Quaternion caneRef = Quaternion.identity;
    private bool hasData = false;
    private bool calibrated = false;
    private int readCount = 0;
    private string lastReading = "";

    // ---- wizard state ----
    private int calStep = -1;
    private Quaternion q0, q1, q2, q3;
    private string status = "";

    private static readonly string[] calMsgs = {
        "1/5  Hold FLAT, logo up,\nUSB toward you. Keep still.\nTap CAPTURE.",
        "2/5  Tip it NOSE-DOWN ~180\n(full half-turn forward).\nTap CAPTURE.",
        "3/5  Back to flat-forward, then\nSPIN RIGHT ~180 (stay flat).\nTap CAPTURE.",
        "4/5  Back to flat-forward, then\nTWIST CLOCKWISE ~180.\nTap CAPTURE.",
        "5/5  Return to flat-forward.\nTap FINISH."
    };

    void Awake()
    {
        caneRef = transform.rotation;
        if (PlayerPrefs.HasKey("cane_map_w"))
        {
            axisMap = new Quaternion(
                PlayerPrefs.GetFloat("cane_map_x"), PlayerPrefs.GetFloat("cane_map_y"),
                PlayerPrefs.GetFloat("cane_map_z"), PlayerPrefs.GetFloat("cane_map_w"));
            chirality = PlayerPrefs.GetInt("cane_chir", 1);
            status = "Loaded saved calibration. chir=" + chirality;
        }
        else status = "Not calibrated - tap CAL.";
    }

    public void ApplyQuaternion(float w, float x, float y, float z)
    {
        sensorNow = new Quaternion(-x, -y, z, w);
        if (!hasData) { hasData = true; Recalibrate(); }
    }

    public void Recalibrate()
    {
        sensorRef = sensorNow;
        caneRef = transform.rotation;
        calibrated = true;
    }

    void Update()
    {
        if (!calibrated || calStep >= 0) return;

        Quaternion delta = Quaternion.Inverse(sensorRef) * sensorNow;
        if (chirality < 0) delta = Quaternion.Inverse(delta);
        Quaternion corrected = axisMap * delta * Quaternion.Inverse(axisMap);
        Quaternion target = caneRef * corrected;

        Vector3 gripBefore = gripPoint.position;
        transform.rotation = Quaternion.Slerp(transform.rotation, target, smoothSpeed * Time.deltaTime);
        transform.position += gripBefore - gripPoint.position;
    }

    // ---- validation reading: cane pose relative to last ZERO ----
    private static float Wrap180(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        if (a < -180f) a += 360f;
        return a;
    }

    private void TakeReading()
    {
        Quaternion rel = Quaternion.Inverse(caneRef) * transform.rotation;
        Vector3 e = rel.eulerAngles;
        float yaw = Wrap180(e.y);
        float pitch = Wrap180(e.x);
        readCount++;
        lastReading = "#" + readCount + "  yaw " + yaw.ToString("F1") + "  pitch " + pitch.ToString("F1");
        Debug.Log("[Reading] #" + readCount + " yaw: " + yaw.ToString("F1") + " pitch: " + pitch.ToString("F1") + " (rel. to last ZERO)");
    }

    private static Vector3 DeltaAxis(Quaternion from, Quaternion to, out float angle)
    {
        Quaternion d = Quaternion.Inverse(from) * to;
        d.ToAngleAxis(out angle, out Vector3 axis);
        if (angle > 180f) { angle = 360f - angle; axis = -axis; }
        return axis.normalized;
    }

    private void FinishCalibration()
    {
        Vector3 aRight = DeltaAxis(q0, q1, out float ang1);
        Vector3 aUp    = DeltaAxis(q0, q2, out float ang2);
        Vector3 aFwd   = DeltaAxis(q0, q3, out float ang3);

        bool angleOk = ang1 > 45f && ang2 > 45f && ang3 > 45f;

        if (Vector3.Dot(Vector3.Cross(aRight, aUp), aFwd) < 0f)
        { chirality = -1; aRight = -aRight; aUp = -aUp; aFwd = -aFwd; }
        else chirality = 1;

        Vector3 r = aRight.normalized;
        Vector3 u = (aUp - Vector3.Dot(aUp, r) * r).normalized;
        Vector3 f = Vector3.Cross(r, u);

        axisMap = Quaternion.Inverse(Quaternion.LookRotation(f, u));

        PlayerPrefs.SetFloat("cane_map_x", axisMap.x);
        PlayerPrefs.SetFloat("cane_map_y", axisMap.y);
        PlayerPrefs.SetFloat("cane_map_z", axisMap.z);
        PlayerPrefs.SetFloat("cane_map_w", axisMap.w);
        PlayerPrefs.SetInt("cane_chir", chirality);
        PlayerPrefs.Save();

        status = (angleOk ? "Calibrated. " : "Calibrated (small angles - redo CAL). ")
                 + "chir=" + chirality;
        Debug.Log("[CaneController] " + status);
        Recalibrate();
    }

    void OnGUI()
    {
        float W = Screen.width, H = Screen.height;
        float bh = H * 0.08f;
        GUI.skin.button.fontSize = (int)(bh * 0.32f);
        GUI.skin.label.fontSize  = (int)(bh * 0.30f);
        GUI.skin.box.fontSize    = (int)(bh * 0.28f);

        if (calStep < 0)
        {
            GUI.Box(new Rect(10, 10, W * 0.62f, bh * 0.7f), status);
            if (lastReading.Length > 0)
                GUI.Box(new Rect(10, 14 + bh * 0.7f, W * 0.62f, bh * 0.7f), lastReading);

            if (GUI.Button(new Rect(W * 0.03f, H - bh * 1.25f, W * 0.22f, bh), "ZERO"))
                Recalibrate();

            if (GUI.Button(new Rect(W * 0.27f, H - bh * 1.25f, W * 0.22f, bh), "READ"))
                TakeReading();

            if (GUI.Button(new Rect(W * 0.51f, H - bh * 1.25f, W * 0.22f, bh), "CAL"))
                calStep = 0;

            if (GUI.Button(new Rect(W * 0.75f, H - bh * 1.25f, W * 0.22f, bh), "FLIP"))
            {
                chirality = -chirality;
                PlayerPrefs.SetInt("cane_chir", chirality);
                PlayerPrefs.Save();
                Recalibrate();
                status = "Direction flipped. chir=" + chirality;
            }
            return;
        }

        GUI.Box(new Rect(W * 0.05f, H * 0.25f, W * 0.9f, bh * 2.4f), calMsgs[calStep]);

        string btn = calStep == 4 ? "FINISH" : "CAPTURE";
        if (GUI.Button(new Rect(W * 0.2f, H - bh * 1.25f, W * 0.6f, bh), btn))
        {
            switch (calStep)
            {
                case 0: q0 = sensorNow; break;
                case 1: q1 = sensorNow; break;
                case 2: q2 = sensorNow; break;
                case 3: q3 = sensorNow; break;
                case 4: FinishCalibration(); calStep = -1; return;
            }
            calStep++;
        }

        if (GUI.Button(new Rect(W * 0.8f, 10, W * 0.16f, bh * 0.8f), "X"))
        { calStep = -1; status = "Cal cancelled."; }
    }
}
