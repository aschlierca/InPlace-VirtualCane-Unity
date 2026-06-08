using UnityEngine;
using System.Collections.Generic;

public class SensorGraph : MonoBehaviour
{
    /*
    public LineRenderer accelX;
    public LineRenderer accelY;
    public LineRenderer accelZ;

    public LineRenderer gyroX;
    public LineRenderer gyroY;
    public LineRenderer gyroZ;

    const int MaxPoints = 200;

    Queue<float> ax = new();
    Queue<float> ay = new();
    Queue<float> az = new();

    Queue<float> gx = new();
    Queue<float> gy = new();
    Queue<float> gz = new();

    public void AddPoint(SensorPacket p)
    {
        Enqueue(ax, p.ax);
        Enqueue(ay, -p.ay);
        Enqueue(az, -p.az);

        Enqueue(gx, p.gx);
        Enqueue(gy, -p.gy);
        Enqueue(gz, -p.gz);

        Refresh(accelX, ax);
        Refresh(accelY, ay);
        Refresh(accelZ, az);

        Refresh(gyroX, gx);
        Refresh(gyroY, gy);
        Refresh(gyroZ, gz);
    }

    void Enqueue(Queue<float> q, float v)
    {
        if (q.Count >= MaxPoints) q.Dequeue();
        q.Enqueue(v);
    }

    void Refresh(LineRenderer lr, Queue<float> q)
    {
        float[] arr = q.ToArray();

        lr.positionCount = arr.Length;

        for (int i = 0; i < arr.Length; i++)
        {
            lr.SetPosition(i, new Vector3(i * 0.05f, arr[i], 0));
        }
    }
    */
}