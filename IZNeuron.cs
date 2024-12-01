using UnityEngine;

public class IZNeuron : NeuronBase
{
    // Izhikevich model parameters
    public float a = 0.02f;
    public float b = 0.2f;
    public float c = -65f;
    public float d = 8f;

    // Variables for membrane potential and recovery variable
    public float v;
    public float u;
    public float inputCurrent;

    public override void Initialize()
    {
        // Initialize variables
        v = c;
        u = b * v;
    }

    public override void SimulateStep()
    {
        // Calculate the coupling term
        //Debug.Log($"Neuron: {gameObject.name} with current of {inputCurrent} mA");
        float couplingTerm = 0;
        if (coupledNeuron != null)
        {
            couplingTerm = couplingStrength * ((IZNeuron)coupledNeuron).v - v;
        }

        // Update equations for Izhikevich model
        float dv = 0.04f * v * v + 5 * v + 140 - u + inputCurrent + couplingTerm;
        float du = a * (b * v - u);

        v += dv * Time.fixedDeltaTime * simulationSpeed;
        u += du * Time.fixedDeltaTime * simulationSpeed;

        // Check for spike
        if (v >= 30)
        {
            v = c;
            u += d;
            //Debug.Log("Spike!");
        }

     
    }

    // Override the Output property to return the membrane potential v
    public override float Output => v;

    // Override the Input property to set the external input current
    public override float Input
    {
        set => inputCurrent = value;
        
    }
}