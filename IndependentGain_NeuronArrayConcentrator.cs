using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndependentGain_NeuronArrayConcentrator : MonoBehaviour
{
    public NeuronMonitor[] Neurons;  // Make this public to access individual neurons
    public float totalSpikes;
    private float spikeAccumulator;

    void Start()
    {
        // Automatically get all NeuronMonitor components on the game object
        Neurons = GetComponentsInChildren<NeuronMonitor>();
    }

    // Update is called once per frame
    void Update()
    {
        // Reset the spike accumulator
        spikeAccumulator = 0;

        // Accumulate spikes from all neuron monitors
        foreach (NeuronMonitor monitor in Neurons)
        {
            spikeAccumulator += monitor.spikeCount;
        }

        // Update totalSpikes after accumulation
        totalSpikes = spikeAccumulator;
    }
}
