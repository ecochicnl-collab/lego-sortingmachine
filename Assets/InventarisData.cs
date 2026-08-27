using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>Eén bakje (vakje) in het inventaris-raster: welke kant, kolom en rij, en wat erin hoort.</summary>
[System.Serializable]
public class InventarisBakje
{
    public int kant;          // 0 = kant A (links), 1 = kant B (rechts)
    public int kolom;         // 0..14 (15 kolommen)
    public int rij;           // 0..24 (25 rijen), rij 0 = onderste rij
    public string grootte;    // "groot" | "medium" | "klein"
    public string part_num;   // leeg = nog niet toegewezen
    public string naam;       // naam van het toegewezen blokje (voor display)
    public string modus;      // "alle" | "kleur" | "categorie"
    public string kleurNaam;  // alleen bij modus "kleur"
    public int categorieId;   // alleen bij modus "categorie"
    public string categorieNaam;

    public bool IsLeeg()
    {
        return string.IsNullOrEmpty(part_num);
    }

    public string Beschrijving()
    {
        if (IsLeeg()) return "leeg";
        string extra = modus == "kleur" ? " (" + kleurNaam + ")" :
                       modus == "categorie" ? " (categorie)" : "";
        return part_num + extra;
    }
}

/// <summary>Vaste afmetingen van het inventaris-systeem: 2 kanten van 15 x 25 = 375 bakjes per kant.</summary>
public static class InventarisConfig
{
    public const int Kanten = 2;
    public const int Kolommen = 15;
    public const int Rijen = 25;

    public const int GroteRijen = 8;      // onderste 8 rijen = groot
    public const int MediumRijen = 7;     // daarboven 7 = medium
    public const int KleineRijen = 10;    // bovenste 10 = klein

    public static string GrootteVanRij(int rij)
    {
        if (rij < GroteRijen) return "groot";
        if (rij < GroteRijen + MediumRijen) return "medium";
        return "klein";
    }

    public static float RijHoogte(string grootte)
    {
        switch (grootte)
        {
            case "groot": return 58f;
            case "medium": return 46f;
            default: return 34f;
        }
    }

    public static float TotaleHoogte()
    {
        float totaal = 0f;
        for (int rij = 0; rij < Rijen; rij++)
        {
            totaal += RijHoogte(GrootteVanRij(rij));
        }
        return totaal;
    }
}

/// <summary>Bepaalt in welk soort bakje een blokje thuishoort, op basis van de afmetingen in de naam.</summary>
public static class InventarisSortering
{
    public static string BepaalGrootte(LegoPart part)
    {
        if (part == null) return "medium";

        string naam = part.name != null ? part.name.ToLower() : "";
        int a = 0, b = 0;

        Match m = Regex.Match(naam, @"(\d+)\s*x\s*(\d+)");
        if (m.Success)
        {
            a = int.Parse(m.Groups[1].Value);
            b = int.Parse(m.Groups[2].Value);
        }

        int oppervlak = a * b;
        if (oppervlak > 0)
        {
            // 1x1, 1x2, 2x2 -> klein; 1x4/2x4/2x3 -> medium; groter -> groot
            if (oppervlak <= 4) return "klein";
            if (oppervlak <= 8) return "medium";
            return "groot";
        }

        // Zonder afmeting in de naam: op trefwoorden schatten
        if (naam.Contains("minifig") || naam.Contains("technic pin") || naam.Contains("bar 1") ||
            naam.Contains("round 1") || naam.Contains("stud"))
            return "klein";

        if (naam.Contains("door") || naam.Contains("window") || naam.Contains("windscreen") ||
            naam.Contains("panel") || naam.Contains("baseplate"))
            return "groot";

        return "medium";
    }
}

/// <summary>Bewaart en laadt de bakjes-toewijzing als JSON in de persistent-data-map, zodat het tussen sessies blijft staan.</summary>
public static class InventarisOpslag
{
    private static HashSet<string> cachePartNummers;

    private static string Pad()
    {
        return Path.Combine(Application.persistentDataPath, "inventaris.json");
    }

    public static void Bewaar(List<InventarisBakje> bakjes)
    {
        try
        {
            InventarisData data = new InventarisData { bakjes = bakjes };
            File.WriteAllText(Pad(), JsonUtility.ToJson(data, true));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Inventaris] Opslaan mislukt: " + e.Message);
        }

        // Cache verouderd: volgende keer opnieuw laden
        cachePartNummers = null;
    }

    /// <summary>Alle part-nummers die aan een bakje in de inventaris zijn toegewezen (blokjes die in de machine zitten).</summary>
    public static HashSet<string> GeefPartNummersInInventaris()
    {
        if (cachePartNummers == null)
        {
            cachePartNummers = new HashSet<string>();
            List<InventarisBakje> bakjes = Laad();
            foreach (var b in bakjes)
            {
                if (b != null && !string.IsNullOrEmpty(b.part_num))
                    cachePartNummers.Add(b.part_num);
            }
        }
        return cachePartNummers;
    }

    public static List<InventarisBakje> Laad()
    {
        try
        {
            if (File.Exists(Pad()))
            {
                InventarisData data = JsonUtility.FromJson<InventarisData>(File.ReadAllText(Pad()));
                if (data != null && data.bakjes != null && data.bakjes.Count > 0)
                    return Normaliseer(data.bakjes);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Inventaris] Laden mislukt: " + e.Message);
        }

        return Nieuw();
    }

    /// <summary>
    /// Zorgt dat elke vaste positie op beide kanten bestaat. Oude of gedeeltelijke JSON-bestanden
    /// blijven geldig: bestaande toewijzingen worden overgenomen en ontbrekende bakjes toegevoegd.
    /// </summary>
    public static List<InventarisBakje> Normaliseer(List<InventarisBakje> opgeslagen)
    {
        Dictionary<string, InventarisBakje> bestaand = new Dictionary<string, InventarisBakje>();
        if (opgeslagen != null)
        {
            foreach (InventarisBakje bakje in opgeslagen)
            {
                if (bakje == null) continue;
                string sleutel = bakje.kant + ":" + bakje.kolom + ":" + bakje.rij;
                bestaand[sleutel] = bakje;
            }
        }

        List<InventarisBakje> compleet = Nieuw();
        foreach (InventarisBakje bakje in compleet)
        {
            string sleutel = bakje.kant + ":" + bakje.kolom + ":" + bakje.rij;
            if (!bestaand.TryGetValue(sleutel, out InventarisBakje oud)) continue;

            bakje.grootte = string.IsNullOrEmpty(oud.grootte)
                ? InventarisConfig.GrootteVanRij(bakje.rij)
                : oud.grootte;
            bakje.part_num = oud.part_num ?? "";
            bakje.naam = oud.naam ?? "";
            bakje.modus = string.IsNullOrEmpty(oud.modus) ? "alle" : oud.modus;
            bakje.kleurNaam = oud.kleurNaam ?? "";
            bakje.categorieId = oud.categorieId;
            bakje.categorieNaam = oud.categorieNaam ?? "";
        }

        return compleet;
    }

    /// <summary>Maakt alle 2 x 375 lege bakjes aan, met de juiste grootte per rij.</summary>
    public static List<InventarisBakje> Nieuw()
    {
        List<InventarisBakje> bakjes = new List<InventarisBakje>();

        for (int kant = 0; kant < InventarisConfig.Kanten; kant++)
        {
            for (int rij = 0; rij < InventarisConfig.Rijen; rij++)
            {
                for (int kolom = 0; kolom < InventarisConfig.Kolommen; kolom++)
                {
                    bakjes.Add(new InventarisBakje
                    {
                        kant = kant,
                        kolom = kolom,
                        rij = rij,
                        grootte = InventarisConfig.GrootteVanRij(rij)
                    });
                }
            }
        }

        return bakjes;
    }
}

[System.Serializable]
public class InventarisData
{
    public List<InventarisBakje> bakjes = new List<InventarisBakje>();
}
