using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // Required for switching scenes

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public Slider volumeSlider;
    public Slider hapticsSlider;
    public Slider sensitivitySlider;
    public TextMeshProUGUI heightText;

    [Header("Settings Config")]
    public float scaleSize = 0.1f;
    private int currentHeightInches = 68;
    
    // Reference to the Step Counter to update stride length
    private StepCount stepCount;

    void Start()
    {
        // --- Initialization ---
        // Try to find StepCount on the same object, or searching the scene if needed
        stepCount = GetComponent<StepCount>(); 
        if(stepCount == null) stepCount = FindObjectOfType<StepCount>();

        // Load saved data
        if(volumeSlider) volumeSlider.value = PlayerPrefs.GetFloat("Volume", 0.5f);
        if(hapticsSlider) hapticsSlider.value = PlayerPrefs.GetFloat("Haptics", 0.5f);
        if(sensitivitySlider) sensitivitySlider.value = PlayerPrefs.GetFloat("Sensitivity", 0.5f);
        currentHeightInches = PlayerPrefs.GetInt("Height", 68);

        UpdateHeightDisplay();
    }

    // ---------------- SETTINGS CONTROLS (Keep existing logic) ---------------- //
    public void VolumeUp() { if(volumeSlider) { volumeSlider.value += scaleSize; SaveSettings(); } }
    public void VolumeDown() { if(volumeSlider) { volumeSlider.value -= scaleSize; SaveSettings(); } }
    
    public void HapticsUp() 
    { 
        if(hapticsSlider) { hapticsSlider.value += scaleSize; Handheld.Vibrate(); SaveSettings(); } 
    }
    public void HapticsDown() { if(hapticsSlider) { hapticsSlider.value -= scaleSize; SaveSettings(); } }

    public void SensitivityUp() { if(sensitivitySlider) { sensitivitySlider.value += scaleSize; SaveSettings(); } }
    public void SensitivityDown() { if(sensitivitySlider) { sensitivitySlider.value -= scaleSize; SaveSettings(); } }

    public void HeightUp() 
    { 
        currentHeightInches++; 
        UpdateHeightAndStride(); 
    }
    public void HeightDown() 
    { 
        currentHeightInches--; 
        UpdateHeightAndStride(); 
    }

    // ---------------- SCENE SWITCHING (New Room Methods) ---------------- //
    
    public void LoadBlueRoom()
    {
        // Replace "ExplorationPart" with your actual Blue Room scene name
        SceneManager.LoadScene("ExplorationPart"); 
    }

    public void LoadGreenRoom()
    {
        // Replace "ExplorationPartV2" with your actual Green Room scene name
        SceneManager.LoadScene("ExplorationPartV2");
    }

    public void LoadRedRoom()
    {
        // Replace "ExplorationPartV3" with your actual Red Room scene name
        SceneManager.LoadScene("ExplorationPartV3");
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // ---------------- HELPER FUNCTIONS ---------------- //
    private void UpdateHeightAndStride()
    {
        UpdateHeightDisplay();
        if(stepCount != null)
        {
            stepCount.stepDistance = 0.415f * currentHeightInches;
        }
        SaveSettings();
    }

    private void UpdateHeightDisplay()
    {
        if(heightText)
        {
            int feet = currentHeightInches / 12;
            int inches = currentHeightInches % 12;
            heightText.text = $"{feet}'{inches}\"";
        }
    }

    private void SaveSettings()
    {
        if(volumeSlider) PlayerPrefs.SetFloat("Volume", volumeSlider.value);
        if(hapticsSlider) PlayerPrefs.SetFloat("Haptics", hapticsSlider.value);
        if(sensitivitySlider) PlayerPrefs.SetFloat("Sensitivity", sensitivitySlider.value);
        PlayerPrefs.SetInt("Height", currentHeightInches);
        PlayerPrefs.Save();
    }
}