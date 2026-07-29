using UnityEngine;

// Drives the virtual cane from the MetaWear fusion quaternion.
// CAL = 5 step calibration wizard, run once per mount. ZERO = set forward, use constantly.
// FLIP = reverse everything if directions come out mirrored. READ = print yaw/pitch for validation.
// The flat spin is the anchor for the axis solve since gravity forces it vertical.
// Tip arc only needs ~90 degrees, a full over-the-shoulder swing is awkward with a real cane.
public class CaneController : MonoBehaviour
{
    public HeightCalibration calibration;
    public Transform gripPoint;

    [Header("Smoothing")]
    public float smoothSpeed = 20f;

    // saved to PlayerPrefs, survives relaunches. dies if the app gets deleted
    private Quaternion axisMap = Quaternion.identity;
    private int chirality = 1;

    // live state
    private Quaternion sensorNow = Quaternion.identity;
    private Quaternion sensorRef = Quaternion.identity;
    private Quaternion caneRef = Quaternion.identity;
    private bool hasData = false;
    private bool calibrated = false;
    private float lastPacketTime = -999f;
    private int readCount = 0;
    private string lastReading = "";

    // wizard
    private int calStep = -1;
    private Quaternion q0, q1, q2, q3;
    private string status = "";
    private string calWarn = "";

    // per step minimum rotation before CAPTURE unlocks. spin and roll stay strict
    // since those are easy to do big, the tip arc floor is low for real cane range
    private static readonly float[] stepMin = { 0f, 60f, 120f, 120f, 0f };
    private const float MaxAngle = 240f;

    private static readonly string[] calTitle = {
        "STEP 1 of 5 - START POSE",
        "STEP 2 of 5 - TIP ARC",
        "STEP 3 of 5 - FLAT SPIN",
        "STEP 4 of 5 - SHAFT ROLL",
        "STEP 5 of 5 - FINISH"
    };

    private static readonly string[] calBody = {
        "Hold the cane in your normal grip,\ntip pointing forward and down.\nHold completely still, then tap CAPTURE.",
        "Swing the TIP down until it points\nstraight at the floor - further if\nyou comfortably can.\nWatch the angle below - tap CAPTURE when GOOD.",
        "Return to the start pose first.\nThen turn the cane's heading a flat\nhalf-turn (turn your body with it if\neasier). CAPTURE when GOOD.",
        "Return to the start pose first.\nThen keep the tip aimed forward and\nROLL the shaft between your fingers,\nabout half a turn. CAPTURE when GOOD.",
        "Return to the start pose\n(normal grip, tip forward-down).\nHold still and tap FINISH."
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
        // handedness swap, sensor is right handed and Unity is left handed
        sensorNow = new Quaternion(-x, -y, z, w);
        lastPacketTime = Time.realtimeSinceStartup;
        if (!hasData) { hasData = true; Recalibrate(); }
    }

    // half a second without a packet means the board is dead or BLE dropped
    private bool SensorLive()
    {
        return Time.realtimeSinceStartup - lastPacketTime < 0.5f;
    }

    // plants the flag: wherever the cane points right now becomes forward
    public void Recalibrate()
    {
        sensorRef = sensorNow;
        caneRef = transform.rotation;
        calibrated = true;
    }

    void Update()
    {
        if (!calibrated || calStep >= 0) return;

        // rotation since the last ZERO, pushed through the measured axis map
        Quaternion delta = Quaternion.Inverse(sensorRef) * sensorNow;
        if (chirality < 0) delta = Quaternion.Inverse(delta);
        Quaternion corrected = axisMap * delta * Quaternion.Inverse(axisMap);
        Quaternion target = caneRef * corrected;

        // rotate around the grip, not the object origin
        Vector3 gripBefore = gripPoint.position;
        transform.rotation = Quaternion.Slerp(transform.rotation, target, smoothSpeed * Time.deltaTime);
        transform.position += gripBefore - gripPoint.position;
    }

    private static float Wrap180(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        if (a < -180f) a += 360f;
        return a;
    }

    // one data point for the floor validation. prints to Xcode console and the screen
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

    // axis and angle of the rotation between two captured poses
    private static Vector3 DeltaAxis(Quaternion from, Quaternion to, out float angle)
    {
        Quaternion d = Quaternion.Inverse(from) * to;
        d.ToAngleAxis(out angle, out Vector3 axis);
        if (angle > 180f) { angle = 360f - angle; axis = -axis; }
        return axis.normalized;
    }

    private float LiveAngleFromStart()
    {
        return Quaternion.Angle(q0, sensorNow);
    }

    private void FinishCalibration()
    {
        // each captured motion defines one cane axis
        Vector3 aRight = DeltaAxis(q0, q1, out float ang1);   // tip arc = pitch
        Vector3 aUp    = DeltaAxis(q0, q2, out float ang2);   // flat spin = yaw
        Vector3 aFwd   = DeltaAxis(q0, q3, out float ang3);   // shaft roll = roll

        if (ang1 < stepMin[1] || ang2 < stepMin[2] || ang3 < stepMin[3])
        {
            calWarn = "A motion was too small (" + ang1.ToString("F0") + "/" + ang2.ToString("F0") + "/" + ang3.ToString("F0") +
                      " deg). Use BACK to redo the small one.";
            return;
        }
        // if two motions rotated the same way the frame is garbage, bail out
        if (Mathf.Abs(Vector3.Dot(aRight, aUp)) > 0.7f ||
            Mathf.Abs(Vector3.Dot(aRight, aFwd)) > 0.7f ||
            Mathf.Abs(Vector3.Dot(aUp, aFwd)) > 0.7f)
        {
            calWarn = "Two motions were too similar - the tip arc, flat spin, and shaft roll must each rotate a different way. Redo CAL.";
            return;
        }

        // handedness check, auto flips if the board is wired the other way
        if (Vector3.Dot(Vector3.Cross(aRight, aUp), aFwd) < 0f)
        { chirality = -1; aRight = -aRight; aUp = -aUp; aFwd = -aFwd; }
        else chirality = 1;

        // spin axis is trusted, gravity keeps it honest. project its error out of the tip arc
        Vector3 u = aUp.normalized;
        Vector3 r = (aRight - Vector3.Dot(aRight, u) * u).normalized;
        Vector3 f = Vector3.Cross(r, u);

        axisMap = Quaternion.Inverse(Quaternion.LookRotation(f, u));

        PlayerPrefs.SetFloat("cane_map_x", axisMap.x);
        PlayerPrefs.SetFloat("cane_map_y", axisMap.y);
        PlayerPrefs.SetFloat("cane_map_z", axisMap.z);
        PlayerPrefs.SetFloat("cane_map_w", axisMap.w);
        PlayerPrefs.SetInt("cane_chir", chirality);
        PlayerPrefs.Save();

        status = "Calibrated. chir=" + chirality;
        calWarn = "";
        Debug.Log("[CaneController] Calibrated. angles=(" + ang1.ToString("F0") + "," + ang2.ToString("F0") + "," + ang3.ToString("F0") + ") chir=" + chirality);
        calStep = -1;
        Recalibrate();
    }

    void OnGUI()
    {
        float W = Screen.width, H = Screen.height;
        float bh = H * 0.08f;
        GUI.skin.button.fontSize = (int)(bh * 0.32f);
        GUI.skin.label.fontSize  = (int)(bh * 0.30f);
        GUI.skin.box.fontSize    = (int)(bh * 0.26f);

        // main screen
        if (calStep < 0)
        {
            GUI.Box(new Rect(10, 10, W * 0.62f, bh * 0.7f), status);
            if (lastReading.Length > 0)
                GUI.Box(new Rect(10, 14 + bh * 0.7f, W * 0.62f, bh * 0.7f), lastReading);
            if (!SensorLive())
                GUI.Box(new Rect(10, 18 + bh * 1.4f, W * 0.62f, bh * 0.7f), "NO SENSOR DATA - check board / Bluetooth");

            if (GUI.Button(new Rect(W * 0.03f, H - bh * 1.25f, W * 0.22f, bh), "ZERO"))
                Recalibrate();
            if (GUI.Button(new Rect(W * 0.27f, H - bh * 1.25f, W * 0.22f, bh), "READ"))
                TakeReading();
            if (GUI.Button(new Rect(W * 0.51f, H - bh * 1.25f, W * 0.22f, bh), "CAL"))
            { calStep = 0; calWarn = ""; }
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

        // wizard screen
        GUI.Box(new Rect(W * 0.05f, H * 0.16f, W * 0.9f, bh * 0.8f), calTitle[calStep]);
        GUI.Box(new Rect(W * 0.05f, H * 0.16f + bh * 0.9f, W * 0.9f, bh * 2.6f), calBody[calStep]);

        bool live = SensorLive();
        string gate;
        bool canCapture;

        if (!live)
        {
            gate = "NO SENSOR DATA - connect the board first";
            canCapture = false;
        }
        else if (calStep == 0 || calStep == 4)
        {
            // still poses, nothing to measure, just need live data
            gate = "Sensor OK - hold still";
            canCapture = true;
        }
        else
        {
            // motion steps show live rotation so nobody has to guess the angle
            float a = LiveAngleFromStart();
            float need = stepMin[calStep];
            bool inBand = a >= need && a <= MaxAngle;
            gate = "Rotated: " + a.ToString("F0") + " deg  " + (inBand ? "- GOOD, tap CAPTURE" : "- keep going (need " + (int)need + "+)");
            canCapture = inBand;
        }

        GUI.Box(new Rect(W * 0.05f, H * 0.16f + bh * 3.6f, W * 0.9f, bh * 0.8f), gate);
        if (calWarn.Length > 0)
            GUI.Box(new Rect(W * 0.05f, H * 0.16f + bh * 4.5f, W * 0.9f, bh * 1.2f), calWarn);

        string btn = calStep == 4 ? "FINISH" : "CAPTURE";
        if (GUI.Button(new Rect(W * 0.28f, H - bh * 1.25f, W * 0.44f, bh), btn))
        {
            if (canCapture)
            {
                calWarn = "";
                switch (calStep)
                {
                    case 0: q0 = sensorNow; calStep++; break;
                    case 1: q1 = sensorNow; calStep++; break;
                    case 2: q2 = sensorNow; calStep++; break;
                    case 3: q3 = sensorNow; calStep++; break;
                    case 4: FinishCalibration(); break;
                }
            }
            else calWarn = live ? "Not enough rotation yet - watch the angle readout." : "No sensor data - can't capture.";
        }

        if (calStep > 0 && GUI.Button(new Rect(W * 0.03f, H - bh * 1.25f, W * 0.2f, bh), "BACK"))
        { calStep--; calWarn = ""; }

        if (GUI.Button(new Rect(W * 0.8f, 10, W * 0.16f, bh * 0.8f), "X"))
        { calStep = -1; status = "Cal cancelled."; calWarn = ""; }
    }
}