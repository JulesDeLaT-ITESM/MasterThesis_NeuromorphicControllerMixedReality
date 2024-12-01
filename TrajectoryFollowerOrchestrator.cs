using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrajectoryFollowerOrchestrator : MonoBehaviour
{
    // Start is called before the first frame update
    private TrajectoryFollowerConcentrator[] Concentrators;
    public float steeringAngleAccumulator;
    public float steeringAngleTotal;
    public float neuronActivityAccumulator;
    public float neuronActivityTotal;

    void Start()
    {
        Concentrators = GetComponentsInChildren<TrajectoryFollowerConcentrator>();
    }

    // Update is called once per frame
    void Update()
    {

        // Reset the spike accumulator
        steeringAngleAccumulator = 0;
        neuronActivityAccumulator = 0;
        // Accumulate spikes from all neuron monitors
        foreach (TrajectoryFollowerConcentrator concentrator in Concentrators)
        {
            steeringAngleAccumulator += concentrator.steeringAngle;
            neuronActivityAccumulator += concentrator.spikeActivity;
        }

        // Update totalSpikes after accumulation
        steeringAngleTotal = steeringAngleAccumulator;
        neuronActivityTotal = neuronActivityAccumulator;
    }
}
