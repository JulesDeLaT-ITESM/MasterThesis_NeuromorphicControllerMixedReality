using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

public class TrajectoryProcessor : MonoBehaviour
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
    private StreamWriter writer;

    void Start()
    {
        if (objectToTrack != null)
        {
            previousPosition = objectToTrack.position;
            previousHeading = objectToTrack.eulerAngles.y;
            previousSpeed = 0f;

            // Initialize CSV file and write headers
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fullPath = Path.Combine(desktopPath, $"trajectory_data_{timestamp}.csv");
            writer = new StreamWriter(fullPath);
            writer.WriteLine("Time,PositionX,PositionY,PositionZ,Heading,Speed,CTE,ATE,HeadingError");
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


                Debug.Log($"CTE: {cte}, ATE: {ate}");

                float speed = CalculateSpeed(currentPosition, previousPosition, Time.deltaTime);


                // Draw debug lines
                Debug.DrawLine(currentPosition, projPoint, cteColor); // CTE line
                Debug.DrawLine(projPoint, nextPoint, ateColor); // ATE line

                if (writer != null)
                {
                    writer.WriteLine($"{Time.time},{currentPosition.x},{currentPosition.y},{currentPosition.z},{currentHeading},{speed},{cte},{ate},{headingError}");
                }

                previousPosition = currentPosition;
                previousHeading = currentHeading;
                previousSpeed = speed;
            }
        }
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
