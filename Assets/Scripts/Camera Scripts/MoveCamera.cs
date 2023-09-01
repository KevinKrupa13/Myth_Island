using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MoveCamera : NetworkBehaviour
{

    public Transform cameraPosition;

    public float mouseSensitivity = 100f;
 
    float xRotation = 0f;
    float YRotation = 0f;
    Camera cam;
    AudioListener aud;
 
    void Start()
    {
      cam = GetComponent<Camera>();
      aud = GetComponent<AudioListener>();
      Cursor.lockState = CursorLockMode.Locked;
    }
 
    void Update()
    {
        if (!IsOwner) return;
        cam.enabled = true;
        aud.enabled = true;
        transform.position = cameraPosition.position;
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
    
        //control rotation around x axis (Look up and down)
        xRotation -= mouseY;
    
        //we clamp the rotation so we cant Over-rotate (like in real life)
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
    
        //control rotation around y axis (Look up and down)
        YRotation += mouseX;
    
        //applying both rotations
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        //transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
 
    }
}
