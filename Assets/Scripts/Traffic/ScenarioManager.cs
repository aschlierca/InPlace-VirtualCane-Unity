using System;
using System.IO;
using System.Threading.Tasks;
//using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;

public class ScenarioManager : MonoBehaviour
{
    // External game objects
    //public GameObject listener;
    public TrafficLight trafficLight;

    // Scenario parameters
    private float baseListenerX;
    private float baseListenerY;
    private int baseAngle;
    public int numCars;

    // Log/annotation related variables
    public string baseFileStem;
    public string labelDataDir = "data/4_channel/labels/";
    public float logInterval = 0.1f;

    // Traffic light/Update related variables
    private float scenarioTimer;
    private float timer;
    private float logTimer;

    // Scenario related variables
    private Car[] scenarioCars;
    private int frameNum = 0;
    public bool isRunning;
    private bool hasStarted;
    public int delay = 1000;    // 1 second delay between cars

    // Logging and recording
    public GameManager gameManager;
    //private AudioListenerCapture recorder;

    // Set current scenario
    public void SetupScenario(
        float listenerX, 
        float listenerY, 
        int angle, 
        int numCarsInScene,
        Car[] currentCars,
        string dateTime
    )
    {
        baseListenerX = listenerX;
        baseListenerY = listenerY;
        baseAngle = angle;
        numCars = numCarsInScene;
        scenarioCars = currentCars;
        baseFileStem = dateTime;

        //recorder = listener.GetComponent<AudioListenerCapture>();
    }

    void Awake()
    {
        // isRecording = false;
        isRunning = true;
        hasStarted = false;
        // numCars = UnityEngine.Random.Range(2, 5);
        // numCars = 5;
        // currentCars = new Car[numCars];
        trafficLight.ResetTrafficLight();
        //SetupListener();
    }

    void Start()
    {
        // Debug.Log("Scenario " + baseFileStem + " started.");
        scenarioTimer = 0f;
        timer = 0f;
        logTimer = 0f;
        if (gameManager.shouldRecord)
        {
            WriteScenarioLogFile();
        }
        GenerateCars();
    }

    void FixedUpdate()
    {
        float floatTimeDelta = Time.fixedDeltaTime;
        timer += floatTimeDelta;
        scenarioTimer += floatTimeDelta;
        logTimer += floatTimeDelta;

        // Updates for traffic light
        if (timer >= 1f)
        {
            timer = 0f;
            // Change light signal if 10 seconds elapsed
            if ((int)scenarioTimer % 10 == 0)
            {
                trafficLight.ChangeLightSignal();
            }
        }

        // Update for car logging
        if (logTimer >= logInterval)
        {
            // Log each car
            logTimer = 0f;
            int carCounter = 0;
            //Debug.LogError("=========Secenario Cars: " + numCars);
            
            for (int i = 0; i < numCars; i += 1)
            {
                //Debug.LogError("=========car " + i + scenarioCars[i]);
                Car currentCar = scenarioCars[i];
                if (currentCar.isActiveAndEnabled)
                {
                    if (gameManager.shouldRecord)
                    {
                        UpdateCarLogFile(i);
                    }
                    carCounter += 1;
                }
            }
            /*if (carCounter == 0 && hasStarted == true)
            {
                if (recorder && recorder.isRecording)
                {
                    recorder.StopRecording();
                }
                isRunning = false;
            }*/
            frameNum += 1;
        }
    }

    /*void SetupListener()
    {
        // Set the listener
        listener.transform.position = new Vector2(baseListenerX, baseListenerY);
        listener.transform.eulerAngles = new Vector3(0, 0, baseAngle);
        //AudioListenerCapture recorder = listener.GetComponent<AudioListenerCapture>();
        //recorder.StartRecording(baseFileStem + "_4_channel.wav");
    }*/
    void WriteScenarioLogFile()
    {
        // Create scenario log file
        string baseFilePath = Path.Combine(labelDataDir, baseFileStem);
        string filePath = baseFilePath + "_scenario.csv";
        File.WriteAllText(filePath, "num_cars,log_interval,listener_x,listener_y,listener_rot_z\n");
        // Write scenario information
        //Vector3 listenerPosition = listener.transform.position;
        //Vector3 listenerRotation = listener.transform.eulerAngles;
        //string dataLine = numCars + "," + logInterval + "," + listenerPosition.x + "," 
        //                + listenerPosition.y + "," + listenerRotation.z;
        //File.AppendAllText(filePath, dataLine);
        Debug.Log(filePath + " written.");
    }

    async void GenerateCars()
    {
        // Delay till listener is set
        await Task.Delay(delay);
        for (int i = 0; i < numCars; i++)
        {
            Car currentCar = scenarioCars[i];
            currentCar.gameObject.SetActive(true);
            
            if (gameManager.shouldRecord)
            {
                CreateCarLogFile(i);
            }
            if (i == 0)
            {
                hasStarted = true;
            }

            // Delay of car creation
            await Task.Delay(delay);
        }
    }

    void CreateCarLogFile(int carNumber)
    {
        int doa = scenarioCars[carNumber].directionOfApproach;
        string carFilePath = Path.Combine(labelDataDir, baseFileStem) 
                           + string.Format("_car_{0}_", carNumber)
                           + ".csv";
        File.WriteAllText(carFilePath, "frame_number,x,y,rot_z,doa\n");
    }

    void UpdateCarLogFile(int carNumber)
    {
        // Get current car data
        Car currentCar = scenarioCars[carNumber];
        int doa = currentCar.directionOfApproach;
        Vector3 carPosition = currentCar.transform.position;
        Vector3 carRotation = currentCar.transform.eulerAngles;
        // Write current car data
        string dataLine = frameNum + "," + carPosition.x + ","
                        + carPosition.y + ","
                        + carRotation.z + ","
                        + doa + "\n";
        string currentCarFile = Path.Combine(labelDataDir, baseFileStem) 
                              + string.Format("_car_{0}_", carNumber)
                              + ".csv";
        File.AppendAllText(currentCarFile, dataLine);
    }

    public void DestroyGameObject()
    {
        for (int i = 0; i < numCars; i++)
        {
            Destroy(scenarioCars[i].gameObject);
        }
        Destroy(gameObject);
    }
}