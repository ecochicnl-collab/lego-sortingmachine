using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class LegoKleurVoorraad
{
    public string kleurNaam;    
    public Color kleurCode;     
    public int aantalInVoorraad; 
}


[System.Serializable]
public class LegoPart
{
    public string part_num;
    public string name;
    public int part_cat_id;
    public string part_cat_naam;
    public string material;
    public string img_url;       
    public string lokaalPad;     

    
    public bool isModified;
    public bool hasSticker;
    public bool isMinifigPart;

    
    public List<LegoKleurVoorraad> beschikbareKleuren = new List<LegoKleurVoorraad>();

    
    public int TotaalAantal()
    {
        int totaal = 0;
        if (beschikbareKleuren != null)
        {
            foreach (var k in beschikbareKleuren) 
            {
                totaal += k.aantalInVoorraad;
            }
        }
        return totaal;
    }
}


[System.Serializable]
public class LegoDatabase
{
    public List<LegoPart> bricks;
}


[CreateAssetMenu(fileName = "LegoDataAsset", menuName = "LEGO/Lego Data Asset")]
public class LegoDataAsset : ScriptableObject
{
    public List<LegoPart> parts = new List<LegoPart>();
}