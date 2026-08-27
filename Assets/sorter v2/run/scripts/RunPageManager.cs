using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.IO;
using TMPro;

[System.Serializable]

public class LiveMachineData
{
    public string type;
    public string hub1_status;
    public string hub2_status;
    public string huidige_blokje;
    public string huidige_kleur;
    public int naar_bakje;
    public int blokjes_per_uur;
    public int totaal_gesorteerd;
} 
   
public class RunPageManager : MonoBehaviour
{
    [Header("UI Elementen (TextMeshPro)")]
    public TMP_Text blokNaamTekst;
    public TMP_Text kleurTekst;
    public TMP_Text bakjeTekst;
    public TMP_Text perUurTekst;
    public TMP_Text totaalTellerTekst;

    [Header("hub indicator (Images)")]
    public HubStatusIndicator hub1Indicator;
    public HubStatusIndicator hub2Indicator;

    private TcpClient client;
    private NetworkStream stream;
    private StreamReader reader;
    private Thread receiveThread;
    private bool isRunning = false;

    private string binnekomendeJson = "";
    private bool nieuweDataVerkregen = false;

    void Start()
    {
        isRunning = true;
        receiveThread = new Thread(LuisterNaarPython);
        receiveThread.Start();
    }

    void LuisterNaarPython()
    {
        while (isRunning)
        {
            try
            {
                if (client == null || !client.Connected)
                {
                    client = new TcpClient("127.0.0.1", 5005);
                    stream = client.GetStream();
                    reader = new StreamReader(stream, Encoding.UTF8);
                    Debug.Log("[Unity Socket] succes vol verbonden met Python");

                }
                string dataRegel = reader.ReadLine();
                if (dataRegel != null)
                {
                    binnekomendeJson = dataRegel;
                    nieuweDataVerkregen = true;
                }

            }
            catch
            {
                Thread.Sleep(2000);
            }
        }
    }

    void Update()
    {
        if (nieuweDataVerkregen)
        {
            UpdateRunUI(binnekomendeJson);
            nieuweDataVerkregen = false;
        }
    }

    void UpdateRunUI(string jsonData)
    {
        try
        {
            LiveMachineData data = JsonUtility.FromJson<LiveMachineData>(jsonData);

            if (data.type == "machine_update")
            {
                if (blokNaamTekst != null) blokNaamTekst.text = data.huidige_blokje;
                if (bakjeTekst != null) bakjeTekst.text = "Bakje: " + data.naar_bakje;

                if (kleurTekst != null)
                {
                    kleurTekst.text = "Kleur: " + data.huidige_kleur;

                    kleurTekst.color = GeefLegoKleur(data.huidige_kleur);
                }

                if (perUurTekst != null) perUurTekst.text = data.blokjes_per_uur.ToString();
                if (totaalTellerTekst != null) totaalTellerTekst.text = data.totaal_gesorteerd.ToString();

                if (hub1Indicator != null) hub1Indicator.UpdateStatus(data.hub1_status);
                if (hub2Indicator != null) hub2Indicator.UpdateStatus(data.hub2_status);
            }   
        }
        catch (System.Exception e)
        {
            Debug.LogError("fout bij het parsen van json: " + e.Message);
        }
    }

    Color GeefLegoKleur(string kleurNaam)
    {
        switch (kleurNaam.ToLower())
        {
            case "rood": return Color.red;
            case "blauw": return new Color(0.2f, 0.4f, 1f);
            case "geel": return Color.yellow;
            case "zwart": return Color.gray;
            case "groen": return Color.green;
            default: return Color.white;
        }
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        reader?.Close();
        stream?.Close();
        client?.Close();
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
    }
}

