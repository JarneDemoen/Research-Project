#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using TMPro;
using System;

public class AirplaneController : Agent
{
    [SerializeField]
    bool isWatching = false;
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

    float pitch;
    float yaw;
    float roll;
    float flap;
    private int steps = 0;
    private int goalsAchieved = 0;
    float distanceToTarget;
    float previousDistanceToTarget = 0;
    float previousHeight = 0;
    float height = 0;

    float thrustPercent;
    bool brake;
    bool brakeInfo = false;
    float speed;
    float angularSpeed;

    bool crashed = false;
    bool collected = false;

    bool thrust;
    bool inverted;
    bool invertedInfo = false;
    bool flaps;

    Target target;
    GameObject targetObject;

    AircraftPhysics aircraftPhysics;
    Rotator propeller;
    Rigidbody rb;
    [SerializeField]

    #nullable enable
    Camera? cam;

    int episode = 0;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI? rewardText;
    [SerializeField] TextMeshProUGUI? episodeText;
    [SerializeField] TextMeshProUGUI? distanceText;
    [SerializeField] TextMeshProUGUI? stepsText;
    [SerializeField] TextMeshProUGUI? goalsText;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        aircraftPhysics = GetComponent<AircraftPhysics>();
        propeller = FindObjectOfType<Rotator>();
        target = FindObjectOfType<Target>();
    }

    public override void OnEpisodeBegin()
    {
        episode++;
        roll = 0;
        pitch = 0;
        yaw = 0;
        flap = 0;
        inverted = false;
        brake = false;
        flaps = false;
        invertedInfo = false;
        brakeInfo = false;
        steps = 0;
        goalsAchieved = 0;

        if (thrustControlSensitivity < 0)
        {
            thrustControlSensitivity *= -1;
            flapControlSensitivity *= -1;
        }
        
        SpawnRandomPosition();

        if (targetObject != null)
        {
            Destroy(targetObject);
        }

        targetObject = target.InstantiateTarget();
        if (isWatching)
        {
            // change color to blue
            targetObject.GetComponent<Renderer>().material.color = Color.blue;
        }
        SetThrust(1500f);
    }

    private void SpawnRandomPosition()
    {
        float randomZ = UnityEngine.Random.Range(-300, 320);

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (cam != null)
        {
            cam.transform.position = new Vector3(0, 6.9f, randomZ-15);
            cam.transform.rotation = Quaternion.Euler(11.504f, 0, 0);
        }
        
        transform.position = new Vector3(0, 2, randomZ);
        transform.rotation = Quaternion.Euler(0, 0, 0);

        if (randomZ > 0)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }

    private void SetThrust(float percent)
    {
        thrustPercent = Mathf.Clamp01(percent);
    }

    private IEnumerator SetCollected(bool value)
    {
        yield return new WaitForSecondsRealtime(1f);

        collected = value;
    }

    private void FinishEpisode()
    {
        crashed = true;
        roll = 0;
        pitch = 0;
        yaw = 0;
        flap = 0;
        inverted = false;
        brake = false;
        flaps = false;
        invertedInfo = false;
        brakeInfo = false;
        height = 0;
        previousHeight = 0;
        Debug.Log("Episode ended with " + GetCumulativeReward() + " reward");
        EndEpisode();
    }


    private void OnCollisionEnter(Collision other) {
        if (other.gameObject.CompareTag("Ground") || other.gameObject.CompareTag("Surface"))
        {
            if (crashed)
            {
                return;
            }
            AddReward((-1000f+(steps/10)));
            FinishEpisode();
        }

        if (other.gameObject.CompareTag("Runway"))
        {
            crashed = false;
            // if the angle of the plane is more than 30 degrees, it is considered a crash
            if (Vector3.Angle(transform.up, Vector3.up) > 30)
            {
                AddReward((-1000f+(steps/10)));
                FinishEpisode();
            }
        }
    }

    private void GoalAchieved()
    {
        Debug.Log("Goal achieved!!!!");
        AddReward(3000f);
        goalsAchieved++;
        Destroy(targetObject);
        targetObject = target.InstantiateTarget();
        collected = true;
        StartCoroutine(SetCollected(false));
        // FinishEpisode();
    }

    private void OnTriggerEnter(Collider other) 
    {
        if (other.gameObject == targetObject.gameObject && collected == false)
        {
            GoalAchieved();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //Observe aircraft velocity
        sensor.AddObservation(transform.InverseTransformDirection(GetComponent<Rigidbody>().velocity));
        //Where is the next checkpoint
        sensor.AddObservation(VectorToNextCheckpoint());
        //Orientation of the next checkpoint
        sensor.AddObservation(transform.InverseTransformDirection(targetObject.transform.forward));
        //Total Observation = 3+3+3 = 9
    }

    private Vector3 VectorToNextCheckpoint()
    {
        Vector3 nextCheckpointDir = targetObject.transform.position - transform.position;
        Vector3 localCheckpointDir = transform.InverseTransformDirection(nextCheckpointDir);
        return localCheckpointDir;
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

        steps++;

        if (thrust)
        {
            SetThrust(thrustPercent + thrustControlSensitivity);
        }

        if (inverted)
        {
            thrustControlSensitivity *= -1;
            flapControlSensitivity *= -1;
            invertedInfo = !invertedInfo;
        }

        if (flaps)
        {
            flap += flapControlSensitivity;
            //clamp
            flap = Mathf.Clamp(flap, 0f, Mathf.Deg2Rad * 40);
        }

        if (brake)
        {
            brakeInfo = !brakeInfo;
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

    private float CalculateReward()
    {
        float reward = 0f;
        distanceToTarget = Vector3.Distance(transform.position, targetObject.transform.position);

        if(distanceToTarget < previousDistanceToTarget)
        {
            reward += 300f/(distanceToTarget + 0.001f);
        }
        else if (distanceToTarget <= 250)
        {
            GoalAchieved();
        }
        else
        {
            reward -= distanceToTarget/10000f;
        }
        
        previousDistanceToTarget = distanceToTarget;

        return reward;
    }

    private void Update()
    {
        propeller.speed = thrustPercent * 1500f;

        if (rewardText != null && episodeText != null && distanceText != null && stepsText != null && goalsText != null)
        {
            rewardText.text = "Reward: " + GetCumulativeReward();
            episodeText.text = "Episode: " + episode;
            distanceText.text = "Distance to goal: " + distanceToTarget;
            stepsText.text = "Steps: " + steps;
            goalsText.text = "Goals achieved: " + goalsAchieved;
        }

        // if (steps > 5000)
        // {
        //     AddReward(-500f);
        //     FinishEpisode();
        // }

    }

    private void FixedUpdate()
    {
        aircraftPhysics.SetControlSurfecesAngles(pitch, roll, yaw, flap);
        aircraftPhysics.SetThrustPercent(thrustPercent);
        aircraftPhysics.Brake(brakeInfo);
    }
}
#endif