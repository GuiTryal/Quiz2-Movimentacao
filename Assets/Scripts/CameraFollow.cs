using UnityEngine;

/// <summary>
/// Câmera em terceira pessoa que segue o jogador.
/// Segure o botão direito do mouse para girar a câmera.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 8f;
    [SerializeField] private float height = 4f;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float mouseSensitivity = 3f;

    private float yaw;
    private Vector3 currentVelocity;

    public void SetTarget(Transform newTarget) => target = newTarget;

    private void LateUpdate()
    {
        if (target == null) return;

        if (Input.GetMouseButton(1))
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;

        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3 desired = target.position + rotation * new Vector3(0f, height, -distance);

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref currentVelocity, smoothTime);
        transform.LookAt(target.position + Vector3.up * 1f);
    }
}
