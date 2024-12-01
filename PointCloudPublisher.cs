using UnityEngine;
using Unity.Collections;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;

public class PointCloudPublisher : MonoBehaviour
{
    // ROS connection
    ROSConnection rosConnection;
    public string rosTopic = "/returnfromunity";
    public int maxPoints = 1000;
    public float publishInterval = 0.1f;
    NativeArray<Vector3> pointCloudData;
    public Vector3 parentWorldPosition = new Vector3(0.0f, 0.0f, 0.0f);


    void Start()
    {
        rosConnection = ROSConnection.GetOrCreateInstance();
        rosConnection.RegisterPublisher(rosTopic, "sensor_msgs/PointCloud", 100, false);
        pointCloudData = new NativeArray<Vector3>(maxPoints, Allocator.Persistent);
    }

    void Update()
    {

    }

    // Method to publish point cloud
    public void PublishPointCloud(NativeArray<Vector3> pointCloudData, string frameID, bool isQcar)
    {
        //Debug.Log("Publish");
        // Create a PointCloud message
        PointCloudMsg pointCloudMsg = new PointCloudMsg();

        parentWorldPosition = transform.position;
        Quaternion parentRotation = transform.rotation;
        Quaternion adjustedRotation = new Quaternion(parentRotation.z, -parentRotation.y, parentRotation.x, parentRotation.w);


        // Set the header of the message
        pointCloudMsg.header.frame_id = frameID;
        pointCloudMsg.header.stamp = new TimeMsg { 
            sec = (uint)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 
            nanosec = (uint)((Time.time - (uint)Time.time) * 1e9f) 
        }; // Set current time

        // Clear existing points in the message
        pointCloudMsg.points = new Point32Msg[pointCloudData.Length];

        // Add points from the native array
        for (int i = 0; i < pointCloudData.Length; i++)
        {
            Vector3 localPoint = pointCloudData[i];
            Vector3 globalPoint = localPoint; // Apply the parent's rotation

            if (isQcar)
            {
                pointCloudMsg.points[i] = new Point32Msg(
                    (globalPoint.x) / 10,
                    (globalPoint.z) / 10,
                    (globalPoint.y) / 10);
            }
            else
            {
                pointCloudMsg.points[i] = new Point32Msg(
                    (globalPoint.z) / 10,
                    ((globalPoint.x * -1)) / 10,
                    (globalPoint.y) / 10);
            }
        }
        // Publish the message
        rosConnection.Publish(rosTopic, pointCloudMsg);
        
    }

    void OnDestroy()
    {
        // Dispose of the native array
        pointCloudData.Dispose();
    }
}