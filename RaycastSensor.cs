using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Mathematics;

public enum MeasurementType
{
    Raycast,
    Lidar
}

public class RaycastSensor : MonoBehaviour
{
    [SerializeField] private NeuronBase neuron;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float Imin = 0f;
    [SerializeField] private float Imax = 1f;
    [SerializeField] private float xmin = 0f;
    [SerializeField] private float xmax = 10f;
    [SerializeField] private float a1 = 0.95f;
    [SerializeField] private float b1 = 1.35f;
    [SerializeField] private float c1 = 0.85f;
    [SerializeField] private float eccentricity = 4.5f;
    [SerializeField] private MeasurementType measurementType = MeasurementType.Raycast;
    [SerializeField] private int lidarDegreeInterval = 10;
    [SerializeField] private int lidarID = 0;  // ID for specific lidar measurement

    public DistanceFromLidar distanceFromLidar;

    private void Start()
    {
        neuron = GetComponent<NeuronBase>();
        neuron.Input = 3.5f;

        

        if (distanceFromLidar != null)
        {
            distanceFromLidar.SetDegreeInterval(lidarDegreeInterval);
        }
        else if (measurementType == MeasurementType.Lidar)
        {
            distanceFromLidar = GetComponent<DistanceFromLidar>();
            Debug.LogError("DistanceFromLidar component not found.");
        }
    }

    void Update()
    {
        switch (measurementType)
        {
            case MeasurementType.Raycast:
                PerformRaycastMeasurement();
                break;
            case MeasurementType.Lidar:
                PerformLidarMeasurement();
                break;
        }
    }

    private void PerformRaycastMeasurement()
    {
        RaycastHit hit;
        Vector3 direction = transform.forward;
        Vector3 origin = transform.position;

        if (Physics.Raycast(origin, direction, out hit, xmax, obstacleLayer))
        {
            float distance = hit.distance;
            ProcessDistance(distance);
            Debug.DrawLine(origin, hit.point, GetLineColor(distance)); // Draw line to the hit point
        }
        else
        {
            float distance = float.MaxValue;
            ProcessDistance(distance);
            Debug.DrawLine(origin, origin + direction * xmax, Color.green); // Draw line to the max distance
        }
    }

    private void PerformLidarMeasurement()
    {
        if (distanceFromLidar != null)
        {
            
            var processedData = distanceFromLidar.GetProcessedData();
            //Debug.Log($"In {gameObject.name}, PD: {processedData}, CK: {processedData.ContainsKey(lidarID)}");
            if (processedData != null && processedData.ContainsKey(lidarID))
            {
                
                var stats = processedData[lidarID];
                float distance = math.length(stats.min);  // Using the magnitude of the mean vector as distance
                //Debug.Log($"ID: {lidarID}, Distance: {distance}");
                ProcessDistance(distance);
            }
            else
            {
                //Debug.Log($"ID: {lidarID} not in Dictionary");
                float distance = float.MaxValue;
                ProcessDistance(distance);
            }
        }
        else
        {
            Debug.LogError($"DistanceFromLidar component not found on GameObject: {gameObject.name}");

        }
    }

    private void ProcessDistance(float distance)
    {
        //Debug.Log($"Distance of {gameObject.name} is {distance}");
        if (distance >= xmax)
        {
            neuron.Input = a1 * Imin;
        }
        else if (distance > xmin)
        {
            //Debug.Log("In Range");
            float amplitude = Imax - Imin;
            float center = ((xmax - xmin) / 2) + xmin;
            float range = (xmax - xmin) / eccentricity;
            float height = (amplitude / 2) + Imin;
            neuron.Input = (float)(b1 * (amplitude / 2) * Math.Tanh((center - distance) / range) + height);
        }
        else
        {
            neuron.Input = c1 * Imax;
        }
    }

    private Color GetLineColor(float distance)
    {
        if (distance >= xmax)
        {
            return Color.blue;
        }
        else if (distance > xmin)
        {
            return Color.yellow;
        }
        else
        {
            return Color.red;
        }
    }
}
