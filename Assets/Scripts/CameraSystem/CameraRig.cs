using UnityEngine;

public class CameraRig : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 angleOffset = new Vector3(0f, 14f, -10f);
    [Header("minDistance")]
    [SerializeField] private float cameraDistance = 8f;
    [SerializeField] private float smoothTime = 0.25f;

    private Vector3 _velocity;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + angleOffset.normalized * cameraDistance + Vector3.up * angleOffset.y;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _velocity, smoothTime);
        transform.LookAt(target.position);
    }
}
