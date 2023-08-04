using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public float zoomSpeed = 5f;
    public float minZoomDistance = 5f;
    public float maxZoomDistance = 20f;

    public float dragSpeed = 2f;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if(Time.timeSinceLevelLoad > 2f)
        {
            // Camera Zoom
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            float zoomAmount = scrollInput * zoomSpeed * Time.deltaTime;
            mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize - zoomAmount, minZoomDistance, maxZoomDistance);

            // Camera Drag
            if (Input.GetMouseButton(0))
            {
                Vector3 dragDelta = new Vector3(-Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"), 0f) * dragSpeed * Time.deltaTime;
                transform.Translate(dragDelta);
            }
        }      
    }
}
