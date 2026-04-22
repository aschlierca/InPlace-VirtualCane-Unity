using UnityEngine;

public class HeightCalibration : MonoBehaviour
{
    [Range(100f, 220f)]
    public float userHeightCm = 170f;

    const float ReferenceHeightCm = 170f;
    const float BaseThreshold = 1.0f;

    public float GetMovementScale()
    {
        return BaseThreshold * (ReferenceHeightCm / userHeightCm);
    }

    public void SetHeight(float heightCm)
    {
        userHeightCm = Mathf.Clamp(heightCm, 100f, 220f);
    }
}