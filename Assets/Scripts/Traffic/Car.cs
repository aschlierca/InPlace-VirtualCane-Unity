using System.Collections;
using UnityEngine;

public class Car : MonoBehaviour
{
    public float initialSpeed = 4f;
    public float moveSpeed = 4f;
    private float targetSpeed = 4f;

    public int directionOfApproach;
    private Vector3 targetPosition;
    private Vector3 targetDirection;

    public float rotationSpeed = 90f;
    private Quaternion targetRotation;
    private bool isRotating = false;
    public TrafficLight trafficLight;
    public bool isFront = false;
    public GameObject frontBumper;
    public LayerMask backBumperMask;
    public LayerMask trafficBoxmask;

    void OnEnable()
    {
        moveSpeed = initialSpeed;
        targetRotation = transform.rotation;
        isRotating = false;
    }

    public IEnumerator FollowWaypoints(Waypoint currentWaypoint)
    {
        if (currentWaypoint == null)
        yield break;

    while (gameObject.activeSelf)
    {
        Waypoint nextWaypoint = null;

        if (currentWaypoint.nextWaypoints != null && currentWaypoint.nextWaypoints.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, currentWaypoint.nextWaypoints.Count);
            nextWaypoint = currentWaypoint.nextWaypoints[randomIndex];
        }
        else
        {
            gameObject.SetActive(false);
            yield break;
        }

        SetTargetPosition(nextWaypoint.transform);
        Vector3 directionToNext = (nextWaypoint.transform.position - transform.position).normalized;
        if (directionToNext != Vector3.zero)
            SetTargetDirection(directionToNext);

        // Move toward next waypoint
        while (Vector3.Distance(transform.position, nextWaypoint.transform.position) > 0.5f && gameObject.activeInHierarchy)
        {
            // --- TRAFFIC LIGHT CHECK ---
            if (trafficLight != null && !trafficLight.ShouldGo(directionOfApproach))
            {
                // Stop at red
                yield return null; // wait one frame, then check again
                continue;
            }

            // Move normally
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // Rotate toward direction
            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.deltaTime);
            }

            yield return null; // wait one frame
        }

        currentWaypoint = nextWaypoint;
        yield return new WaitForSeconds(0.05f);
    }
    }



    void FixedUpdate()
    {
        HandleRotation();
        HandleMovement();
    }

    void HandleRotation()
    {
        if (isRotating)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
            {
                transform.rotation = targetRotation;
                isRotating = false;
            }
        }
    }

    void HandleMovement()
    {
        if (trafficLight != null && !trafficLight.ShouldGo(directionOfApproach))
        {
            // Red light — stop the car
            return;
        }

        float currentSpeed = moveSpeed;

        // Optional: reduce speed when turning
        if (isRotating)
            currentSpeed *= 0.7f;

        if (currentSpeed > targetSpeed)
            currentSpeed = Mathf.Max(currentSpeed - 0.5f, targetSpeed);
        else
            currentSpeed = Mathf.Min(currentSpeed + 0.01f, targetSpeed);

        if (!isRotating)
            moveSpeed = currentSpeed;
        if (currentSpeed <= 4f)
            currentSpeed = 4f;
        // Move toward target
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.fixedDeltaTime);
    }

    

    public void SetInitialPosition(Transform startPos, int direction)
    {
        transform.position = startPos.position;
        transform.rotation = startPos.rotation;
        directionOfApproach = direction;
        targetRotation = transform.rotation;
    }

    public void SetTargetPosition(Transform waypoint)
    {
        targetPosition = waypoint.position;

    }



    public void SetTargetDirection(Vector3 direction)
    {
        targetDirection = direction.normalized;

    }

    private void Update()
    {
    // Only move if we have a target
    if (targetPosition != Vector3.zero)
    {
        // Move
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // Calculate direction to face
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }
    }
}