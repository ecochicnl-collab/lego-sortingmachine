using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GridSlot : MonoBehaviour
{
    public TMP_Text slotText;
    public Image slotImage;
    public GameObject plusObject; 
    public int breedte;
    public int lengte;

    private Color origineleKleur;
    private search searchScript;
    private LegoPart gekoppeldBlokje;
    private string saveKey;

    void Start()
    {
        searchScript = Object.FindFirstObjectByType<search>();
        if (slotImage != null)
        {
            origineleKleur = slotImage.color;
        }
        
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnSlotKlik);
        }

        saveKey = "GridSlot_" + breedte + "_" + lengte;
        LoadSlotData();
    }

    public void ZetGeel()
    {
        if (slotImage != null) slotImage.color = Color.yellow;
    }

    public void ResetKleur()
    {
        if (slotImage != null) slotImage.color = origineleKleur;
    }

    public void KoppelBlokje(LegoPart part, string korteNaam)
    {
        gekoppeldBlokje = part;
        
        if (slotText != null)
        {
            slotText.text = korteNaam;
        }

        if (plusObject != null)
        {
            plusObject.SetActive(false);
        }

        if (part != null)
        {
            PlayerPrefs.SetString(saveKey, part.name);
            PlayerPrefs.Save();
            
            if (searchScript != null)
            {
                searchScript.VoegBezetBlokjeToe(part.name);
            }
        }

        ResetKleur();
    }

    private void LoadSlotData()
    {
        if (PlayerPrefs.HasKey(saveKey))
        {
            string savedName = PlayerPrefs.GetString(saveKey);
            
            if (searchScript != null)
            {
                LegoPart part = searchScript.VindBlokjeOpNaam(savedName);
                if (part != null)
                {
                    string korteNaam = FormatteerNaamStijf(part.name).Replace(" bij ", "x ");
                    gekoppeldBlokje = part;
                    
                    if (slotText != null) slotText.text = korteNaam;
                    if (plusObject != null) plusObject.SetActive(false);
                    
                    searchScript.VoegBezetBlokjeToe(part.name);
                }
            }
        }
        else
        {
            if (plusObject != null) plusObject.SetActive(true);
        }
    }

    private string FormatteerNaamStijf(string origineleNaam)
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

    void OnSlotKlik()
    {
        if (searchScript != null)
        {
            if (searchScript.isSelectieModus)
            {
                searchScript.KoppelAanSlot(this);
            }
            else
            {
                searchScript.SetGridMaat(breedte, lengte);
            }
        }
    }
}