using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class search : MonoBehaviour
{
    public LegoDataAsset databaseAsset;
    public Transform panelContent;
    public GameObject itemPrefab;
    public TMP_InputField zoekInputField;
    public GameObject gridPanel;
    public GameObject scrollRectPanel;
    public TMP_Text infoText; 

    public bool verbergPrintEnStickers = true;
    public bool alleenModified = true;
    public bool alleenScharnieren = true;
    public bool alleenStickersEnBars = true;

    public int breedte = 0;
    public int lengte = 0;

    [HideInInspector] public bool isSelectieModus = false;
    private LegoPart geselecteerdBlokje;
    private List<LegoPart> alleBlokjes;
    private string huidigeTekstInvoer = "";
    private List<GridSlot> alleGridSlots = new List<GridSlot>();
    private HashSet<string> bezetteBlokjesNamen = new HashSet<string>();

    void Awake()
    {
        if (databaseAsset != null)
        {
            alleBlokjes = databaseAsset.parts;
            Debug.Log("[LegoSearch] Database succesvol ingeladen! Totaal: " + alleBlokjes.Count);
        }
    }

    void Start()
    {
        if (gridPanel != null)
        {
            alleGridSlots = new List<GridSlot>(gridPanel.GetComponentsInChildren<GridSlot>(true));
        }

        if (scrollRectPanel != null) scrollRectPanel.SetActive(false);
        if (gridPanel != null) gridPanel.SetActive(true);
    }

    public LegoPart VindBlokjeOpNaam(string origineleNaam)
    {
        if (alleBlokjes == null) return null;
        return alleBlokjes.FirstOrDefault(p => p.name == origineleNaam);
    }

    public void VoegBezetBlokjeToe(string naam)
    {
        bezetteBlokjesNamen.Add(naam);
    }

    public List<LegoPart> ZoekOnderdelen(string input)
    {
        List<LegoPart> resultaten = new List<LegoPart>();
        if (alleBlokjes == null) return resultaten;

        huidigeTekstInvoer = input;
        string schoneZoekopdracht = NormaliseerTekst(input);

        foreach (LegoPart part in alleBlokjes)
        {
            string partNaamLower = part.name.ToLower();

            if (verbergPrintEnStickers && (partNaamLower.Contains("print") || partNaamLower.Contains("sticker") || partNaamLower.Contains("pattern")))
            {
                continue;
            }

            if (!alleenModified && partNaamLower.Contains("modified")) continue;
            if (!alleenScharnieren && partNaamLower.Contains("hinge")) continue;
            if (!alleenStickersEnBars && (partNaamLower.Contains("bar") || partNaamLower.Contains("stick") || partNaamLower.Contains("shaft"))) continue;

            if (breedte > 0 && lengte > 0)
            {
                string schoneNaam = NormaliseerTekst(part.name);
                string m1 = breedte + "x" + lengte;
                string m2 = lengte + "x" + breedte;

                bool matchMaat = schoneNaam.Contains(m1) || schoneNaam.Contains(m2);

                if (!matchMaat && (partNaamLower.Contains("bar") || partNaamLower.Contains("hinge") || partNaamLower.Contains("stick")))
                {
                    int grootsteMaat = Mathf.Max(breedte, lengte);
                    if (partNaamLower.Contains(grootsteMaat + "l") || partNaamLower.Contains(grootsteMaat + " l"))
                    {
                        matchMaat = true;
                    }
                }

                if (!matchMaat) continue;
            }

            if (!string.IsNullOrEmpty(schoneZoekopdracht))
            {
                string schoneNaam = NormaliseerTekst(part.name);
                if (!schoneNaam.Contains(schoneZoekopdracht))
                {
                    continue;
                }
            }

            resultaten.Add(part);
        }
        return resultaten;
    }

    private string NormaliseerTekst(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input.ToLower().Replace("bij", "x").Replace(" ", "").Replace("_", "");
    }

    public void OnSearchInputChanged(string nieuweInvoer)
    {
        huidigeTekstInvoer = nieuweInvoer;
        Herbereken();
    }

    public void SetGridMaat(int x, int y) 
    { 
        breedte = x; 
        lengte = y; 
        Herbereken(); 
    }

    public void ResetGrid() 
    { 
        breedte = 0; 
        lengte = 0; 
        Herbereken(); 
    }

    public void ToonGrid()
    {
        isSelectieModus = false;
        foreach (GridSlot slot in alleGridSlots) slot.ResetKleur();
        if (scrollRectPanel != null) scrollRectPanel.SetActive(false);
        if (gridPanel != null) gridPanel.SetActive(true);
    }

    private void Herbereken()
    {
        List<LegoPart> gevonden = ZoekOnderdelen(huidigeTekstInvoer);
        UpdateScrollList(gevonden);
    }

    private void UpdateScrollList(List<LegoPart> resultaten)
    {
        if (panelContent == null || itemPrefab == null) return;

        if (gridPanel != null) gridPanel.SetActive(false);
        if (scrollRectPanel != null) scrollRectPanel.SetActive(true);

        foreach (Transform child in panelContent)
        {
            Destroy(child.gameObject);
        }

        var uniekeResultaten = resultaten
            .Select(p => new { Part = p, SchoneNaam = FormatteerNaam(p.name) })
            .Where(x => !string.IsNullOrEmpty(x.SchoneNaam))
            .GroupBy(x => x.SchoneNaam)
            .Select(g => g.First())
            .ToList();

        int limiet = Mathf.Min(uniekeResultaten.Count, 300);

        for (int i = 0; i < limiet; i++)
        {
            GameObject nieuwItem = Instantiate(itemPrefab, panelContent);
            LegoPart gekozenPart = uniekeResultaten[i].Part;
            string getoondeNaam = uniekeResultaten[i].SchoneNaam;

            if (bezetteBlokjesNamen.Contains(gekozenPart.name))
            {
                getoondeNaam = "(Bezet) " + getoondeNaam;
            }

            TMP_Text tmpTekst = nieuwItem.GetComponentInChildren<TMP_Text>();
            if (tmpTekst != null)
            {
                tmpTekst.text = getoondeNaam;
            }

            Button btn = nieuwItem.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnBlokjeGeklikt(gekozenPart, uniekeResultaten[i].SchoneNaam));
            }
        }

        if (zoekInputField != null)
        {
            zoekInputField.ActivateInputField();
        }
    }

    private void OnBlokjeGeklikt(LegoPart part, string schoneNaam)
    {
        geselecteerdBlokje = part;
        
        if (infoText != null)
        {
            infoText.text = part.name; 
        }

        isSelectieModus = true;

        if (scrollRectPanel != null) scrollRectPanel.SetActive(false);
        if (gridPanel != null) gridPanel.SetActive(true);

        foreach (GridSlot slot in alleGridSlots)
        {
            slot.ZetGeel();
        }
    }

    public void KoppelAanSlot(GridSlot slot)
    {
        if (geselecteerdBlokje == null) return;

        string korteNaam = FormatteerNaam(geselecteerdBlokje.name).Replace(" bij ", "x ");

        slot.KoppelBlokje(geselecteerdBlokje, korteNaam);

        foreach (GridSlot s in alleGridSlots)
        {
            s.ResetKleur();
        }

        isSelectieModus = false;
        geselecteerdBlokje = null;
        
        Herbereken();
    }

    private string FormatteerNaam(string origineleNaam)
    {
        if (string.IsNullOrEmpty(origineleNaam)) return "";
        string schoneNaam = origineleNaam.ToLower();
        schoneNaam = System.Text.RegularExpressions.Regex.Replace(schoneNaam, @"\bno\.?\s*\d+", "");

        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(schoneNaam, @"(\d+)\s*x\s*(\d+)");
        if (match.Success)
        {
            string maatTekst = match.Groups[1].Value + " bij " + match.Groups[2].Value;
            schoneNaam = schoneNaam.Replace(match.Value, "").Trim();
            schoneNaam = maatTekst + " " + schoneNaam;
        }

        schoneNaam = System.Text.RegularExpressions.Regex.Replace(schoneNaam, @"\s+", " ").Trim();
        if (schoneNaam.Length > 0)
        {
            schoneNaam = char.ToUpper(schoneNaam[0]) + schoneNaam.Substring(1);
        }
        return schoneNaam;
    }
}