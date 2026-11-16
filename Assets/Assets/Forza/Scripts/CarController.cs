using System;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [SerializeField] private int motorTorque = 1500;
    [SerializeField] private int brakeTorque = 3000;
    [SerializeField] private float coastBrakeTorque = 200f;
    [SerializeField] private int maxSteerAngle = 30;
    [SerializeField] private Vector3 centreOfMass = new Vector3(0f, 0.25f, 0f);

    [SerializeField] private WheelCollider wheelColliderRRW;
    [SerializeField] private WheelCollider wheelColliderRFW;
    [SerializeField] private WheelCollider wheelColliderLRW;
    [SerializeField] private WheelCollider wheelColliderLFW;

    private Rigidbody rb;
    private CarInputBase carInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        carInput = GetComponent<CarInputBase>();

        rb.centerOfMass = centreOfMass;
    }

    void FixedUpdate()
    {


        HandleSteering();
        HandleThrottle();
        HandleHandbrake();
    }

    void Update()
    {
        Debug.Log("Speed : " + wheelColliderRRW.motorTorque);
    }

    private void HandleHandbrake()
    {
        if (carInput.Handbrake)
        {
            wheelColliderRFW.brakeTorque = 0f;
            wheelColliderLFW.brakeTorque = 0f;
            wheelColliderRRW.brakeTorque = brakeTorque;
            wheelColliderLRW.brakeTorque = brakeTorque;
        }
        else if (carInput.Throttle == 0f)
        {
            wheelColliderRFW.brakeTorque = coastBrakeTorque;
            wheelColliderLFW.brakeTorque = coastBrakeTorque;
            wheelColliderRRW.brakeTorque = coastBrakeTorque;
            wheelColliderLRW.brakeTorque = coastBrakeTorque;
        }
        else
        {
            wheelColliderRRW.brakeTorque = 0f;
            wheelColliderLRW.brakeTorque = 0f;
            wheelColliderRFW.brakeTorque = 0f;
            wheelColliderLFW.brakeTorque = 0f;
        }
    }

    private void HandleSteering()
    {

        float speed = rb.linearVelocity.magnitude;
        float reduction = 1 / (1 + speed / 20);
        float steerAngle = carInput.Steer * maxSteerAngle * reduction;
        wheelColliderRFW.steerAngle = steerAngle;
        wheelColliderLFW.steerAngle = steerAngle;
    }

    private void HandleThrottle()
    {
        float throttle = carInput.Throttle * motorTorque;
        wheelColliderRRW.motorTorque = throttle;
        wheelColliderLRW.motorTorque = throttle;
    }
}
