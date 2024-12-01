using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;

//Simulate Lidar OnDemand from the PointCloudAquisition Phase, takes the information
//of the point cloud and based on the information simulates a lidar using raycasts
//according to the recieved pointcloud data
public class Reaction_LidarController : MonoBehaviour
{
    //Max Distance to raycast (Static) ToDo. Calculate input distance and pass
    //it as parameter to avoid ray independent occlusion check
    public float maxRayDistance = 10f; 
    // Native arrays to store raycast commands and results
    NativeArray<RaycastCommand> commands;
    NativeArray<RaycastHit> results;
    JobHandle jobHandle;
    JobHandle jobHandle2;

    private NativeArray<bool> hasCollider; // Boolean array to store collider information

    private NativeArray<Vector3> outputData;
    public NativeArray<Vector3> visualData;

    public Vector3 parentWorldPosition = new Vector3(0.0f, 0.0f, 0.0f);

    public Quaternion parentWorldRotation;

    public PointCloudPublisher publisher;

    public Color lineColor = new Color(0.2f, 0.1f, 0.8f, 0.3f);

    //Initialize NativeArray on Start
    void Start()
    {
        commands = new NativeArray<RaycastCommand>(0, Allocator.Persistent);
        results = new NativeArray<RaycastHit>(0, Allocator.Persistent);
    }

    //Ensure Job Completion and then dispose of NativeArray memory on destroy
    void OnDestroy()
    {
        jobHandle.Complete();
        jobHandle2.Complete();
        commands.Dispose();
        results.Dispose();
        outputData.Dispose();
        visualData.Dispose();
    }

    // Schedule a job to perform raycasting based on point cloud data
    public void ScheduleJob(NativeArray<Vector3> pointCloudData, string frameID, bool isQcar)
    {
        //Debug.Log("Scheduled Raycast");
        if (pointCloudData.Length == 0)
        {
            Debug.LogWarning("Received empty point cloud data.");
            return;
        }

        parentWorldPosition = transform.position;
        parentWorldRotation = transform.rotation;
        commands = new NativeArray<RaycastCommand>(pointCloudData.Length, Allocator.Persistent);
        results = new NativeArray<RaycastHit>(pointCloudData.Length, Allocator.Persistent);


        // Define the raycast job
        var job = new RaycastJob2
        {
            commands = commands,
            results = results,
            maxRayDistance = maxRayDistance,
            pointCloudData = pointCloudData,
            parentPosition = parentWorldPosition // Pass the position of the GameObject
        };
        //Debug.Log("Defined job");
        // Schedule the job to run in parallel
        jobHandle = job.Schedule(pointCloudData.Length, 64);
        jobHandle.Complete();
        RaycastCommand.ScheduleBatch(commands, results, 16, jobHandle).Complete();

        outputData = new NativeArray<Vector3>(pointCloudData.Length, Allocator.Persistent);
        visualData = new NativeArray<Vector3>(pointCloudData.Length, Allocator.Persistent);
        hasCollider = new NativeArray<bool>(results.Length, Allocator.Persistent);

        for (int i = 0; i < results.Length; i++)
        {
            hasCollider[i] = results[i].collider != null;
        }




        var processJob = new ProcessRaycastResultsJob
        {
            hasCollider = hasCollider,
            pointCloudData = pointCloudData,
            results = results,
            outputData = outputData,
            visualData = visualData,
            parentPosition = parentWorldPosition,
            parentRotation = parentWorldRotation
        };

        JobHandle jobHandle2 = processJob.Schedule(pointCloudData.Length, 64); // Use a batch size of 64 for better performance
        jobHandle2.Complete();
        
        //Use Output Data 

        if (publisher != null)
        {
            publisher.PublishPointCloud(outputData, frameID, isQcar); //Schedule LidarController RaycastCommand NativeArray construction Job
            
        }

        //UnityEngine.Profiling.Profiler.EndSample();
    }
    // Process raycast results in the update loop ToDo. Merge PointCloud (Revise doing it in Async and render
    // the point cloud on demand
    void Update()
    {
        if (jobHandle.IsCompleted)
        {
            jobHandle2.Complete(); // Ensure job completion before accessing results

            for (int i = 0; i < visualData.Length; i++)
            {
                //Debug.DrawLine(parentWorldPosition, visualData[i], lineColor);

                if (results[i].collider != null)
                {
                    //Debug.Log(results[i].point);
                    //Debug.Log("Ray hit at distance: " + results[i].distance);
                }
            }
        }
    }

    // Define a job to construct raycasting commands in parallel based on point cloud data
    [BurstCompile]
    public struct RaycastJob2 : IJobParallelFor
    {
        public NativeArray<RaycastCommand> commands;
        public NativeArray<RaycastHit> results;
        public float maxRayDistance;
        public NativeArray<Vector3> pointCloudData;
        public Vector3 parentPosition;

        public void Execute(int index)
        {
            Vector3 direction = pointCloudData[index].normalized;
            float distance = pointCloudData[index].magnitude*10;
            if(float.IsNaN(direction.x)){
                //Debug.Log($"NAN:{pointCloudData[index]}");
            }
            RaycastCommand command = new RaycastCommand(parentPosition, direction, QueryParameters.Default, distance);
            commands[index] = command;
        }
    }

    [BurstCompile]
    public struct ProcessRaycastResultsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> pointCloudData;
        [ReadOnly] public NativeArray<RaycastHit> results;
        [ReadOnly] public NativeArray<bool> hasCollider;
        public NativeArray<Vector3> outputData;
        public NativeArray<Vector3> visualData;
        public Vector3 parentPosition;
        public Quaternion parentRotation;

        public void Execute(int index)
        {
            // Store the necessary data for processing on the main thread
            Quaternion adjustedRotation = new Quaternion(parentRotation.z, -parentRotation.y, parentRotation.x, parentRotation.w);

            //FIX THIS!!!!!!!!!!!!!!!!!
            Vector3 point = hasCollider[index] ? adjustedRotation * (results[index].point - parentPosition)  : (pointCloudData[index] * 10);
            outputData[index] = Quaternion.Inverse(parentRotation) * point;

            Vector3 visualPoint = hasCollider[index] ? (results[index].point) : (pointCloudData[index] * 10) + parentPosition;
            visualData[index] = visualPoint;
        }
    }
}