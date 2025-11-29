using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public string videoDataDir = "data/4_channel/video/";
    public ScenarioFactory scenarioFactory;
    public CarFactory carFactory;
    public TrafficLight trafficLight;
    //public RecorderFactory recorderFactory;
    public bool shouldRecord = true;
    public bool isFourDirections = false;
    //private GameRecorder gameRecorder;
    private ScenarioManager currentScenario;
    private int numScenarios;
    public const int maxCars = 16;
    public const int maxScenarios = 200;
    private int totalScenarios = maxScenarios;
    private bool hasEnded = false;

    // Scenario related constants
    private float minX = 7f;
    private float maxX = 12f;
    private float minY = 7f;
    private float maxY = 12f;

    // Per scenario parameters and objects
    public float newX;
    public float newY;
    public int newAngle;
    public int numCars;
    public string dateTimeString;
    public Car[] currentCars;
    public int startPosInt;

    public Waypoint startWaypointNorth;
    //public Waypoint startWaypointEast;
    //public Waypoint startWaypointSouth;
    //public Waypoint startWaypointWest;
    public Waypoint startWaypoint; //isStart true waypoint

    void Awake()
    {
        Application.targetFrameRate = 50;
        // UnityEngine.Random.InitState(28);
    }

    void Start()
    {
        AudioConfiguration configs = AudioSettings.GetConfiguration();
        Debug.Log("Speaker Mode: " + configs.speakerMode);
        Debug.Log("Driver Capabilities: " + AudioSettings.driverCapabilities);
        Debug.Log("Real Voices: " + configs.numRealVoices);
        Debug.Log("Virtual Voices: " + configs.numVirtualVoices);

        numScenarios = 0;
        CreateParameters();
        GenerateScenario();
    }

    void FixedUpdate()
    {
        if (numScenarios >= totalScenarios && !currentScenario.isRunning && !hasEnded)
        {
            Debug.Log("Simulation complete.");
            hasEnded = true;
        }
    }

    void CreateParameters()
    {
        // Setup listener parameters
    newX = UnityEngine.Random.Range(minX, maxX);
    newY = UnityEngine.Random.Range(minY, maxY);
    newAngle = UnityEngine.Random.Range(0, 360);

    // Setup scenario parameters
    numCars = UnityEngine.Random.Range(1, maxCars);
    dateTimeString = DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss");

    // Create array once
    currentCars = new Car[numCars];

    // Start spawning cars with delay
    StartCoroutine(SpawnCarsWithDelay(numCars, 1f)); // 1 second gap between cars
    }
    private IEnumerator SpawnCarsWithDelay(int totalCars, float delay)
    {
        for (int i = 0; i < numCars; i++)
        {
            InstantiateCar(i);
            yield return new WaitForSeconds(delay);
        }
    }
    void InstantiateCar(int carNumber)
    {
        currentCars[carNumber] = carFactory.GetCar();
        Car currentCar = currentCars[carNumber];

        currentCar.trafficLight = trafficLight;

        int randomDirection = UnityEngine.Random.Range(0, 4);
        int north = 0;
        Waypoint startWaypoint = null;

        switch (north)
        {
            case 0:
                startWaypoint = startWaypointNorth;
                break;
                /*case 1:
                    startWaypoint = startWaypointEast;
                    break;
                case 2:
                    startWaypoint = startWaypointSouth;
                    break;
                case 3:
                    startWaypoint = startWaypointWest;
                    break;
                */
        }



        currentCar.transform.position = startWaypoint.transform.position;
        //Debug.Log($"Car {currentCar.name} instantiated at position {currentCar.transform.position}");
        Vector3 firstTargetPos = startWaypoint.nextWaypoints[0].transform.position;
        Vector3 dir = (firstTargetPos - startWaypoint.transform.position).normalized;
        if (dir != Vector3.zero)
        {
            currentCar.transform.rotation = Quaternion.LookRotation(dir);
        }
        currentCar.SetInitialPosition(startWaypoint.transform, randomDirection);

        currentCar.gameObject.SetActive(true);
        StartCoroutine(currentCar.FollowWaypoints(startWaypoint));

        if (carNumber == 0)
        {
            Destroy(currentCar.gameObject);
            return;
        }
    }


    // Helper method to find the starting waypoint in a waypoint container
    private Waypoint FindStartWaypoint(GameObject waypointContainer)
    {
        Waypoint[] waypoints = waypointContainer.GetComponentsInChildren<Waypoint>();
        
        // First try to find a waypoint marked as start
        foreach (Waypoint waypoint in waypoints)
        {
            if (waypoint.isStart)
            {
                return waypoint;
            }
        }
        
        // If no waypoint is marked as start, return the first one
        if (waypoints.Length > 0)
        {
            return waypoints[0];
        }
        
        return null;
    }


    async void GenerateScenario()
    {
        // Pause 1 second between new scenario
        await Task.Delay(1000);

        // Start front scenario
        currentScenario = scenarioFactory.GetScenario();
        currentScenario.SetupScenario(newX, newY, newAngle, numCars, currentCars, dateTimeString);
        currentScenario.gameObject.SetActive(true);
        string filePath = videoDataDir + currentScenario.baseFileStem;
        /*if (shouldRecord)
        {
            gameRecorder = recorderFactory.GetRecorder();
            gameRecorder.SetupRecorder(filePath);
            gameRecorder.StartRecording();
        }
        */
        numScenarios += 1;
        Debug.Log("Scenario " + numScenarios + " created.");
    }
}