using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

public class StatisticsWithCollision : MonoBehaviour
{
    public Transform objectToTrack; // The object to track
    public TrajectoryManager trajectoryManager; // Reference to the TrajectoryManager
    public Color cteColor = Color.red; // Color for CTE line
    public Color ateColor = Color.green; // Color for ATE line
    public Color headingErrorColor = Color.blue; // Color for heading error line
    public string saveFilePath = "trajectory_data.csv"; // File path to save CSV

    private int currentSegmentIndex = 0;
    private Vector3 previousPosition;
    private float previousHeading;
    private float previousSpeed;
    private int collisionCount = 0; // Collision count
    private bool inCollision = false; // Flag to latch collision state
    private StreamWriter writer;

    // Obstacle-related variables
    public Transform obstacleParent; // Parent GameObject that holds all obstacles (e.g., "CilindricalObstaclesMat")
    private GameObject[] obstacles; // Array of obstacles to track
    private GameObject nearestObstacle;
    private float boundaryDistance = 0f;
    private float yawRate = 0f;

    // Vehicle bounding box dimensions (for illustration, adjust as per your vehicle dimensions)
    public float vehicleWidth = 2.0f;
    public float vehicleHeight = 1.0f;

    // Assuming the vehicle and obstacles have BoxCollider or Collider components
    public BoxCollider vehicleCollider;

    void Start()
    {
        if (objectToTrack != null)
        {
            previousPosition = objectToTrack.position;
            previousHeading = objectToTrack.eulerAngles.y;
            previousSpeed = 0f;

            // Fetch all obstacle GameObjects from the parent in the scene
            obstacles = new GameObject[obstacleParent.childCount];
            for (int i = 0; i < obstacleParent.childCount; i++)
            {
                obstacles[i] = obstacleParent.GetChild(i).gameObject;
            }

            // Append timestamp to the save file name
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileNameWithTimestamp = Path.GetFileNameWithoutExtension(saveFilePath) + "_" + timestamp + Path.GetExtension(saveFilePath);

            // Combine the desktop path and the new file name with the timestamp
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fullPath = Path.Combine(desktopPath, fileNameWithTimestamp);
            writer = new StreamWriter(fullPath);
            writer.WriteLine("Time,PositionX,PositionY,PositionZ,Heading,Speed,CTE,ATE,HeadingError,CollisionCount,YawRate,NearestObstacleID,BoundaryDistance,ObstaclePositionX,ObstaclePositionY,ObstaclePositionZ");

            // Print the file path to the console
            Debug.Log($"Data is being saved at: {fullPath}");
        }
    }

    void Update()
    {
        if (objectToTrack != null && trajectoryManager != null)
        {
            List<Vector3> recordedPoints = trajectoryManager.GetRecordedPoints();

            if (recordedPoints.Count > 1)
            {
                Vector3 currentPosition = objectToTrack.position;
                float currentHeading = objectToTrack.eulerAngles.y;
                (float cte, float ate, Vector3 projPoint, Vector3 nextPoint) = CalculateErrors(currentPosition, recordedPoints);
                float headingError = CalculateHeadingError(projPoint, nextPoint, currentHeading);
                float speed = CalculateSpeed(currentPosition, previousPosition, Time.deltaTime);

                // Calculate yaw rate based on heading change
                yawRate = (currentHeading - previousHeading) / Time.deltaTime;

                // Find nearest obstacle and calculate boundary distance
                FindNearestObstacle(currentPosition);
                if (nearestObstacle != null)
                {
                    // Calculate boundary-to-boundary distance and get closest points
                    boundaryDistance = CalculateBoundaryToBoundaryDistance(nearestObstacle);

                    Vector3 closestPointOnVehicle = vehicleCollider.ClosestPoint(nearestObstacle.transform.position);
                    Vector3 closestPointOnObstacle = nearestObstacle.GetComponent<MeshCollider>().ClosestPoint(closestPointOnVehicle);

                    // Draw debug line from the closest points (boundary-to-boundary)
                    Debug.DrawLine(closestPointOnVehicle, closestPointOnObstacle, Color.yellow);

                    // Log the boundary distance for debugging
                    //Debug.Log($"Calculated Boundary Distance: {boundaryDistance}");

                    // Check for collision (boundary distance <= 0)
                    if (boundaryDistance <= 0)
                    {
                        if (!inCollision)  // Collision latching logic
                        {
                            collisionCount++;
                            inCollision = true;
                            Debug.Log($"Collision detected with {nearestObstacle.name}, Collision Count: {collisionCount}");
                        }
                    }
                    else
                    {
                        inCollision = false; // Reset collision flag when not in collision
                    }
                }

                // Write the data to the CSV file
                if (writer != null)
                {
                    // Log the boundary distance right before writing to the CSV
                    //Debug.Log($"Writing Boundary Distance to CSV: {boundaryDistance}");

                    writer.WriteLine($"{Time.time},{currentPosition.x},{currentPosition.y},{currentPosition.z},{currentHeading},{speed},{cte},{ate},{headingError},{collisionCount},{yawRate},{nearestObstacle.name},{boundaryDistance},{nearestObstacle.transform.position.x},{nearestObstacle.transform.position.y},{nearestObstacle.transform.position.z}");
                }

                previousPosition = currentPosition;
                previousHeading = currentHeading;
                previousSpeed = speed;
            }
        }
    }


    void OnApplicationQuit()
    {
        if (writer != null)
        {
            writer.Close();
            Debug.Log("CSV file closed properly on application quit.");
        }
    }

    void OnDestroy()
    {
        if (writer != null)
        {
            writer.Close();
            Debug.Log("CSV file closed properly on script destruction.");
        }
    }

    void FindNearestObstacle(Vector3 currentPosition)
    {
        float minDistance = float.MaxValue;
        nearestObstacle = null;

        foreach (GameObject obstacle in obstacles)
        {
            float distanceToObstacle = Vector3.Distance(currentPosition, obstacle.transform.position);
            if (distanceToObstacle < minDistance)
            {
                minDistance = distanceToObstacle;
                nearestObstacle = obstacle;
            }
        }
    }

    float CalculateBoundaryToBoundaryDistance(GameObject obstacle)
    {
        if (vehicleCollider == null)
        {
            Debug.LogError("VehicleCollider is not assigned.");
            return float.MaxValue;  // Return a large value if the vehicle collider is not assigned
        }

        // Get the MeshCollider of the obstacle
        MeshCollider obstacleMeshCollider = obstacle.GetComponent<MeshCollider>();

        if (obstacleMeshCollider == null)
        {
            Debug.LogError($"Obstacle {obstacle.name} does not have a MeshCollider.");
            return float.MaxValue;  // Return a large value if no collider is found
        }

        // Get the closest point on the vehicle's BoxCollider to the obstacle
        Vector3 closestPointOnVehicle = vehicleCollider.ClosestPoint(obstacle.transform.position);

        // Get the closest point on the obstacle's MeshCollider to the vehicle
        Vector3 closestPointOnObstacle = obstacleMeshCollider.ClosestPoint(closestPointOnVehicle);

        // Calculate the distance between the closest points (boundary-to-boundary distance)
        float distanceBetweenBoundaries = Vector3.Distance(closestPointOnVehicle, closestPointOnObstacle);

        return distanceBetweenBoundaries;
    }

    // Calculate the closest point on the vehicle's bounding box to the obstacle
    Vector3 GetClosestPointOnBoundingBox(Vector3 vehiclePosition, Vector3 obstaclePosition)
    {
        // Vehicle bounds (assuming it's centered on vehiclePosition)
        float halfWidth = vehicleWidth / 2;
        float halfHeight = vehicleHeight / 2;

        // Calculate the closest point on the vehicle's bounding box to the obstacle
        Vector3 closestPoint = new Vector3(
            Mathf.Clamp(obstaclePosition.x, vehiclePosition.x - halfWidth, vehiclePosition.x + halfWidth),
            obstaclePosition.y, // Keep the Y-position the same as we assume 2D plane in this example
            Mathf.Clamp(obstaclePosition.z, vehiclePosition.z - halfHeight, vehiclePosition.z + halfHeight)
        );

        return closestPoint;
    }

    (float, float, Vector3, Vector3) CalculateErrors(Vector3 currentPosition, List<Vector3> trajectoryPoints)
    {
        float minDistance = float.MaxValue;
        Vector3 closestPoint = Vector3.zero;
        Vector3 alongTrackPoint = Vector3.zero;
        float alongTrackDistance = 0;

        for (int i = 0; i < trajectoryPoints.Count - 1; i++)
        {
            Vector3 segmentStart = trajectoryPoints[i];
            Vector3 segmentEnd = trajectoryPoints[i + 1];

            Vector3 projectedPoint = ProjectPointOnLineSegment(segmentStart, segmentEnd, currentPosition);
            float distance = Vector3.Distance(currentPosition, projectedPoint);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint = projectedPoint;
                alongTrackDistance = (segmentEnd - projectedPoint).magnitude;
                alongTrackPoint = segmentEnd;
            }
        }

        float crossTrackError = minDistance;
        float alongTrackError = alongTrackDistance;

        return (crossTrackError, alongTrackError, closestPoint, alongTrackPoint);
    }

    Vector3 ProjectPointOnLineSegment(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        Vector3 lineToPoint = point - lineStart;
        float t = Vector3.Dot(lineToPoint, lineDirection) / lineDirection.sqrMagnitude;
        t = Mathf.Clamp01(t);
        return lineStart + t * lineDirection;
    }

    float CalculateHeadingError(Vector3 segmentStart, Vector3 segmentEnd, float currentHeading)
    {
        Vector3 segmentDirection = segmentEnd - segmentStart;
        float segmentHeading = Mathf.Atan2(segmentDirection.z, segmentDirection.x) * Mathf.Rad2Deg;
        float headingError = Mathf.DeltaAngle(currentHeading, segmentHeading);
        return headingError;
    }

    float CalculateSpeed(Vector3 currentPosition, Vector3 previousPosition, float deltaTime)
    {
        return Vector3.Distance(currentPosition, previousPosition) / deltaTime;
    }
}