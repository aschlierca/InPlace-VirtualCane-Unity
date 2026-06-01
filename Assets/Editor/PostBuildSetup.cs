using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class PostBuildSetup
{
    [PostProcessBuild(1)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict root = plist.root;

        // PlistDocument is keyed, so every Set/Create below overwrites the existing
        // entry in place (Unity writes many of these from Player Settings). That means
        // the resulting Info.plist contains exactly one entry per key — no duplicates.

        // --- Scene delegate wiring (UnitySceneDelegate) ---
        PlistElementDict sceneManifest = root.CreateDict("UIApplicationSceneManifest");
        sceneManifest.SetBoolean("UIApplicationSupportsMultipleScenes", false);
        PlistElementDict sceneConfigs = sceneManifest.CreateDict("UISceneConfigurations");
        PlistElementArray appRole = sceneConfigs.CreateArray("UIWindowSceneSessionRoleApplication");
        PlistElementDict sceneCfg = appRole.AddDict();
        sceneCfg.SetString("UISceneConfigurationName", "Default Configuration");
        sceneCfg.SetString("UISceneDelegateClassName", "UnitySceneDelegate");

        // --- Frame pacing ---
        root.SetBoolean("CADisableMinimumFrameDuration", false);
        root.SetBoolean("CADisableMinimumFrameDurationOnPhone", false);

        // --- Bundle identity (mirrors Player Settings; template vars resolved by Xcode) ---
        root.SetBoolean("CFBundleAllowMixedLocalizations", true);
        root.SetString("CFBundleDevelopmentRegion", "en");
        root.SetString("CFBundleDisplayName", "MRCaneMW");
        root.SetString("CFBundleExecutable", "${EXECUTABLE_NAME}");
        root.SetString("CFBundleIdentifier", "${PRODUCT_BUNDLE_IDENTIFIER}");
        root.SetString("CFBundleInfoDictionaryVersion", "6.0");
        root.SetString("CFBundleName", "${PRODUCT_NAME}");
        root.SetString("CFBundlePackageType", "APPL");
        root.SetString("CFBundleShortVersionString", "0.1.0");
        root.SetString("CFBundleVersion", "0");

        root.SetBoolean("LSRequiresIPhoneOS", true);
        root.SetBoolean("LSSupportsOpeningDocumentsInPlace", true);

        // --- Privacy usage descriptions ---
        root.SetString("NSBluetoothAlwaysUsageDescription", "This app uses Bluetooth to connect to MetaWear sensors.");
        root.SetString("NSBluetoothPeripheralUsageDescription", "This app uses Bluetooth to communicate with external sensors.");
        root.SetString("NSCameraUsageDescription", "This app uses motion data to track steps and device movement.");
        root.SetString("NSMotionUsageDescription", "");

        // --- File sharing ---
        root.SetBoolean("UIFileSharingEnabled", true);

        // --- Launch storyboards ---
        root.SetString("UILaunchStoryboardName", "LaunchScreen-iPhone");
        root.SetString("UILaunchStoryboardName~ipad", "LaunchScreen-iPad");
        root.SetString("UILaunchStoryboardName~iphone", "LaunchScreen-iPhone");
        root.SetString("UILaunchStoryboardName~ipod", "LaunchScreen-iPhone");

        root.SetBoolean("UIPrerenderedIcon", false);

        // --- Required device capabilities ---
        PlistElementArray caps = root.CreateArray("UIRequiredDeviceCapabilities");
        caps.AddString("arm64");
        caps.AddString("metal");

        root.SetBoolean("UIRequiresFullScreen", true);
        root.SetBoolean("UIRequiresPersistentWiFi", false);
        root.SetBoolean("UIStatusBarHidden", true);
        root.SetString("UIStatusBarStyle", "UIStatusBarStyleDefault");

        // --- Supported orientations ---
        PlistElementArray orientations = root.CreateArray("UISupportedInterfaceOrientations");
        orientations.AddString("UIInterfaceOrientationPortrait");
        orientations.AddString("UIInterfaceOrientationPortraitUpsideDown");
        orientations.AddString("UIInterfaceOrientationLandscapeRight");
        orientations.AddString("UIInterfaceOrientationLandscapeLeft");

        root.SetBoolean("UIViewControllerBasedStatusBarAppearance", true);
        root.SetInteger("Unity_LoadingActivityIndicatorStyle", -1);

        plist.WriteToFile(plistPath);
    }
}
