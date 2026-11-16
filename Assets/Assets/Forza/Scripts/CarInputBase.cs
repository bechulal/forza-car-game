using UnityEngine;

public abstract class CarInputBase : MonoBehaviour
{
    public float Throttle { get; protected set; }
    public float Steer { get; protected set; }
    public bool Handbrake { get; protected set; }
}
