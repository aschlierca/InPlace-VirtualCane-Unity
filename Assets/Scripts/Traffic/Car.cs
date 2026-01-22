using System.Collections;
using System.Collections.Generic;
using System.Globalization;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;

public class Car : MonoBehaviour
{
    public float initialSpeed = 5f;
    public float moveSpeed = 5f;
    private float targetSpeed = 5f;
    private Vector3 initialPosition;
    public int directionOfApproach;
    private Vector3 targetPosition;
    public LayerMask backBumperMask;
    public LayerMask trafficBoxMask;
    public GameObject frontBumper;
    public TrafficLight trafficLight;
    public bool isFront = false;


    void Start()
    {
        // Debug.Log(gameObject.name + " created.");
    }

    void OnEnable()
    {
        transform.position = initialPosition;
        moveSpeed = initialSpeed;
    }

    void FixedUpdate()
    {
        // Move forward
        HandleMovement();

        // Raycast to detect cars ahead
        Vector3 origin = frontBumper.transform.position;
        // By default, cars travel west to east (0 degrees)
        Vector3 direction = -transform.forward;
        float distance = 5f;
        RaycastHit hit;
        Physics.Raycast(origin, direction, out hit, distance);
        Debug.DrawRay(origin, direction * distance, Color.green);
        // If there is a car or traffic light close ahead
        if (hit.collider != null)
        {
            // Match car speed to car ahead or has traffic light near
            Car hitCar = hit.collider.GetComponentInParent<Car>();
            if (hitCar != null)
            {
                // Debug.Log(gameObject.name + " detects " + hitCar.name + " at " + hitCar.moveSpeed + " m/s.");
                float carAheadSpeed = hitCar.moveSpeed;

                SetTargetSpeed(carAheadSpeed);
            }
            else
            {
                if (!trafficLight.ShouldGo(directionOfApproach))
                {
                    SetTargetSpeed(0f);
                    isFront = true;
                }
            }
        }
        else
        {
            SetTargetSpeed(initialSpeed);
            isFront = false;
        }

        // Debug.LogError(gameObject.name + " is front: " + isFront);
        if (isFront)
        {
            // Debug.Log(gameObject.name + " is front car.");
            if (trafficLight.ShouldGo(directionOfApproach))
            {
                // Debug.Log(gameObject.name + " called to go.");
                // MovementRestart();
                SetTargetSpeed(initialSpeed);
            }
        }
    }

    // void OnTriggerEnter2D(Collider2D collision)
    // {
    //     // Debug.LogError("Car collided with " + col.name);
    //     if (!trafficLight.ShouldGo(directionOfApproach))
    //     {
    //         MovementStop();
    //         isFront = true;
    //     }
    // }

    public void HandleMovement()
    {
        // Quicker slow down
        if (moveSpeed > targetSpeed)
        {
            float diff = moveSpeed - targetSpeed;
            if (diff > 0.5f)
            {
                moveSpeed = moveSpeed - 0.5f;
            }
            else
            {
                moveSpeed = targetSpeed;
            }
        }
        else
        {
            float diff = targetSpeed - moveSpeed;
            if (diff > 0.01f)
            {
                moveSpeed = moveSpeed + 0.01f;
            }
            else
            {
                moveSpeed = targetSpeed;
            }
        }

        // Move towards target
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.fixedDeltaTime);

        // Check if car reached target, stop car
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            moveSpeed = 0f;
            gameObject.SetActive(false);
            // Destroy(gameObject);
        }
    }

    // public void SetTrafficBoxMask(LayerMask curMask)
    // {
    //     trafficBoxMask = curMask;
    // }

    public void MovementStop()
    {
        moveSpeed = 0f;
    }

    public void MovementRestart()
    {
        moveSpeed = initialSpeed;
    }

    public void SetInitialPosition(Transform newPosition, int directionOfApproachInt)
    {
        initialPosition = newPosition.position;
        transform.rotation = newPosition.rotation;
        transform.position = initialPosition;
        directionOfApproach = directionOfApproachInt;
    }

    public void SetTargetPosition(Transform newPosition)
    {
        targetPosition = newPosition.position;
    }

    public void SetTargetSpeed(float speed)
    {
        targetSpeed = speed;
    }

    public void SetInitialSpeed(float speed)
    {
        initialSpeed = speed;
    }
    public void StartCar()
    {
        moveSpeed = initialSpeed;
    }
}