using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

public class DistanceFromLidar : MonoBehaviour
{
    private NativeArray<Vector3> outputData;
    private int degreeInterval = 10;
    private NativeQueue<GroupedData> groupedData;
    private Dictionary<int, MeasurementStats> processedData;
    private Dictionary<int, List<float3>> groupedPoints;
    private Reaction_LidarController lidarController;
    [SerializeField] private float updateInterval = 0.1f;  // Interval for updating Lidar data

    void Start()
    {
        // Get the Reaction_LidarController component from the same GameObject
        lidarController = GetComponent<Reaction_LidarController>();

        if (lidarController != null)
        {
            // Obtain the array from the Reaction_LidarController public variable
            outputData = lidarController.visualData;
            Debug.Log(outputData);
            groupedData = new NativeQueue<GroupedData>(Allocator.Persistent);
            InvokeRepeating(nameof(UpdateLidarData), 0, updateInterval);
        }
        else
        {
            Debug.LogError("Reaction_LidarController component not found.");
        }
    }

    void OnDestroy()
    {
        if (groupedData.IsCreated)
        {
            groupedData.Dispose();
        }
    }

    public void SetDegreeInterval(int interval)
    {
        degreeInterval = interval;
        if (outputData.IsCreated)
        {
            ProcessData(outputData, degreeInterval);
        }
    }

    public Dictionary<int, MeasurementStats> GetProcessedData()
    {
        if (processedData == null)
        {
            Debug.LogError("Processed data is not available. Make sure to set the output data first.");
            return null;
        }
        return processedData;
    }

    public Dictionary<int, List<float3>> GetGroupedPoints()
    {
        if (groupedPoints == null)
        {
            Debug.LogError("Grouped points data is not available. Make sure to set the output data first.");
            return null;
        }
        return groupedPoints;
    }

    private void UpdateLidarData()
    {
        if (lidarController != null)
        {
            outputData = lidarController.visualData;  // Update the output data
            ProcessData(outputData, degreeInterval);  // Reprocess the data
        }
    }

    private void ProcessData(NativeArray<Vector3> data, int degrees)
    {
        if (groupedData.IsCreated)
        {
            groupedData.Dispose();
        }
        groupedData = new NativeQueue<GroupedData>(Allocator.Persistent);

        var job = new ProcessLidarDataJob
        {
            data = data.Reinterpret<float3>(),
            degreeInterval = degrees,
            groupedData = groupedData.AsParallelWriter(),
            forward = new float3(transform.forward.x, transform.forward.y, transform.forward.z),
            up = new float3(transform.up.x, transform.up.y, transform.up.z),
            position = new float3(transform.position.x, transform.position.y, transform.position.z)
        };

        JobHandle jobHandle = job.Schedule(data.Length, 64);
        jobHandle.Complete();

        var groupDictionary = new Dictionary<int, List<float3>>();
        while (groupedData.Count > 0)
        {
            var item = groupedData.Dequeue();
            if (!groupDictionary.ContainsKey(item.groupID))
            {
                groupDictionary[item.groupID] = new List<float3>();
            }
            groupDictionary[item.groupID].Add(item.point);
        }

        float3 currentPosition = new float3(transform.position.x, transform.position.y, transform.position.z);
        processedData = new Dictionary<int, MeasurementStats>();
        groupedPoints = new Dictionary<int, List<float3>>();
        foreach (var keyValue in groupDictionary)
        {
            var points = keyValue.Value;
            var stats = new MeasurementStats
            {
                mean = 0f,
                max = math.length(points[0] - currentPosition),
                min = math.length(points[0] - currentPosition)
            };
            float totalLength = 0f;
            int count = points.Count;
            for (int i = 0; i < count; i++)
            {
                float length = math.length(points[i] - currentPosition);
                totalLength += length;
                stats.max = math.max(stats.max, length);
                stats.min = math.min(stats.min, length);
            }
            stats.mean = totalLength / count;
            processedData[keyValue.Key] = stats;
            groupedPoints[keyValue.Key] = points;
        }
    }

    // Structure to hold measurement stats based on vector lengths
    public struct MeasurementStats
    {
        public float mean;
        public float max;
        public float min;
    }



    private void Update()
    {
        if (groupedPoints != null)
        {
            // Draw debug lines for each group
            foreach (var keyValue in groupedPoints)
            {
                DrawDebugLinesForGroup(keyValue.Key, keyValue.Value);
            }
        }
    }

    private void DrawDebugLinesForGroup(int groupID, List<float3> points)
    {
        Color color = GetColorForGroup(groupID);
        if (color == Color.black) return; // Skip drawing for undefined groups
        foreach (var point in points)
        {
            Vector3 worldPoint = new Vector3(point.x, point.y, point.z);
            //Debug.DrawLine(transform.position, worldPoint, color);
        }
    }


    private Color GetColorForGroup(int groupID)
    {
        switch (groupID)
        {
            
            case -4: return Color.white;
            case -3: return Color.grey;
            case -2: return Color.magenta;
            case -1: return Color.cyan;
            case 0: return Color.red;
            case 1: return Color.green;
            case 2: return Color.blue;
            case 3: return Color.yellow;
            default: return Color.black; // For undefined groups
        }
    }

    [BurstCompile]
    private struct ProcessLidarDataJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> data;
        public int degreeInterval;
        public NativeQueue<GroupedData>.ParallelWriter groupedData;
        public float3 forward;
        public float3 up;
        public float3 position;

        public void Execute(int index)
        {
            float3 point = data[index] - position; // Reference point relative to the vehicle

            // Generate the forward vector in the XZ plane
            float3 forwardXZ = new float3(forward.x, 0, forward.z);

            // Generate the vector towards the given point in the XZ plane
            float3 pointXZ = new float3(point.x, 0, point.z);

            // Calculate the angle between the forward vector and the vector towards the point
            float angle = Vector3.Angle(forwardXZ, pointXZ);

            // Determine the sign of the angle based on the cross product
            float sign = math.sign(math.cross(forwardXZ, pointXZ).y);
            angle *= sign;

            // Normalize the angle to be between 0 and 360 degrees
            //if (angle < 0) angle += 360;

            // Calculate group ID
            int groupID = Mathf.FloorToInt(angle / degreeInterval);

            groupedData.Enqueue(new GroupedData { groupID = groupID, point = data[index] });
        }
    }




    private struct GroupedData
    {
        public int groupID;
        public float3 point;
    }
}

public struct MeasurementStats
{
    public float3 mean;
    public float3 max;
    public float3 min;
}
