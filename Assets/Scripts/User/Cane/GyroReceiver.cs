using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class GyroReceiver : MonoBehaviour
{
    [Header("Cane to Rotate")]
    public Transform caneTransform;

    [Header("UDP Settings")]
    public bool useUDP = true;      // Set false for test mode
    public int listenPort = 5005;

    [Header("Smoothing")]
    public float slerpSpeed = 10f;  // Speed of rotation smoothing

    private UdpClient udpClient;
    private Thread receiveThread;
    private Quaternion targetRotation = Quaternion.identity;
    private bool running = true;

    void Start()
    {
        if (useUDP)
        {
            udpClient = new UdpClient(listenPort);
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            Debug.Log("UDP Receiver started on port " + listenPort);
        }
    }

    void Update()
    {
        if (caneTransform == null) return;

        if (!useUDP)
        {
            // ----- TEST MODE -----
            float speed = 200f; // degrees per second
            float xRot = Mathf.Sin(Time.time) * speed * Time.deltaTime;
            float yRot = Mathf.Cos(Time.time) * speed * Time.deltaTime;
            float zRot = Mathf.Sin(Time.time * 0.5f) * speed * Time.deltaTime;

            caneTransform.Rotate(xRot, yRot, zRot, Space.Self);
        }
        else
        {
            // ----- UDP MODE -----
            caneTransform.rotation = Quaternion.Slerp(
                caneTransform.rotation,
                targetRotation,
                Time.deltaTime * slerpSpeed
            );
        }
    }

    private void ReceiveData()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, listenPort);
        while (running)
        {
            try
            {
                if (udpClient.Available > 0)
                {
                    byte[] data = udpClient.Receive(ref remoteEP);
                    string text = Encoding.UTF8.GetString(data).Trim();
                    string[] values = text.Split(',');

                    if (values.Length == 4)
                    {
                        float qx = float.Parse(values[0]);
                        float qy = float.Parse(values[1]);
                        float qz = float.Parse(values[2]);
                        float qw = float.Parse(values[3]);

                        // Adjust iOS -> Unity coordinate system if needed
                        targetRotation = new Quaternion(-qx, -qy, -qz, qw);
                    }
                }
                else
                {
                    Thread.Sleep(1); // Avoid busy loop
                }
            }
            catch (SocketException ex)
            {
                // This will occur when udpClient is closed; ignore it
                // Debug.Log("Socket closed: " + ex.Message);
                break;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("UDP receive error: " + ex.Message);
            }
        }
    }

    void OnApplicationQuit()
    {
        running = false;           // Stop receive loop
        if (udpClient != null)
            udpClient.Close();     // Unblock Receive()
    }
}
