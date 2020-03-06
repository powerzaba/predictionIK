using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    public Transform targetToLookAt;
    public float distance = 5f;
    public float distanceMin = 3f;
    public float distanceMax = 10f;
    public float distanceSmooth = 0.05f;
    public float xMouseSensitivity = 5f;
    public float yMouseSensitivity = 5f;
    public float mouseWheelSensitivity = 5f;
    public float xSmooth = 0.05f;
    public float ySmooth = 0.1f;
    public float yMinLimit = -40f;
    public float yMaxLimit = 80f;

    private float mouseX = 0;
    private float mouseY = 0;
    private float velX = 0f;
    private float velY = 0f;
    private float velZ = 0f;
    private float velDistance = 0f;
    private float startDistance = 0f;
    private Vector3 position = Vector3.zero;
    private Vector3 desiredPosition = Vector3.zero;
    private float desiredDistance = 0f;

    // Update is called once per frame
    void LateUpdate()
    {
        HandlePlayerInput();

        CalculateDesiredPosition();

        UpdatePosition();

    }

    void HandlePlayerInput()
    {
        if (Input.GetMouseButton(1))
        {
            mouseX += Input.GetAxis("Mouse X") * xMouseSensitivity;
            mouseY -= Input.GetAxis("Mouse Y") * yMouseSensitivity;
        }
        desiredDistance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * mouseWheelSensitivity,
                distanceMin, distanceMax);
    }

    void CalculateDesiredPosition()
    {
        distance = Mathf.SmoothDamp(distance, desiredDistance, ref velDistance, distanceSmooth);

        desiredPosition = CalculatePosition(mouseY, mouseX, distance);
    }

    Vector3 CalculatePosition(float rotationX, float rotationY, float distance)
    {
        Vector3 direction = new Vector3(0, 0, -distance);
        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0);

        return targetToLookAt.position + rotation * direction;
    }

    void UpdatePosition()
    {
        var positionX = Mathf.SmoothDamp(position.x, desiredPosition.x, ref velX, xSmooth);
        var positionY = Mathf.SmoothDamp(position.y, desiredPosition.y, ref velY, ySmooth);
        var positionZ = Mathf.SmoothDamp(position.z, desiredPosition.z, ref velZ, xSmooth);

        position = new Vector3(positionX, positionY, positionZ);

        transform.position = position;
        transform.LookAt(targetToLookAt);
    }
}