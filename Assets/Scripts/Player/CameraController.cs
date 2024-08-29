using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{
    Vector3 rotation = Vector3.zero;
    const string xAxis = "Mouse X";
    const string yAxis = "Mouse Y";
    private ServiceLocator _serviceLocator;
    private bool isCameraInitialized;
    
    [Range(0.1f, 9f)][SerializeField] private float sensitivity = 2.0f;
    [Range(0f, 90f)][SerializeField] private float xRotationLimit = 88.0f;
    [Range(0f, 90f)][SerializeField] private float yRotationLimit = 88.0f;
    [Range(0f, 90f)][SerializeField] private float zRotationLimit = 0.01f;
    
    public float Sensitivity
    {
        get { return sensitivity; }
        set { sensitivity = value; }
    }




    void Start()
    {
        _serviceLocator = ServiceLocator.Global;
        _serviceLocator.Register<CameraController>(GetType(), this);
        Cursor.lockState = CursorLockMode.Confined;
    }

    public void InitCamera(Transform cameraPos)
    {
        transform.position = cameraPos.position;
        Cursor.lockState = CursorLockMode.Locked;
        isCameraInitialized = true;
    }
    
    void Update()
    {
        if (isCameraInitialized)
        {
            rotation.x += Input.GetAxis(xAxis) * sensitivity;
            rotation.x = Mathf.Clamp( rotation.x, -xRotationLimit, xRotationLimit);
            rotation.y += Input.GetAxis(yAxis) * sensitivity;
            rotation.y = Mathf.Clamp(rotation.y, -yRotationLimit, yRotationLimit);
            rotation.z = Mathf.Clamp(transform.localRotation.z,-zRotationLimit, zRotationLimit);
            Quaternion xQuat = Quaternion.AngleAxis(rotation.x, Vector3.up);
            Quaternion yQuat = Quaternion.AngleAxis(rotation.y, Vector3.left);
            Quaternion zQuat = Quaternion.AngleAxis(rotation.z, Vector3.back);

            transform.localRotation = xQuat * yQuat * zQuat;
        }
    }
}
