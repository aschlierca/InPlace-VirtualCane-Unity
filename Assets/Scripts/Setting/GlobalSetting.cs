using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*This script is used to change the overall settings throughout the project*/
public class GlobalSetting : MonoBehaviour
{
    public bool useSettingMenuChoice = true;          // Variable indicates whether using choice from SettingsMenu/AdvancedSettingsMenu or not (If setting menu never opened, it will use the default value)
    string endOfMessage;                              // The end string at message after updating the value according the SettingsMenu
    string endOfMessage_Advanced;                     // The end string at message after updating the value according the AdvancedSettingsMenu
    int defaultSpeechRateInScene = 60;                // Te default speech rate for the scene if we don't use value from SettingsMenu


    /// <summary>
    /// Awake is the first function called in Unity execution loop
    /// </summary>
    void Awake()
    {
        /* For all scene, we need to implement the dimming setting */
        DisableDimming();

        /* Get settings from SettingsMenu/AdvancedSettingsMenu if we want the choice from there */
        if (!useSettingMenuChoice)
            Debug.Log("You chose to use setting in the current scene instead of choices from setting menu");
        else
        {
            PrepareEndOfMessage();                      // End part of message

            SetHeadRotateUserChoice();                  // Set how to control avatar head rotation
            SetUseInstructionUserChoice();              // Set if the system allow instruction system or not
            SetHeadphoneEnhanceUserChoice();            // Set if enhancing the headphone control or not
            SetRotationsNearAngleUserChoice();          // Set "rotations are near angle" for enhancing headphone control
            SetAllowNodInfoRetrieverUserChoice();       // Set if allowing the user to use "Head nod information retriever"
            SetNodDetectMethodUserChoice();             // Set the method for detecting head nodding
            SetNodSpeedThresholdUserChoice();           // Set the speed threshold for detecting head nodding
            SetNodFramesThresholdUserChoice();          // Set the "continuous frame" threshold for judging if a motion is lowering or raising head in a head-nodding motion 
            SetNodWaitTimeThresholdUserChoice();        // Set the "wait time" threshold. After users lower their heads, we will wait for them to raise up their head for X seconds. This x seconds is the threshold.
        }

        SetSpeechRateUserChoice();                      // Set the speech rate in the App
    }


    /// <summary>
    /// Function disables the App from dimming
    /// </summary>
    void DisableDimming()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }


    /// <summary>
    /// Function tailor message for showing after updating the settings
    /// using value from the SettingsMenu/ AdvancedSettingsMenu
    /// </summary>
    void PrepareEndOfMessage()
    {
        endOfMessage = endOfMessage_Advanced = "user's choice.";

        /* If "SettingsMenu" or "AdvancedSettingsMenu"scene is never activated
         * ===> the value we get from it must be default value */
        if (!SettingsMenu.beenActivated)
            endOfMessage = "default value.";

        if (!AdvancedSettingsMenu.beenActivated)
            endOfMessage_Advanced = "default value.";
    }


    /// <summary>
    /// Function gets value from SettingsMenu to set user's choice of
    /// "how to control avatar's head" across the whole App
    /// </summary>
    void SetHeadRotateUserChoice()
    {
        /* Assign the value */
        UserMovement_IOS.headControlChoice = SettingsMenu.headControlChoice;

        /* Print a message */
        string message = $"Update head rotation to {UserMovement_IOS.headControlChoice} control according to the settings menu {endOfMessage}";
        Debug.Log(message);
    }


    /// <summary>
    /// Function decides what is the speed to speak in the App
    /// </summary>
    void SetSpeechRateUserChoice()
    {
        /* Use the Default Speech Rate of the scene */
        if (!useSettingMenuChoice)
        { 
            UAP_AccessibilityManager.SetSpeechRate(defaultSpeechRateInScene);
            return;
        }

        /* Assign the value */
        UAP_AccessibilityManager.SetSpeechRate(SettingsMenu.appSpeechRate);

        /* Print a message */
        string message = $"Update speech rate to {UAP_AccessibilityManager.GetSpeechRate()} according to the settings menu {endOfMessage}";
        Debug.Log(message);
    }


    /// <summary>
    /// Function gets value from "UseInstructionToggle" in SettingsMenu,
    /// and ask the "InstructionManager.cs" to use it
    /// </summary>
    void SetUseInstructionUserChoice()
    {
        /* Assign the value */
        InstructionManager.allowInstruction = SettingsMenu.useInstruction;

        /* Print a message */
        string message = $"Update 'Use Instruction' to {InstructionManager.allowInstruction} according to the settings menu {endOfMessage}";
        Debug.Log(message);
    }


    /// <summary>
    /// Function gets user's choice value from AdvancedSettingsMenu to decide
    /// if we want to enhance the performance of avatar's head rotation control when using headphone/airpod
    /// </summary>
    void SetHeadphoneEnhanceUserChoice()
    {
        /* Assign the value */
        UserMovement_IOS.enhanceAirpodRotate = AdvancedSettingsMenu.doAirpodControlEnhance;

        /* Print a message */
        string message = $"Update 'Enhancement Airpod Rotate' to {UserMovement_IOS.enhanceAirpodRotate} according to the advanced settings menu {endOfMessage_Advanced}";
        Debug.Log(message);
    }


    /// <summary>
    /// Function gets the "Rotations are near angle" value from AdvancedSettingsMenu,
    /// and ask the "HeadphoneTrackEnhancer.cs" to use it
    /// </summary>
    void SetRotationsNearAngleUserChoice()
    {
        /* Assign the value */
        HeadphoneTrackEnhancer.rotNearErrorMargin = AdvancedSettingsMenu.rotationsNearAngle;

        /* Print a message */
        string message = $"Update 'Rotations Are Near Angle' to {HeadphoneTrackEnhancer.rotNearErrorMargin} according to the advanced settings menu {endOfMessage_Advanced}";
        Debug.Log(message);
    }


    /// <summary>
    /// Function gets the "Allow nod info retriever" value from AdvancedSettingsMenu,
    /// and ask the "FrontInfoRetriever.cs" and "SurroundInfoRetriever.cs" to use it
    /// </summary>
    void SetAllowNodInfoRetrieverUserChoice()
    {
        FrontInfoRetriever.allowRetriever = AdvancedSettingsMenu.allowNodInfoRetriever;
        SurroundInfoRetriever.allowRetriever = AdvancedSettingsMenu.allowNodInfoRetriever;

        /* Print a message */
        string message = $"Update 'Allow Nod Info Retriever' to {FrontInfoRetriever.allowRetriever} according to the advanced settings menu {endOfMessage_Advanced}";
        Debug.Log(message);
    }


    /// <summary>
    /// Function gets the "Method for detecting head-nodding" value from AdvancedSettingsMenu,
    /// and ask the "HeadNodDetector.cs" to use it
    /// </summary>
    void SetNodDetectMethodUserChoice()
    {
        HeadNodDetector.nodDetectMethod = AdvancedSettingsMenu.nodDetectMethod;

        /* Print a message */
        string message = $"Update 'Head Nod Detection Method' to {HeadNodDetector.nodDetectMethod} according to the advanced settings menu {endOfMessage_Advanced}";
        Debug.Log(message);
    }


    /// <summary>
    /// Function gets the "Head nod speed Threshold" from AdvancedSettingsMenu,
    /// and ask the "HeadNodDetector.cs" to use it
    /// </summary>
    void SetNodSpeedThresholdUserChoice()
    {
        HeadNodDetector.minRotXSpeed = AdvancedSettingsMenu.nodSpeedThreshold;

        /* Print a message */
        string message = $"Update 'Head Nod Speed Threshold' to {HeadNodDetector.minRotXSpeed} according to the advanced settings menu {endOfMessage_Advanced}";
        Debug.Log(message);
    }


    /// <summary>
    /// Function gets the "Head nod frames threshold" from AdvancedSettingsMenu,
    /// and ask the "HeadNodDetector.cs" to use it
    /// </summary>
    void SetNodFramesThresholdUserChoice()
    {
        HeadNodDetector.minFrameReq = AdvancedSettingsMenu.nodFramesThreshold;

        /* Print a message */
        string message = $"Update 'Head Nod Frames Threshold' to {HeadNodDetector.minFrameReq} according to the advanced settings menu {endOfMessage_Advanced}";
        Debug.Log(message);
    }


    /// <summary>
    /// Function gets the "Head wait time threshold" from AdvancedSettingsMenu,
    /// and ask the "HeadNodDetector.cs" to use it
    /// </summary>
    void SetNodWaitTimeThresholdUserChoice()
    {
        HeadNodDetector.waitWindowTime = AdvancedSettingsMenu.nodWaitTimeThreshold;

        /* Print a message */
        string message = $"Update 'Head Nod Wait Time Threshold' to {HeadNodDetector.waitWindowTime} according to the advanced settings menu {endOfMessage_Advanced}";
        Debug.Log(message);
    }

}

