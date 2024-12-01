using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HeadingErrorSensor : MonoBehaviour
{
    public TrajectoryManager trajectoryManager; // Reference to the TrajectoryManager
    public NeuronBase leftNeuron;
    public NeuronBase rightNeuron;

    public string Vehicle;
    public float error;
    public int lookahead;

    public float Imin = 0f;
    public float Imax = 1f;
    public float minAngle = 0f;
    public float maxAngle = 10f;
    public float eccentricity = 4.5f;

    private List<Vector3> trajectoryPoints;
    private int currentPointIndex = 0;
    private const float retryDelay = 1.0f; // Time to wait before retrying in seconds
    private const int maxRetries = 5; // Maximum number of retries

    private LineRenderer lineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        if (trajectoryManager == null)
        {
            Debug.LogError("No TrajectoryManager specified.");
            return;
        }

        // Initialize the LineRenderer
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogWarning("No LineRenderer found on the GameObject. A line will not be rendered.");
        }

        StartCoroutine(LoadTrajectoryPoints());
    }

    private IEnumerator LoadTrajectoryPoints()
    {
        int retries = 0;

        while (retries < maxRetries)
        {
            trajectoryPoints = trajectoryManager.RecordedPoints;

            if (trajectoryPoints != null && trajectoryPoints.Count > 0)
            {
                //Debug.Log(trajectoryPoints[1]); // Debug line, ensure you have at least two points
                yield break; // Exit the coroutine if points are successfully loaded
            }

            Debug.LogWarning("Trajectory points not available yet. Retrying...");
            retries++;
            yield return new WaitForSeconds(retryDelay);
        }

        Debug.LogError("Failed to load trajectory points after multiple attempts.");
    }

    // Update is called once per frame
    void Update()
    {
        if (trajectoryPoints == null || currentPointIndex >= trajectoryPoints.Count)
        {
            return;
        }

        // Ensure the index does not go beyond the array length
        int targetIndex = Mathf.Min(currentPointIndex + lookahead, trajectoryPoints.Count - 1);

        Vector3 targetPoint = trajectoryPoints[targetIndex];
        Vector3 nextPoint = trajectoryPoints[currentPointIndex];

        //Debug.Log($"Lookahead: {lookahead}, Point Index: {currentPointIndex}, Total: {currentPointIndex + lookahead}");
        Vector3 currentPosition = transform.position;
        Transform vehicleCenter = GameObject.Find(Vehicle).transform;

        // Update the LineRenderer positions
        if (lineRenderer != null)
        {
            // Update the LineRenderer positions
            lineRenderer.SetPosition(0, transform.parent.position); // Start at the parent's position
            lineRenderer.SetPosition(1, targetPoint); // End at the target point
        }

        Vector3 targetPointLocal = transform.InverseTransformPoint(targetPoint);
        Vector3 currentPositionLocal = transform.InverseTransformPoint(transform.position);

        Vector3 flatCurrentPosition = new Vector3(currentPositionLocal.x, 0, currentPositionLocal.z);
        Vector3 flatTargetPoint = new Vector3(targetPointLocal.x, 0, targetPointLocal.z);

        // Calculate the desired heading
        Vector3 directionToTarget = (flatTargetPoint - flatCurrentPosition).normalized;
        float desiredHeading = (Mathf.Atan2(directionToTarget.z, directionToTarget.x) * Mathf.Rad2Deg) - 90;

        // Calculate the current heading
        float currentHeading = transform.eulerAngles.z;

        // Compute the error
        error = Mathf.DeltaAngle(currentHeading, desiredHeading);
        //Debug.Log(error);
        //Debug.Log($"Distance: {Vector3.Distance(vehicleCenter, targetPoint)}");

        // Visualization
        Debug.DrawLine(currentPosition, targetPoint, Color.red); // Line to target point

        // Check if the object is close enough to the current target point
        if (Vector3.Distance(vehicleCenter.position, nextPoint) < 3.0f)
        {
            currentPointIndex++;
        }

        calculateCurrent(Mathf.Clamp(error, 0f, 90f) * -1, leftNeuron);
        calculateCurrent(Mathf.Clamp(error, -90f, 0f), rightNeuron);
        //Debug.Log($"Original Error: {error}, Left Error: {Mathf.Clamp(error, 0, 90)}, Right Error: {Mathf.Clamp(error, -90, 0)}");
    }

    void calculateCurrent(float error, NeuronBase neuron)
    {
        if (error >= maxAngle)
        {
            neuron.Input = Imin;
        }
        else if (error > minAngle)
        {
            float amplitude = Imax - Imin;
            float center = ((maxAngle - minAngle) / 2) + minAngle;
            float range = (maxAngle - minAngle) / eccentricity;
            float height = (amplitude / 2) + Imin;
            neuron.Input = (float)((amplitude / 2) * Math.Tanh((center - error) / range) + height);
        }
        else
        {
            neuron.Input = Imax;
        }
    }
}