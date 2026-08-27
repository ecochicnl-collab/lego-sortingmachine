using UnityEngine;

public class CarouselBakje : MonoBehaviour
{
    private LayerMask layerFilter;
    private carouselmovingsecond controller;
    private bool isSetup = false;

    public void Setup(LayerMask mask, carouselmovingsecond mainController)
    {
        layerFilter = mask;
        controller = mainController;
        isSetup = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isSetup || controller == null || controller.IsBusy) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        GameObject geraaktObject = rb.gameObject;

        if (((1 << geraaktObject.layer) & layerFilter) != 0)
        {
            if (geraaktObject.transform.parent != transform)
            {
                geraaktObject.transform.SetParent(transform);
                
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;

                controller.OntvangDraaiSeintje();
            }
        }
    }
}