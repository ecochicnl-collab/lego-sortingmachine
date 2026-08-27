using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SimCameraWisselaar : MonoBehaviour
{
    [Header("Camera's om tussen te wisselen")]
    public Camera[] cameras;
    public int huidigeIndex = 0;

    [Header("Status teksten (optioneel)")]
    public TextMeshProUGUI statusTekst;
    public TextMeshProUGUI volgStatusTekst;

    [Header("Volg modus")]
    public bool volgModus = false;

    private Transform volgDoel;
    private Vector3 volgOffset;
    private float zoekTimer = 0f;

    void Start()
    {
        if (cameras == null || cameras.Length == 0)
        {
            Debug.LogWarning("[SimCameraWisselaar] Geen camera's ingesteld.");
            return;
        }

        // Zet alleen de huidige camera aan, alle andere uit.
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] == null) continue;
            SetCameraActief(cameras[i], i == huidigeIndex);
        }
        ToonStatus();
    }

    void Update()
    {
        if (Keyboard.current != null)
        {
            // Druk op C om naar de volgende camera te wisselen.
            if (Keyboard.current.cKey.wasPressedThisFrame) VolgendeCamera();
            // Druk op F om de volg-modus aan/uit te zetten.
            if (Keyboard.current.fKey.wasPressedThisFrame) ToggleVolgModus();
        }

        if (volgModus) UpdateVolgen();
    }

    public void VolgendeCamera()
    {
        if (cameras == null || cameras.Length == 0) return;
        int volgende = (huidigeIndex + 1) % cameras.Length;
        ActiverCamera(volgende);
    }

    public void VorigeCamera()
    {
        if (cameras == null || cameras.Length == 0) return;
        int vorige = (huidigeIndex - 1 + cameras.Length) % cameras.Length;
        ActiverCamera(vorige);
    }

    // Zet de volg-modus aan/uit: de actieve camera volgt dan het vallende blokje.
    public void ToggleVolgModus()
    {
        volgModus = !volgModus;

        Camera cam = HuidigeCamera();
        if (cam != null)
        {
            // Tijdens het volgen mag de handmatige draai/zoom-besturing niet meevechten.
            SetBesturingActief(cam, !volgModus);
        }

        if (volgModus)
        {
            volgDoel = ZoekVallendBlokje();
            if (volgDoel != null && cam != null)
            {
                volgOffset = cam.transform.position - volgDoel.position;
            }
        }
        else
        {
            volgDoel = null;
        }

        ToonStatus();
    }

    private void ActiverCamera(int index)
    {
        if (cameras == null || index < 0 || index >= cameras.Length) return;

        if (cameras[huidigeIndex] != null)
        {
            SetCameraActief(cameras[huidigeIndex], false);
        }

        huidigeIndex = index;

        if (cameras[huidigeIndex] != null)
        {
            SetCameraActief(cameras[huidigeIndex], true);
            // Als de volg-modus aan staat, blijft de besturing uit op de nieuwe camera.
            if (volgModus) SetBesturingActief(cameras[huidigeIndex], false);
        }

        ToonStatus();
    }

    private Camera HuidigeCamera()
    {
        if (cameras == null || huidigeIndex < 0 || huidigeIndex >= cameras.Length) return null;
        return cameras[huidigeIndex];
    }

    private void UpdateVolgen()
    {
        Camera cam = HuidigeCamera();
        if (cam == null) return;

        // Blijf het doel alleen volgen zolang het echt nog valt.
        if (volgDoel != null)
        {
            Rigidbody rb = volgDoel.GetComponent<Rigidbody>();
            if (rb == null || rb.isKinematic || rb.linearVelocity.y >= -0.1f)
            {
                volgDoel = null;
            }
        }

        // Zoek niet elke frame, maar af en toe naar een nieuw vallend blokje.
        if (volgDoel == null)
        {
            zoekTimer -= Time.deltaTime;
            if (zoekTimer <= 0f)
            {
                zoekTimer = 0.3f;
                volgDoel = ZoekVallendBlokje();
                if (volgDoel != null)
                {
                    volgOffset = cam.transform.position - volgDoel.position;
                }
            }
        }

        if (volgDoel == null) return;

        Transform camT = cam.transform;
        camT.position = Vector3.Lerp(camT.position, volgDoel.position + volgOffset, Time.deltaTime * 6f);
        camT.LookAt(volgDoel.position);
    }

    // Zoek het blokje dat nu aan het vallen is: een Rigidbody die naar beneden beweegt,
    // en pak het hoogste (dus het nieuwste gespawnde) blokje.
    private Transform ZoekVallendBlokje()
    {
        Transform best = null;
        float hoogsteY = float.MinValue;

        foreach (Rigidbody rb in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
        {
            if (rb == null || rb.isKinematic) continue;
            if (rb.linearVelocity.y < -0.1f && rb.transform.position.y > hoogsteY)
            {
                hoogsteY = rb.transform.position.y;
                best = rb.transform;
            }
        }
        return best;
    }

    // Alleen de Camera, AudioListener en het draai/zoom-script aan/uit.
    // Het GameObject zelf blijft actief, zodat snapshot-camera's blijven werken.
    private void SetCameraActief(Camera cam, bool actief)
    {
        cam.enabled = actief;

        AudioListener listener = cam.GetComponent<AudioListener>();
        if (listener != null) listener.enabled = actief;

        SetBesturingActief(cam, actief);
    }

    private void SetBesturingActief(Camera cam, bool actief)
    {
        simulationcamerascript besturing = cam.GetComponent<simulationcamerascript>();
        if (besturing != null) besturing.enabled = actief;
    }

    private void ToonStatus()
    {
        Camera cam = HuidigeCamera();
        if (statusTekst != null && cam != null)
        {
            statusTekst.text = "camera: " + cam.gameObject.name;
        }
        if (volgStatusTekst != null)
        {
            volgStatusTekst.text = volgModus ? "volg: aan" : "volg: uit";
        }
    }
}
