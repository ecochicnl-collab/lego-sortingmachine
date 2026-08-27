using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TelemetryPanelManager : MonoBehaviour
{
    [Header("--- live blokje info ---")]
    public TMP_Text blokNaamTekst;
    public TMP_Text kleurTekst;
    public TMP_Text bakjeTekst;

    [Header("--- statistieken ---")]
    public TMP_Text perUurTekst;
    public TMP_Text totaalTellerTekst;

    [Header("--- instellingen ---")]
    public bool kleurTekstAanpassen = true;

    private string nieuwsteJson = "";
    private bool heeftNieuweData = false;

    private void Update()
    {
        if (heeftNieuweData)
        {
            heeftNieuweData = false;
            VerwerkJsonData(nieuwsteJson);
        }
    }

    public void UpdateVanuitJson(string jsonTekst)
    {
        nieuwsteJson = jsonTekst;
        heeftNieuweData = true;
    }

    private void VerwerkJsonData(string jsonTekst)
    {
        try
        {
            TelemetryData data = JsonUtility.FromJson<TelemetryData>(jsonTekst);

            if (data != null && data.type == "machine_update")
            {
                UpdateBlokjeInfo(data.huidig_blokje, data.huidige_kleur, data.naar_bakje);
                UpdateStatistieken(data.blokjes_per_uur, data.totaal_gesorteerd);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[TelemetryPanel] Fout bij verwerken JSON: " + e.Message);
        }
    }

    public void UpdateBlokjeInfo(string naam, string kleur, int bakje, Sprite blokIcoon = null)
    {
        if (blokNaamTekst != null)
            blokNaamTekst.text = string.IsNullOrEmpty(naam) ? "geen detectie" : naam;

        if (bakjeTekst != null)
            bakjeTekst.text = "geleid naar: Bakje " + bakje;

        if (kleurTekst != null)
        {
            kleurTekst.text = "kleur: " + kleur;

            if (kleurTekstAanpassen)
            {
                kleurTekst.color = BepaalKleur(kleur);
            }
        }
    }

    public void UpdateStatistieken(int perUur, int totaal)
    {
        if (perUurTekst != null)
            perUurTekst.text = perUur.ToString("N0");

        if (totaalTellerTekst != null)
            totaalTellerTekst.text = totaal.ToString("N0");
    }

    private Color BepaalKleur(string kleurNaam)
    {
        if (string.IsNullOrEmpty(kleurNaam)) return Color.white;

        switch (kleurNaam.ToLower())
        {
            case "rood":
            case "red":
                return new Color(1f, 0.35f, 0.35f);
            case "blauw":
            case "blue":
                return new Color(0.35f, 0.65f, 1f);
            case "geel":
            case "yellow":
                return new Color(1f, 0.9f, 0.25f);
            case "groen":
            case "green":
                return new Color(0.35f, 1f, 0.45f);
            case "zwart":
            case "black":
                return new Color(0.6f, 0.6f, 0.6f);
            case "wit":
            case "white":
                return Color.white;
            case "oranje":
            case "orange":
                return new Color(1f, 0.6f, 0.2f);
            default:
                return Color.white;
        }
    }
}

[System.Serializable]
public class TelemetryData
{
    public string type;
    public string huidig_blokje;
    public string huidige_kleur;
    public int naar_bakje;
    public int blokjes_per_uur;
    public int totaal_gesorteerd;
}