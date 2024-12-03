using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    public Camera mainCamera;

    void Start()
    {
        // Get the main camera in the scene
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (mainCamera != null)
        {
            // Make the text object face the camera
            transform.LookAt(mainCamera.transform);
            // Adjust rotation to compensate for the text's orientation
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
        }
    }
}
