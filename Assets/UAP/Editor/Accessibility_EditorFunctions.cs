using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if UNITY_2018_3_OR_NEWER
using UnityEngine.Networking;
#endif

[InitializeOnLoad]
public class Accessibility_EditorFunctions
{
	static public string PluginFolder = "Assets/UAP";

	static Texture2D AccessibilityIcon = null;

	// FIXED: use HTTPS to avoid "Insecure connection not allowed"
	static string versionURL = "https://www.metalpopgames.com/assetstore/accessibility/UAP_Version.txt";

	static bool VersionCheckRunning = false;
#if UNITY_2018_3_OR_NEWER
	static UnityWebRequest wwwVersionCheck = null;
#else
    static WWW wwwVersionCheck = null;
#endif
	static string ignoreVersion = "";
	static bool automaticCheck = false;

	static Accessibility_EditorFunctions()
	{
		EditorApplication.update += WaitForLoadingDone;
	}

	static void WaitForLoadingDone()
	{
		if (EditorApplication.isUpdating)
			return;

		EditorApplication.update -= WaitForLoadingDone;
		PerformStartUpRoutine();
	}

	private static void PerformStartUpRoutine()
	{
		EditorApplication.hierarchyWindowItemOnGUI += DrawAccessibilityIcon;

		// Show welcome window first time
		if (EditorPrefs.GetInt("UAP_WelcomeWindowShown_" + PlayerSettings.productName, 0) == 0)
		{
			EditorPrefs.SetInt("UAP_WelcomeWindowShown_" + PlayerSettings.productName, 1);
			EditorPrefs.SetFloat("UAP_Version_" + PlayerSettings.productName, UAP_AccessibilityManager.PluginVersionAsFloat);
			OpenAboutWindow();
			return;
		}

		// Open About window if plugin updated
		if (EditorPrefs.GetFloat("UAP_Version_" + PlayerSettings.productName, 0.9f) < UAP_AccessibilityManager.PluginVersionAsFloat)
		{
			EditorPrefs.SetFloat("UAP_Version_" + PlayerSettings.productName, UAP_AccessibilityManager.PluginVersionAsFloat);
			OpenAboutWindow();
			return;
		}

		// Automated update check every week
		if (!PlayerPrefs.HasKey("UAP_UpdateCheck_NextCheck"))
			PlayerPrefs.SetString("UAP_UpdateCheck_NextCheck", DateTime.Now.AddDays(3).ToBinary().ToString());

		string nextCheckTimestamp = PlayerPrefs.GetString("UAP_UpdateCheck_NextCheck", DateTime.Now.ToBinary().ToString());
		long temp = Convert.ToInt64(nextCheckTimestamp);
		DateTime date = DateTime.FromBinary(temp);
		if (date <= DateTime.Now)
		{
			PlayerPrefs.SetString("UAP_UpdateCheck_NextCheck", DateTime.Now.AddDays(7).ToBinary().ToString());
			ignoreVersion = PlayerPrefs.GetString("UAP_SkipVersion", "0.9.0");
			automaticCheck = true;
			CheckForUpdate();
		}
	}

	[MenuItem("Tools/UAP Accessibility/About")]
	static public void OpenAboutWindow()
	{
		UAP_WelcomeWindow.Init();
	}

	[MenuItem("Tools/UAP Accessibility/Add Accessibility Manager to Scene")]
	static public void AddPrefabToScene()
	{
		if (GameObject.FindObjectOfType<UAP_AccessibilityManager>() != null)
		{
			EditorUtility.DisplayDialog("UAP already present", "There is already an Accessibility Manager in the scene", "OK");
			return;
		}

		PrefabUtility.InstantiatePrefab(Resources.Load("Accessibility Manager"));
	}

	[MenuItem("Tools/UAP Accessibility/Documentation")]
	static public void OpenDocumentation()
	{
		Application.OpenURL("https://www.metalpopgames.com/assetstore/accessibility/doc");
	}

	[MenuItem("Tools/UAP Accessibility/Check For Updates")]
	static public void CheckForUpdate()
	{
		if (VersionCheckRunning)
			return;

		VersionCheckRunning = true;

#if UNITY_2018_3_OR_NEWER
		wwwVersionCheck = UnityWebRequest.Get(versionURL);
		wwwVersionCheck.downloadHandler = new DownloadHandlerBuffer();
		wwwVersionCheck.SendWebRequest();
#else
        wwwVersionCheck = new WWW(versionURL);
#endif

		UAP_UpdateWindow.StartVersionCheck();
		if (!automaticCheck)
			UAP_UpdateWindow.Init();
		EditorApplication.update += UpdateCheckVersion;
	}

#if ACCESS_NGUI
    static public void DisableNGUISupport()
    {
        UAP_WelcomeWindow.DisableNGUISupport();
    }
#else
	static public void EnableNGUISupport()
	{
		if (EditorUtility.DisplayDialog("Really enable NGUI support?", "Make sure you have NGUI added to your project before you enable UAP NGUI support.\n\nOtherwise the plugin will not compile.", "Go Ahead", "Cancel"))
			UAP_WelcomeWindow.EnableNGUISupport();
	}
#endif

	static public void UpdateCheckVersion()
	{
		if (wwwVersionCheck.isDone)
		{
			EditorApplication.update -= UpdateCheckVersion;

#if UNITY_2018_3_OR_NEWER
			string resultText = wwwVersionCheck.downloadHandler.text;
#else
            string resultText = wwwVersionCheck.text;
#endif

			if (!string.IsNullOrEmpty(resultText) && resultText.StartsWith("VERSION"))
			{
				int index = resultText.IndexOf("\n", 0);
				string latestVersionString = resultText.Substring(8, index - 8);
				string latestVersionRaw = latestVersionString;

				int firstDot = latestVersionString.IndexOf('.');
				int lastDot = latestVersionString.LastIndexOf('.');
				while (lastDot > firstDot)
				{
					latestVersionString = latestVersionString.Remove(lastDot, 1);
					lastDot = latestVersionString.LastIndexOf('.');
				}

				float latestVersion = UAP_AccessibilityManager.PluginVersionAsFloat;
				if (float.TryParse(latestVersionString, out latestVersion))
				{
					if (!automaticCheck || ignoreVersion.CompareTo(latestVersionRaw) != 0)
					{
						if (automaticCheck)
							UAP_UpdateWindow.Init();
						FoundLatestVersion(latestVersion, latestVersionRaw, resultText);
					}
				}
			}
			else
			{
				if (!automaticCheck)
					UAP_UpdateWindow.NoInternet();
			}

			ignoreVersion = "";
			automaticCheck = false;
			VersionCheckRunning = false;
			wwwVersionCheck = null;
		}
	}

	static public void FoundLatestVersion(float latestVersion, string latestVersionAsString, string fullChangeLog)
	{
		UAP_UpdateWindow.VersionCheckComplete((latestVersion > UAP_AccessibilityManager.PluginVersionAsFloat), latestVersionAsString, fullChangeLog);
	}

	static public void DrawAccessibilityIcon(int instanceID, Rect selectionRect)
	{
		GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
		if (gameObject == null)
			return;

		if (AccessibilityIcon == null)
		{
			string iconPath = PluginFolder + "/Editor/img/UAP_Icon.png";
			AccessibilityIcon = AssetDatabase.LoadAssetAtPath(iconPath, typeof(Texture2D)) as Texture2D;
			if (AccessibilityIcon == null)
			{
				Debug.LogWarning("[Accessibility] Could not load accessibility icon at '" + iconPath + "'");
				return;
			}
		}

		if (gameObject.GetComponent<UAP_BaseElement>() != null ||
			gameObject.GetComponent<AccessibleUIGroupRoot>() != null ||
			gameObject.GetComponent<UAP_AccessibilityManager>() != null)
		{
			DrawUAPIcon(selectionRect);
		}
	}

	private static void DrawUAPIcon(Rect selectionRect)
	{
		Rect r = new Rect(selectionRect);
		r.x = r.x + r.width - 20;
		r.width = 18;
		GUI.Label(r, AccessibilityIcon);
	}
}
