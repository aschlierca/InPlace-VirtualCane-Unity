using UnityEngine;

public class FixWaypointRotation : MonoBehaviour
{
    void Awake()
    {
        transform.rotation = Quaternion.Euler(0, 0, 0); // set to 0,0,0 on spawn
    }

    void LateUpdate()
    {
        transform.rotation = Quaternion.identity; // override any runtime changes
    }
}