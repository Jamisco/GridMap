using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public float zoomSpeed = 10;
    public float minZoomDistance = 5f;
    public float maxZoomDistance = 50f;

    public float dragSpeed = 20f;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private Vector3 dragOrigin;
    private Vector3 originalPosition;
    bool go = false;
    private void Update()
    {
        if(Time.timeSinceLevelLoad > 1f)
        {
            // Camera Zoom
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            float zoomAmount = scrollInput * zoomSpeed * Time.deltaTime;
            mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize - zoomAmount, minZoomDistance, maxZoomDistance);

            // Camera Drag

            
            if (Input.GetMouseButtonDown(0))
            {
                dragOrigin = Input.mousePosition;
                originalPosition = transform.position;
                go = true;
            }

            if (Input.GetMouseButton(0) && go)
            {
                Vector3 offset = Camera.main.ScreenToWorldPoint(dragOrigin) - Camera.main.ScreenToWorldPoint(Input.mousePosition);
                transform.position = originalPosition + offset;
            }

           if(Input.GetMouseButton(0) == false)
            {
                go = false;
            }
        }      
    }
}
