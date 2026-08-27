using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class ZoekTermEntry
{
    public string term;
    public int teller;
    public int laatsteKeer; // unix-seconden
}

[System.Serializable]
public class ZoekGeschiedenisData
{
    public List<ZoekTermEntry> entries = new List<ZoekTermEntry>();
}

/// <summary>
/// Bewaart zoektermen (met hoe vaak ze zijn gebruikt en wanneer voor het laatst) in PlayerPrefs.
/// Wordt gedeeld door de pak-pagina en de inventaris, zodat populaire/recente zoektermen
/// overal hetzelfde zijn en blijven staan tussen sessies.
/// </summary>
public static class ZoekGeschiedenis
{
    private const string PlayerPrefsKey = "ZoekGeschiedenis";
    private const int MaxEntries = 50;

    private static List<ZoekTermEntry> cache;
    private static bool geladen;

    private static void ZorgDatGeladen()
    {
        if (geladen) return;
        geladen = true;
        cache = new List<ZoekTermEntry>();

        try
        {
            string json = PlayerPrefs.GetString(PlayerPrefsKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                ZoekGeschiedenisData data = JsonUtility.FromJson<ZoekGeschiedenisData>(json);
                if (data != null && data.entries != null)
                    cache = data.entries;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ZoekGeschiedenis] Laden mislukt: " + e.Message);
        }
    }

    private static void Opslaan()
    {
        try
        {
            ZoekGeschiedenisData data = new ZoekGeschiedenisData { entries = cache };
            PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ZoekGeschiedenis] Opslaan mislukt: " + e.Message);
        }
    }

    /// <summary>Registreert een gebruikte zoekterm (wordt populairder en verschijnt bovenaan recent).</summary>
    public static void Registreer(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return;
        term = term.Trim().ToLower();
        if (term.Length > 40) term = term.Substring(0, 40);

        ZorgDatGeladen();

        int nu = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

        foreach (var entry in cache)
        {
            if (entry.term == term)
            {
                entry.teller++;
                entry.laatsteKeer = nu;
                Opslaan();
                return;
            }
        }

        cache.Add(new ZoekTermEntry { term = term, teller = 1, laatsteKeer = nu });

        if (cache.Count > MaxEntries)
        {
            cache.Sort((a, b) => b.laatsteKeer.CompareTo(a.laatsteKeer));
            cache.RemoveRange(MaxEntries, cache.Count - MaxEntries);
        }

        Opslaan();
    }

    /// <summary>De n meest recent gebruikte zoektermen.</summary>
    public static List<string> Recente(int n)
    {
        ZorgDatGeladen();

        List<ZoekTermEntry> kopie = new List<ZoekTermEntry>(cache);
        kopie.Sort((a, b) => b.laatsteKeer.CompareTo(a.laatsteKeer));

        List<string> result = new List<string>();
        for (int i = 0; i < Mathf.Min(n, kopie.Count); i++)
            result.Add(kopie[i].term);
        return result;
    }

    /// <summary>De n populairste zoektermen (meest gebruikt).</summary>
    public static List<string> Populair(int n)
    {
        ZorgDatGeladen();

        List<ZoekTermEntry> kopie = new List<ZoekTermEntry>(cache);
        kopie.Sort((a, b) =>
        {
            if (a.teller != b.teller) return b.teller.CompareTo(a.teller);
            return b.laatsteKeer.CompareTo(a.laatsteKeer);
        });

        List<string> result = new List<string>();
        for (int i = 0; i < Mathf.Min(n, kopie.Count); i++)
            result.Add(kopie[i].term);
        return result;
    }
}

/// <summary>
/// Kleine herbruikbare UI-helper: vult een container met klikbare zoekterm-chips.
/// De container moet een HorizontalLayoutGroup hebben.
/// </summary>
public static class ZoekChips
{
    public static void Vul(Transform container, IList<string> termen, Action<string> onKlik, float maxChipBreedte = 190f)
    {
        if (container == null) return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(container.GetChild(i).gameObject);
        }

        if (termen == null) return;

        foreach (string term in termen)
        {
            if (string.IsNullOrEmpty(term)) continue;

            GameObject chip = new GameObject("Chip", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            chip.transform.SetParent(container, false);

            Image bg = chip.GetComponent<Image>();
            bg.color = new Color(0.25f, 0.4f, 0.6f, 1f);

            LayoutElement le = chip.GetComponent<LayoutElement>();
            le.preferredWidth = Mathf.Clamp(52f + term.Length * 7.5f, 60f, maxChipBreedte);
            le.preferredHeight = 26f;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            GameObject labelGo = new GameObject("Tekst", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(chip.transform, false);

            TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
            label.text = term;
            label.fontSize = 14;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            Button knop = chip.GetComponent<Button>();
            knop.targetGraphic = bg;

            string t = term;
            knop.onClick.AddListener(() => onKlik(t));
        }
    }
}
