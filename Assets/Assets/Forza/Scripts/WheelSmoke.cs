using UnityEngine;

public class WheelSmoke : MonoBehaviour
{
    [SerializeField] private WheelCollider wheelCollider;
    [SerializeField] private ParticleSystem smoke;

    [SerializeField] private float slipThreshold = 0.4f;
    [SerializeField] private float smokeAmount = 40f;

    void Update()
    {
        bool grounded = wheelCollider.GetGroundHit(out WheelHit hit);

        float slip = Mathf.Max(Mathf.Abs(hit.forwardSlip), Mathf.Abs(hit.sidewaysSlip));

        var emission = smoke.emission;

        if (grounded && slip > slipThreshold)
            emission.rateOverTime = smokeAmount;
        else
            emission.rateOverTime = 0f;
    }
}
