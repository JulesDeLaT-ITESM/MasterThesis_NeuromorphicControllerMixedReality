using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using Unity.Collections;
using UnityEngine;

public class LaserScanAquisition : MonoBehaviour
{
    ROSConnection rosConnection;
    public string topicName = "/scan";
    public NativeArray<Vector3> scanPoints;
    public Reaction_LidarController lidarController; // Reference to the Reaction_LidarController script
    public bool isQcar;

    void Start()
    {
        rosConnection = ROSConnection.GetOrCreateInstance();
        rosConnection.Subscribe<LaserScanMsg>(topicName, LaserScanCallback);
    }

    void LaserScanCallback(LaserScanMsg msg)
    {
        int count = msg.ranges.Length;
        if (scanPoints.IsCreated)
        {
            scanPoints.Dispose();
        }
        scanPoints = new NativeArray<Vector3>(count, Allocator.Persistent);

        float angle = msg.angle_min;
        Quaternion lidarRotation = transform.rotation; // Get the current rotation of the LIDAR
        Quaternion inverseRotation = Quaternion.Inverse(lidarRotation); // Compute the inverse rotation

        for (int i = 0; i < count; i++)
        {
            float range = msg.ranges[i];
            if (range < msg.range_min || range > msg.range_max)
            {
                range = 12f; // Skip invalid measurements
            }
            float x = range * Mathf.Cos(angle);
            float z = range * Mathf.Sin(angle);

            Vector3 point = new Vector3(x, 0, z); // Assuming y = 0 in the LaserScan plane
            scanPoints[i] = lidarRotation * point; // Apply the inverse rotation to each point

            angle += msg.angle_increment;
        }

        if (lidarController != null)
        {
            lidarController.ScheduleJob(scanPoints, msg.header.frame_id, isQcar); // Schedule LidarController RaycastCommand NativeArray construction Job
        }
    }

    void OnDestroy()
    {
        if (scanPoints.IsCreated)
        {
            scanPoints.Dispose();
        }
    }
}
