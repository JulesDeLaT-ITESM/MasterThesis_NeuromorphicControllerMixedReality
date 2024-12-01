using Unity.Collections;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosScan = RosMessageTypes.Sensor.PointCloudMsg; // Alias for PointCloudMsg from ROS

public class PointCloudAquisition : MonoBehaviour
{
    public int maxPoints = 10000; 
    ROSConnection rosConnection;

    public NativeArray<Vector3> pointCloudData; // Point cloud verctor data stored in a native array
    public Reaction_LidarController lidarController; // Reference to the Reaction_LidarController script
    public bool isQcar;
    public string rosTopic = "/point_cloud"; // Expose the ROS topic as a public variable



    void Start()
    {
        Debug.Log("Start");
        pointCloudData = new NativeArray<Vector3>(maxPoints, Allocator.Persistent); // Initialize the native array to store point cloud data
        rosConnection = ROSConnection.GetOrCreateInstance(); // Get or create the ROS connection instance
        Debug.Log("Connected");
        rosConnection.Subscribe<RosScan>(rosTopic, ReceivePointCloud); // Subscribe to the topic specified in the rosTopic variable


    }

    void OnDestroy()
    {
        // Dispose of the native array when the script is destroyed
        if (pointCloudData.IsCreated)
            pointCloudData.Dispose(); 
    }

    public void ReceivePointCloud(RosScan message)
    {
        //UnityEngine.Profiling.Profiler.BeginSample("CloudPointProcessing");
        // Dispose of the current native array
        if (pointCloudData.IsCreated)
            pointCloudData.Dispose();

        // Create a new native array with the length of the received points
        pointCloudData = new NativeArray<Vector3>(message.points.Length, Allocator.Persistent);

        // Iterate through the points in the message and save them to the native array
        for (int i = 0; i < message.points.Length; i++)
        {
            pointCloudData[i] = message.points[i].From<FLU>();
        }
        //Debug.Log("PointCloud");
        
        if (lidarController != null)
        {
            lidarController.ScheduleJob(pointCloudData, message.header.frame_id, isQcar); //Schedule LidarController RaycastCommand NativeArray construction Job
            //Debug.Log(pointCloudData.Length);
        }
    }
}