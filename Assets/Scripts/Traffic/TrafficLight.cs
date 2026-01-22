using UnityEngine;

public class TrafficLight : MonoBehaviour
{
    public int lightSignal;


    void Start()
    {
        lightSignal = 0;
    }

    public void ChangeLightSignal()
    {
        if (lightSignal == 0)
        {
            lightSignal = 1;
        }
        else
        {
            lightSignal = 0;
        }
    }

    public bool ShouldGo(int directionOfApproach)
    {
        if ((directionOfApproach + lightSignal) % 2 == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void ResetTrafficLight()
    {
        lightSignal = 0;
    }
}