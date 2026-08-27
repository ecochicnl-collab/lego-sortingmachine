using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UISwitcher;

public class PakBlokjesPanelManager : MonoBehaviour
{
    [Header("Data Source")]
    public LegoDataAsset databaseAsset;

    [Header("UI Input Control")]
    public TMP_InputField zoekInputField;
    public TMP_Text resultatenAantalText;

    public UISwitcher.UISwitcher modifiedToggle;
    public UISwitcher.UISwitcher stickersToggle;
    public UISwitcher.UISwitcher minifigToggle;

    [Header("ScrollView Instellingen")]
    public ScrollRect scrollRect;
    public Transform contentParent;
    public GameObject blokjeRijPrefab;

    [Header("Performance Instellingen")]
    [Tooltip("Hoeveel rijen er tegelijk worden getoond. Scroll naar beneden om er meer te laden.")]
    public int maxRijenInScherm = 25;

    private class ZoekScoreEntry
    {
        public LegoPart part;
        public int score;
    }

    private List<GameObject> rijPool = new List<GameObject>();
    private List<BlokjeRijItem> rijItemsPool = new List<BlokjeRijItem>();
    private List<LegoPart> gefilterdeParts = new List<LegoPart>();
    private string zoekTerm = "";
    private int zichtbaarAantal = 0;
    private bool bezigMetUpdaten = false;
    private Button toonMeerKnop;
    private Transform chipsPopulairContainer;
    private Transform chipsRecentContainer;

    private void Start()
    {
        Debug.Log("[PakBlokjesPanelManager] Start: initialisatie gestart");
        InitialiseerContent();
        MaakToonMeerKnop();
        MaakZoekChips();

        if (zoekInputField != null)
            zoekInputField.onValueChanged.AddListener(delegate { UpdateLijst(); });

        if (modifiedToggle != null)
            modifiedToggle.onValueChanged.AddListener(delegate { UpdateLijst(); });

        if (stickersToggle != null)
            stickersToggle.onValueChanged.AddListener(delegate { UpdateLijst(); });

        if (minifigToggle != null)
            minifigToggle.onValueChanged.AddListener(delegate { UpdateLijst(); });

        if (scrollRect != null)
            scrollRect.onValueChanged.AddListener(OnScrollGewijzigd);

        UpdateLijst();
    }

    private void InitialiseerContent()
    {
        if (blokjeRijPrefab == null || contentParent == null)
        {
            Debug.LogWarning($"[PakBlokjesPanelManager] Prefab of parent ontbreekt: prefab={blokjeRijPrefab}, parent={contentParent}");
            return;
        }

        var contentRect = contentParent.GetComponent<RectTransform>();
        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
        }

        var verticalLayout = contentParent.GetComponent<VerticalLayoutGroup>();
        if (verticalLayout == null)
        {
            verticalLayout = contentParent.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        verticalLayout.childControlHeight = true;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.spacing = 10f;
        verticalLayout.padding = new RectOffset(8, 8, 8, 8);

        var fitter = contentParent.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = contentParent.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Debug.Log($"[PakBlokjesPanelManager] InitialiseerContent: prefab={blokjeRijPrefab.name}, parent={contentParent.name}, laadStap={maxRijenInScherm}");

        ZorgVoorGenoegRijen(maxRijenInScherm);
    }

    private void ZorgVoorGenoegRijen(int aantal)
    {
        if (blokjeRijPrefab == null || contentParent == null) return;

        while (rijPool.Count < aantal)
        {
            GameObject nieuweRij = Instantiate(blokjeRijPrefab, contentParent, false);

            RectTransform rect = nieuweRij.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(0f, 220f);
                rect.anchoredPosition3D = Vector3.zero;
            }

            LayoutElement layoutElement = nieuweRij.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = nieuweRij.AddComponent<LayoutElement>();
            }
            layoutElement.preferredHeight = 220f;
            layoutElement.minHeight = 220f;
            layoutElement.flexibleHeight = 0f;

            nieuweRij.SetActive(false);

            rijPool.Add(nieuweRij);
            rijItemsPool.Add(nieuweRij.GetComponent<BlokjeRijItem>());
        }
    }

    public void UpdateLijst()
    {
        bezigMetUpdaten = true;

        if (databaseAsset == null || databaseAsset.parts == null)
        {
            Debug.LogWarning("[PakBlokjesPanelManager] UpdateLijst: databaseAsset of parts is null");
            bezigMetUpdaten = false;
            return;
        }

        zoekTerm = zoekInputField != null ? zoekInputField.text.ToLower().Trim() : "";

        bool modifiedAan = modifiedToggle != null && modifiedToggle.isOn;
        bool stickersAan = stickersToggle != null && stickersToggle.isOn;
        bool minifigAan = minifigToggle != null && minifigToggle.isOn;

        // 1) Filteren: slider UIT = die soort blokjes weglaten, slider AAN = alleen die soort tonen.
        //    Slim scoren: exacte en prefix-matches komen eerst (zie ZoekHelper).
        gefilterdeParts.Clear();

        List<ZoekScoreEntry> gescoord = new List<ZoekScoreEntry>();

        for (int i = 0; i < databaseAsset.parts.Count; i++)
        {
            LegoPart part = databaseAsset.parts[i];
            if (part == null) continue;

            int score = string.IsNullOrEmpty(zoekTerm) ? 1 : ZoekHelper.ScoreVoorZoekterm(part, zoekTerm);
            if (score <= 0) continue;

            if (modifiedToggle != null)
            {
                if (modifiedAan && !part.isModified) continue;
                if (!modifiedAan && part.isModified) continue;
            }

            if (stickersToggle != null)
            {
                if (stickersAan && !part.hasSticker) continue;
                if (!stickersAan && part.hasSticker) continue;
            }

            if (minifigToggle != null)
            {
                if (minifigAan && !part.isMinifigPart) continue;
                if (!minifigAan && part.isMinifigPart) continue;
            }

            gescoord.Add(new ZoekScoreEntry { part = part, score = score });
        }

        // 2) Sorteren: eerst de blokjes die al in de machine (inventaris) zitten, dan de beste zoek-match,
        //    dan normale blokjes vóór modified/prints/minifig. Binnen dezelfde groep eerst op voorraad.
        HashSet<string> inInventaris = InventarisOpslag.GeefPartNummersInInventaris();

        gescoord.Sort((a, b) =>
        {
            bool aIn = inInventaris.Contains(a.part.part_num);
            bool bIn = inInventaris.Contains(b.part.part_num);
            if (aIn != bIn) return bIn.CompareTo(aIn);

            if (a.score != b.score) return b.score.CompareTo(a.score);
            return CompareNormaliteit(a.part, b.part);
        });

        foreach (var entry in gescoord)
        {
            gefilterdeParts.Add(entry.part);
        }

        // 3) Eerste lading tonen; de rest komt erbij tijdens het scrollen.
        zichtbaarAantal = Mathf.Min(maxRijenInScherm, gefilterdeParts.Count);
        ToonRijen();

        if (resultatenAantalText != null)
        {
            resultatenAantalText.text = $"Gevonden resultaten: {gefilterdeParts.Count}";
        }

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }

        Debug.Log($"[PakBlokjesPanelManager] UpdateLijst klaar: gevonden={gefilterdeParts.Count}, getoond={zichtbaarAantal}, zoek='{zoekTerm}', modified={modifiedAan}, stickers={stickersAan}, minifig={minifigAan}");

        bezigMetUpdaten = false;
    }

    private static int CompareNormaliteit(LegoPart a, LegoPart b)
    {
        int rankA = NormaliteitRank(a);
        int rankB = NormaliteitRank(b);
        if (rankA != rankB) return rankA.CompareTo(rankB);

        bool stockA = a.TotaalAantal() > 0;
        bool stockB = b.TotaalAantal() > 0;
        if (stockA != stockB) return stockB.CompareTo(stockA);

        return string.Compare(a.part_num, b.part_num, System.StringComparison.Ordinal);
    }

    private static int NormaliteitRank(LegoPart part)
    {
        if (part == null) return 99;
        if (part.isMinifigPart) return 3;
        if (part.hasSticker) return 2;
        if (part.isModified) return 1;
        return 0;
    }

    private void ToonRijen()
    {
        if (gefilterdeParts == null || contentParent == null) return;

        ZorgVoorGenoegRijen(zichtbaarAantal);

        for (int i = 0; i < rijPool.Count; i++)
        {
            if (i < zichtbaarAantal && i < gefilterdeParts.Count)
            {
                BlokjeRijItem rijItem = rijItemsPool[i];
                if (rijItem != null)
                {
                    rijItem.Setup(gefilterdeParts[i], zoekTerm);
                }
                rijPool[i].SetActive(true);
            }
            else
            {
                rijPool[i].SetActive(false);
            }
        }

        UpdateToonMeerKnop();
    }

    /// <summary>Maakt de "Toon meer"-knop onderaan de resultaten (binnen de scroll), naast het automatisch laden.</summary>
    private void MaakToonMeerKnop()
    {
        if (contentParent == null) return;
        if (contentParent.Find("ToonMeerKnop") != null) return;

        GameObject knopGo = new GameObject("ToonMeerKnop", typeof(RectTransform), typeof(Image), typeof(Button));
        knopGo.transform.SetParent(contentParent, false);

        Image achtergrond = knopGo.GetComponent<Image>();
        achtergrond.color = new Color(0.2f, 0.35f, 0.6f, 1f);

        LayoutElement layoutElement = knopGo.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 50f;
        layoutElement.minHeight = 50f;
        layoutElement.flexibleHeight = 0f;

        RectTransform rt = knopGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 50f);

        GameObject tekstGo = new GameObject("Tekst", typeof(RectTransform), typeof(TextMeshProUGUI));
        tekstGo.transform.SetParent(knopGo.transform, false);

        TextMeshProUGUI tekst = tekstGo.GetComponent<TextMeshProUGUI>();
        tekst.text = "Toon meer";
        tekst.fontSize = 24;
        tekst.alignment = TextAlignmentOptions.Center;
        tekst.color = Color.white;
        tekst.fontStyle = FontStyles.Bold;

        RectTransform tekstRt = tekstGo.GetComponent<RectTransform>();
        tekstRt.anchorMin = Vector2.zero;
        tekstRt.anchorMax = Vector2.one;
        tekstRt.offsetMin = Vector2.zero;
        tekstRt.offsetMax = Vector2.zero;

        Button knop = knopGo.GetComponent<Button>();
        knop.targetGraphic = achtergrond;
        knop.onClick.AddListener(ToonMeerKlikken);

        toonMeerKnop = knop;
        knopGo.SetActive(false);
    }

    private void UpdateToonMeerKnop()
    {
        if (toonMeerKnop == null) return;

        int resterend = gefilterdeParts.Count - zichtbaarAantal;
        bool heeftMeer = resterend > 0;

        toonMeerKnop.gameObject.SetActive(heeftMeer);

        if (heeftMeer)
        {
            TextMeshProUGUI txt = toonMeerKnop.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = "Toon meer (" + resterend + ")";
            }
        }
    }

    private void ToonMeerKlikken()
    {
        zichtbaarAantal = Mathf.Min(zichtbaarAantal + maxRijenInScherm, gefilterdeParts.Count);
        ToonRijen();
    }

    /// <summary>Maakt de zoekgeschiedenis-chips onder de zoekbalk (populair + recent).</summary>
    private void MaakZoekChips()
    {
        if (zoekInputField == null) return;

        RectTransform searchRt = zoekInputField.GetComponent<RectTransform>();
        if (searchRt == null || searchRt.parent == null) return;
        if (searchRt.parent.Find("ZoekChips") != null) return;

        GameObject chipsGo = new GameObject("ZoekChips", typeof(RectTransform));
        RectTransform chipsRt = chipsGo.GetComponent<RectTransform>();
        chipsRt.SetParent(searchRt.parent, false);
        chipsRt.anchorMin = new Vector2(0.5f, 0.5f);
        chipsRt.anchorMax = new Vector2(0.5f, 0.5f);
        chipsRt.pivot = new Vector2(0.5f, 0.5f);
        // Compact in de ruimte tussen de zoekbalk en de resultatenlijst
        chipsRt.anchoredPosition = new Vector2(searchRt.anchoredPosition.x, searchRt.anchoredPosition.y - searchRt.rect.height / 2f - 10f - 31f);
        chipsRt.sizeDelta = new Vector2(Mathf.Min(searchRt.rect.width, 1050f), 62f);

        chipsPopulairContainer = MaakChipRij("Populair", chipsRt.transform, new Vector2(0f, -13f));
        chipsRecentContainer = MaakChipRij("Recent", chipsRt.transform, new Vector2(0f, -45f));

        HerlaadZoekChips();
    }

    private Transform MaakChipRij(string naam, Transform ouder, Vector2 positie)
    {
        GameObject rij = new GameObject(naam, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform rt = rij.GetComponent<RectTransform>();
        rt.SetParent(ouder, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = positie;
        rt.sizeDelta = new Vector2(0f, 26f);

        HorizontalLayoutGroup hlg = rij.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 6f;
        return rij.transform;
    }

    private void HerlaadZoekChips()
    {
        if (chipsPopulairContainer == null || chipsRecentContainer == null) return;

        List<string> populair = ZoekGeschiedenis.Populair(5);
        List<string> recent = ZoekGeschiedenis.Recente(5);

        // Recente termen die ook populair zijn niet dubbel tonen
        List<string> recentSchoon = new List<string>();
        foreach (string t in recent)
        {
            if (!populair.Contains(t)) recentSchoon.Add(t);
        }

        ZoekChips.Vul(chipsPopulairContainer, populair, KlikZoekChip, 190f);
        ZoekChips.Vul(chipsRecentContainer, recentSchoon, KlikZoekChip, 190f);
    }

    private void KlikZoekChip(string term)
    {
        if (zoekInputField != null) zoekInputField.text = term;
        UpdateLijst();
    }

    /// <summary>Registreert de huidige zoekterm in de geschiedenis en ververst de chips (aanroepbaar vanuit BlokjeRijItem na een PAK).</summary>
    public void RegistreerHuidigeZoektermEnHerlaad()
    {
        string term = zoekInputField != null ? zoekInputField.text : "";
        ZoekGeschiedenis.Registreer(term);
        HerlaadZoekChips();
    }

    private void OnScrollGewijzigd(Vector2 positie)
    {
        if (bezigMetUpdaten) return;
        if (scrollRect == null || gefilterdeParts == null) return;

        // Dicht bij de onderkant -> volgende lading laden
        if (scrollRect.verticalNormalizedPosition < 0.05f && zichtbaarAantal < gefilterdeParts.Count)
        {
            zichtbaarAantal = Mathf.Min(zichtbaarAantal + maxRijenInScherm, gefilterdeParts.Count);
            ToonRijen();
        }
    }
}
