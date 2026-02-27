using UnityEngine;
using TMPro;
using System.Runtime.InteropServices;

public class StepCount : MonoBehaviour
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void StartStepCounter();

    [DllImport("__Internal")]
    private static extern int GetStepCount();
#else
    private static void StartStepCounter() { }
    private static int GetStepCount() { return 0; }
#endif

    [Header("UI")]
    public TMP_Text stepText;

    [Header("Player to Move")]
    public Transform player;
    [SerializeField] public float stepDistance = 1f;

    private int realSteps = 0;     // from pedometer
    private int manualSteps = 0;   // from buttons

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartStepCounter();
        UpdateStepUI();
    }

    void Update()
    {
#if UNITY_IOS && !UNITY_EDITOR
        int newRealSteps = GetStepCount();
        int diff = newRealSteps - realSteps;

        if (diff > 0)
        {
            realSteps = newRealSteps;
            MovePlayer(diff);
            UpdateStepUI();
        }


#endif
    }

    // ---------- Buttons ----------
    public void AddStep()
    {
        manualSteps++;
        MovePlayer(1);
        UpdateStepUI();
    }

    public void SubtractStep()
    {
        if (manualSteps + realSteps > 0)
        {
            manualSteps--;
            MovePlayer(-1);
            UpdateStepUI();
        }
    }

    private void UpdateStepUI()
    {
        int displayedSteps = realSteps + manualSteps;
        if (stepText != null)
            stepText.text = displayedSteps.ToString();
    }

    private void MovePlayer(int steps)
    {
        if (player != null && steps != 0)
            player.Translate(Vector3.forward * stepDistance * steps);
    }
}
