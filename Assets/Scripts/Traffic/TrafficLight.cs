using System.Collections;
using UnityEngine;

public class TrafficLight : MonoBehaviour
{
    public int lightSignal; // 0 = East/West Green, 1 = North/South Green, 2 = ALL red
    public PedestrianLightEW pedestrianLightEW;
    public PedestrianLightNS pedestrianLightNS;

    [Header("East-West Light Meshes")]
    public MeshRenderer eastWestGreen;
    public MeshRenderer eastWestYellow;
    public MeshRenderer eastWestRed;

    [Header("North-South Light Meshes")]
    public MeshRenderer northSouthGreen;
    public MeshRenderer northSouthYellow;
    public MeshRenderer northSouthRed;

    private void Start()
    {
        LightReset();
        //Debug.Log("Traffic Light System Initialized");
        StartCoroutine(CycleTrafficLights());
    }

    public IEnumerator CycleTrafficLights()
    {
        while (true)
        {
            // EAST-WEST GREEN, NORTH-SOUTH RED
            //Debug.Log("EAST-WEST GREEN");
            lightSignal = 0;
            //LPI for NS
            pedestrianLightEW.NSCrossing = true;
            pedestrianLightNS.NSCrossing = true;
            SetLightColor(eastWestGreen, Color.green);
            SetLightColor(northSouthRed, Color.red);
            yield return new WaitForSeconds(10f);

            // EAST-WEST YELLOW, NORTH-SOUTH RED
            //Debug.Log("EAST-WEST YELLOW");
            SetLightColor(eastWestGreen, Color.black);
            SetLightColor(eastWestYellow, Color.yellow);
            yield return new WaitForSeconds(3f);

            // ALL RED
            //Debug.Log("ALL RED");
            lightSignal = 2;
            pedestrianLightEW.NSCrossing = false;
            pedestrianLightNS.NSCrossing = false;
            //LPI for EW
            pedestrianLightEW.EWCrossing = true;
            pedestrianLightNS.EWCrossing = true;
            SetLightColor(eastWestYellow, Color.black);
            SetLightColor(eastWestRed, Color.red);
            yield return new WaitForSeconds(7f);

            // NORTH-SOUTH GREEN, EAST-WEST RED
            //Debug.Log("NORTH-SOUTH GREEN");
            lightSignal = 1;
            SetLightColor(northSouthRed, Color.black);
            SetLightColor(northSouthGreen, Color.green);
            yield return new WaitForSeconds(10f);

            // NORTH-SOUTH YELLOW, EAST-WEST RED
            //Debug.Log("NORTH-SOUTH YELLOW");
            SetLightColor(northSouthGreen, Color.black);
            SetLightColor(northSouthYellow, Color.yellow);
            yield return new WaitForSeconds(3f);

            // ALL RED
            //Debug.Log("ALL RED");
            lightSignal = 2;
            pedestrianLightEW.EWCrossing = false;
            pedestrianLightNS.EWCrossing = false;
            //LPI for NS
            pedestrianLightEW.NSCrossing = true;
            pedestrianLightNS.NSCrossing = true;
            SetLightColor(northSouthYellow, Color.black);
            SetLightColor(northSouthRed, Color.red);
            yield return new WaitForSeconds(7f);
            
            LightReset();
        }
    }

    void LightReset()
    {
        SetLightColor(eastWestGreen, Color.green);
        SetLightColor(eastWestYellow, Color.black);
        SetLightColor(eastWestRed, Color.black);
        SetLightColor(northSouthGreen, Color.black);
        SetLightColor(northSouthYellow, Color.black);
        SetLightColor(northSouthRed, Color.red);
    }

    void SetLightColor(MeshRenderer renderer, Color color)
    {
        if (renderer != null)
        {
            renderer.sharedMaterial.color = color;
        }
    }

    public bool ShouldGo(int directionOfApproach)
    {
        if (lightSignal == 2)
        {
            return false; // All red, no cars should go
        }
        else if (lightSignal == 0 && (directionOfApproach == 0 || directionOfApproach == 2))
        {
            return true; // East/West green
        }
        else if (lightSignal == 1 && (directionOfApproach == 1 || directionOfApproach == 3))
        {
            return true; // North/South green
        }
        return false;
    }
}
