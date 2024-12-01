using System.Collections.Generic;
using UnityEngine;

public class NeuronMonitor : MonoBehaviour
{
    private NeuronBase neuron;  // Reference to the HRNeuron component
    private Queue<KeyValuePair<float, float>> voltageTimestamps;  // Stores pairs of (time, voltage)
    private Queue<float> spikeTimestamps;
    private bool isFirstEmaCalculation = true;
    private bool spikeFlag = true;

    public float measurementWindowDuration = 100f;  // Voltage measurement window duration in milliseconds
    public float spikeWindowDuration = 100f;  // Spike window duration in milliseconds

    public float spikeThreshold = 0.1f;
    public float spikeCount = 0;


    public float emaVoltage;
    public float emaAlpha = 0.1f;  // Smoothing factor for EMA, adjust as needed

    void Start()
    {
        neuron = GetComponent<NeuronBase>();
        voltageTimestamps = new Queue<KeyValuePair<float, float>>();
        spikeTimestamps = new Queue<float>();
    }

    void Update()
    {
        if (neuron != null)
        {
            // Spike detection and counting
            DetectSpike();

            // Measure average voltage in a moving window
            MeasureEMAVoltage();

            // Remove spikes that are outside the moving window
            UpdateSpikeCount();

        }
    }

    private void DetectSpike()
    {
        // Check if the voltage crosses the threshold (from below to above)
        if (neuron.Output >= spikeThreshold && spikeFlag)
        {
            spikeTimestamps.Enqueue(Time.time * 1000);  // Store timestamp in milliseconds
            spikeFlag = false;
            //Debug.Log("Spike detected! Total spike count in the last " + spikeWindowDuration + " milliseconds: " + spikeTimestamps.Count);
        }
        else if (neuron.Output < spikeThreshold && !spikeFlag)
        {
            spikeFlag = true;
        }
    }

    private void MeasureEMAVoltage()
    {
        if (isFirstEmaCalculation)
        {
            emaVoltage = neuron.Output;
            isFirstEmaCalculation = false;
        }
        else
        {
            emaVoltage = emaAlpha * neuron.Output + (1 - emaAlpha) * emaVoltage;
        }

        //Debug.Log("EMA voltage: " + emaVoltage);
    }

    private void UpdateSpikeCount()
    {
        float currentTime = Time.time * 1000;  // Current time in milliseconds
        while (spikeTimestamps.Count > 0 && currentTime - spikeTimestamps.Peek() > spikeWindowDuration)
        {
            spikeTimestamps.Dequeue();
        }
        spikeCount = spikeTimestamps.Count;
        //Debug.Log("Spikes in the last " + spikeWindowDuration + " milliseconds: " + spikeTimestamps.Count);
    }
}
