using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AirplaneController : MonoBehaviour
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

    float thrustPercent;
    bool brake = false;

    AircraftPhysics aircraftPhysics;
    Rotator propeller;

    private void Start()
    {
        aircraftPhysics = GetComponent<AircraftPhysics>();
        propeller = FindObjectOfType<Rotator>();
        SetThrust(1500f);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(0);
        }

        // if (Input.GetKey(KeyCode.Space))
        // {
        //     SetThrust(thrustPercent + thrustControlSensitivity);
        // }
        propeller.speed = thrustPercent * 1500f;

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
            SceneManager.LoadScene(0);
        }
    }
}
