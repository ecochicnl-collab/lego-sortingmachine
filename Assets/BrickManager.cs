using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BrickManager : MonoBehaviour
{
    public LegoDataAsset databaseAsset;

    private List<LegoPart> alleBlokjes;


    void Start()
    {
        if (databaseAsset != null)
        {
            alleBlokjes = databaseAsset.parts;
            Debug.Log("Aantal blokjes geladen: " + alleBlokjes.Count);
        }
        else
        {
            Debug.LogError("Database asset is niet toegewezen in de inspector.");
        }
    }
    public List<LegoPart> ZoekBlokjes(string zoekopdracht)
    {
        List<LegoPart> resulataten = new List<LegoPart>();
        string schoneZoekopdracht = NormaliseerTekst(zoekopdracht);

        if (string.IsNullOrEmpty(schoneZoekopdracht))
        {
            return resulataten;
        }

        foreach (LegoPart blokje in alleBlokjes)
        {
            string schoneNaam = NormaliseerTekst(blokje.name);
            if (schoneNaam.Contains(schoneZoekopdracht))
            {
                resulataten.Add(blokje);
            }
        }
        return resulataten;
    }

    private string NormaliseerTekst(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "";
        }

        string result = input.ToLower();
        result = result.Replace("bij", "x");
        result = result.Replace(" ", "");
        return result;

        

    }

    
}
