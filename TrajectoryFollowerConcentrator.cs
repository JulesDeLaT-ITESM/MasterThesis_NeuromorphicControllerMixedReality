using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrajectoryFollowerConcentrator : MonoBehaviour
{
    // Start is called before the first frame update
    public HeadingErrorSensor leftSensor;
    public HeadingErrorSensor rightSensor;

    public NeuronMonitor innerLeft;
    public NeuronMonitor innerRight;
    public NeuronMonitor outerLeft;
    public NeuronMonitor outerRight;

    public float steeringGain;
    public float spikeActivity;

    private float error;
    private float innerSpikeError;
    private float outerSpikeError;
    public float steeringAngle;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        error = (rightSensor.error + leftSensor.error);

        //Debug.Log($"Error: {error}, Left Error: {leftSensor.error}, Right Error: {rightSensor.error}");
        //float spikeDifference = leftMonitor.spikeCount - rightMonitor.spikeCount;
        //Debug.Log($"Error: {spikeDifference}, Left Spikes: {leftMonitor.spikeCount}, Right Spikes: {rightMonitor.spikeCount}");
        spikeActivity = ((outerRight.spikeCount + innerLeft.spikeCount) - (outerLeft.spikeCount + innerRight.spikeCount));
        steeringAngle = steeringGain * ((outerRight.spikeCount+innerLeft.spikeCount)- (outerLeft.spikeCount + innerRight.spikeCount));
        //Debug.Log(steeringAngle);
        //Add 4 Neurons per follower, inner and outer neurons go paired
        //Informationmust be relayed at orchestrator level (ClampValues)
    }
}
