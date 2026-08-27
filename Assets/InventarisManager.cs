using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Inventaris-tabblad: 2 kanten van 15 x 25 bakjes (onderste 8 rijen groot, dan 7 medium, dan 10 klein).
/// Blokjes zoeken (zelfde systeem als de pak-pagina) en aan een bakje toewijzen, met de keuze
/// alle kleuren / alleen die kleur / hele categorie. Alles wordt bij het draaien opgebouwd en
/// de toewijzing wordt bewaard in inventaris.json (persistentDataPath).
/// </summary>
public class InventarisManager : MonoBehaviour
{
    private static InventarisManager instantie;

    private class InventarisCel
    {
        public InventarisBakje bakje;
        public Button knop;
        public Image beeld;
        public Image plaatje;
        public TMP_Text tekst;
        public string verwachtPartNummer;
    }

    private class ZoekResultaat
    {
        public LegoPart part;
        public int score;
    }

    // --- Databron ---
    private LegoDataAsset database;
    private Dictionary<string, LegoPart> partOpNummer = new Dictionary<string, LegoPart>();

    // --- Toestand ---
    private List<InventarisBakje> bakjes = new List<InventarisBakje>();
    private InventarisBakje geselecteerdBakje;
    private LegoPart gekozenPart;
    private string modus = "alle";                 // "alle" | "kleur" | "categorie"
    private LegoKleurVoorraad gekozenKleur;
    private string zoekTerm = "";
    private List<LegoPart> zoekResultaten = new List<LegoPart>();
    private int zichtbaarResultaten = MaxResultaten;
    private string zoekGrootte = "";   // "" = alle groottes; anders "klein"/"medium"/"groot" (van het geselecteerde bakje)
    private TMP_Text zoekPlaceholder;
    private Button toonMeerKnop;
    private Button afbeeldingenKnop;
    private TMP_Text afbeeldingenKnopTekst;
    private Button kantAKnop;
    private Button kantBKnop;
    private TMP_Text kantAKnopTekst;
    private TMP_Text kantBKnopTekst;

    [Header("Inventaris UI")]
    [Tooltip("Gebruik dezelfde rij-prefab als Pak Blokjes voor zoekresultaten.")]
    public GameObject rijPrefab;
    [Tooltip("Laad externe/lokale afbeeldingen pas wanneer dit aan staat.")]
    public bool afbeeldingenLaden = false;
    private readonly List<GameObject> resultaatPrefabRijen = new List<GameObject>();
    private readonly List<BlokjeRijItem> resultaatPrefabItems = new List<BlokjeRijItem>();

    // --- Plaatjes ---
    private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private HashSet<string> bezigMetDownloaden = new HashSet<string>();
    private Queue<InventarisCel> downloadQueue = new Queue<InventarisCel>();
    private int actieveDownloads = 0;
    private const int MaxGelijktijdigeDownloads = 4;

    // --- UI ---
    private List<InventarisCel> cellen = new List<InventarisCel>();
    private TMP_InputField zoekInput;
    private TMP_Text bakjeInfoTekst;
    private TMP_Text gekozenBlokjeTekst;
    private TMP_Text suggestiesTitel;
    private Button modusAlleKnop;
    private Button modusKleurKnop;
    private Button modusCategorieKnop;
    private RectTransform resultatenContainer;
    private RectTransform kleurLijstContainer;
    private RectTransform suggestiesContainer;
    private GameObject kleurLijstGo;
    private Transform chipsPopulairContainer;
    private Transform chipsRecentContainer;
    private List<GameObject> resultaatRijen = new List<GameObject>();
    private List<GameObject> kleurRijen = new List<GameObject>();
    private List<Button> suggestieKnoppen = new List<Button>();

    private const int MaxResultaten = 30;
    private const int MaxKleuren = 40;
    private const int AantalSuggesties = 4;
    private const float LabelBreedte = 40f;
    private const float ResultaatRijHoogte = 112f;
    private const float ResultaatRijTussenruimte = 8f;

    private bool allesLeegBevestigen = false;
    private float allesLeegTijd = 0f;
    private bool uiGebouwd = false;
    private int actieveKant;
    private const string ActieveKantKey = "Inventaris.ActieveKant";

    public static void Open()
    {
        // Zoek eerst naar een scene-panel met InventarisManager
        // BELANGRIJK: FindObjectsInactive.Include, anders wordt het inactieve scene-panel nooit gevonden!
        if (instantie == null)
        {
            instantie = FindFirstObjectByType<InventarisManager>(FindObjectsInactive.Include);
        }

        if (instantie != null)
        {
            // Activeer vóór BouwUI, anders kan StartCoroutine (raster bouwen) niet starten
            instantie.gameObject.SetActive(true);

            // Bouw de UI maar één keer; de vaste layout staat in run.unity.
            if (!instantie.uiGebouwd)
            {
                instantie.BouwUI();
            }
            instantie.Toon();
            return;
        }

        // Geen scene-panel gevonden: dit is alleen een veiligheidsfallback voor oude scenes.
        GameObject go = new GameObject("Inventaris Tab");
        instantie = go.AddComponent<InventarisManager>();
        instantie.gameObject.SetActive(true);
        instantie.BouwUI();
        instantie.Toon();
    }

    private void Toon()
    {
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        VernieuwAlleCellen();
    }

    private void Update()
    {
        if (allesLeegBevestigen && Time.time - allesLeegTijd > 2f)
        {
            allesLeegBevestigen = false;
        }
    }

    #region --- UI OPBOUWEN ---

    private void BouwUI()
    {
        database = FindFirstObjectByType<LegoDataAsset>();
        if (database == null)
            database = Resources.Load<LegoDataAsset>("LegoDataAsset");

        if (database == null)
        {
            Debug.LogError("[Inventaris] Geen LegoDataAsset gevonden (in scene of Resources).");
            return;
        }

        partOpNummer.Clear();
        foreach (var p in database.parts)
        {
            if (p != null && !string.IsNullOrEmpty(p.part_num))
                partOpNummer[p.part_num] = p;
        }

        bakjes = InventarisOpslag.Laad();
        if (bakjes == null || bakjes.Count == 0)
            bakjes = InventarisOpslag.Nieuw();
        else
            bakjes = InventarisOpslag.Normaliseer(bakjes);

        // Schrijf de complete positie-index direct weg, zodat ook oude/gedeeltelijke bestanden
        // na de eerste opening alle 750 vaste bakjes bevatten.
        InventarisOpslag.Bewaar(bakjes);

        actieveKant = Mathf.Clamp(PlayerPrefs.GetInt(ActieveKantKey, 0), 0, InventarisConfig.Kanten - 1);
        HerstelGeselecteerdBakje();

        // De layout en alle vaste bedieningselementen staan in run.unity.
        // De code vult alleen de data-afhankelijke rasters en lijsten.
        Transform lichaam = transform.Find("Lichaam");
        Transform kantA = lichaam != null ? lichaam.Find("Kant A") : null;
        Transform bediening = lichaam != null ? lichaam.Find("Bediening") : null;
        Transform kantB = lichaam != null ? lichaam.Find("Kant B") : null;

        if (lichaam == null || kantA == null || bediening == null || kantB == null)
        {
            Debug.LogError("[Inventaris] Scene-layout ontbreekt onder Inventaris Panel.");
            return;
        }

        bakjeInfoTekst = VindTekst(bediening, "BakjeInfo");
        gekozenBlokjeTekst = VindTekst(bediening, "GekozenBlokje");
        suggestiesTitel = VindTekst(bediening, "SuggestiesTitel");
        zoekInput = VindComponent<TMP_InputField>(bediening, "Zoekveld");
        zoekPlaceholder = zoekInput != null ? zoekInput.placeholder as TMP_Text : null;
        resultatenContainer = VindRect(bediening, "Resultaten/Viewport/Content");
        ConfigureerResultatenScrollRect(bediening);
        ConfigureerResultatenLayout();
        ZorgVoorClipping(bediening);
        ZorgVoorClipping(kantA);
        ZorgVoorClipping(kantB);
        kleurLijstGo = VindObject(bediening, "KleurLijst");
        kleurLijstContainer = kleurLijstGo != null ? kleurLijstGo.GetComponent<RectTransform>() : null;
        suggestiesContainer = VindRect(bediening, "Suggesties");
        ZorgVoorClipping(suggestiesContainer);
        modusAlleKnop = VindComponent<Button>(bediening, "AlleKleuren");
        modusKleurKnop = VindComponent<Button>(bediening, "DezeKleur");
        modusCategorieKnop = VindComponent<Button>(bediening, "Categorie");
        afbeeldingenKnop = VindComponent<Button>(bediening, "PlaatjesAan");
        afbeeldingenKnopTekst = VindTekst(bediening, "PlaatjesAan/Label");
        kantAKnop = VindComponent<Button>(transform, "Kopbalk/KantAKnop");
        kantBKnop = VindComponent<Button>(transform, "Kopbalk/KantBKnop");
        kantAKnopTekst = VindTekst(transform, "Kopbalk/KantAKnop/Label");
        kantBKnopTekst = VindTekst(transform, "Kopbalk/KantBKnop/Label");

        Transform kruisObject = transform.Find("Kopbalk/Kruisknop");
        Button kruis = kruisObject != null ? kruisObject.GetComponent<Button>() : null;
        if (kruis != null)
            kruis.onClick.AddListener(Sluit);

        KoppelKnop(modusAlleKnop, () => KiesModus("alle"));
        KoppelKnop(modusKleurKnop, () => KiesModus("kleur"));
        KoppelKnop(modusCategorieKnop, () => KiesModus("categorie"));
        KoppelKnop(VindComponent<Button>(bediening, "Toewijzen"), PasToeOpBakje);
        KoppelKnop(VindComponent<Button>(bediening, "Leegmaken"), MaakBakjeLeeg);
        KoppelKnop(VindComponent<Button>(bediening, "AutoVul"), AutoVullen);
        KoppelKnop(VindComponent<Button>(bediening, "AllesLeeg"), AllesLeegmaken);
        KoppelKnop(afbeeldingenKnop, WisselAfbeeldingen);
        KoppelKnop(kantAKnop, () => WisselKant(0));
        KoppelKnop(kantBKnop, () => WisselKant(1));

        if (zoekInput != null)
        {
            zoekInput.onSubmit.RemoveListener(OnZoekIngediend);
            zoekInput.onSubmit.AddListener(OnZoekIngediend);
        }

        BouwRasterVanafScene(kantA, 0);
        BouwRasterVanafScene(kantB, 1);
        StelZichtbareKantIn(actieveKant);
        BouwSceneSuggesties();

        UpdateModusKnoppen();
        ToonKleurLijst();
        UpdateAfbeeldingenKnop();
        UpdateKantKnoppen();
        UpdateZoekPlaceholder();
        VernieuwInformatie();
        uiGebouwd = true;
    }

    private void BouwRasterVanafScene(Transform kolom, int kant)
    {
        // De scene heeft momenteel "ScrollView"; oudere sceneversies gebruikten "Scroll View".
        Transform scrollObject = VindKind(kolom, "ScrollView", "Scroll View");
        Transform viewport = scrollObject != null ? VindKind(scrollObject, "Viewport") : null;
        RectTransform content = viewport != null ? VindRect(viewport, "Content") : null;
        RectTransform scroll = scrollObject as RectTransform;

        if (content == null || scroll == null || viewport == null)
        {
            Debug.LogError("[Inventaris] Kant " + (kant == 0 ? "A" : "B") + " mist ScrollView/Viewport/Content in de scene.");
            return;
        }

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = scrollObject.gameObject.AddComponent<ScrollRect>();
            Debug.LogWarning("[Inventaris] ScrollRect ontbrak op Kant " + (kant == 0 ? "A" : "B") + "; component automatisch hersteld.");
        }

        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.viewport = viewport as RectTransform;
        scrollRect.content = content;

        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, InventarisConfig.TotaleHoogte());
        StartCoroutine(VulRasterLater(scroll, content, kant));
    }

    private static void KoppelKnop(Button knop, UnityEngine.Events.UnityAction actie)
    {
        if (knop == null) return;
        knop.onClick.RemoveAllListeners();
        knop.onClick.AddListener(actie);
    }

    private void BouwSceneSuggesties()
    {
        if (suggestiesContainer == null || suggestieKnoppen.Count > 0) return;

        for (int i = 0; i < AantalSuggesties; i++)
        {
            Transform suggestie = suggestiesContainer.Find("Suggestie " + i);
            Button knop = suggestie != null ? suggestie.GetComponent<Button>() : null;
            if (knop == null) continue;

            int index = i;
            KoppelKnop(knop, () => KiesSuggestie(index));
            suggestieKnoppen.Add(knop);
        }
    }

    private void OnZoekIngediend(string waarde)
    {
        OnZoekVeranderd(waarde);
    }

    private System.Collections.IEnumerator VulRasterLater(RectTransform scrollRt, RectTransform contentRt, int kant)
    {
        RectTransform viewportRt = scrollRt.Find("Viewport") as RectTransform;

        // Kant A of B kan tijdens het openen nog inactief zijn. Wacht kort op de echte
        // Canvas-layout, zodat de cellen niet met een kleine fallbackbreedte worden gebouwd.
        for (int poging = 0; poging < 30; poging++)
        {
            Canvas.ForceUpdateCanvases();
            if (viewportRt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRt);
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRt);

            float gemeten = viewportRt != null ? viewportRt.rect.width : scrollRt.rect.width;
            if (gemeten < 100f && scrollRt.parent is RectTransform ouderRt)
                gemeten = ouderRt.rect.width;
            if (gemeten > 100f)
                break;
            yield return null;
        }

        Canvas.ForceUpdateCanvases();
        if (viewportRt != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRt);

        float breedte = viewportRt != null ? viewportRt.rect.width : scrollRt.rect.width;
        if (breedte < 100f && scrollRt.parent is RectTransform ouderRect)
            breedte = ouderRect.rect.width;
        if (breedte < 100f)
            breedte = Mathf.Max(300f, Screen.width * 0.5f - 16f);
        breedte = Mathf.Max(300f, breedte);

        // Content blijft horizontaal gestretcht; de breedte wordt dus niet door een oude
        // scenewaarde van 600px beperkt.
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = new Vector2(0f, InventarisConfig.TotaleHoogte());
        Canvas.ForceUpdateCanvases();
        float werkelijkeBreedte = contentRt.rect.width;
        if (werkelijkeBreedte > 100f)
            breedte = werkelijkeBreedte;

        float celBreedte = (breedte - LabelBreedte) / InventarisConfig.Kolommen;

        // Rijposities: rij 0 = onderste (groot) rij; bovenste rijen komen bovenaan in de content
        float[] rijTop = new float[InventarisConfig.Rijen];
        float y = 0f;
        for (int rij = InventarisConfig.Rijen - 1; rij >= 0; rij--)
        {
            rijTop[rij] = y;
            y += InventarisConfig.RijHoogte(InventarisConfig.GrootteVanRij(rij));
        }

        // Rijnummers links
        for (int rij = 0; rij < InventarisConfig.Rijen; rij++)
        {
            string grootte = InventarisConfig.GrootteVanRij(rij);
            float hoogte = InventarisConfig.RijHoogte(grootte);

            TMP_Text nummer = MaakTekst("Rij " + (rij + 1), contentRt.transform, (rij + 1).ToString(), 16, new Color(0.7f, 0.7f, 0.7f, 1f));
            nummer.alignment = TextAlignmentOptions.Center;
            RectTransform nrRt = nummer.rectTransform;
            float labelMax = LabelBreedte / breedte;
            nrRt.anchorMin = new Vector2(0f, 1f);
            nrRt.anchorMax = new Vector2(labelMax, 1f);
            nrRt.pivot = new Vector2(0.5f, 1f);
            nrRt.offsetMin = new Vector2(0f, -rijTop[rij] - hoogte);
            nrRt.offsetMax = new Vector2(0f, -rijTop[rij]);
        }

        // Bakjes
        for (int rij = 0; rij < InventarisConfig.Rijen; rij++)
        {
            string grootte = InventarisConfig.GrootteVanRij(rij);
            float hoogte = InventarisConfig.RijHoogte(grootte);

            for (int kolom = 0; kolom < InventarisConfig.Kolommen; kolom++)
            {
                InventarisBakje bakje = BakjeOp(kant, kolom, rij);
                if (bakje == null) continue;

                GameObject celGo = Maak("Bakje " + (kolom + 1) + "x" + (rij + 1), contentRt.transform, typeof(RectTransform), typeof(Image), typeof(Button), typeof(VergrotenZoom));
                RectTransform celRt = celGo.GetComponent<RectTransform>();
                float celMin = (LabelBreedte + kolom * celBreedte) / breedte;
                float celMax = (LabelBreedte + (kolom + 1) * celBreedte) / breedte;
                celRt.anchorMin = new Vector2(celMin, 1f);
                celRt.anchorMax = new Vector2(celMax, 1f);
                celRt.pivot = new Vector2(0.5f, 1f);
                celRt.offsetMin = new Vector2(1f, -rijTop[rij] - hoogte + 1f);
                celRt.offsetMax = new Vector2(-1f, -rijTop[rij] - 1f);

                Image beeld = celGo.GetComponent<Image>();
                Button knop = celGo.GetComponent<Button>();
                knop.targetGraphic = beeld;

                TMP_Text celTekst = MaakTekst("Tekst", celGo.transform, "", 14, Color.white, FontStyles.Bold);
                celTekst.alignment = TextAlignmentOptions.Center;
                celTekst.textWrappingMode = TextWrappingModes.NoWrap;
                celTekst.raycastTarget = false;
                RectTransform tRt = celTekst.rectTransform;
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.offsetMin = Vector2.zero;
                tRt.offsetMax = Vector2.zero;

                // Plaatje van het toegewezen blokje (bovenop de tekst als achtervang)
                GameObject plaatjeGo = Maak("Plaatje", celGo.transform, typeof(RectTransform), typeof(Image));
                Image plaatje = plaatjeGo.GetComponent<Image>();
                plaatje.preserveAspect = true;
                plaatje.raycastTarget = false;
                RectTransform pRt = plaatje.rectTransform;
                pRt.anchorMin = new Vector2(0.08f, 0.08f);
                pRt.anchorMax = new Vector2(0.92f, 0.92f);
                pRt.pivot = new Vector2(0.5f, 0.5f);
                pRt.offsetMin = Vector2.zero;
                pRt.offsetMax = Vector2.zero;
                plaatjeGo.SetActive(false);

                InventarisCel cel = new InventarisCel { bakje = bakje, knop = knop, beeld = beeld, plaatje = plaatje, tekst = celTekst };
                cellen.Add(cel);

                InventarisBakje gevangen = bakje;
                knop.onClick.AddListener(() => SelecteerBakje(gevangen));
            }
        }

        VernieuwAlleCellen();
    }

    private void BouwBediening(RectTransform kolom)
    {
        // Geselecteerd bakje
        TMP_Text bakjeLabel = MaakTekst("BakjeLabel", kolom, "Geselecteerd bakje:", 26, new Color(0.8f, 0.8f, 0.8f, 1f));
        PlaatsProportioneel(bakjeLabel.rectTransform, 0.0078f, 0.0203f);

        bakjeInfoTekst = MaakTekst("BakjeInfo", kolom, "—", 34, Color.white, FontStyles.Bold);
        bakjeInfoTekst.alignment = TextAlignmentOptions.Center;
        PlaatsProportioneel(bakjeInfoTekst.rectTransform, 0.0328f, 0.0266f);

        TMP_Text gekozenLabel = MaakTekst("GekozenLabel", kolom, "Gekozen blokje:", 26, new Color(0.8f, 0.8f, 0.8f, 1f));
        PlaatsProportioneel(gekozenLabel.rectTransform, 0.0641f, 0.0203f);

        gekozenBlokjeTekst = MaakTekst("GekozenBlokje", kolom, "—", 28, new Color(0.5f, 0.9f, 0.6f, 1f));
        gekozenBlokjeTekst.alignment = TextAlignmentOptions.Center;
        PlaatsProportioneel(gekozenBlokjeTekst.rectTransform, 0.0875f, 0.0219f);

        // Zoekveld
        TMP_Text zoekLabel = MaakTekst("ZoekLabel", kolom, "Zoek blokje:", 26, new Color(0.8f, 0.8f, 0.8f, 1f));
        PlaatsProportioneel(zoekLabel.rectTransform, 0.1188f, 0.0203f);

        zoekInput = MaakInputField(kolom, "Zoekveld", "Typ part-nummer of naam...");
        PlaatsProportioneel(zoekInput.GetComponent<RectTransform>(), 0.1438f, 0.0344f);
        // Zoeken gebeurt bewust alleen via TMP_InputField.onSubmit (Enter).

        // Resultatenlijst (scrollbaar)
        GameObject resultatenScroll = Maak("Resultaten", kolom, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        RectTransform rsRt = resultatenScroll.GetComponent<RectTransform>();
        PlaatsProportioneel(rsRt, 0.2281f, 0.125f);
        resultatenScroll.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.16f, 1f);

        ScrollRect rsScroll = resultatenScroll.GetComponent<ScrollRect>();
        rsScroll.movementType = ScrollRect.MovementType.Clamped;
        rsScroll.vertical = true;
        rsScroll.horizontal = false;

        GameObject rsViewport = Maak("Viewport", resultatenScroll.transform, typeof(RectTransform), typeof(RectMask2D));
        RectTransform rsViewportRt = rsViewport.GetComponent<RectTransform>();
        Stretch(rsViewportRt);
        rsScroll.viewport = rsViewportRt;

        GameObject rsContent = Maak("Content", rsViewport.transform, typeof(RectTransform));
        resultatenContainer = rsContent.GetComponent<RectTransform>();
        resultatenContainer.anchorMin = new Vector2(0f, 1f);
        resultatenContainer.anchorMax = new Vector2(1f, 1f);
        resultatenContainer.pivot = new Vector2(0.5f, 1f);
        resultatenContainer.sizeDelta = new Vector2(0f, 0f);
        resultatenContainer.anchoredPosition = Vector2.zero;
        rsScroll.content = resultatenContainer;

        // Zoekgeschiedenis-chips (populair + recent)
        GameObject chipsGo = Maak("ZoekChips", kolom, typeof(RectTransform));
        RectTransform chipsRt = chipsGo.GetComponent<RectTransform>();
        PlaatsProportioneel(chipsRt, 0.3242f, 0.0469f);

        chipsPopulairContainer = MaakChipRij("Populair", chipsRt, new Vector2(0f, -15f));
        chipsRecentContainer = MaakChipRij("Recent", chipsRt, new Vector2(0f, -43f));

        // "Toon meer"-knop onder de resultatenlijst
        toonMeerKnop = MaakKnop("ToonMeer", kolom, "Toon meer", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -478f), new Vector2(220f, 38f), new Color(0.2f, 0.35f, 0.6f, 1f));
        PlaatsProportioneel(toonMeerKnop.GetComponent<RectTransform>(), 0.3734f, 0.0297f);
        toonMeerKnop.onClick.AddListener(ToonMeerKlikken);
        toonMeerKnop.gameObject.SetActive(false);

        // Kleine legende voor de match-kwaliteit-symbolen
        TMP_Text legende = MaakTekst("Legende", kolom, "★ exact    ▶ begint met    • bevat", 18, new Color(0.7f, 0.75f, 0.85f, 1f));
        PlaatsProportioneel(legende.rectTransform, 0.4047f, 0.0141f);
        legende.alignment = TextAlignmentOptions.Center;

        HerlaadZoekChips();

        // Modus
        TMP_Text modusLabel = MaakTekst("ModusLabel", kolom, "Modus:", 26, new Color(0.8f, 0.8f, 0.8f, 1f));
        PlaatsProportioneel(modusLabel.rectTransform, 0.4219f, 0.0203f);

        modusAlleKnop = MaakKnop("AlleKleuren", kolom, "Alle kleuren", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -575f), new Vector2(170f, 44f), new Color(0.2f, 0.5f, 0.25f, 1f));
        PlaatsProportioneel(modusAlleKnop.GetComponent<RectTransform>(), 0.4492f, 0.0344f);
        modusAlleKnop.onClick.AddListener(() => KiesModus("alle"));

        modusKleurKnop = MaakKnop("DezeKleur", kolom, "Deze kleur", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -625f), new Vector2(170f, 44f), new Color(0.25f, 0.25f, 0.5f, 1f));
        PlaatsProportioneel(modusKleurKnop.GetComponent<RectTransform>(), 0.4883f, 0.0344f);
        modusKleurKnop.onClick.AddListener(() => KiesModus("kleur"));

        modusCategorieKnop = MaakKnop("Categorie", kolom, "Categorie", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -675f), new Vector2(170f, 44f), new Color(0.5f, 0.25f, 0.5f, 1f));
        PlaatsProportioneel(modusCategorieKnop.GetComponent<RectTransform>(), 0.5273f, 0.0344f);
        modusCategorieKnop.onClick.AddListener(() => KiesModus("categorie"));

        // Kleurenlijst (alleen zichtbaar bij modus "kleur")
        kleurLijstGo = Maak("KleurLijst", kolom, typeof(RectTransform));
        kleurLijstContainer = kleurLijstGo.GetComponent<RectTransform>();
        PlaatsProportioneel(kleurLijstContainer, 0.5703f, 0.0938f);
        kleurLijstGo.SetActive(false);

        // Actieknoppen
        Button toewijsKnop = MaakKnop("Toewijzen", kolom, "Toewijzen", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-95f, -880f), new Vector2(180f, 56f), new Color(0.15f, 0.6f, 0.2f, 1f));
        PlaatsProportioneel(toewijsKnop.GetComponent<RectTransform>(), 0.6875f, 0.0438f, 0.04f, 0.49f);
        toewijsKnop.onClick.AddListener(PasToeOpBakje);

        Button leegKnop = MaakKnop("Leegmaken", kolom, "Leegmaken", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(95f, -880f), new Vector2(180f, 56f), new Color(0.6f, 0.35f, 0.15f, 1f));
        PlaatsProportioneel(leegKnop.GetComponent<RectTransform>(), 0.6875f, 0.0438f, 0.51f, 0.96f);
        leegKnop.onClick.AddListener(MaakBakjeLeeg);

        Button autoKnop = MaakKnop("AutoVul", kolom, "Auto-vul", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-95f, -945f), new Vector2(180f, 56f), new Color(0.2f, 0.35f, 0.7f, 1f));
        PlaatsProportioneel(autoKnop.GetComponent<RectTransform>(), 0.7383f, 0.0438f, 0.04f, 0.49f);
        autoKnop.onClick.AddListener(AutoVullen);

        Button allesLeegKnop = MaakKnop("AllesLeeg", kolom, "Alles leeg", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(95f, -945f), new Vector2(180f, 56f), new Color(0.5f, 0.15f, 0.15f, 1f));
        PlaatsProportioneel(allesLeegKnop.GetComponent<RectTransform>(), 0.7383f, 0.0438f, 0.51f, 0.96f);
        allesLeegKnop.onClick.AddListener(AllesLeegmaken);

        // Suggesties
        suggestiesTitel = MaakTekst("SuggestiesTitel", kolom, "Suggesties:", 26, new Color(0.8f, 0.8f, 0.8f, 1f));
        PlaatsProportioneel(suggestiesTitel.rectTransform, 0.793f, 0.0203f);

        suggestiesContainer = Maak("Suggesties", kolom, typeof(RectTransform)).GetComponent<RectTransform>();
        PlaatsProportioneel(suggestiesContainer, 0.8359f, 0.125f);

        for (int i = 0; i < AantalSuggesties; i++)
        {
            int index = i;
            Button knop = MaakKnop("Suggestie " + i, suggestiesContainer, "", new Vector2(0.02f, 1f), new Vector2(0.98f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -i * 44f), new Vector2(0f, 40f), new Color(0.2f, 0.35f, 0.45f, 1f));
            knop.onClick.AddListener(() => KiesSuggestie(index));
            suggestieKnoppen.Add(knop);
        }

        UpdateModusKnoppen();
        VernieuwInformatie();
    }

    private TMP_InputField MaakInputField(Transform ouder, string naam, string placeholderTekst)
    {
        GameObject go = Maak(naam, ouder, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        go.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.25f, 1f);

        TMP_InputField input = go.GetComponent<TMP_InputField>();

        GameObject textArea = Maak("Text Area", go.transform, typeof(RectTransform), typeof(RectMask2D));
        RectTransform taRt = textArea.GetComponent<RectTransform>();
        Stretch(taRt);

        TMP_Text text = MaakTekst("Text", textArea.transform, "", 26, Color.white);
        Stretch(text.rectTransform);

        zoekPlaceholder = MaakTekst("Placeholder", textArea.transform, placeholderTekst, 26, new Color(0.6f, 0.6f, 0.6f, 1f));
        Stretch(zoekPlaceholder.rectTransform);

        input.textViewport = taRt;
        input.textComponent = text;
        input.placeholder = zoekPlaceholder;
        input.characterLimit = 80;

        return input;
    }

    #endregion

    #region --- BAKJES & CEL WEERGAVE ---

    private InventarisBakje BakjeOp(int kant, int kolom, int rij)
    {
        foreach (var b in bakjes)
        {
            if (b.kant == kant && b.kolom == kolom && b.rij == rij)
                return b;
        }

        // Niet gevonden (bijv. oud bestand): maak aan
        InventarisBakje nieuw = new InventarisBakje
        {
            kant = kant,
            kolom = kolom,
            rij = rij,
            grootte = InventarisConfig.GrootteVanRij(rij)
        };
        bakjes.Add(nieuw);
        return nieuw;
    }

    private void VernieuwAlleCellen()
    {
        foreach (var cel in cellen)
        {
            KleurCel(cel);
            LaadPlaatjeVoorCel(cel);
        }
    }

    private void KleurCel(InventarisCel cel)
    {
        bool geselecteerd = geselecteerdBakje != null &&
                            cel.bakje.kant == geselecteerdBakje.kant &&
                            cel.bakje.kolom == geselecteerdBakje.kolom &&
                            cel.bakje.rij == geselecteerdBakje.rij;

        if (geselecteerd)
        {
            cel.beeld.color = new Color(1f, 0.85f, 0.2f, 1f);
        }
        else if (cel.bakje.IsLeeg())
        {
            cel.beeld.color = new Color(0.28f, 0.28f, 0.34f, 1f);
        }
        else
        {
            switch (cel.bakje.modus)
            {
                case "kleur": cel.beeld.color = new Color(0.2f, 0.35f, 0.75f, 1f); break;
                case "categorie": cel.beeld.color = new Color(0.6f, 0.3f, 0.65f, 1f); break;
                default: cel.beeld.color = new Color(0.2f, 0.55f, 0.3f, 1f); break;
            }
        }

    }

    private void LaadPlaatjeVoorCel(InventarisCel cel)
    {
        if (cel.plaatje == null) return;

        if (!afbeeldingenLaden || cel.bakje.IsLeeg())
        {
            cel.plaatje.gameObject.SetActive(false);
            cel.plaatje.sprite = null;
            if (cel.tekst != null)
            {
                cel.tekst.text = cel.bakje.IsLeeg() ? "" : cel.bakje.part_num;
                cel.tekst.gameObject.SetActive(true);
            }
            return;
        }

        string num = cel.bakje.part_num;
        cel.verwachtPartNummer = num;

        if (cel.tekst != null)
        {
            cel.tekst.text = num;
            cel.tekst.gameObject.SetActive(true);
        }

        if (!partOpNummer.TryGetValue(num, out LegoPart part))
            return;

        // 1) Lokaal gedownloade top-400 plaatjes
        if (!string.IsNullOrEmpty(part.lokaalPad))
        {
            Sprite lokaal = Resources.Load<Sprite>(part.lokaalPad);
            if (lokaal != null)
            {
                ToonSpriteInCel(cel, lokaal);
                return;
            }
        }

        // 2) Al eerder gedownload (cache)
        if (spriteCache.TryGetValue(num, out Sprite cache))
        {
            if (cache != null)
            {
                ToonSpriteInCel(cel, cache);
            }
            return;
        }

        // 3) Via de URL downloaden, maar nooit te veel tegelijk
        if (string.IsNullOrEmpty(part.img_url)) return;
        if (bezigMetDownloaden.Contains(num)) return;

        bezigMetDownloaden.Add(num);
        downloadQueue.Enqueue(cel);
        StartDownloadIndienMogelijk();
    }

    private void StartDownloadIndienMogelijk()
    {
        while (actieveDownloads < MaxGelijktijdigeDownloads && downloadQueue.Count > 0)
        {
            InventarisCel cel = downloadQueue.Dequeue();
            StartCoroutine(DownloadPlaatje(cel));
            actieveDownloads++;
        }
    }

    private System.Collections.IEnumerator DownloadPlaatje(InventarisCel cel)
    {
        string num = cel.bakje.part_num;
        LegoPart part = partOpNummer.TryGetValue(num, out LegoPart p) ? p : null;
        string url = part != null ? part.img_url : "";

        if (!string.IsNullOrEmpty(url))
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D tex = DownloadHandlerTexture.GetContent(request);
                    if (tex != null)
                    {
                        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        spriteCache[num] = sprite;

                        // Deze cel direct bijwerken; andere cellen met hetzelfde blokje via VernieuwAlleCellen
                        ToonSpriteInCel(cel, sprite);
                        VernieuwAlleCellen();
                    }
                }
            }
        }

        bezigMetDownloaden.Remove(num);
        actieveDownloads--;
        StartDownloadIndienMogelijk();
    }

    private void ToonSpriteInCel(InventarisCel cel, Sprite sprite)
    {
        if (cel == null || cel.plaatje == null || !afbeeldingenLaden) return;

        // Cel is inmiddels gewijzigd of leeg -> niet meer tonen
        if (cel.bakje.IsLeeg() || cel.bakje.part_num != cel.verwachtPartNummer) return;

        cel.plaatje.sprite = sprite;
        cel.plaatje.gameObject.SetActive(true);

        if (cel.tekst != null)
            cel.tekst.gameObject.SetActive(false);
    }

    #endregion

    #region --- INTERACTIE ---

    private void SelecteerBakje(InventarisBakje bakje)
    {
        if (bakje == null) return;
        actieveKant = bakje.kant;
        PlayerPrefs.SetInt(ActieveKantKey, actieveKant);
        PlayerPrefs.Save();
        geselecteerdBakje = bakje;
        BewaarGeselecteerdBakje();
        VernieuwAlleCellen();

        // Zoeken koppelen aan de grootte van het geselecteerde bakje
        zoekGrootte = bakje.grootte;
        UpdateZoekPlaceholder();
        // De zoekterm blijft staan; opnieuw zoeken gebeurt alleen na Enter.
        ToonZoekResultaten();

        // Bestaande toewijzing inladen in de bediening
        LaadSelectieUitBakje();

        UpdateModusKnoppen();
        VernieuwInformatie();
        ToonKleurLijst();
        ToonSuggesties();
    }

    private void OnZoekVeranderd(string waarde)
    {
        zoekTerm = waarde;
        zoekResultaten.Clear();
        zichtbaarResultaten = MaxResultaten;

        if (string.IsNullOrWhiteSpace(zoekTerm))
        {
            ToonZoekResultaten();
            return;
        }

        string term = zoekTerm.ToLower().Trim();

        // Slim scoren: exacte en prefix-matches eerst, daarna woord- en substring-matches
        List<ZoekResultaat> gescoord = new List<ZoekResultaat>();

        foreach (var part in database.parts)
        {
            if (part == null) continue;

            // Alleen blokjes die in een bakje van deze grootte passen
            if (!string.IsNullOrEmpty(zoekGrootte) && InventarisSortering.BepaalGrootte(part) != zoekGrootte) continue;

            int score = ZoekHelper.ScoreVoorZoekterm(part, term);
            if (score <= 0) continue;

            gescoord.Add(new ZoekResultaat { part = part, score = score });
        }

        gescoord.Sort(CompareZoekResultaten);

        foreach (var r in gescoord)
        {
            zoekResultaten.Add(r.part);
        }

        ToonZoekResultaten();
    }

    private static int CompareZoekResultaten(ZoekResultaat a, ZoekResultaat b)
    {
        if (a.score != b.score) return b.score.CompareTo(a.score);

        int va = a.part.TotaalAantal();
        int vb = b.part.TotaalAantal();
        if (va != vb) return vb.CompareTo(va);

        int la = a.part.part_num != null ? a.part.part_num.Length : 0;
        int lb = b.part.part_num != null ? b.part.part_num.Length : 0;
        if (la != lb) return la.CompareTo(lb);

        return string.Compare(a.part.part_num, b.part.part_num, System.StringComparison.Ordinal);
    }

    private void UpdateZoekPlaceholder()
    {
        if (zoekPlaceholder == null) return;

        zoekPlaceholder.text = string.IsNullOrEmpty(zoekGrootte)
            ? "Typ part-nummer of naam..."
            : "Zoek blokje (" + zoekGrootte + " bakje)...";
    }

    private void ToonZoekResultaten()
    {
        if (resultatenContainer == null) return;

        int teTonen = Mathf.Min(zichtbaarResultaten, zoekResultaten.Count);

        ZorgVoorResultaatRijen(teTonen);

        for (int i = 0; i < resultaatRijen.Count; i++)
        {
            if (i < teTonen)
            {
                LegoPart part = zoekResultaten[i];
                BlokjeRijItem prefabItem = i < resultaatPrefabItems.Count ? resultaatPrefabItems[i] : null;
                if (prefabItem != null)
                {
                    // Activeer eerst: de prefab mag pas daarna een eventuele afbeelding-coroutine starten.
                    resultaatRijen[i].SetActive(true);
                    prefabItem.SetupVoorInventaris(part, zoekTerm, afbeeldingenLaden, KiesResultaatPart);
                }
                else
                {
                    TextMeshProUGUI txt = resultaatRijen[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null)
                    {
                        MatchKwaliteit kwal = ZoekHelper.Kwaliteit(part, zoekTerm);
                        string hex = ColorUtility.ToHtmlStringRGB(ZoekHelper.KleurVoor(kwal));
                        txt.text = $"<color=#{hex}>{ZoekHelper.SymboolVoor(kwal)}</color> {part.part_num} - {part.name}";
                    }
                }
                resultaatRijen[i].SetActive(true);
            }
            else
            {
                resultaatRijen[i].SetActive(false);
            }
        }

        // Pas nadat de zichtbare prefabrijen actief zijn, kan Unity hun volledige breedte
        // uit de actuele viewport berekenen.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(resultatenContainer);
        UpdateToonMeerKnop();
    }

    private void UpdateToonMeerKnop()
    {
        if (toonMeerKnop == null) return;

        int zichtbaar = Mathf.Min(zichtbaarResultaten, zoekResultaten.Count);
        int nogTeTonen = zoekResultaten.Count - zichtbaar;
        bool heeftMeer = nogTeTonen > 0;

        toonMeerKnop.gameObject.SetActive(heeftMeer);

        if (heeftMeer)
        {
            TextMeshProUGUI txt = toonMeerKnop.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = "Toon meer (" + nogTeTonen + ")";
            }
        }
    }

    private void ToonMeerKlikken()
    {
        zichtbaarResultaten += 30;
        ToonZoekResultaten();
    }

    private void ZorgVoorResultaatRijen(int aantal)
    {
        if (resultatenContainer == null) return;

        while (resultaatRijen.Count < aantal)
        {
            int index = resultaatRijen.Count;
            GameObject rij;

            if (rijPrefab != null)
            {
                rij = Instantiate(rijPrefab, resultatenContainer, false);
                ConfigureerInventarisRij(rij, index);

                BlokjeRijItem item = rij.GetComponent<BlokjeRijItem>();
                if (item != null)
                {
                    resultaatPrefabItems.Add(item);
                }
                else
                {
                    Debug.LogWarning("[Inventaris] rijPrefab heeft geen BlokjeRijItem; resultaat valt terug op tekstknop.");
                }
            }
            else
            {
                rij = MaakKnop("Resultaat " + index, resultatenContainer, "", new Vector2(0.02f, 1f), new Vector2(0.98f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -index * 34f), new Vector2(0f, 32f), new Color(0.18f, 0.22f, 0.3f, 1f)).gameObject;
                int resultaatIndex = index;
                rij.GetComponent<Button>().onClick.AddListener(() => KiesResultaatPart(zoekResultaten[resultaatIndex]));
            }

            rij.SetActive(false);
            resultaatRijen.Add(rij);
        }
    }

    private void ConfigureerResultatenLayout()
    {
        if (resultatenContainer == null) return;

        RectTransform content = resultatenContainer;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null) layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = ResultaatRijTussenruimte;
        layout.padding = new RectOffset(8, 8, 8, 8);

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static void ConfigureerInventarisRij(GameObject rij, int index)
    {
        if (rij == null) return;

        RectTransform rijRt = rij.GetComponent<RectTransform>();
        if (rijRt != null)
        {
            // De VerticalLayoutGroup beheert positie en breedte; de rij krijgt alleen een vaste hoogte.
            rijRt.anchorMin = new Vector2(0f, 1f);
            rijRt.anchorMax = new Vector2(1f, 1f);
            rijRt.pivot = new Vector2(0.5f, 1f);
            rijRt.anchoredPosition = Vector2.zero;
            rijRt.sizeDelta = new Vector2(0f, ResultaatRijHoogte);
            rijRt.localScale = Vector3.one;
        }

        LayoutElement element = rij.GetComponent<LayoutElement>();
        if (element == null) element = rij.AddComponent<LayoutElement>();
        element.minHeight = ResultaatRijHoogte;
        element.preferredHeight = ResultaatRijHoogte;
        element.flexibleHeight = 0f;
        element.minWidth = 0f;
        element.preferredWidth = 0f;
        element.flexibleWidth = 1f;

        // Panel.prefab is ontworpen voor een veel breder scherm. De inventaris gebruikt dezelfde
        // prefab, maar de inhoud moet in de beschikbare rijbreedte worden verdeeld.
        BlokjeRijItem item = rij.GetComponent<BlokjeRijItem>();
        if (item != null)
            item.StelInventarisLayoutIn(ResultaatRijHoogte);
    }

    private void KiesResultaatPart(LegoPart part)
    {
        if (part == null) return;

        gekozenPart = part;
        gekozenKleur = null;
        modus = "alle";
        UpdateModusKnoppen();
        ToonKleurLijst();
        PasToeOpBakje();

        // Gebruikte zoekterm onthouden voor geschiedenis en populaire termen
        ZoekGeschiedenis.Registreer(zoekTerm);
        HerlaadZoekChips();
    }

    private void WisselAfbeeldingen()
    {
        afbeeldingenLaden = !afbeeldingenLaden;
        downloadQueue.Clear();
        VernieuwAlleCellen();
        ToonZoekResultaten();
        UpdateAfbeeldingenKnop();
    }

    public void ZetAfbeeldingenAan(bool aan)
    {
        if (afbeeldingenLaden == aan) return;
        afbeeldingenLaden = aan;
        if (!aan)
        {
            downloadQueue.Clear();
            bezigMetDownloaden.Clear();
        }
        VernieuwAlleCellen();
        ToonZoekResultaten();
        UpdateAfbeeldingenKnop();
    }

    private void UpdateAfbeeldingenKnop()
    {
        if (afbeeldingenKnopTekst != null)
            afbeeldingenKnopTekst.text = afbeeldingenLaden ? "Plaatjes: aan" : "Plaatjes: uit";
        if (afbeeldingenKnop != null)
        {
            Image beeld = afbeeldingenKnop.GetComponent<Image>();
            if (beeld != null)
                beeld.color = afbeeldingenLaden ? new Color(0.18f, 0.58f, 0.28f, 1f) : new Color(0.25f, 0.3f, 0.35f, 1f);
        }
    }

    private void ConfigureerResultatenScrollRect(Transform bediening)
    {
        Transform resultaten = VindKind(bediening, "Resultaten");
        if (resultaten == null) return;

        Transform viewport = VindKind(resultaten, "Viewport");
        RectTransform content = viewport != null ? VindRect(viewport, "Content") : null;
        ScrollRect scrollRect = resultaten.GetComponent<ScrollRect>();

        if (scrollRect == null)
        {
            scrollRect = resultaten.gameObject.AddComponent<ScrollRect>();
            Debug.LogWarning("[Inventaris] ScrollRect ontbrak op Resultaten; component automatisch hersteld.");
        }

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.viewport = viewport as RectTransform;
        scrollRect.content = content;
    }

    private static void ZorgVoorClipping(Transform doel)
    {
        if (doel == null) return;

        RectTransform rect = doel as RectTransform;
        if (rect == null) return;

        RectMask2D mask = doel.GetComponent<RectMask2D>();
        if (mask == null)
            mask = doel.gameObject.AddComponent<RectMask2D>();
        mask.enabled = true;
    }

    private static Transform VindKind(Transform ouder, params string[] namen)
    {
        if (ouder == null) return null;
        foreach (string naam in namen)
        {
            Transform gevonden = ouder.Find(naam);
            if (gevonden != null) return gevonden;
        }
        return null;
    }

    private static GameObject VindObject(Transform ouder, string pad)
    {
        Transform gevonden = ouder != null ? ouder.Find(pad) : null;
        return gevonden != null ? gevonden.gameObject : null;
    }

    private static RectTransform VindRect(Transform ouder, string pad)
    {
        GameObject gevonden = VindObject(ouder, pad);
        return gevonden != null ? gevonden.GetComponent<RectTransform>() : null;
    }

    private static T VindComponent<T>(Transform ouder, string pad) where T : Component
    {
        GameObject gevonden = VindObject(ouder, pad);
        return gevonden != null ? gevonden.GetComponent<T>() : null;
    }

    private static TMP_Text VindTekst(Transform ouder, string pad)
    {
        return VindComponent<TMP_Text>(ouder, pad);
    }

    private Transform MaakChipRij(string naam, RectTransform ouder, Vector2 positie)
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

        List<string> populair = ZoekGeschiedenis.Populair(3);
        List<string> recent = ZoekGeschiedenis.Recente(3);

        // Recente termen die ook populair zijn niet dubbel tonen
        List<string> recentSchoon = new List<string>();
        foreach (string t in recent)
        {
            if (!populair.Contains(t)) recentSchoon.Add(t);
        }

        ZoekChips.Vul(chipsPopulairContainer, populair, KlikZoekChip, 110f);
        ZoekChips.Vul(chipsRecentContainer, recentSchoon, KlikZoekChip, 110f);
    }

    private void KlikZoekChip(string term)
    {
        // Een chip vult alleen het zoekveld; de filter start pas na Enter.
        if (zoekInput != null)
            zoekInput.text = term;
    }

    private void KiesModus(string nieuweModus)
    {
        modus = nieuweModus;

        if (modus != "kleur")
        {
            gekozenKleur = null;
        }

        UpdateModusKnoppen();
        ToonKleurLijst();
        PasToeOpBakje();
    }

    private void UpdateModusKnoppen()
    {
        if (modusAlleKnop != null)
            modusAlleKnop.GetComponent<Image>().color = modus == "alle" ? new Color(0.15f, 0.75f, 0.25f, 1f) : new Color(0.25f, 0.3f, 0.35f, 1f);
        if (modusKleurKnop != null)
            modusKleurKnop.GetComponent<Image>().color = modus == "kleur" ? new Color(0.2f, 0.5f, 0.9f, 1f) : new Color(0.25f, 0.3f, 0.35f, 1f);
        if (modusCategorieKnop != null)
            modusCategorieKnop.GetComponent<Image>().color = modus == "categorie" ? new Color(0.75f, 0.3f, 0.85f, 1f) : new Color(0.25f, 0.3f, 0.35f, 1f);
    }

    private void ToonKleurLijst()
    {
        if (kleurLijstGo == null) return;

        bool zichtbaar = modus == "kleur" && gekozenPart != null &&
                         gekozenPart.beschikbareKleuren != null && gekozenPart.beschikbareKleuren.Count > 0;

        ZorgVoorClipping(kleurLijstGo.transform);
        kleurLijstGo.SetActive(zichtbaar);
        if (!zichtbaar) return;

        // Rijen opnieuw vullen
        ZorgVoorKleurRijen(MaxKleuren);

        for (int i = 0; i < kleurRijen.Count; i++)
        {
            if (i < gekozenPart.beschikbareKleuren.Count)
            {
                LegoKleurVoorraad k = gekozenPart.beschikbareKleuren[i];
                Image swatch = kleurRijen[i].transform.Find("Staaltje").GetComponent<Image>();
                swatch.color = k.kleurCode != default(Color) ? k.kleurCode : Color.white;

                TextMeshProUGUI txt = kleurRijen[i].GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = $"{k.kleurNaam} ({k.aantalInVoorraad}x)";
                }
                kleurRijen[i].SetActive(true);
            }
            else
            {
                kleurRijen[i].SetActive(false);
            }
        }
    }

    private void ZorgVoorKleurRijen(int aantal)
    {
        if (kleurLijstContainer == null) return;

        while (kleurRijen.Count < aantal)
        {
            int index = kleurRijen.Count;
            GameObject rij = Maak("Kleur " + index, kleurLijstContainer, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rt = rij.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.02f, 1f);
            rt.anchorMax = new Vector2(0.98f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 34f);
            rt.anchoredPosition = new Vector2(0f, -index * 36f);

            Image bg = rij.GetComponent<Image>();
            bg.color = new Color(0.18f, 0.22f, 0.3f, 1f);
            Button knop = rij.GetComponent<Button>();
            knop.targetGraphic = bg;

            GameObject staaltje = Maak("Staaltje", rij.transform, typeof(RectTransform), typeof(Image));
            RectTransform stRt = staaltje.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0f, 0.5f);
            stRt.anchorMax = new Vector2(0f, 0.5f);
            stRt.pivot = new Vector2(0f, 0.5f);
            stRt.sizeDelta = new Vector2(24f, 24f);
            stRt.anchoredPosition = new Vector2(6f, 0f);

            TMP_Text txt = MaakTekst("Tekst", rij.transform, "", 16, Color.white);
            RectTransform tRt = txt.rectTransform;
            tRt.anchorMin = new Vector2(0f, 0.5f);
            tRt.anchorMax = new Vector2(1f, 0.5f);
            tRt.pivot = new Vector2(0.5f, 0.5f);
            tRt.offsetMin = new Vector2(36f, 0f);
            tRt.offsetMax = new Vector2(-4f, 0f);

            knop.onClick.AddListener(() => KiesKleur(index));
            kleurRijen.Add(rij);
        }
    }

    private void KiesKleur(int index)
    {
        if (gekozenPart == null || gekozenPart.beschikbareKleuren == null) return;
        if (index < 0 || index >= gekozenPart.beschikbareKleuren.Count) return;

        gekozenKleur = gekozenPart.beschikbareKleuren[index];
        PasToeOpBakje();
    }

    private void PasToeOpBakje()
    {
        if (geselecteerdBakje == null)
        {
            VernieuwInformatie();
            return;
        }

        if (gekozenPart == null)
        {
            VernieuwInformatie();
            return;
        }

        geselecteerdBakje.part_num = gekozenPart.part_num;
        geselecteerdBakje.naam = gekozenPart.name;
        geselecteerdBakje.modus = modus;
        geselecteerdBakje.kleurNaam = modus == "kleur" && gekozenKleur != null ? gekozenKleur.kleurNaam : "";
        geselecteerdBakje.categorieId = gekozenPart.part_cat_id;
        geselecteerdBakje.categorieNaam = gekozenPart.part_cat_naam;

        OpslaanEnVernieuwen();
        ToonSuggesties();
    }

    private void MaakBakjeLeeg()
    {
        if (geselecteerdBakje == null) return;

        geselecteerdBakje.part_num = "";
        geselecteerdBakje.naam = "";
        geselecteerdBakje.modus = "alle";
        geselecteerdBakje.kleurNaam = "";
        geselecteerdBakje.categorieId = 0;
        geselecteerdBakje.categorieNaam = "";

        gekozenPart = null;
        gekozenKleur = null;
        modus = "alle";
        UpdateModusKnoppen();
        ToonKleurLijst();
        OpslaanEnVernieuwen();
    }

    private void AutoVullen()
    {
        // Per grootte: de meest gebruikte blokjes (met voorraad) eerst
        List<LegoPart> klein = new List<LegoPart>();
        List<LegoPart> medium = new List<LegoPart>();
        List<LegoPart> groot = new List<LegoPart>();

        foreach (var part in database.parts)
        {
            if (part == null || part.TotaalAantal() <= 0) continue;

            switch (InventarisSortering.BepaalGrootte(part))
            {
                case "klein": klein.Add(part); break;
                case "groot": groot.Add(part); break;
                default: medium.Add(part); break;
            }
        }

        klein.Sort((a, b) => b.TotaalAantal().CompareTo(a.TotaalAantal()));
        medium.Sort((a, b) => b.TotaalAantal().CompareTo(a.TotaalAantal()));
        groot.Sort((a, b) => b.TotaalAantal().CompareTo(a.TotaalAantal()));

        Dictionary<string, int> teller = new Dictionary<string, int>
        {
            { "klein", 0 }, { "medium", 0 }, { "groot", 0 }
        };

        // Rijen onderaan (groot) eerst vullen
        int toegewezen = 0;

        for (int kant = 0; kant < InventarisConfig.Kanten; kant++)
        {
            for (int rij = 0; rij < InventarisConfig.Rijen; rij++)
            {
                for (int kolom = 0; kolom < InventarisConfig.Kolommen; kolom++)
                {
                    InventarisBakje bakje = BakjeOp(kant, kolom, rij);
                    if (bakje == null || !bakje.IsLeeg()) continue;

                    List<LegoPart> lijst = bakje.grootte == "klein" ? klein : bakje.grootte == "groot" ? groot : medium;
                    int idx = teller[bakje.grootte];

                    if (idx >= lijst.Count) continue;

                    LegoPart part = lijst[idx];
                    teller[bakje.grootte] = idx + 1;

                    bakje.part_num = part.part_num;
                    bakje.naam = part.name;
                    bakje.modus = "alle";
                    bakje.kleurNaam = "";
                    bakje.categorieId = part.part_cat_id;
                    bakje.categorieNaam = part.part_cat_naam;

                    toegewezen++;
                }
            }
        }

        Debug.Log($"[Inventaris] Auto-vul: {toegewezen} bakjes gevuld.");
        OpslaanEnVernieuwen();
    }

    private void AllesLeegmaken()
    {
        if (!allesLeegBevestigen)
        {
            allesLeegBevestigen = true;
            allesLeegTijd = Time.time;
            return;
        }

        allesLeegBevestigen = false;

        foreach (var bakje in bakjes)
        {
            bakje.part_num = "";
            bakje.naam = "";
            bakje.modus = "alle";
            bakje.kleurNaam = "";
            bakje.categorieId = 0;
            bakje.categorieNaam = "";
        }

        gekozenPart = null;
        gekozenKleur = null;
        modus = "alle";
        UpdateModusKnoppen();
        ToonKleurLijst();
        OpslaanEnVernieuwen();
    }

    private void KiesSuggestie(int index)
    {
        if (index < 0 || index >= suggestieKnoppen.Count) return;
        if (geselecteerdBakje == null) return;

        // Suggesties zijn gekoppeld aan de part_num in de knopnaam via de tekst
        TextMeshProUGUI txt = suggestieKnoppen[index].GetComponentInChildren<TextMeshProUGUI>();
        if (txt == null) return;

        string[] delen = txt.text.Split(' ');
        if (delen.Length < 1) return;
        string num = delen[0];

        if (partOpNummer.TryGetValue(num, out LegoPart part))
        {
            gekozenPart = part;
            gekozenKleur = null;
            modus = "alle";
            UpdateModusKnoppen();
            ToonKleurLijst();
            PasToeOpBakje();
        }
    }

    private void ToonSuggesties()
    {
        if (suggestiesContainer == null) return;

        string grootte = geselecteerdBakje != null ? geselecteerdBakje.grootte : "medium";

        if (suggestiesTitel != null)
        {
            suggestiesTitel.text = "Suggesties (" + grootte + " bakje):";
        }

        List<LegoPart> suggesties = new List<LegoPart>();

        // Eerst wat kandidaten verzamelen, dan sorteren en de top 4 tonen
        foreach (var part in database.parts)
        {
            if (part == null || part.TotaalAantal() <= 0) continue;
            if (InventarisSortering.BepaalGrootte(part) != grootte) continue;

            suggesties.Add(part);
            if (suggesties.Count >= 60) break;
        }

        suggesties.Sort((a, b) => b.TotaalAantal().CompareTo(a.TotaalAantal()));

        for (int i = 0; i < suggestieKnoppen.Count; i++)
        {
            TextMeshProUGUI txt = suggestieKnoppen[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txt == null) continue;

            if (i < suggesties.Count)
            {
                txt.text = $"{suggesties[i].part_num} - {suggesties[i].name}";
            }
            else
            {
                txt.text = "";
            }
        }
    }

    private void OpslaanEnVernieuwen()
    {
        InventarisOpslag.Bewaar(InventarisOpslag.Normaliseer(bakjes));
        VernieuwAlleCellen();
        VernieuwInformatie();
        BewaarGeselecteerdBakje();
    }

    private void WisselKant(int nieuweKant)
    {
        if (nieuweKant < 0 || nieuweKant >= InventarisConfig.Kanten) return;

        BewaarGeselecteerdBakje();
        actieveKant = nieuweKant;
        PlayerPrefs.SetInt(ActieveKantKey, actieveKant);
        PlayerPrefs.Save();
        HerstelGeselecteerdBakje();
        StelZichtbareKantIn(actieveKant);
        VernieuwAlleCellen();

        zoekResultaten.Clear();
        zichtbaarResultaten = MaxResultaten;
        UpdateZoekPlaceholder();
        UpdateKantKnoppen();
        ToonZoekResultaten();
        VernieuwInformatie();
        ToonSuggesties();
    }

    private void StelZichtbareKantIn(int kant)
    {
        Transform lichaam = transform.Find("Lichaam");
        if (lichaam == null) return;

        Transform kantA = lichaam.Find("Kant A");
        Transform kantB = lichaam.Find("Kant B");
        if (kantA != null) kantA.gameObject.SetActive(kant == 0);
        if (kantB != null) kantB.gameObject.SetActive(kant == 1);
    }

    private void UpdateKantKnoppen()
    {
        if (kantAKnopTekst != null) kantAKnopTekst.text = "Kant A";
        if (kantBKnopTekst != null) kantBKnopTekst.text = "Kant B";

        if (kantAKnop != null)
        {
            Image beeld = kantAKnop.GetComponent<Image>();
            if (beeld != null) beeld.color = actieveKant == 0
                ? new Color(0.18f, 0.58f, 0.28f, 1f)
                : new Color(0.25f, 0.3f, 0.35f, 1f);
        }
        if (kantBKnop != null)
        {
            Image beeld = kantBKnop.GetComponent<Image>();
            if (beeld != null) beeld.color = actieveKant == 1
                ? new Color(0.18f, 0.58f, 0.28f, 1f)
                : new Color(0.25f, 0.3f, 0.35f, 1f);
        }
    }

    private void BewaarGeselecteerdBakje()
    {
        if (geselecteerdBakje == null) return;

        string prefix = "Inventaris.Selected." + actieveKant + ".";
        PlayerPrefs.SetInt(prefix + "Heeft", 1);
        PlayerPrefs.SetInt(prefix + "Kolom", geselecteerdBakje.kolom);
        PlayerPrefs.SetInt(prefix + "Rij", geselecteerdBakje.rij);
        PlayerPrefs.Save();
    }

    private void HerstelGeselecteerdBakje()
    {
        geselecteerdBakje = null;
        zoekGrootte = "";
        string prefix = "Inventaris.Selected." + actieveKant + ".";
        if (PlayerPrefs.GetInt(prefix + "Heeft", 0) != 1) return;

        int kolom = PlayerPrefs.GetInt(prefix + "Kolom", -1);
        int rij = PlayerPrefs.GetInt(prefix + "Rij", -1);
        geselecteerdBakje = VindBakje(actieveKant, kolom, rij);
        if (geselecteerdBakje != null)
            zoekGrootte = geselecteerdBakje.grootte;
        LaadSelectieUitBakje();
    }

    private InventarisBakje VindBakje(int kant, int kolom, int rij)
    {
        foreach (InventarisBakje bakje in bakjes)
        {
            if (bakje != null && bakje.kant == kant && bakje.kolom == kolom && bakje.rij == rij)
                return bakje;
        }
        return null;
    }

    private void LaadSelectieUitBakje()
    {
        gekozenPart = null;
        gekozenKleur = null;
        modus = "alle";
        if (geselecteerdBakje == null) return;

        if (!geselecteerdBakje.IsLeeg() && partOpNummer.TryGetValue(geselecteerdBakje.part_num, out LegoPart part))
        {
            gekozenPart = part;
            modus = string.IsNullOrEmpty(geselecteerdBakje.modus) ? "alle" : geselecteerdBakje.modus;
            if (modus == "kleur" && part.beschikbareKleuren != null)
            {
                foreach (LegoKleurVoorraad kleur in part.beschikbareKleuren)
                {
                    if (kleur.kleurNaam == geselecteerdBakje.kleurNaam)
                    {
                        gekozenKleur = kleur;
                        break;
                    }
                }
            }
        }
    }

    private void VernieuwInformatie()
    {
        if (bakjeInfoTekst != null)
        {
            if (geselecteerdBakje == null)
            {
                bakjeInfoTekst.text = "Klik een bakje aan";
            }
            else
            {
                string kantNaam = geselecteerdBakje.kant == 0 ? "A" : "B";
                bakjeInfoTekst.text = $"Kant {kantNaam} • Kolom {geselecteerdBakje.kolom + 1} • Rij {geselecteerdBakje.rij + 1} ({geselecteerdBakje.grootte})";
            }
        }

        if (gekozenBlokjeTekst != null)
        {
            if (gekozenPart == null)
            {
                gekozenBlokjeTekst.text = geselecteerdBakje != null && !geselecteerdBakje.IsLeeg()
                    ? geselecteerdBakje.part_num + " - " + geselecteerdBakje.naam
                    : "—";
            }
            else
            {
                gekozenBlokjeTekst.text = $"{gekozenPart.part_num} - {gekozenPart.name}";
            }
        }
    }

    private void Sluit()
    {
        gameObject.SetActive(false);

        // FindObjectsInactive.Include: de appmanager zit op de "Analytics en beheer"-sectie
        // die verborgen is zolang de inventaris open is — zonder deze flag wordt hij niet gevonden!
        appmanager manager = FindFirstObjectByType<appmanager>(FindObjectsInactive.Include);
        if (manager != null)
        {
            manager.TerugNaarHome();
        }
    }

    #endregion

    #region --- UI HELPERS ---

    /// <summary>Plaatst een element proportioneel in zijn ouder: yTop/hoogte zijn fracties van de
    /// kolomhoogte (vanaf de bovenkant), xMin/xMax fracties van de breedte. Zo schalen alle
    /// knoppen en velden automatisch mee met de grootte van het paneel.</summary>
    private static void PlaatsProportioneel(RectTransform rt, float yTop, float hoogte, float xMin = 0.04f, float xMax = 0.96f)
    {
        rt.anchorMin = new Vector2(xMin, 1f - yTop - hoogte);
        rt.anchorMax = new Vector2(xMax, 1f - yTop);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

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

        TMP_Text label = MaakTekst("Label", go.transform, tekst, 28, Color.white, FontStyles.Bold);
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
