using UnityEngine;
using UnityEngine.UIElements;

public class WheelMeshSync : MonoBehaviour
{
    [SerializeField] private WheelCollider wheelCollider;
    [SerializeField] private Transform wheelMesh;

    void LateUpdate()
    {
        wheelCollider.GetWorldPose(out Vector3 position, out Quaternion rotation);

        wheelMesh.position = position;
        wheelMesh.rotation = rotation;
    }

}
