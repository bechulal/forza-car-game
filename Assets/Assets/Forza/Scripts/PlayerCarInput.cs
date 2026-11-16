using UnityEngine;

public class PlayerCarInput : CarInputBase
{
    private IA_Car control;

    [SerializeField] private float steerSpeed = 3f;

    void Awake()
    {
        control = new IA_Car();

    }

    void OnEnable()
    {
        control.Enable();
    }

    void OnDisable()
    {
        control.Disable();
    }

    void Update()
    {
        Throttle = control.Driving.Throttle.ReadValue<float>();
        float steerTarget = control.Driving.Steer.ReadValue<float>();
        Steer = Mathf.MoveTowards(Steer, steerTarget, steerSpeed * Time.deltaTime);
        Handbrake = control.Driving.Handbrake.IsPressed();

    }


}
