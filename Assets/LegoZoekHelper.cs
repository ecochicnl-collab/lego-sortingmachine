using System;
using UnityEngine;

/// <summary>
/// Slim zoeken voor blokjes: geeft een score aan hoe goed een blokje bij een zoekterm past.
/// Exacte part-nummers en prefix-matches komen eerst, daarna woord-matches, dan substring-matches.
/// Bij meerdere woorden (bijv. "tile 2x4") moeten álle woorden ergens matchen (AND).
/// Gebruikt door zowel de pak-pagina als de inventaris, zodat overal dezelfde logische volgorde geldt.
/// </summary>
public static class ZoekHelper
{
    /// <summary>Score van een blokje voor een zoekterm (0 = geen match). Hoe hoger, hoe logischer het antwoord.</summary>
    public static int ScoreVoorZoekterm(LegoPart part, string term)
    {
        if (part == null || string.IsNullOrEmpty(term)) return 0;

        string[] tokens = term.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return 0;

        int totaal = 0;
        foreach (string token in tokens)
        {
            int s = ScoreToken(part, token);
            if (s <= 0) return 0; // één token matcht nergens -> geen resultaat
            totaal += s;
        }
        return totaal;
    }

    private static int ScoreToken(LegoPart part, string token)
    {
        string num = part.part_num != null ? part.part_num.ToLower() : "";
        string naam = part.name != null ? part.name.ToLower() : "";
        string compact = naam.Replace(" ", ""); // "2 x 4" wordt "2x4"

        if (num == token) return 1000;              // exact part-nummer
        if (num.StartsWith(token)) return 800;      // part-nummer begint ermee
        if (naam == token) return 700;              // exacte naam
        if (naam.StartsWith(token)) return 600;     // naam begint ermee
        if (WoordBegintMet(naam, token)) return 550; // een woord in de naam begint ermee (bijv. "brick")
        if (compact.Contains(token)) return 450;    // in compacte naam (bijv. "2x4" in "tile2x4")
        if (num.Contains(token)) return 350;        // ergens in het part-nummer
        if (naam.Contains(token)) return 200;       // ergens in de naam

        return 0;
    }

    private static bool WoordBegintMet(string naam, string token)
    {
        string[] woorden = naam.Split(' ');
        foreach (string w in woorden)
        {
            if (!string.IsNullOrEmpty(w) && w.StartsWith(token)) return true;
        }
        return false;
    }

    /// <summary>Hoe goed matcht het blokje: Exact (bijv. precies "3001"), Prefix (begint ermee) of Substring (bevat het ergens).</summary>
    public static MatchKwaliteit Kwaliteit(LegoPart part, string term)
    {
        if (part == null || string.IsNullOrEmpty(term)) return MatchKwaliteit.Geen;

        string[] tokens = term.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return MatchKwaliteit.Geen;

        // De zwakste token bepaalt de kwaliteit (bijv. "3001 tile": exact nummer maar zwakke naam -> Substring)
        MatchKwaliteit zwakste = MatchKwaliteit.Exact;

        foreach (string token in tokens)
        {
            int s = ScoreToken(part, token);
            if (s <= 0) return MatchKwaliteit.Geen;

            MatchKwaliteit kwal = s >= 800 ? MatchKwaliteit.Exact : s >= 500 ? MatchKwaliteit.Prefix : MatchKwaliteit.Substring;

            if ((int)kwal < (int)zwakste) zwakste = kwal;
        }

        return zwakste;
    }

    /// <summary>Symbool voor de match-kwaliteit om in de resultaten te tonen.</summary>
    public static string SymboolVoor(MatchKwaliteit kwaliteit)
    {
        switch (kwaliteit)
        {
            case MatchKwaliteit.Exact: return "★";
            case MatchKwaliteit.Prefix: return "▶";
            default: return "•";
        }
    }

    /// <summary>Kleur voor de match-kwaliteit (groen = exact, blauw = prefix, oranje = substring).</summary>
    public static Color KleurVoor(MatchKwaliteit kwaliteit)
    {
        switch (kwaliteit)
        {
            case MatchKwaliteit.Exact: return new Color(0.15f, 0.85f, 0.3f);
            case MatchKwaliteit.Prefix: return new Color(0.25f, 0.6f, 1f);
            default: return new Color(1f, 0.65f, 0.2f);
        }
    }
}

public enum MatchKwaliteit
{
    Geen = 0,
    Substring = 1,
    Prefix = 2,
    Exact = 3
}
