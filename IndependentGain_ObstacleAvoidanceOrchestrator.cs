using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndependentGain_ObstacleAvoidanceOrchestrator : MonoBehaviour
{
    public IndependentGain_NeuronArrayConcentrator leftMonitor;
    public IndependentGain_NeuronArrayConcentrator rightMonitor;

    public float gain1 = 1f; // Gain for Left 1 - Right 1 pair
    public float gain2 = 1f; // Gain for Left 2 - Right 2 pair
    public float gain3 = 1f; // Gain for Left 3 - Right 3 pair
    public float gain4 = 1f; // Gain for Left 4 - Right 4 pair

    public float steeringAngle = 0f;
    public float potentialDifference;

    // Update is called once per frame
    void Update()
    {
        // Assuming neurons are arranged in order in NeuronArray_Concentrator's Neurons array
        NeuronMonitor left1 = leftMonitor.Neurons[0];
        NeuronMonitor right1 = rightMonitor.Neurons[0];
        NeuronMonitor left2 = leftMonitor.Neurons[1];
        NeuronMonitor right2 = rightMonitor.Neurons[1];
        NeuronMonitor left3 = leftMonitor.Neurons[2];
        NeuronMonitor right3 = rightMonitor.Neurons[2];
        NeuronMonitor left4 = leftMonitor.Neurons[3];
        NeuronMonitor right4 = rightMonitor.Neurons[3];

        // Print neuron pairings
        //Debug.Log($"Pair 1: {left1.gameObject.name} (Left 1) with {right1.gameObject.name} (Right 1)");
        //Debug.Log($"Pair 2: {left2.gameObject.name} (Left 2) with {right2.gameObject.name} (Right 2)");
        //Debug.Log($"Pair 3: {left3.gameObject.name} (Left 3) with {right3.gameObject.name} (Right 3)");
        //Debug.Log($"Pair 4: {left4.gameObject.name} (Left 4) with {right4.gameObject.name} (Right 4)");

        // Apply gain to each left-right neuron pair
        float pair1Activity = (left1.spikeCount - right1.spikeCount) * gain1;
        float pair2Activity = (left2.spikeCount - right2.spikeCount) * gain2;
        float pair3Activity = (left3.spikeCount - right3.spikeCount) * gain3;
        float pair4Activity = (left4.spikeCount - right4.spikeCount) * gain4;

        // Log neuron spike counts for debugging
        //Debug.Log($"Spike counts - Left 1: {left1.spikeCount}, Right 1: {right1.spikeCount}");
        //Debug.Log($"Spike counts - Left 2: {left2.spikeCount}, Right 2: {right2.spikeCount}");
        //Debug.Log($"Spike counts - Left 3: {left3.spikeCount}, Right 3: {right3.spikeCount}");
        //Debug.Log($"Spike counts - Left 4: {left4.spikeCount}, Right 4: {right4.spikeCount}");

        // Sum the potential differences from all pairs
        potentialDifference = pair1Activity + pair2Activity + pair3Activity + pair4Activity;

        // Apply the total steering angle
        steeringAngle = potentialDifference;
    }
}
