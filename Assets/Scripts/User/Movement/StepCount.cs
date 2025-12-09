using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Runtime.InteropServices;

public class StepCount : MonoBehaviour
{
    private bool manualOverride = false;
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void StartStepCounter();

    [DllImport("__Internal")]
    private static extern int GetStepCount();
#else
    // Editor-safe mock functions
    private static void StartStepCounter()
    {
        //Debug.Log("StartStepCounter() called — iOS only.");
    }

    private static int GetStepCount()
    {
        return 0;
    }
#endif

    [Header("UI")]
    public TMP_Text stepText;

    [Header("Player to Move")]
    public Transform player;
    [SerializeField] public float stepDistance = 1f; // how far the player moves per step

    private int displayedSteps = 0;
    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;


        StartStepCounter();  // begin iOS step counting
        displayedSteps = 0;
        UpdateStepUI();
    }

    void Update()
    {
#if UNITY_IOS && !UNITY_EDITOR
    if (manualOverride) return;

    int realSteps = GetStepCount();
    if (realSteps != displayedSteps)
    {
        int diff = realSteps - displayedSteps;
        MovePlayer(diff);
        displayedSteps = realSteps;
        UpdateStepUI();
    }
#endif
    }

    // ---------- UI Buttons ----------
    public void AddStep()
    {
        manualOverride = true;
        displayedSteps++;
        MovePlayer(+1);
        UpdateStepUI();

        manualOverride = false;
    }

    public void SubtractStep()
    {
        manualOverride = true;
        displayedSteps = Mathf.Max(0, displayedSteps - 1);
        MovePlayer(-1);
        UpdateStepUI();

        manualOverride = false;
    }

    // ---------- Helper Functions ----------
    private void UpdateStepUI()
    {
        if (stepText != null)
        {
            stepText.text = displayedSteps.ToString();
        }
    }

    private void MovePlayer(int steps)
    {
        if (player != null && steps != 0)
        {
            player.Translate(Vector3.forward * stepDistance * steps);
        }
    }

    void LateUpdate()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
