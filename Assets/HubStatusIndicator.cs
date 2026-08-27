using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HubStatusIndicator : MonoBehaviour
{
    [Header("HubIdentificatie")]
    public string hubNaam = "Hub 1 (Aanvoer)";
    public TMP_Text labelTekst;

    [Header("UI lagen")]
    public Image hubCore;
    public Image hubGlow;

    [Header("glow animatiefactoren")]
    public float minimaleGlow = 0.15f;
    public float maximaleGlow = 0.45f;
    public float pulseSnelheid = 2.5f;

    private Color actieveKleur;
    private bool isVerbonden = false;

    private readonly Color groen = new Color(0.3f, 1f, 0.4f, 1f);
    private readonly Color rood = new Color(1f, 0.2f, 0.2f, 1f);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (labelTekst != null)
        {
            labelTekst.text = hubNaam;
        }

        UpdateStatus("DISCONNECTED");
    }

    /// <summary>
    /// 
    /// </summary>
    
    public void UpdateStatus(string status)
    {
        isVerbonden = (status.ToUpper() == "CONNECTED");
        actieveKleur = isVerbonden ? groen : rood;

        if (hubCore != null)
        {
            hubCore.color = actieveKleur;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (hubGlow != null)
        {
            float alpha;
            if (isVerbonden)
            {
                float lerpValue = (Mathf.Sin(Time.time * pulseSnelheid) + 1f) / 2f;
                alpha = Mathf.Lerp(minimaleGlow, maximaleGlow, lerpValue);
            }
            else
            {
                float lerpValue = (Mathf.Sin(Time.time * pulseSnelheid * 2f) + 1f) / 2f;
                alpha = Mathf.Lerp(0.1f, 0.3f, lerpValue);
            }

            Color nieuweGloeiKleur = actieveKleur;
            nieuweGloeiKleur.a = alpha;
            hubGlow.color = nieuweGloeiKleur;

        }
    }
}
