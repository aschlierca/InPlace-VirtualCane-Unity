using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Waypoint : MonoBehaviour
{

    //[Header("Connected Waypoints")]
    public bool isStart = false;
    public List<Waypoint> nextWaypoints = new List<Waypoint>();

    //[Header("Gizmo Settings")]
    public float gizmoSphereRadius = 0.3f;
    public Color gizmoColor;
    public Color lineColor;

    
    void Start()
    {
        
    }

    private void OnDrawGizmos()
    {
        // Draw the waypoint node
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoSphereRadius);

        // Draw connections to each connected waypoint
        if (nextWaypoints != null)
        {
            Gizmos.color = lineColor;

            foreach (Waypoint wp in nextWaypoints)
            {
                if (wp != null)
                {
                    Gizmos.DrawLine(transform.position, wp.transform.position);
                }
            }
        }

#if UNITY_EDITOR
        // Optional: Show label above the waypoint
        //Handles.Label(transform.position + Vector3.up * 0.5f, gameObject.name);
#endif
    }
}
