using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianLightEW : MonoBehaviour
{
    public TrafficLight trafficLight;

    public bool EWCrossing;
    public bool NSCrossing;

    [Header("Pedestrian Light Meshes")]
    public MeshRenderer eastWestGreen;
    public MeshRenderer eastWestRed;
    //public MeshRenderer northSouthGreen;
    //public MeshRenderer northSouthRed;


    private void Start()
    {
        EWCrossing = false;
        NSCrossing = false;
        LightReset();
        StartCoroutine(CyclePedestrianLights());
    }
    public IEnumerator CyclePedestrianLights()
    {
        while (true)
        {
            // Wait for the traffic light to signal for pedestrian crossing
            yield return new WaitUntil(() => EWCrossing || NSCrossing);

            if (EWCrossing)
            {
                //Debug.Log("Pedestrian crossing East-West");
                // Logic for East-West pedestrian crossing
                SetLightColor(eastWestGreen, Color.green);
                SetLightColor(eastWestRed, Color.black);
                //SetLightColor(northSouthGreen, Color.black);
                //SetLightColor(northSouthRed, Color.red);
                yield return new WaitForSeconds(13f);
                EWCrossing = false;
            }

            if (NSCrossing)
            {
                //Debug.Log("Pedestrian crossing North-South");
                // Logic for North-South pedestrian crossing
                SetLightColor(eastWestGreen, Color.black);
                SetLightColor(eastWestRed, Color.red);
                //SetLightColor(northSouthGreen, Color.green);
                //SetLightColor(northSouthRed, Color.black);
                yield return new WaitForSeconds(13f);
                NSCrossing = false;
            }

        }
    }

    void SetLightColor(MeshRenderer meshRenderer, Color color)
    {
        if (meshRenderer != null)
        {
            meshRenderer.sharedMaterial.color = color;
        }
    }

    void LightReset()
    {
        // Reset pedestrian lights to red
        SetLightColor(eastWestGreen, Color.black);
        SetLightColor(eastWestRed, Color.red);
        //SetLightColor(northSouthGreen, Color.black);
        //SetLightColor(northSouthRed, Color.black);
    }
}
