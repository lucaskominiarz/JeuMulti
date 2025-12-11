using UnityEngine;

public class SpectatorMode : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float lookSpeed = 3f;

    private float pitch = 0f;
    private float yaw = 0f;

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}