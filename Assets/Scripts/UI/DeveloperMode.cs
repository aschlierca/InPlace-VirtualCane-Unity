using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Developer mode, toggled by a small switch in the top-right corner.
// When on: the step add/subtract buttons are shortened (bottom edge kept in
// place) and a top-down camera view of the user fills the freed space between
// the buttons and the room view. The state persists across scenes via PlayerPrefs.
// The switch has no audio/haptics and no UAP accessible component, so it stays
// out of the VoiceOver navigation order.
public class DeveloperMode : MonoBehaviour
{
    [Header("UI (found by name under this canvas if empty)")]
    public RectTransform switchParent;     // GlobalUI
    public RectTransform stepsButtons;     // StepsButtons layout group
    public RectTransform[] stepButtons;    // AddSteps, SubtractSteps
    public RectTransform homePanel;
    public RectTransform roomView;         // VirtualRoomRawImage

    [Header("Developer layout")]
    public float devButtonHeight = 400f;
    public float viewGap = 20f;

    [Header("Top-down camera")]
    public Transform user;                 // defaults to StepCount.player
    public float cameraHeight = 10f;
    public float viewHalfHeight = 2.5f;    // metres visible above/below the user
    public bool rotateWithUser = true;     // user's forward points up on screen

    private const string PrefKey = "DeveloperMode";
    private static readonly Color OnColor = new Color(0.20f, 0.75f, 0.35f);
    private static readonly Color OffColor = new Color(0.55f, 0.55f, 0.55f);
    private static Sprite circleSprite;

    private bool devOn;
    private Vector2[] normalSizes;
    private Vector2 normalGroupPos;
    private Image switchTrack;
    private RectTransform switchKnob;
    private RawImage topDownImage;
    private Camera topDownCam;
    private RenderTexture topDownRT;

    void Start()
    {
        ResolveReferences();

        normalGroupPos = stepsButtons != null ? stepsButtons.anchoredPosition : Vector2.zero;
        normalSizes = new Vector2[stepButtons.Length];
        for (int i = 0; i < stepButtons.Length; i++)
            normalSizes[i] = stepButtons[i].sizeDelta;

        BuildSwitch();
        SetDeveloperMode(PlayerPrefs.GetInt(PrefKey, 0) == 1);
    }

    public void SetDeveloperMode(bool on)
    {
        devOn = on;
        PlayerPrefs.SetInt(PrefKey, on ? 1 : 0);
        PlayerPrefs.Save();

        UpdateSwitchVisual();
        ApplyButtonLayout();
        if (on) ShowTopDown();
        else HideTopDown();
    }

    void LateUpdate()
    {
        if (!devOn || topDownCam == null) return;
        if (user == null) user = FindUser();
        if (user == null) return;

        topDownCam.transform.position = user.position + Vector3.up * cameraHeight;
        float yaw = rotateWithUser ? user.eulerAngles.y : 0f;
        topDownCam.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
    }

    void OnDestroy()
    {
        if (topDownCam != null) Destroy(topDownCam.gameObject);
        if (topDownRT != null) topDownRT.Release();
    }

    // ---- step buttons ----

    private void ApplyButtonLayout()
    {
        if (stepsButtons == null || stepButtons.Length == 0) return;

        stepsButtons.anchoredPosition = normalGroupPos;
        for (int i = 0; i < stepButtons.Length; i++)
            stepButtons[i].sizeDelta = normalSizes[i];
        if (!devOn) return;

        RectTransform space = (RectTransform)stepsButtons.parent;
        Canvas.ForceUpdateCanvases();
        float bottom = BottomIn(space, stepButtons[0]);

        for (int i = 0; i < stepButtons.Length; i++)
            stepButtons[i].sizeDelta = new Vector2(normalSizes[i].x, devButtonHeight);
        LayoutRebuilder.ForceRebuildLayoutImmediate(stepsButtons);

        // the layout group centres the buttons; shift so the bottom edge stays put
        stepsButtons.anchoredPosition += Vector2.up * (bottom - BottomIn(space, stepButtons[0]));
    }

    // ---- top-down view ----

    private void ShowTopDown()
    {
        if (homePanel == null) return;

        Canvas.ForceUpdateCanvases();
        float y0 = stepButtons.Length > 0 ? TopIn(homePanel, stepButtons[0]) + viewGap : homePanel.rect.yMin;
        float y1 = roomView != null ? BottomIn(homePanel, roomView) - viewGap : homePanel.rect.yMax;
        float height = Mathf.Max(100f, y1 - y0);
        float width = homePanel.rect.width;

        if (topDownImage == null)
        {
            var go = new GameObject("DevTopDownView", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(homePanel, false);
            topDownImage = go.GetComponent<RawImage>();
            topDownImage.raycastTarget = false;
        }
        RectTransform rt = topDownImage.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(0f, (y0 + y1) * 0.5f - homePanel.rect.center.y);

        float scale = GetComponentInParent<Canvas>().scaleFactor;
        int pw = Mathf.Max(64, Mathf.RoundToInt(width * scale));
        int ph = Mathf.Max(64, Mathf.RoundToInt(height * scale));
        if (topDownRT == null || topDownRT.width != pw || topDownRT.height != ph)
        {
            if (topDownRT != null) topDownRT.Release();
            topDownRT = new RenderTexture(pw, ph, 16) { name = "DevTopDownRT" };
        }

        if (topDownCam == null)
        {
            topDownCam = new GameObject("DevTopDownCamera").AddComponent<Camera>();
            topDownCam.orthographic = true;
            topDownCam.clearFlags = CameraClearFlags.SolidColor;
            topDownCam.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            topDownCam.farClipPlane = cameraHeight + 50f;
        }
        topDownCam.orthographicSize = viewHalfHeight;
        topDownCam.targetTexture = topDownRT;
        topDownCam.enabled = true;

        topDownImage.texture = topDownRT;
        topDownImage.gameObject.SetActive(true);
    }

    private void HideTopDown()
    {
        if (topDownImage != null) topDownImage.gameObject.SetActive(false);
        if (topDownCam != null) topDownCam.enabled = false;
    }

    private Transform FindUser()
    {
        StepCount steps = FindFirstObjectByType<StepCount>();
        if (steps != null && steps.player != null) return steps.player;
        GameObject go = GameObject.Find("User");
        return go != null ? go.transform : null;
    }

    // ---- switch ----

    private void BuildSwitch()
    {
        if (switchParent == null) return;

        var root = new GameObject("DeveloperModeSwitch", typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(switchParent, false);
        root.transform.SetAsLastSibling();
        var rt = (RectTransform)root.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(140f, 72f);

        switchTrack = root.GetComponent<Image>();
        switchTrack.sprite = CircleSprite();
        switchTrack.type = Image.Type.Sliced;
        switchTrack.pixelsPerUnitMultiplier = (CircleSprite().border.x) / (rt.sizeDelta.y * 0.5f);
        root.GetComponent<Button>().onClick.AddListener(() => SetDeveloperMode(!devOn));

        var knob = new GameObject("Knob", typeof(RectTransform), typeof(Image));
        knob.transform.SetParent(root.transform, false);
        switchKnob = (RectTransform)knob.transform;
        switchKnob.sizeDelta = new Vector2(60f, 60f);
        var knobImage = knob.GetComponent<Image>();
        knobImage.sprite = CircleSprite();
        knobImage.raycastTarget = false;

        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(root.transform, false);
        var lrt = (RectTransform)label.transform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0f, 0.5f);
        lrt.pivot = new Vector2(1f, 0.5f);
        lrt.anchoredPosition = new Vector2(-12f, 0f);
        lrt.sizeDelta = new Vector2(120f, 72f);
        var text = label.GetComponent<TextMeshProUGUI>();
        text.text = "DEV";
        text.fontSize = 34;
        text.alignment = TextAlignmentOptions.MidlineRight;
        text.raycastTarget = false;
    }

    private void UpdateSwitchVisual()
    {
        if (switchTrack == null) return;
        switchTrack.color = devOn ? OnColor : OffColor;
        float x = devOn ? 1f : 0f;
        switchKnob.anchorMin = switchKnob.anchorMax = switchKnob.pivot = new Vector2(x, 0.5f);
        switchKnob.anchoredPosition = new Vector2(devOn ? -6f : 6f, 0f);
    }

    // White anti-aliased circle; sliced, it also draws the switch's pill track.
    private static Sprite CircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        const int size = 64;
        float r = size * 0.5f;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(r - d) * 255));
            }
        tex.SetPixels32(px);
        tex.Apply();
        float b = r - 1f;
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                                     100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        return circleSprite;
    }

    // ---- helpers ----

    private void ResolveReferences()
    {
        if (switchParent == null) switchParent = FindRect("GlobalUI");
        if (stepsButtons == null) stepsButtons = FindRect("StepsButtons");
        if (homePanel == null) homePanel = FindRect("HomePanel");
        if (roomView == null) roomView = FindRect("VirtualRoomRawImage");
        if (stepButtons == null || stepButtons.Length == 0)
        {
            RectTransform add = FindRect("AddSteps"), sub = FindRect("SubtractSteps");
            stepButtons = (add != null && sub != null) ? new[] { add, sub } : new RectTransform[0];
        }
    }

    private RectTransform FindRect(string objectName)
    {
        foreach (RectTransform rt in GetComponentsInChildren<RectTransform>(true))
            if (rt.name == objectName) return rt;
        Debug.LogWarning("[DeveloperMode] Could not find " + objectName);
        return null;
    }

    private static float BottomIn(RectTransform space, RectTransform target)
    {
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        return space.InverseTransformPoint(corners[0]).y;
    }

    private static float TopIn(RectTransform space, RectTransform target)
    {
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        return space.InverseTransformPoint(corners[1]).y;
    }
}
