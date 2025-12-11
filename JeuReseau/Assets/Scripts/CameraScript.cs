using System;
using DG.Tweening;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public float hauteur = 1;
    public Transform cible;
    
    [SerializeField] private float vitesseRotation = 2;
    [SerializeField] private float distance = 0;
    [SerializeField] private float yMaxAxis = 50;
    [SerializeField] private Camera camera;
    [SerializeField] private float tiltAngle = 10f;
    [SerializeField] private float tiltDuration = 0.15f;

    
    private float rotationX = 0;
    private float rotationY = 0;
    private float actualTilt;

    private void Awake()
    {
        camera = GetComponent<Camera>();
    }

    void Update()
    {
        rotationY += Input.GetAxis("Mouse X") * vitesseRotation;
        rotationX -= Input.GetAxis("Mouse Y") * vitesseRotation;

        rotationX = Mathf.Clamp(rotationX, -yMaxAxis, yMaxAxis);

        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0 + actualTilt);
        transform.rotation = rotation;

        Vector3 position = cible.position + rotation * new Vector3(0, 0f, -distance) + new Vector3(0, hauteur, 0);
        transform.position = position;

    }
    
    public void TiltCamera(int direction)
    {
        actualTilt = direction * tiltAngle;
    }



    public void ChangeFov(float newFov = 100, float transitionDuration = 0.5f)
    {
        DOTween.To(() => camera.fieldOfView, x => camera.fieldOfView = x, newFov, transitionDuration);
    }

}
