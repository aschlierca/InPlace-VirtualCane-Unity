using UnityEngine;

// EDITOR-ONLY test harness for CaneController. Simulates the MetaWear fusion
// quaternion stream so calibration, frame conversion, mount offset, and the
// grip pivot can be verified without the physical board.
//
// Keys chosen to avoid BlueRoom's UserMovement_Editor bindings (WASD/Q/E/R/F/mouse):
//   J / L   sweep (yaw) left / right
//   I / K   dip (pitch) up / down
//   U / O   twist about the shaft (roll)
//   C       Calibrate (zero the cane at current simulated pose)
//   X       toggle simulated yaw drift (10 deg/min)
//   N       toggle a fake 90-degree mount twist (tests mountOffsetEuler)
public class CaneSimulator : MonoBehaviour
{
#if UNITY_EDITOR
    public CaneController cane;

    [Tooltip("Simulated angular speed of the physical cane, deg/s")]
    public float degPerSecond = 60f;

    [Tooltip("Disable the scene's UserMovement_Editor while simulating, so WASD/mouse don't fight the test")]
    public bool disableSceneMovement = true;

    private float yaw, pitch, roll;
    private float drift;
    private bool driftOn;
    private bool mountTwistOn;
    private const float DriftDegPerMin = 10f;

    void Start()
    {
        Debug.Log("[CaneSimulator] active. J/L sweep, I/K dip, U/O twist, C calibrate, X drift, N mount twist.");

        if (cane == null)
            Debug.LogError("[CaneSimulator] Cane field is NOT assigned — drag RoomsText into it and save the scene.");

        if (disableSceneMovement)
        {
            foreach (var m in FindObjectsByType<UserMovement_Editor>(FindObjectsSortMode.None))
            {
                m.enabled = false;
                Debug.Log("[CaneSimulator] Disabled UserMovement_Editor on " + m.gameObject.name);
            }
        }
    }

    void Update()
    {
        if (cane == null) return;

        float step = degPerSecond * Time.deltaTime;
        if (Input.GetKey(KeyCode.J)) yaw   -= step;
        if (Input.GetKey(KeyCode.L)) yaw   += step;
        if (Input.GetKey(KeyCode.I)) pitch -= step;
        if (Input.GetKey(KeyCode.K)) pitch += step;
        if (Input.GetKey(KeyCode.U)) roll  -= step;
        if (Input.GetKey(KeyCode.O)) roll  += step;

        if (Input.GetKeyDown(KeyCode.C)) { Debug.Log("[CaneSimulator] C pressed -> Recalibrate"); cane.Recalibrate(); }
        if (Input.GetKeyDown(KeyCode.X)) { driftOn = !driftOn; Debug.Log("[CaneSimulator] drift " + (driftOn ? "ON" : "off")); }
        if (Input.GetKeyDown(KeyCode.N)) { mountTwistOn = !mountTwistOn; Debug.Log("[CaneSimulator] mount twist " + (mountTwistOn ? "ON" : "off")); }

        if (driftOn) drift += (DriftDegPerMin / 60f) * Time.deltaTime;

        // Simulated PHYSICAL cane orientation, in Unity terms.
        Quaternion physical = Quaternion.Euler(pitch, yaw + drift, roll);

        // Optional simulated mounting twist of the board on the shaft.
        if (mountTwistOn)
            physical *= Quaternion.Euler(0, 0, 90);

        // Convert BACK to the sensor's RH Z-up frame — exact inverse of the
        // remap in CaneController.ApplyQuaternion (unity = (x, z, y, -w)).
        cane.ApplyQuaternion(-physical.w, physical.x, physical.z, physical.y);
    }

    void OnGUI()
    {
        GUI.Box(new Rect(5, 5, 660, 70), "");
        GUI.Label(new Rect(10, 10, 650, 60),
            $"SIM  yaw {yaw:F1}  pitch {pitch:F1}  roll {roll:F1}  drift {drift:F2} ({(driftOn ? "ON" : "off")})  mount {(mountTwistOn ? "ON" : "off")}\n" +
            $"CANE euler {(cane != null ? cane.transform.eulerAngles.ToString("F1") : "NULL — cane not assigned")}\n" +
            "J/L sweep · I/K dip · U/O twist · C calibrate · X drift · N mount");
    }
#endif
}