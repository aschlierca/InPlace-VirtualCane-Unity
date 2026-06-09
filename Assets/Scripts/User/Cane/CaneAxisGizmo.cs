using UnityEngine;

// Draws the cane's local X/Y/Z axes as colored "laser" lines (red/green/blue) so its
// orientation can be watched live at runtime — in the Scene view Gizmos only show in the
// editor, but LineRenderers also render in device builds, which is what we need to debug
// the sensor-driven rotation on-device.
public class CaneAxisGizmo : MonoBehaviour
{
    public Transform target;     // axes are drawn relative to this transform; defaults to self
    public float length = 0.5f;
    public float lineWidth = 0.01f;

    private LineRenderer xAxis;
    private LineRenderer yAxis;
    private LineRenderer zAxis;

    void Awake()
    {
        if (target == null)
            target = transform;

        xAxis = CreateAxisLine("AxisGizmo_X", Color.red);
        yAxis = CreateAxisLine("AxisGizmo_Y", Color.green);
        zAxis = CreateAxisLine("AxisGizmo_Z", Color.blue);
    }

    void Update()
    {
        DrawAxis(xAxis, target.right);
        DrawAxis(yAxis, target.up);
        DrawAxis(zAxis, target.forward);
    }

    private void DrawAxis(LineRenderer line, Vector3 direction)
    {
        line.SetPosition(0, target.position);
        line.SetPosition(1, target.position + direction * length);
    }

    private LineRenderer CreateAxisLine(string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;
        line.useWorldSpace = true;

        return line;
    }
}
