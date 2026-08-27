using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Aparte bevestigingspagina die opent na de PAK-knop.
/// Toont het blokje, de gekozen kleur en het aantal, en bevestigt de pak-actie.
/// De hele UI wordt automatisch opgebouwd onder de actieve Canvas, dus er hoeft
/// niets in de scene of Inspector te worden gekoppeld.
/// </summary>
public class BevestigPakPanel : MonoBehaviour
{
    private static BevestigPakPanel instantie;

    private LegoPart huidigPart;
    private LegoKleurVoorraad huidigeKleur;
    private int aantal = 1;

    private TMP_Text blokjeTekst;
    private TMP_Text kleurNaamTekst;
    private TMP_Text voorraadTekst;
    private TMP_Text aantalTekst;
    private Image kleurStaaltje;
    private Button minKnop;
    private Button plusKnop;

    /// <summary>Opent de bevestigingspagina voor het gegeven blokje en kleur.</summary>
    public static void Open(LegoPart part, LegoKleurVoorraad kleur)
    {
        if (instantie == null)
        {
            instantie = FindFirstObjectByType<BevestigPakPanel>();
        }

        if (instantie == null)
        {
            GameObject go = new GameObject("Bevestig Pak Panel");
            instantie = go.AddComponent<BevestigPakPanel>();
            instantie.BouwUI();
        }

        instantie.Toon(part, kleur);
    }

    private void BouwUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[BevestigPakPanel] Geen Canvas gevonden in de scene.");
            return;
        }

        transform.SetParent(canvas.transform, false);
        transform.SetAsLastSibling();

        // Volledig scherm overlay die klikken erachter blokkeert
        GameObject overlayGo = Maak("Achtergrond", transform, typeof(RectTransform), typeof(Image));
        Image overlay = overlayGo.GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.65f);
        Stretch(overlayGo.GetComponent<RectTransform>());

        // Centraal paneel
        GameObject paneel = Maak("Paneel", transform, typeof(RectTransform), typeof(Image));
        Image paneelImg = paneel.GetComponent<Image>();
        paneelImg.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        RectTransform paneelRt = paneel.GetComponent<RectTransform>();
        paneelRt.anchorMin = new Vector2(0.5f, 0.5f);
        paneelRt.anchorMax = new Vector2(0.5f, 0.5f);
        paneelRt.pivot = new Vector2(0.5f, 0.5f);
        paneelRt.sizeDelta = new Vector2(640f, 520f);
        paneelRt.anchoredPosition = Vector2.zero;

        // Titel
        TMP_Text titel = MaakTekst("Titel", paneel.transform, "Bevestig gepakt blokje", 34, Color.black, FontStyles.Bold);
        Positie(titel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(580f, 50f));
        titel.alignment = TextAlignmentOptions.Center;

        // Blokje-regel (part_num - naam)
        blokjeTekst = MaakTekst("Blokje", paneel.transform, "", 26, Color.black, FontStyles.Bold);
        Positie(blokjeTekst.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(580f, 40f));
        blokjeTekst.alignment = TextAlignmentOptions.Center;

        // Kleur: staaltje + naam + voorraad
        kleurStaaltje = Maak("KleurStaaltje", paneel.transform, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        Positie(kleurStaaltje.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-180f, -185f), new Vector2(60f, 60f));

        kleurNaamTekst = MaakTekst("KleurNaam", paneel.transform, "", 24, Color.black);
        Positie(kleurNaamTekst.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(70f, -180f), new Vector2(400f, 40f));

        voorraadTekst = MaakTekst("Voorraad", paneel.transform, "", 20, new Color(0.2f, 0.2f, 0.2f, 1f));
        Positie(voorraadTekst.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(70f, -220f), new Vector2(400f, 36f));

        // Aantal-stepper: [-] [aantal] [+]
        TMP_Text aantalLabel = MaakTekst("AantalLabel", paneel.transform, "Aantal:", 24, Color.black);
        Positie(aantalLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-190f, -300f), new Vector2(120f, 40f));

        minKnop = MaakKnop("Min", paneel.transform, "-", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-120f, -300f), new Vector2(70f, 70f), new Color(0.8f, 0.8f, 0.8f, 1f));
        minKnop.onClick.AddListener(() => Aanpassen(-1));

        aantalTekst = MaakTekst("Aantal", paneel.transform, "1", 30, Color.black, FontStyles.Bold);
        Positie(aantalTekst.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-25f, -300f), new Vector2(60f, 70f));
        aantalTekst.alignment = TextAlignmentOptions.Center;

        plusKnop = MaakKnop("Plus", paneel.transform, "+", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(70f, -300f), new Vector2(70f, 70f), new Color(0.8f, 0.8f, 0.8f, 1f));
        plusKnop.onClick.AddListener(() => Aanpassen(1));

        // Bevestigen / Annuleren
        Button bevestigKnop = MaakKnop("Bevestigen", paneel.transform, "Bevestigen", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-140f, -420f), new Vector2(240f, 70f), new Color(0.15f, 0.6f, 0.2f, 1f));
        bevestigKnop.onClick.AddListener(Bevestig);

        Button annuleerKnop = MaakKnop("Annuleren", paneel.transform, "Annuleren", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(140f, -420f), new Vector2(240f, 70f), new Color(0.6f, 0.6f, 0.6f, 1f));
        annuleerKnop.onClick.AddListener(Annuleer);

        // X rechtsboven
        Button kruisKnop = MaakKnop("Kruisknop", paneel.transform, "✕", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(55f, 55f), new Color(0.75f, 0.2f, 0.2f, 0.9f));
        kruisKnop.onClick.AddListener(Annuleer);

        gameObject.SetActive(false);
    }

    private void Toon(LegoPart part, LegoKleurVoorraad kleur)
    {
        huidigPart = part;
        huidigeKleur = kleur;
        aantal = 1;

        transform.SetAsLastSibling();
        gameObject.SetActive(true);

        if (blokjeTekst != null)
            blokjeTekst.text = $"{part.part_num} - {part.name}";

        if (kleurStaaltje != null)
        {
            kleurStaaltje.color = kleur.kleurCode != default(Color) ? kleur.kleurCode : Color.white;
        }

        if (kleurNaamTekst != null)
            kleurNaamTekst.text = kleur.kleurNaam;

        UpdateVoorraadEnAantal();
    }

    private void UpdateVoorraadEnAantal()
    {
        if (huidigeKleur == null) return;

        int maxAantal = Mathf.Max(0, huidigeKleur.aantalInVoorraad);

        if (voorraadTekst != null)
        {
            voorraadTekst.text = maxAantal > 0
                ? $"Op voorraad: {maxAantal} stuks"
                : "Niet op voorraad";
            voorraadTekst.color = maxAantal > 0 ? new Color(0.1f, 0.5f, 0.1f, 1f) : Color.red;
        }

        aantal = Mathf.Clamp(aantal, 1, Mathf.Max(1, maxAantal));

        if (aantalTekst != null)
            aantalTekst.text = aantal.ToString();

        if (minKnop != null)
            minKnop.interactable = aantal > 1;

        if (plusKnop != null)
            plusKnop.interactable = maxAantal > 0 && aantal < maxAantal;
    }

    private void Aanpassen(int stap)
    {
        int maxAantal = huidigeKleur != null ? Mathf.Max(1, huidigeKleur.aantalInVoorraad) : 1;
        aantal = Mathf.Clamp(aantal + stap, 1, maxAantal);
        UpdateVoorraadEnAantal();
    }

    private void Bevestig()
    {
        if (huidigPart == null || huidigeKleur == null)
        {
            Annuleer();
            return;
        }

        int tePaken = Mathf.Clamp(aantal, 1, Mathf.Max(1, huidigeKleur.aantalInVoorraad));
        huidigeKleur.aantalInVoorraad = Mathf.Max(0, huidigeKleur.aantalInVoorraad - tePaken);

        Debug.Log($"[PakBevestiging] GEPAKT: {tePaken}x {huidigPart.part_num} - {huidigPart.name} ({huidigeKleur.kleurNaam})");

        // Klaar met pakken -> terug naar het startscherm
        gameObject.SetActive(false);

        appmanager manager = FindFirstObjectByType<appmanager>();
        if (manager != null)
        {
            manager.TerugNaarHome();
        }
    }

    private void Annuleer()
    {
        // Overlay weghalen; de zoekpagina (beheer) blijft open zodat je verder kunt pakken
        gameObject.SetActive(false);
    }

    #region --- UI HELPERS ---

    private static GameObject Maak(string naam, Transform ouder, params System.Type[] componenten)
    {
        GameObject go = new GameObject(naam, componenten);
        go.transform.SetParent(ouder, false);
        return go;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void Positie(RectTransform rt, Vector2 ankerMin, Vector2 ankerMax, Vector2 positie, Vector2 grootte)
    {
        rt.anchorMin = ankerMin;
        rt.anchorMax = ankerMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = positie;
        rt.sizeDelta = grootte;
    }

    private static TMP_Text MaakTekst(string naam, Transform ouder, string inhoud, float grootte, Color kleur, FontStyles stijl = FontStyles.Normal)
    {
        GameObject go = Maak(naam, ouder, typeof(RectTransform), typeof(TextMeshProUGUI));
        TMP_Text tekst = go.GetComponent<TextMeshProUGUI>();
        tekst.text = inhoud;
        tekst.fontSize = grootte;
        tekst.color = kleur;
        tekst.fontStyle = stijl;
        tekst.alignment = TextAlignmentOptions.MidlineLeft;
        return tekst;
    }

    private static Button MaakKnop(string naam, Transform ouder, string tekst, Vector2 ankerMin, Vector2 ankerMax, Vector2 pivot, Vector2 positie, Vector2 grootte, Color kleur)
    {
        GameObject go = Maak(naam, ouder, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = ankerMin;
        rt.anchorMax = ankerMax;
        rt.pivot = pivot;
        rt.anchoredPosition = positie;
        rt.sizeDelta = grootte;

        Image img = go.GetComponent<Image>();
        img.color = kleur;

        Button knop = go.GetComponent<Button>();
        knop.targetGraphic = img;

        TMP_Text label = MaakTekst("Label", go.transform, tekst, 26, Color.white, FontStyles.Bold);
        RectTransform labelRt = label.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        label.alignment = TextAlignmentOptions.Center;

        return knop;
    }

    #endregion
}
