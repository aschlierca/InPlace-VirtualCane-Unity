using UnityEngine;
using System.IO;
using System.Text;

public class DataLogger : MonoBehaviour
{
    StreamWriter writer;

    void Awake()
    {
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        string path = Path.Combine(
            Application.persistentDataPath,
            "CaneSession_" + timestamp + ".csv"
        );

        writer = new StreamWriter(path, false, Encoding.UTF8);

        writer.WriteLine("Epoch,AccX,AccY,AccZ,GyroX,GyroY,GyroZ");
    }

    public void Log(SensorPacket p)
    {
        writer.WriteLine(
            $"{p.epoch},{p.ax},{-p.ay},{-p.az},{p.gx},{-p.gy},{-p.gz}"
        );
    }

    void OnApplicationQuit()
    {
        writer?.Close();
    }
}