using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
public class MLAgent : Agent
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
    
    float pitch;
    float yaw;
    float roll;
    float flap;
    bool slowDown = false;

    float thrustPercent;
    bool brake = false;

    AircraftPhysics aircraftPhysics;
    Rotator propeller;

    public override void OnEpisodeBegin()
    {
        float randomX = Random.Range(-10f, 10f);
        float randomZ = Random.Range(-10f, 10f);
        transform.position = new Vector3(randomX, 282, randomZ);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);
        sensor.AddObservation(transform.rotation);

        sensor.AddObservation(aircraftPhysics.velocity);
        sensor.AddObservation(aircraftPhysics.angularVelocity);

        sensor.AddObservation(pitch);
        sensor.AddObservation(roll);
        sensor.AddObservation(yaw);

        sensor.AddObservation(slowDown);
        sensor.AddObservation(flap);
        sensor.AddObservation(brake);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        pitch = pitchControlSensitivity * actionBuffers.ContinuousActions[0];
        roll = rollControlSensitivity * actionBuffers.ContinuousActions[1];
        yaw = yawControlSensitivity * actionBuffers.ContinuousActions[2];

        slowDown = actionBuffers.ContinuousActions[3] > 0;
        flap = actionBuffers.ContinuousActions[4];
        brake = actionBuffers.ContinuousActions[5] > 0;

        AddReward(+1f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
        continuousActionsOut[2] = Input.GetAxis("Yaw");

        continuousActionsOut[3] = Input.GetKey(KeyCode.LeftShift) ? 1f : 0f;
        continuousActionsOut[4] = Input.GetKey(KeyCode.LeftControl) ? 1f : 0f;
        continuousActionsOut[5] = Input.GetKey(KeyCode.B) ? 1f : 0f;

    }

    private void OnCollisionEnter(Collision other) 
    {
        if (other.gameObject.CompareTag("Surface"))
        {
            AddReward(-100f);
            EndEpisode();

            Debug.Log("Crashed, episode ended");
            Debug.Log("Reward: " + GetCumulativeReward());
        }
    }

    private void Start()
    {
        aircraftPhysics = GetComponent<AircraftPhysics>();
        propeller = FindObjectOfType<Rotator>();
        SetThrust(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(0);
        }

        if (Input.GetKey(KeyCode.Space))
        {
            SetThrust(thrustPercent + thrustControlSensitivity);
        }
        propeller.speed = thrustPercent * 1500f;

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            thrustControlSensitivity *= -1;
            flapControlSensitivity *= -1;
            if (thrustControlSensitivity < 0)
            {
                Debug.Log("Inverted");
            }
            else
            {
                Debug.Log("Normal");
            }
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            brake = !brake;

            if (brake)
            {
                Debug.Log("Brake is on");
            }
            else
            {
                Debug.Log("Brake is off");
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            flap += flapControlSensitivity;
            //clamp
            flap = Mathf.Clamp(flap, 0f, Mathf.Deg2Rad * 40);
            Debug.Log("Flap angle is " + flap * Mathf.Rad2Deg + " degrees");
        }

        pitch = pitchControlSensitivity * Input.GetAxis("Vertical");
        roll = rollControlSensitivity * Input.GetAxis("Horizontal");
        yaw = yawControlSensitivity * Input.GetAxis("Yaw");
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
}
