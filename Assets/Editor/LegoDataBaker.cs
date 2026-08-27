using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

public class LegoDataBaker : EditorWindow
{
    private TextAsset partsCsv;
    private TextAsset categoriesCsv;
    private TextAsset inventoryPartsCsv;
    private TextAsset colorsCsv;

    private int maxStudGrootte = 8;
    private int topPlaatjesAantal = 400;
    private bool downloadTopPlaatjes = true;
    private string statusBericht = "Selecteer de 4 rebrickable csv-bestanden en klik op bake.";

    private const string PlaatjesMap = "Assets/Resources/BrickImages";
    private const string PlaatjesResourcesPrefix = "BrickImages/";

    [MenuItem("LEGO/Bake csv to Asset")]
    public static void ShowWindow()
    {
        GetWindow<LegoDataBaker>("LEGO CSV Data Baker");
    }

    private void OnGUI()
    {
        GUILayout.Label("LEGO CSV Database Baker & Filter Tool", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        partsCsv = (TextAsset)EditorGUILayout.ObjectField("Parts CSV (parts.csv)", partsCsv, typeof(TextAsset), false);
        categoriesCsv = (TextAsset)EditorGUILayout.ObjectField("Categories CSV (part_categories.csv)", categoriesCsv, typeof(TextAsset), false);
        inventoryPartsCsv = (TextAsset)EditorGUILayout.ObjectField("Inventory Parts CSV (inventory_parts.csv)", inventoryPartsCsv, typeof(TextAsset), false);
        colorsCsv = (TextAsset)EditorGUILayout.ObjectField("Colors CSV (colors.csv)", colorsCsv, typeof(TextAsset), false);

        EditorGUILayout.Space();
        maxStudGrootte = EditorGUILayout.IntField("Max Stud Grootte (Bakje)", maxStudGrootte);
        topPlaatjesAantal = EditorGUILayout.IntField("Aantal lokale plaatjes (top)", topPlaatjesAantal);
        downloadTopPlaatjes = EditorGUILayout.Toggle("Download plaatjes na het bakken", downloadTopPlaatjes);

        EditorGUILayout.Space();

        if (GUILayout.Button("Bake CSV naar ScriptableObject Asset", GUILayout.Height(35)))
        {
            if (partsCsv == null || categoriesCsv == null)
            {
                statusBericht = "FOUT: Selecteer in ieder geval parts.csv en part_categories.csv!";
            }
            else
            {
                VerwerkEnBakeCsvData();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(statusBericht, MessageType.Info);
    }

    private void VerwerkEnBakeCsvData()
    {
        statusBericht = "Bezig met verwerken...";
        Repaint();

        // 1) Categorieen (welke mogen er niet in = geen echt bouwblokje)
        Dictionary<string, string> categorieNaamOpId = new Dictionary<string, string>();
        HashSet<string> ongewensteCategorieIds = new HashSet<string>();
        ParseCategorieen(categorieNaamOpId, ongewensteCategorieIds);

        // 2) Kleuren uit colors.csv (color_id -> naam + kleur)
        Dictionary<int, KleurInfo> kleuren = ParseKleuren();

        // 3) Voorraad + plaatjes uit inventory_parts.csv (grote file, streamen vanaf disk)
        Dictionary<string, PartInvAgg> voorraad = ParseInventoryParts();

        // 4) Parts.csv inlezen, filteren en varianten samenvoegen
        Dictionary<string, LegoPart> schoneDatabase = new Dictionary<string, LegoPart>();
        string[] partLijnen = SplitCsvTekst(partsCsv.text);

        for (int i = 1; i < partLijnen.Length; i++)
        {
            string line = partLijnen[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] velden = SplitCsvLine(line);
            if (velden.Length < 3) continue;

            string ruwePartNummer = velden[0].Trim();
            string origineleNaam = velden[1].Trim();
            string catId = velden[2].Trim();

            if (ongewensteCategorieIds.Contains(catId)) continue;

            string naamLower = origineleNaam.ToLower();
            if (naamLower.Contains("modulex") || naamLower.Contains("sticker") ||
                naamLower.Contains("sheet") || naamLower.Contains("decal") ||
                naamLower.Contains("duplo") || naamLower.Contains("quatro") ||
                naamLower.Contains("primo") || naamLower.Contains("znap") ||
                naamLower.Contains("belville") || naamLower.Contains("scala") ||
                naamLower.Contains("fabuland") || naamLower.Contains("clikits") ||
                naamLower.Contains("ho scale") || naamLower.Contains("minifig")) continue;

            // Sticker-vellen herken je in Rebrickable ook aan "stk" in het part_num
            if (ruwePartNummer.Contains("stk")) continue;

            if (!PastInSorteerMachine(origineleNaam, maxStudGrootte)) continue;

            // a/b/c mold-varianten worden 1 blokje; bedrukte versies (pr/pat/pb/pa) blijven apart
            string sleutel = VariantSleutel(ruwePartNummer);
            if (string.IsNullOrEmpty(sleutel)) sleutel = ruwePartNummer;

            string schoneNaam = MaakSchoneNaam(origineleNaam);

            if (schoneDatabase.ContainsKey(sleutel)) continue;

            string catNaam = categorieNaamOpId.TryGetValue(catId, out string cn) ? cn : string.Empty;
            string catNaamLower = catNaam.ToLower();

            LegoPart part = new LegoPart();
            part.part_num = sleutel;
            part.name = schoneNaam;
            part.part_cat_id = int.TryParse(catId, out int parsedCatId) ? parsedCatId : 0;
            part.part_cat_naam = catNaam;
            part.material = velden.Length > 3 ? velden[3].Trim() : string.Empty;

            // "Modified" blokjes zitten in Rebrickable in de Special-categorieen (Bricks/Plates/Tiles Special)
            part.isModified = naamLower.Contains("modified") || catNaamLower.Contains("modified") ||
                              parsedCatId == 5 || parsedCatId == 9 || parsedCatId == 15;
            part.isMinifigPart = catNaamLower.Contains("minifig");
            part.hasSticker = IsPrintVariant(ruwePartNummer) ||
                              naamLower.Contains("print") ||
                              naamLower.Contains("pattern");

            if (voorraad.TryGetValue(sleutel, out PartInvAgg agg))
            {
                part.img_url = agg.BesteImgUrl;
                part.beschikbareKleuren = MaakKleurVoorraad(agg, kleuren);
            }
            else
            {
                part.img_url = string.Empty;
                part.beschikbareKleuren = new List<LegoKleurVoorraad>();
            }

            schoneDatabase.Add(sleutel, part);
        }

        List<LegoPart> opgeschoondeLijst = schoneDatabase.Values.ToList();

        // 5) Bepaal de top-N meest gebruikte blokjes en geef die een lokaal pad
        List<string> topSleutels = voorraad
            .Where(kv => schoneDatabase.ContainsKey(kv.Key))
            .OrderByDescending(kv => kv.Value.TotaalAantal)
            .Take(topPlaatjesAantal)
            .Select(kv => kv.Key)
            .ToList();

        foreach (string sleutel in topSleutels)
        {
            schoneDatabase[sleutel].lokaalPad = PlaatjesResourcesPrefix + sleutel;
        }

        // 6) Asset wegschrijven
        LegoDataAsset asset = ScriptableObject.CreateInstance<LegoDataAsset>();
        asset.parts = opgeschoondeLijst;

        string savePath = "Assets/Resources/LegoDataAsset.asset";
        if (!Directory.Exists("Assets/Resources"))
        {
            Directory.CreateDirectory("Assets/Resources");
        }

        if (File.Exists(savePath))
        {
            AssetDatabase.DeleteAsset(savePath);
        }

        AssetDatabase.CreateAsset(asset, savePath);
        AssetDatabase.SaveAssets();

        int metKleur = opgeschoondeLijst.Count(p => p.beschikbareKleuren != null && p.beschikbareKleuren.Count > 0);
        statusBericht = $"Succes! {opgeschoondeLijst.Count} unieke blokjes gebakken ({metKleur} met kleur/voorraad). Top {topSleutels.Count} krijgt een lokaal plaatje.";
        Debug.Log(statusBericht);

        // 7) Optioneel de top-N plaatjes downloaden
        if (downloadTopPlaatjes && topSleutels.Count > 0)
        {
            DownloadTopPlaatjes(schoneDatabase, topSleutels);
        }
    }

    private void ParseCategorieen(Dictionary<string, string> naamOpId, HashSet<string> ongewenst)
    {
        string[] catLijnen = SplitCsvTekst(categoriesCsv.text);

        for (int i = 1; i < catLijnen.Length; i++)
        {
            string line = catLijnen[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] velden = SplitCsvLine(line);
            if (velden.Length >= 2)
            {
                string catId = velden[0].Trim();
                string catNaam = velden[1].Trim();
                naamOpId[catId] = catNaam;

                string catNaamLower = catNaam.ToLower();
                if (catNaamLower.Contains("modulex") || catNaamLower.Contains("sticker") ||
                    catNaamLower.Contains("minifig") || catNaamLower.Contains("decal") ||
                    catNaamLower.Contains("non-buildable") || catNaamLower.Contains("sheet") ||
                    catNaamLower.Contains("duplo") || catNaamLower.Contains("quatro") ||
                    catNaamLower.Contains("primo") || catNaamLower.Contains("znap") ||
                    catNaamLower.Contains("belville") || catNaamLower.Contains("scala") ||
                    catNaamLower.Contains("fabuland") || catNaamLower.Contains("clikits") ||
                    catNaamLower.Contains("ho scale") || catNaamLower.Contains("large buildable") ||
                    catNaamLower.Contains("non-system") || catNaamLower.Contains("pen & watch"))
                {
                    ongewenst.Add(catId);
                }
            }
        }
    }

    private Dictionary<int, KleurInfo> ParseKleuren()
    {
        Dictionary<int, KleurInfo> result = new Dictionary<int, KleurInfo>();
        if (colorsCsv == null) return result;

        string[] lijnen = SplitCsvTekst(colorsCsv.text);
        for (int i = 1; i < lijnen.Length; i++)
        {
            string line = lijnen[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] velden = SplitCsvLine(line);
            if (velden.Length < 3) continue;
            if (!int.TryParse(velden[0].Trim(), out int colorId)) continue;

            KleurInfo info = new KleurInfo();
            info.naam = velden[1].Trim();
            info.kleur = ParseHexKleur(velden[2].Trim());
            result[colorId] = info;
        }
        return result;
    }

    private Dictionary<string, PartInvAgg> ParseInventoryParts()
    {
        Dictionary<string, PartInvAgg> result = new Dictionary<string, PartInvAgg>();
        if (inventoryPartsCsv == null) return result;

        string assetPath = AssetDatabase.GetAssetPath(inventoryPartsCsv);
        string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"[LegoDataBaker] inventory_parts.csv niet gevonden op {fullPath}, probeer de TextAsset opnieuw toe te wijzen.");
            return result;
        }

        using (StreamReader reader = new StreamReader(fullPath))
        {
            // header overslaan
            reader.ReadLine();

            string line;
            long teller = 0;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length == 0) continue;

                string[] velden = SplitCsvLine(line);
                if (velden.Length < 5) continue;

                string partNum = velden[1].Trim();
                if (!int.TryParse(velden[2].Trim(), out int colorId)) continue;
                if (!long.TryParse(velden[3].Trim(), out long qty)) continue;

                // Resterende onderdelen in een set tellen we niet mee als "gebruikt"
                bool isSpare = velden.Length > 4 && velden[4].Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
                if (isSpare) continue;

                string imgUrl = velden.Length > 5 ? velden[5].Trim() : string.Empty;
                int inventoryId = 0;
                int.TryParse(velden[0].Trim(), out inventoryId);

                string sleutel = VariantSleutel(partNum);
                if (string.IsNullOrEmpty(sleutel)) continue;

                if (!result.TryGetValue(sleutel, out PartInvAgg agg))
                {
                    agg = new PartInvAgg();
                    result[sleutel] = agg;
                }

                agg.TotaalAantal += qty;
                agg.Inventories.Add(inventoryId);

                if (!agg.KleurAantallen.ContainsKey(colorId)) agg.KleurAantallen[colorId] = 0;
                agg.KleurAantallen[colorId] += qty;

                if (!string.IsNullOrEmpty(imgUrl) && !agg.KleurImgUrl.ContainsKey(colorId))
                {
                    agg.KleurImgUrl[colorId] = imgUrl;
                }

                teller++;
            }

            Debug.Log($"[LegoDataBaker] inventory_parts.csv verwerkt: {teller} regels, {result.Count} unieke blokjes.");
        }

        return result;
    }

    private List<LegoKleurVoorraad> MaakKleurVoorraad(PartInvAgg agg, Dictionary<int, KleurInfo> kleuren)
    {
        List<LegoKleurVoorraad> lijst = new List<LegoKleurVoorraad>();

        foreach (var kv in agg.KleurAantallen.OrderByDescending(x => x.Value))
        {
            LegoKleurVoorraad voorraad = new LegoKleurVoorraad();
            if (kleuren.TryGetValue(kv.Key, out KleurInfo info))
            {
                voorraad.kleurNaam = info.naam;
                voorraad.kleurCode = info.kleur;
            }
            else
            {
                voorraad.kleurNaam = "Kleur " + kv.Key;
                voorraad.kleurCode = Color.white;
            }
            voorraad.aantalInVoorraad = (int)Mathf.Min(kv.Value, int.MaxValue);
            lijst.Add(voorraad);
        }

        return lijst;
    }

    private void DownloadTopPlaatjes(Dictionary<string, LegoPart> database, List<string> topSleutels)
    {
        if (!Directory.Exists(PlaatjesMap))
        {
            Directory.CreateDirectory(PlaatjesMap);
        }

        int gelukt = 0;
        int overgeslagen = 0;
        List<string> gedownloadePaden = new List<string>();

        using (HttpClient client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromSeconds(40);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "LegoSorter/1.0 (persoonlijk gebruik)");

            for (int i = 0; i < topSleutels.Count; i++)
            {
                LegoPart part = database[topSleutels[i]];
                string url = part.img_url;

                if (string.IsNullOrEmpty(url))
                {
                    overgeslagen++;
                    continue;
                }

                string extensie = BepaalExtensie(url);
                string bestand = part.part_num + extensie;
                string pad = Path.Combine(PlaatjesMap, bestand).Replace('\\', '/');

                if (File.Exists(pad))
                {
                    overgeslagen++;
                    continue;
                }

                try
                {
                    byte[] data = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
                    File.WriteAllBytes(pad, data);
                    gedownloadePaden.Add(pad);
                    gelukt++;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[LegoDataBaker] Download mislukt voor {part.part_num}: {e.Message}");
                }

                if ((i + 1) % 25 == 0 || i == topSleutels.Count - 1)
                {
                    statusBericht = $"Plaatjes downloaden... {i + 1}/{topSleutels.Count} ({gelukt} gedownload)";
                    Repaint();
                }
            }
        }

        // Nieuwe textures als Sprite importeren zodat Resources.Load<Sprite> werkt
        if (gedownloadePaden.Count > 0)
        {
            AssetDatabase.Refresh();
            foreach (string pad in gedownloadePaden)
            {
                TextureImporter importer = AssetImporter.GetAtPath(pad) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }
            }
        }

        statusBericht = $"Bake klaar. {gelukt} plaatjes gedownload, {overgeslagen} overgeslagen (al aanwezig of geen URL).";
        Debug.Log($"[LegoDataBaker] {statusBericht}");
        Repaint();
    }

    private static string BepaalExtensie(string url)
    {
        string zonderQuery = url.Split('?')[0];
        string ext = Path.GetExtension(zonderQuery).ToLower();
        if (ext == ".jpg" || ext == ".jpeg" || ext == ".png") return ext;
        return ".jpg";
    }

    private static bool PastInSorteerMachine(string naam, int maxGrootte)
    {
        Match match = Regex.Match(naam.ToLower(), @"(\d+)\s*x\s*(\d+)");
        if (match.Success)
        {
            int breedte = int.Parse(match.Groups[1].Value);
            int lengte = int.Parse(match.Groups[2].Value);

            if (breedte > maxGrootte || lengte > maxGrootte)
            {
                return false;
            }
        }
        return true;
    }

    private static string MaakSchoneNaam(string origineleNaam)
    {
        if (string.IsNullOrEmpty(origineleNaam)) return "";

        string schoneNaam = origineleNaam.ToLower();

        schoneNaam = Regex.Replace(schoneNaam, @"\bno\.?\s*\d+", "");
        schoneNaam = Regex.Replace(schoneNaam, @"\bvariant\b", "");

        Match match = Regex.Match(schoneNaam, @"(\d+)\s*x\s*(\d+)");
        if (match.Success)
        {
            string maatTekst = match.Groups[1].Value + " bij " + match.Groups[2].Value;
            schoneNaam = schoneNaam.Replace(match.Value, "").Trim();
            schoneNaam = maatTekst + " " + schoneNaam;
        }

        schoneNaam = Regex.Replace(schoneNaam, @"\s+", " ").Trim();

        if (schoneNaam.Length > 0)
        {
            schoneNaam = char.ToUpper(schoneNaam[0]) + schoneNaam.Substring(1);
        }

        return schoneNaam;
    }

    /// <summary>
    /// Mold-varianten (3001a/3001b/48379c04) worden het basisnummer.
    /// Bedrukte versies (pr/pat/pb/pa) blijven apart zodat de print-filter blijft werken.
    /// </summary>
    private static string VariantSleutel(string partNum)
    {
        if (string.IsNullOrEmpty(partNum)) return partNum;

        if (IsPrintVariant(partNum)) return partNum;

        // Verwijder een trailing mold/assembly suffix zoals "a", "b1" of "c04"
        return Regex.Replace(partNum, @"[a-z][0-9]*$", "");
    }

    private static bool IsPrintVariant(string partNum)
    {
        return Regex.IsMatch(partNum, @"(?i)(pb|pr|pat|pa|px|cpb|cpr)\d");
    }

    private static string[] SplitCsvTekst(string tekst)
    {
        if (string.IsNullOrEmpty(tekst)) return new string[0];

        // Zorg dat een eventuele BOM aan het begin weg is
        if (tekst.Length > 0 && tekst[0] == '\uFEFF')
        {
            tekst = tekst.Substring(1);
        }

        return tekst.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
    }

    private static string[] SplitCsvLine(string line)
    {
        List<string> velden = new List<string>();
        StringBuilder huidig = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        huidig.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    huidig.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    velden.Add(huidig.ToString());
                    huidig.Length = 0;
                }
                else
                {
                    huidig.Append(c);
                }
            }
        }

        velden.Add(huidig.ToString());
        return velden.ToArray();
    }

    private static Color ParseHexKleur(string hex)
    {
        Color result = Color.white;
        if (string.IsNullOrEmpty(hex)) return result;

        hex = hex.Trim().TrimStart('#');
        if (hex.Length < 6) return result;

        try
        {
            float r = Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
            float g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
            float b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
            result = new Color(r, g, b, 1f);
        }
        catch
        {
            // ongeldige hex -> wit
        }

        return result;
    }

    private class KleurInfo
    {
        public string naam;
        public Color kleur;
    }

    private class PartInvAgg
    {
        public long TotaalAantal;
        public Dictionary<int, long> KleurAantallen = new Dictionary<int, long>();
        public Dictionary<int, string> KleurImgUrl = new Dictionary<int, string>();
        public HashSet<int> Inventories = new HashSet<int>();

        public string BesteImgUrl
        {
            get
            {
                int besteKleur = -1;
                long besteAantal = -1;

                foreach (var kv in KleurAantallen)
                {
                    if (KleurImgUrl.ContainsKey(kv.Key) && kv.Value > besteAantal)
                    {
                        besteAantal = kv.Value;
                        besteKleur = kv.Key;
                    }
                }

                return besteKleur >= 0 ? KleurImgUrl[besteKleur] : string.Empty;
            }
        }
    }
}
