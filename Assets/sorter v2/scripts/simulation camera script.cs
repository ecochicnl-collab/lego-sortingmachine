using UnityEngine;
using UnityEngine.InputSystem;

public class simulationcamerascript : MonoBehaviour
{

    float zoomspeed = 10f;
    Vector3 pivotPoint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                pivotPoint = hit.point;
            }
        }


        float scrollinput = Mouse.current.scroll.ReadValue().y;
        
        transform.position += transform.forward * scrollinput * zoomspeed * Time.deltaTime;

        if (Mouse.current.rightButton.isPressed)
        {
            float mouseX = Mouse.current.delta.x.ReadValue();
            float mouseY = Mouse.current.delta.y.ReadValue();

            transform.RotateAround(pivotPoint, Vector3.up, mouseX * 10f * Time.deltaTime);
            transform.RotateAround(pivotPoint, transform.right, -mouseY * 10f * Time.deltaTime);
        }
    }
}
