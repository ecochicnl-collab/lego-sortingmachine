using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VergrotenZoom : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Zoom Instellingen")]
    public float maximaleZoom = 1.5f;
    public float minimaleZoom = 1.0f;
    public float zoomStraal = 200f;
    public float zoomSnelheid = 12f;

    private RectTransform mijnRect;
    private Canvas parentCanvas;
    private bool aanwijzerBinnen;

    void Start()
    {
        mijnRect = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

    }

    void Update()
    {
        if (parentCanvas == null) return;

        // Alleen de aangeraakte cel schaalt; honderden cellen hoeven niet continu muisafstand te berekenen.
        float doelSchaalFactor = aanwijzerBinnen ? maximaleZoom : minimaleZoom;
        Vector3 nieuweSchaal = new Vector3(doelSchaalFactor, doelSchaalFactor, 1f);
        transform.localScale = Vector3.Lerp(transform.localScale, nieuweSchaal, Time.deltaTime * zoomSnelheid);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        aanwijzerBinnen = true;
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        aanwijzerBinnen = false;
    }

}