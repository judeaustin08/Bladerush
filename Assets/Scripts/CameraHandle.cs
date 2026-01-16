using UnityEngine;

public class CameraHandle : MonoBehaviour
{
    void Update()
    {
        // Apply transform to main camera
        Camera cam = Camera.main;
        cam.transform.position = Vector3.Lerp(cam.transform.position, transform.position, GameManager.InterpolationConstant);
        cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, transform.rotation, GameManager.InterpolationConstant);
    }
}
