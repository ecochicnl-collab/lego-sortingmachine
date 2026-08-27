using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class appmanager : MonoBehaviour
{
    public enum SpeedSetting { Invoer, Sorteren, Trilband, ValTijd }

    [Header("--- Navigate & Panels ---")]
    public GameObject homePanel;
    public GameObject analyticsPanel;
    public GameObject beheerPanel;

    [Header("--- Snelheid Waardes ---")]
    [Range(0, 100)] public float invoerSnelheid = 80f;
    [Range(0, 100)] public float sorteerSnelheid = 65f;
    [Range(0, 100)] public float trilbandSnelheid = 50f;
    [Range(0f, 1f)] public float valTijd = 0.35f;

    public SpeedSetting actieveInstelling = SpeedSetting.Invoer;

    [Header("--- Slider UI ---")]
    public Slider hoofdSlider;
    public TMP_Text sliderWaarderTekst;
    public TMP_Text actiefTitelTekst;

    [Header("--- Kaart Waarde Teksten ---")]
    public TMP_Text invoerWaardeTekst;
    public TMP_Text sorteerWaardeTekst;
    public TMP_Text trilbandWaardeTekst;
    public TMP_Text valTijdWaardeTekst;

    [Header("--- Netwerk & Telemetry ---")]
    public string piIpAdres = "127.0.0.1";
    public int piPoort = 5005;
    public TelemetryPanelManager telemetryPanel;

    private TcpClient client;
    private NetworkStream stream;
    private StreamReader reader;
    private StreamWriter writer;
    private bool isVerbonden = false;

    private string binnengekomenJson = "";
    private bool nieuweDataBeschikbaar = false;

    void Start()
    {
        LaadInstellingen();
        ZoekPanelenAutomatisch();
        VoorziePanelenVanKruisknop();
        ToonEnkelHomePanel();

        if (hoofdSlider != null)
        {
            hoofdSlider.onValueChanged.AddListener(OnSliderAangepast);
        }

        if (telemetryPanel == null)
        {
            telemetryPanel = FindFirstObjectByType<TelemetryPanelManager>();
        }

        UpdateAlleKaartTeksten();
        SelecteerSnelheidInstelling((int)SpeedSetting.Invoer);
        VerbindenMetPi();
    }

    void Update()
    {
        if (nieuweDataBeschikbaar)
        {
            nieuweDataBeschikbaar = false;

            if (telemetryPanel != null)
            {
                telemetryPanel.UpdateVanuitJson(binnengekomenJson);
            }
        }
    }

    private void LaadInstellingen()
    {
        invoerSnelheid = PlayerPrefs.GetFloat("InvoerSnelheid", 80f);
        sorteerSnelheid = PlayerPrefs.GetFloat("SorteerSnelheid", 65f);
        trilbandSnelheid = PlayerPrefs.GetFloat("TrilbandSnelheid", 50f);
        valTijd = PlayerPrefs.GetFloat("ValTijd", 0.35f);

        Debug.Log("[AppManager] Instellingen succesvol ingeladen uit PlayerPrefs");
    }

    #region --- PANEL BEHEER ---

    /// <summary>Maakt automatisch een X-knop (kruisje) rechtsboven op de beheer- en analytics-panels die TerugNaarHome aanroept.</summary>
    private void VoorziePanelenVanKruisknop()
    {
        VoegKruisknopToe(beheerPanel);
        VoegKruisknopToe(analyticsPanel);
    }

    private void VoegKruisknopToe(GameObject panel)
    {
        if (panel == null) return;
        if (panel.transform.Find("Kruisknop") != null) return;

        // Knop zelf (afbeelding + button)
        GameObject knopGo = new GameObject("Kruisknop", typeof(RectTransform), typeof(Image), typeof(Button));
        knopGo.transform.SetParent(panel.transform, false);
        knopGo.transform.SetAsLastSibling();

        Image achtergrond = knopGo.GetComponent<Image>();
        achtergrond.color = new Color(0.75f, 0.2f, 0.2f, 0.9f);

        RectTransform knopRt = knopGo.GetComponent<RectTransform>();
        knopRt.anchorMin = new Vector2(1f, 1f);
        knopRt.anchorMax = new Vector2(1f, 1f);
        knopRt.pivot = new Vector2(1f, 1f);
        knopRt.anchoredPosition = new Vector2(-20f, -20f);
        knopRt.sizeDelta = new Vector2(55f, 55f);

        // X-tekst erin
        GameObject tekstGo = new GameObject("Tekst", typeof(RectTransform), typeof(TextMeshProUGUI));
        tekstGo.transform.SetParent(knopGo.transform, false);

        TextMeshProUGUI tekst = tekstGo.GetComponent<TextMeshProUGUI>();
        tekst.text = "✕";
        tekst.fontSize = 36;
        tekst.alignment = TextAlignmentOptions.Center;
        tekst.color = Color.white;

        RectTransform tekstRt = tekstGo.GetComponent<RectTransform>();
        tekstRt.anchorMin = Vector2.zero;
        tekstRt.anchorMax = Vector2.one;
        tekstRt.offsetMin = Vector2.zero;
        tekstRt.offsetMax = Vector2.zero;

        Button knop = knopGo.GetComponent<Button>();
        knop.targetGraphic = achtergrond;
        knop.onClick.AddListener(TerugNaarHome);
    }

    /// <summary>Zoekt de panels automatisch op naam als ze nog niet in de Inspector zijn toegewezen.</summary>
    private void ZoekPanelenAutomatisch()
    {
        if (homePanel == null) homePanel = GameObject.Find("home");
        if (beheerPanel == null) beheerPanel = GameObject.Find("beheer");
        if (analyticsPanel == null)
        {
            analyticsPanel = GameObject.Find("' Analytics");
            if (analyticsPanel == null) analyticsPanel = GameObject.Find("analytics");
        }
    }

    public void ToonEnkelHomePanel()
    {
        if (homePanel != null) homePanel.SetActive(true);
        if (analyticsPanel != null) analyticsPanel.SetActive(false);
        if (beheerPanel != null) beheerPanel.SetActive(false);
    }

    private GameObject ZoekSectieInHome(string naamFragment)
    {
        if (homePanel == null) return null;
        foreach (Transform kind in homePanel.transform)
        {
            if (kind.name.Contains(naamFragment))
                return kind.gameObject;
        }
        return null;
    }

    /// <summary>Opent het beheer-tabblad: verbergt de analytics/beheer-sectie, toont beheer binnen home.</summary>
    public void OpenBeheerPanel()
    {
        ZoekPanelenAutomatisch();
        if (analyticsPanel != null) analyticsPanel.SetActive(false);
        GameObject sectie = ZoekSectieInHome("Analytics en beheer");
        if (sectie != null) sectie.SetActive(false);
        if (beheerPanel != null) beheerPanel.SetActive(true);
        if (homePanel != null) homePanel.SetActive(true);
    }

    /// <summary>Opent het analytics-tabblad: verbergt de analytics/beheer-sectie, toont analytics binnen home.</summary>
    public void OpenAnalyticsPanel()
    {
        ZoekPanelenAutomatisch();
        if (beheerPanel != null) beheerPanel.SetActive(false);
        GameObject sectie = ZoekSectieInHome("Analytics en beheer");
        if (sectie != null) sectie.SetActive(false);
        if (analyticsPanel != null) analyticsPanel.SetActive(true);
        if (homePanel != null) homePanel.SetActive(true);
    }

    /// <summary>Opent het inventaris-tabblad: verbergt analytics/beheer-sectie en panels, toont inventaris.</summary>
    public void OpenInventarisPanel()
    {
        ZoekPanelenAutomatisch();
        if (analyticsPanel != null) analyticsPanel.SetActive(false);
        if (beheerPanel != null) beheerPanel.SetActive(false);
        GameObject sectie = ZoekSectieInHome("Analytics en beheer");
        if (sectie != null) sectie.SetActive(false);
        if (homePanel != null) homePanel.SetActive(true);
        InventarisManager.Open();
    }

    /// <summary>Kleine home-knop: laadt het echte homescherm (met de knoppen naar run/sim/inventaris).</summary>
    public void TerugNaarHomeScherm()
    {
        SceneManager.LoadScene("home screen");
    }

    /// <summary>Knop in run: laadt de simulatiescene.</summary>
    public void GaNaarSimulatieScherm()
    {
        SceneManager.LoadScene("simulation");
    }

    /// <summary>Knop in run/sim: laadt de run-scene.</summary>
    public void GaNaarRunScherm()
    {
        SceneManager.LoadScene("run");
    }

    /// <summary>Kruisje/terug-knop: terug naar het homescherm met alle knoppen.</summary>
    public void TerugNaarHome()
    {
        ZoekPanelenAutomatisch();
        if (analyticsPanel != null) analyticsPanel.SetActive(false);
        if (beheerPanel != null) beheerPanel.SetActive(false);

        // Sluit de inventaris expliciet (scene-panel én dynamische overlay)
        InventarisManager inventaris = FindFirstObjectByType<InventarisManager>(FindObjectsInactive.Include);
        if (inventaris != null && inventaris.gameObject.activeSelf)
        {
            inventaris.gameObject.SetActive(false);
        }

        if (homePanel != null) homePanel.SetActive(true);
        GameObject sectie = ZoekSectieInHome("Analytics en beheer");
        if (sectie != null) sectie.SetActive(true);
    }

    private static bool IsDescendantVan(Transform ouder, GameObject kind)
    {
        if (kind == null) return false;
        Transform t = kind.transform;
        while (t != null)
        {
            if (t == ouder) return true;
            t = t.parent;
        }
        return false;
    }

    public void OpenPanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    public void SluitPanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    #endregion

    #region --- SNELHEID & SLIDER LOGICA ---

    public void SelecteerSnelheidInstelling(int index)
    {
        actieveInstelling = (SpeedSetting)index;

        switch (actieveInstelling)
        {
            case SpeedSetting.Invoer:
                InstellenSlider(0f, 100f, invoerSnelheid, " C1");
                break;
            case SpeedSetting.Sorteren:
                InstellenSlider(0f, 100f, sorteerSnelheid, "C2");
                break;
            case SpeedSetting.Trilband:
                InstellenSlider(0f, 100f, trilbandSnelheid, "C3");
                break;
            case SpeedSetting.ValTijd:
                InstellenSlider(0f, 1f, valTijd, "valtijd");
                break;
        }
    }

    private void InstellenSlider(float min, float max, float huidigeWaarde, string titel)
    {
        if (hoofdSlider == null) return;

        hoofdSlider.minValue = min;
        hoofdSlider.maxValue = max;
        hoofdSlider.value = huidigeWaarde;

        if (actiefTitelTekst != null) actiefTitelTekst.text = titel;
        UpdateSliderTekst(huidigeWaarde);
    }

    private void UpdateSliderTekst(float huidigeWaarde)
    {
        if (sliderWaarderTekst == null) return;

        if (actieveInstelling == SpeedSetting.ValTijd)
        {
            sliderWaarderTekst.text = huidigeWaarde.ToString("F2") + "s";
        }
        else
        {
            sliderWaarderTekst.text = Mathf.RoundToInt(huidigeWaarde) + "%";
        }
    }

    public void OnSliderAangepast(float nieuweWaarde)
    {
        string instellingNaam = " ";

        switch (actieveInstelling)
        {
            case SpeedSetting.Invoer:
                invoerSnelheid = nieuweWaarde;
                instellingNaam = "Invoer_Snelheid";
                PlayerPrefs.SetFloat("InvoerSnelheid", invoerSnelheid);
                if (invoerWaardeTekst != null) invoerWaardeTekst.text = Mathf.RoundToInt(nieuweWaarde) + "%";
                break;
            case SpeedSetting.Sorteren:
                sorteerSnelheid = nieuweWaarde;
                instellingNaam = "Sorteer_Snelheid";
                PlayerPrefs.SetFloat("SorteerSnelheid", sorteerSnelheid);
                if (sorteerWaardeTekst != null) sorteerWaardeTekst.text = Mathf.RoundToInt(nieuweWaarde) + "%";
                break;
            case SpeedSetting.Trilband:
                trilbandSnelheid = nieuweWaarde;
                instellingNaam = "Trilband_Snelheid";
                PlayerPrefs.SetFloat("TrilbandSnelheid", trilbandSnelheid);
                if (trilbandWaardeTekst != null) trilbandWaardeTekst.text = Mathf.RoundToInt(nieuweWaarde) + "%";
                break;
            case SpeedSetting.ValTijd:
                valTijd = nieuweWaarde;
                instellingNaam = "Val_tijd";
                PlayerPrefs.SetFloat("ValTijd", valTijd);
                if (valTijdWaardeTekst != null) valTijdWaardeTekst.text = nieuweWaarde.ToString("F2") + "s";
                break;
        }

        PlayerPrefs.Save();
        UpdateSliderTekst(nieuweWaarde);
        StuurSnelheidNaarPi(instellingNaam, nieuweWaarde);
    }

    private void UpdateAlleKaartTeksten()
    {
        if (invoerWaardeTekst != null) invoerWaardeTekst.text = Mathf.RoundToInt(invoerSnelheid) + "%";
        if (sorteerWaardeTekst != null) sorteerWaardeTekst.text = Mathf.RoundToInt(sorteerSnelheid) + "%";
        if (trilbandWaardeTekst != null) trilbandWaardeTekst.text = Mathf.RoundToInt(trilbandSnelheid) + "%";
        if (valTijdWaardeTekst != null) valTijdWaardeTekst.text = valTijd.ToString("F2") + "s";
    }

    #endregion

    #region --- COMMUNICATIE NAAR / VAN ESP32 ---

    private async void VerbindenMetPi()
    {
        try
        {
            client = new TcpClient();
            await client.ConnectAsync(piIpAdres, piPoort);
            stream = client.GetStream();
            reader = new StreamReader(stream, Encoding.UTF8);
            writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            isVerbonden = true;
            Debug.Log("Verbonden met ESP32 op " + piIpAdres + ":" + piPoort);

            _ = Task.Run(() => LeesNetwerkData());
        }
        catch (Exception e)
        {
            Debug.LogError("Kan geen verbinding maken met ESP32: " + e.Message);
        }
    }

    private async Task LeesNetwerkData()
    {
        try
        {
            while (isVerbonden && client != null && client.Connected)
            {
                string regel = await reader.ReadLineAsync();
                if (!string.IsNullOrEmpty(regel))
                {
                    binnengekomenJson = regel;
                    nieuweDataBeschikbaar = true;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AppManager] Netwerkverbinding verbroken: " + e.Message);
            isVerbonden = false;
        }
    }

    public void StuurSnelheidNaarPi(string instelling, float waarde)
    {
        if (client == null || !client.Connected)
        {
            VerbindenMetPi();
        }

        if (client != null && client.Connected && writer != null)
        {
            try
            {
                SpeedPayload payload = new SpeedPayload
                {
                    type = "control_update",
                    setting = instelling,
                    value = waarde
                };

                string json = JsonUtility.ToJson(payload);
                writer.WriteLine(json);
            }
            catch (Exception e)
            {
                Debug.LogError("[AppManager] Fout bij versturen naar ESP32: " + e.Message);
            }
        }
    }

    void OnDestroy()
    {
        isVerbonden = false;

        try { reader?.Close(); } catch { }
        try { writer?.Close(); } catch { }
        try { stream?.Close(); } catch { }
        try { client?.Close(); } catch { }
    }

    #endregion
}

[System.Serializable]
public class SpeedPayload
{
    public string type;
    public string setting;
    public float value;
}