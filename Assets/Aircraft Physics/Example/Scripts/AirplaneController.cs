using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using TMPro;

public class AirplaneController : Agent
{
    [SerializeField]
    float rollControlSensitivity = 0.15f;
    [SerializeField]
    float pitchControlSensitivity = 0.2f;
    [SerializeField]
    float yawControlSensitivity = 0.6f;
    [SerializeField]
    float thrustControlSensitivity = 0.2f;
    [SerializeField]
    float flapControlSensitivity = 0.1745f;

    [SerializeField]
    GameObject target;

    float pitch;
    float yaw;
    float roll;
    float flap;

    float thrustPercent;
    bool brake;
    float speed;
    float angularSpeed;

    bool thrust;
    bool inverted;
    bool flaps;

    AircraftPhysics aircraftPhysics;
    Rotator propeller;
    Rigidbody rb;
    private Camera cam;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI velocityText;
    [SerializeField] TextMeshProUGUI angularVelocityText;
    [SerializeField] TextMeshProUGUI rewardText;
    [SerializeField] TextMeshProUGUI rollText;
    [SerializeField] TextMeshProUGUI yawText;
    [SerializeField] TextMeshProUGUI pitchText;
    [SerializeField] TextMeshProUGUI thrustText;
    [SerializeField] TextMeshProUGUI brakeText;
    [SerializeField] TextMeshProUGUI invertedText;
    [SerializeField] TextMeshProUGUI flapsText;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        SpawnRandomPosition();
        SetTargetPosition();
        aircraftPhysics = GetComponent<AircraftPhysics>();
        propeller = FindObjectOfType<Rotator>();
        SetThrust(1500f);
    }

    private void SetTargetPosition()
    {
        int randomX = Random.Range(-1200, 900);
        int randomY = Random.Range(100, 400);
        int randomZ = Random.Range(-300, 600);
        target.transform.position = new Vector3(randomX, randomY, randomZ);
    }

    private void SpawnRandomPosition()
    {
        int randomZ = Random.Range(-190, 190);

        if (randomZ > 0)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        transform.position = new Vector3(0, 2, randomZ);
    }

    private void Update()
    {
        // velocityText.text = "Velocity: " + speed;
        // angularVelocityText.text = "Angular Velocity: " + angularSpeed;
        rewardText.text = "Reward: " + GetCumulativeReward();
        // rollText.text = "Roll: " + roll;
        // yawText.text = "Yaw: " + yaw;
        // pitchText.text = "Pitch: " + pitch;
        // flapsText.text = "Flaps: " + flap;
        // thrustText.text = "Thrust: " + thrustPercent;
        // brakeText.text = "Brake: " + brake;
        // invertedText.text = "Inverted: " + inverted;

        propeller.speed = thrustPercent * 1500f;

        // if (Input.GetKeyDown(KeyCode.R))
        // {
        //     SceneManager.LoadScene(0);
        // }

        // if (Input.GetKey(KeyCode.Space))
        // {
        //     SetThrust(thrustPercent + thrustControlSensitivity);
        // }

        // if (Input.GetKeyDown(KeyCode.LeftShift))
        // {
        //     thrustControlSensitivity *= -1;
        //     flapControlSensitivity *= -1;
        //     if (thrustControlSensitivity < 0)
        //     {
        //         Debug.Log("Inverted");
        //     }
        //     else
        //     {
        //         Debug.Log("Normal");
        //     }
        // }

        // if (Input.GetKeyDown(KeyCode.B))
        // {
        //     brake = !brake;

        //     if (brake)
        //     {
        //         Debug.Log("Brake is on");
        //     }
        //     else
        //     {
        //         Debug.Log("Brake is off");
        //     }
        // }

        // if (Input.GetKeyDown(KeyCode.LeftControl))
        // {
        //     flap += flapControlSensitivity;
        //     //clamp
        //     flap = Mathf.Clamp(flap, 0f, Mathf.Deg2Rad * 40);
        //     Debug.Log("Flap angle is " + flap * Mathf.Rad2Deg + " degrees");
        // }

        // pitch = pitchControlSensitivity * Input.GetAxis("Vertical");
        // roll = rollControlSensitivity * Input.GetAxis("Horizontal");
        // yaw = yawControlSensitivity * Input.GetAxis("Yaw");
    }

    private void SetThrust(float percent)
    {
        thrustPercent = Mathf.Clamp01(percent);
    }

    private void FixedUpdate()
    {
        aircraftPhysics.SetControlSurfecesAngles(pitch, roll, yaw, flap);
        aircraftPhysics.SetThrustPercent(thrustPercent);
        aircraftPhysics.Brake(brake);
    }

    private void OnCollisionEnter(Collision other) {
        if (other.gameObject.CompareTag("Ground") || other.gameObject.CompareTag("Surface"))
        {
            EndEpisode();
            AddReward(-100f);
        }
    }

    public override void OnEpisodeBegin()
    {
        SpawnRandomPosition();
        SetTargetPosition();
        SetThrust(1500f);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position); // 3
        sensor.AddObservation(transform.rotation); // 3

        speed = rb.velocity.magnitude;
        sensor.AddObservation(speed); // 1

        angularSpeed = rb.angularVelocity.magnitude;
        sensor.AddObservation(angularSpeed); // 1
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float rollInput = actions.ContinuousActions[0]; // -1 to 1
        float yawInput = actions.ContinuousActions[1]; // -1 to 1
        float pitchInput = actions.ContinuousActions[2]; // -1 to 1

        thrust = actions.DiscreteActions[0] == 1; // 0 or 1
        inverted = actions.DiscreteActions[1] == 1; // 0 or 1
        brake = actions.DiscreteActions[2] == 1; // 0 or 1
        flaps = actions.DiscreteActions[3] == 1; // 0 or 1

        if (thrust)
        {
            SetThrust(thrustPercent + thrustControlSensitivity);
        }

        if (inverted)
        {
            thrustControlSensitivity *= -1;
            flapControlSensitivity *= -1;
        }

        if (flaps)
        {
            flap += flapControlSensitivity;
            //clamp
            flap = Mathf.Clamp(flap, 0f, Mathf.Deg2Rad * 40);
        }

        roll = rollControlSensitivity * rollInput;
        yaw = yawControlSensitivity * yawInput;
        pitch = pitchControlSensitivity * pitchInput;

        float reward = CalculateReward();
        AddReward(reward);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        float rollAction = Input.GetAxis("Horizontal");
        float yawAction = Input.GetAxis("Yaw");
        float pitchAction = Input.GetAxis("Vertical");

        bool thrustAction = Input.GetKeyDown(KeyCode.Space);
        bool invertedAction = Input.GetKeyDown(KeyCode.LeftShift);
        bool brakeAction = Input.GetKeyDown(KeyCode.B);
        bool flapAction = Input.GetKeyDown(KeyCode.LeftControl);

        continuousActions[0] = rollAction;
        continuousActions[1] = yawAction;
        continuousActions[2] = pitchAction;

        discreteActions[0] = thrustAction ? 1 : 0;
        discreteActions[1] = invertedAction ? 1 : 0;
        discreteActions[2] = brakeAction ? 1 : 0;
        discreteActions[3] = flapAction ? 1 : 0;
    }

    public float CalculateReward()
    {
        float reward = 0f;

        // calculate the euclidian distance between the position of the target and the position of the agent
        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        reward = 1/(distanceToTarget + 0.1f);
        return reward;
    }

    private void OnTriggerEnter(Collider other) 
    {
        if (other.gameObject.CompareTag("Target"))
        {
            AddReward(1000f);
            EndEpisode();
        }
    }
}
