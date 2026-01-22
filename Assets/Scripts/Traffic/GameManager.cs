using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public string videoDataDir = "data/4_channel/video/";
    public ScenarioFactory scenarioFactory;
    public CarFactory carFactory;
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

    // In-game waypoints
    public Transform startEast;
    public Transform endEast;
    public Transform startWest;
    public Transform endWest;
    public Transform startNorth;
    public Transform endNorth;
    public Transform startSouth;
    public Transform endSouth;

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
        /*if (currentScenario != null && !currentScenario.isRunning)
        {
            if (shouldRecord)
            {
                if (gameRecorder.isRecording)
                {
                    gameRecorder.StopRecording();
                    gameRecorder.DestroyGameObject();
                }
            }
            if (numScenarios < totalScenarios)
            {
                currentScenario.DestroyGameObject();
                // If back scenario was just run, create new parameters
                CreateParameters();
                GenerateScenario();
            }
            
        }
        */
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

        // Parallel for testing
        // newAngle = 0;
        
        // Setup scenario parameters
        numCars = UnityEngine.Random.Range(1, maxCars);
        dateTimeString = DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss");

        // Setup car parameters
        currentCars = new Car[numCars];
        for (int i = 0; i < numCars; i++)
        {
            InstantiateCar(i);
        }
    }

    void InstantiateCar(int carNumber)
    {
        //Debug.LogError("==========num " + carNumber);
        Transform startPos;
        Transform endPos;

        int startPosInt;
        
        startPosInt = UnityEngine.Random.Range(0, 4);
        //System.Random random = new System.Random();
        //startPosInt = random.Next(0, 5);
        //Debug.Log("Car " + carNumber + " starting at position: " + startPosInt);
        



        // Initial/ideal speed of car
        float carSpeed = UnityEngine.Random.Range(6.7f, 15.6f);

        // Set starting position of car
        if (startPosInt == 0)
        {
            startPos = startNorth;
            endPos = endSouth;
            //Debug.Log("Car " + carNumber + " starting at North.");
        }
        else if (startPosInt == 1)
        {
            startPos = startEast;
            endPos = endWest;
            //Debug.Log("Car " + carNumber + " starting at East.");
        }
        else if (startPosInt == 2)
        {
            startPos = startSouth;
            endPos = endNorth;
            //Debug.Log("Car " + carNumber + " starting at South.");
        }
        else
        {
            startPos = startWest;
            endPos = endEast;
            //Debug.Log("Car " + carNumber + " starting at West.");
        }
            currentCars[carNumber] = carFactory.GetCar();
            Car currentCar = currentCars[carNumber];
            currentCar.gameObject.SetActive(false);
            currentCar.SetInitialPosition(startPos, startPosInt);
            //Debug.Log("Car " + carNumber + " initial position: " + startPos);
            currentCar.SetTargetPosition(endPos);
            currentCar.SetInitialSpeed(carSpeed);
            currentCar.name = string.Format("car_{0}", carNumber);
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
        Debug.Log("Scenario "+ numScenarios + " created.");
    }
}