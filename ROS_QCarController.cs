using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class RosQCarController : MonoBehaviour
{
    // Reference to the front wheels for steering
    [SerializeField] private Transform frontWheelLeft;
    [SerializeField] private Transform frontWheelRight;

    //Reference to the neuron orchestrators for spike and angle acquisition
    [SerializeField] private IndependentGain_ObstacleAvoidanceOrchestrator avoidanceOrchestrator;   //Reference
    [SerializeField] private TrajectoryFollowerOrchestrator followerOrchestrator;

    //Reference to the Filter Manager for angle and speed filtering
    [SerializeField] private FilterManager filterManager;

    [SerializeField] private float maxSteeringAngle = 30f;  // Maximum steering angle
    [SerializeField] private float acceleration = 2f;  // Acceleration speed
    [SerializeField] private float deceleration = 2f;  // Deceleration speed

    //Working Variables
    [Range(0.0f, 1.0f)] [SerializeField] private float baseVelocity;    //Max speed expected for the vehicle under no avoidance and on straight paths
    [Range(0.0f, 0.2f)] [SerializeField] private float avoidanceStrength;   //Influence of neuronal activity of the avoidance component on the velocity per spike
    [Range(0.0f, 0.2f)] [SerializeField] private float followerStrength;    //influence of neuronal activity of the following component on the velocity per spike
    [Range(-0.2f, 0.2f)] [SerializeField] private float maxSpeedROS = 2f;  
    [Range(-0.2f, 0.2f)] [SerializeField] private float minSpeedROS = 2f;  
    [Range(1, 20)] [SerializeField] private int updateRate = 8; //update rate to ROS (in Hz)

    private float currentSpeed = 0f;
    private float currentSteeringAngle;
    private bool angleControlEnabled = true;

    private ROSConnection ros;

    private float filteredSteeringAngle;
    private float filteredSpeed;

    void Start()
    {
        // Initialize ROS connection
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<Float32Msg>("steering_angle");
        ros.RegisterPublisher<Float32Msg>("vehicle_speed");

        // Ensure FilterManager is assigned
        if (filterManager == null)
        {
            Debug.LogError("FilterManager is not assigned.");
        }

        // Start publishing at the specified rate
        float publishInterval = 1f / updateRate;
        InvokeRepeating(nameof(PublishRosData), 0, publishInterval);
    }

    void Update()
    {
        UpdateState();

        // Apply filters if filterManager is available
        if (filterManager != null)
        {
            filteredSteeringAngle = filterManager.ApplySteeringAngleFilter(currentSteeringAngle);
            //Debug.Log(filteredSteeringAngle);
            filteredSpeed = filterManager.ApplyVelocityFilter(currentSpeed);
        }
        else if (filterManager == null)
        {
            filteredSteeringAngle = currentSteeringAngle;
            filteredSpeed = currentSpeed;
        }
        if (angleControlEnabled)
        { 
            filteredSteeringAngle = 0.0f;
            filteredSpeed = 0.0f;
        }
        

        // Update the front wheel rotation for visual representation
        UpdateWheelRotation(frontWheelLeft, currentSteeringAngle);
        UpdateWheelRotation(frontWheelRight, currentSteeringAngle);

        // Toggle angle control with space bar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            angleControlEnabled = !angleControlEnabled;
        }
    }

    private void UpdateState()
    {
        if (angleControlEnabled)
        {
            // Manual control
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");

            currentSteeringAngle = horizontalInput * maxSteeringAngle;

            if (verticalInput != 0)
            {
                currentSpeed += verticalInput * (verticalInput > 0 ? acceleration : deceleration) * Time.deltaTime;
                currentSpeed = Mathf.Clamp(currentSpeed, 0, 0.06f);
            }
        }
        else
        {
            // Autonomous control
            currentSteeringAngle = avoidanceOrchestrator.steeringAngle + followerOrchestrator.steeringAngleTotal;
            
            currentSpeed = Mathf.Clamp(baseVelocity - Mathf.Abs(avoidanceOrchestrator.potentialDifference) * avoidanceStrength - Mathf.Abs(followerOrchestrator.neuronActivityTotal) * followerStrength, minSpeedROS, maxSpeedROS);
        }
        //Debug.Log(angleControlEnabled);
    }

    private void PublishRosData()
    {
        Float32Msg steeringAngleMsg = new Float32Msg(Mathf.Clamp(filteredSteeringAngle, -maxSteeringAngle, maxSteeringAngle));
        Float32Msg speedMsg = new Float32Msg(filteredSpeed);

        ros.Publish("steering_angle", steeringAngleMsg);
        ros.Publish("vehicle_speed", speedMsg);
    }

    private void UpdateWheelRotation(Transform wheel, float angle)
    {
        if (wheel != null)
        {
            wheel.localRotation = Quaternion.Euler(0, angle, 0);
        }
    }

    void OnDestroy()
    {
        // Ensure to cancel the repeating invoke when the object is destroyed to avoid any potential issues
        CancelInvoke(nameof(PublishRosData));
    }
}