using UnityEngine;
using UnityEngine.UI;
using TMPro; // Needed for TextMeshPro

public class SettingsManager : MonoBehaviour
{
    [Header("UI References")]
    public Slider volumeSlider;
    public Slider hapticsSlider;
    public Slider sensitivitySlider;
    public TextMeshProUGUI heightText;

    [Header("Settings Config")]
    // How much the slider moves per click (0.1 = 10%)
    public float stepSize = 0.1f; 
    
    // Default height in inches (5'8" = 68 inches)
    private int currentHeightInches = 68; 

    void Start()
    {
        // 1. Load saved data (or use defaults)
        volumeSlider.value = PlayerPrefs.GetFloat("Volume", 0.5f);
        hapticsSlider.value = PlayerPrefs.GetFloat("Haptics", 0.5f);
        sensitivitySlider.value = PlayerPrefs.GetFloat("Sensitivity", 0.5f);
        currentHeightInches = PlayerPrefs.GetInt("Height", 68);

        // 2. Refresh the text display
        UpdateHeightDisplay();
    }

    // ---------------- VOLUME CONTROLS ---------------- //
    public void VolumeUp()
    {
        volumeSlider.value += stepSize;
        SaveSettings();
    }

    public void VolumeDown()
    {
        volumeSlider.value -= stepSize;
        SaveSettings();
    }

    // ---------------- HAPTICS CONTROLS ---------------- //
    public void HapticsUp()
    {
        hapticsSlider.value += stepSize;
        Handheld.Vibrate(); // Physical feedback!
        SaveSettings();
    }

    public void HapticsDown()
    {
        hapticsSlider.value -= stepSize;
        SaveSettings();
    }

    // ---------------- SENSITIVITY CONTROLS ---------------- //
    public void SensitivityUp()
    {
        sensitivitySlider.value += stepSize;
        SaveSettings();
    }

    public void SensitivityDown()
    {
        sensitivitySlider.value -= stepSize;
        SaveSettings();
    }

    // ---------------- HEIGHT CONTROLS ---------------- //
    public void HeightUp()
    {
        currentHeightInches++;
        UpdateHeightDisplay();
        SaveSettings();
    }

    public void HeightDown()
    {
        currentHeightInches--;
        UpdateHeightDisplay();
        SaveSettings();
    }

    // ---------------- HELPER FUNCTIONS ---------------- //
    private void UpdateHeightDisplay()
    {
        // Math: 68 inches / 12 = 5 feet
        int feet = currentHeightInches / 12;
        
        // Math: 68 inches % 12 = 8 remainder (inches)
        int inches = currentHeightInches % 12;

        heightText.text = $"{feet}'{inches}\"";
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("Volume", volumeSlider.value);
        PlayerPrefs.SetFloat("Haptics", hapticsSlider.value);
        PlayerPrefs.SetFloat("Sensitivity", sensitivitySlider.value);
        PlayerPrefs.SetInt("Height", currentHeightInches);
        PlayerPrefs.Save();
    }
}