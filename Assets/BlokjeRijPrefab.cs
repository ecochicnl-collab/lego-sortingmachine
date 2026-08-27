using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class BlokjeRijItem : MonoBehaviour
{
    [Header("UI Elementen")]
    public Image iconImage;
    public TMP_Text titelText;
    public TMP_Text statusText;
    public TMP_Dropdown kleurDropdown;
    public Button pakButton;

    private LegoPart huidigPart;
    private LegoKleurVoorraad geselecteerdeKleur;
    private string huidigeAfbeeldingUrl = "";

    private void Awake()
    {
        FindUIReferences();
    }

    private void FindUIReferences()
    {
        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>(true);

        var texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var txt in texts)
        {
            if (txt == null) continue;
            string name = txt.gameObject.name.ToLower();
            if (titelText == null && (name.Contains("title") || name.Contains("titel") || name.Contains("naam")))
                titelText = txt;
            if (statusText == null && (name.Contains("status") || name.Contains("voorraad") || name.Contains("state")))
                statusText = txt;
        }

        if (kleurDropdown == null)
            kleurDropdown = GetComponentInChildren<TMP_Dropdown>(true);
        if (pakButton == null)
            pakButton = GetComponentInChildren<Button>(true);

        if (statusText != null)
            statusText.color = Color.black;
    }

    public void Setup(LegoPart part, string zoekTerm = "")
    {
        if (part == null)
        {
            Debug.LogWarning("[BlokjeRijItem] Setup aangeroepen met null part");
            return;
        }

        huidigPart = part;
        FindUIReferences();

        if (titelText != null)
        {
            if (!string.IsNullOrEmpty(zoekTerm))
            {
                // Symbool + kleur tonen voor de match-kwaliteit (★ exact, ▶ begint met, • bevat)
                MatchKwaliteit kwal = ZoekHelper.Kwaliteit(part, zoekTerm);
                string hex = ColorUtility.ToHtmlStringRGB(ZoekHelper.KleurVoor(kwal));
                titelText.text = $"<color=#{hex}>{ZoekHelper.SymboolVoor(kwal)}</color> {part.part_num} - {part.name}";
            }
            else
            {
                titelText.text = $"{part.part_num} - {part.name}";
            }
        }

        LaadAfbeelding(part);

        if (kleurDropdown != null)
        {
            kleurDropdown.ClearOptions();
            List<string> opties = new List<string>();

            if (part.beschikbareKleuren != null && part.beschikbareKleuren.Count > 0)
            {
                foreach (var k in part.beschikbareKleuren)
                {
                    opties.Add($"{k.kleurNaam} ({k.aantalInVoorraad}x)");
                }
                kleurDropdown.AddOptions(opties);
                kleurDropdown.gameObject.SetActive(true);
                kleurDropdown.onValueChanged.RemoveAllListeners();
                kleurDropdown.onValueChanged.AddListener(OnKleurGekozen);
                OnKleurGekozen(0);
            }
            else
            {
                opties.Add("Geen voorraad");
                kleurDropdown.AddOptions(opties);
                kleurDropdown.gameObject.SetActive(true);
                if (statusText != null)
                {
                    statusText.text = "Niet op voorraad";
                    statusText.color = Color.red;
                }
                if (pakButton != null)
                    pakButton.interactable = false;
            }
        }
        else if (statusText != null)
        {
            statusText.text = "Geen kleurselectie";
            statusText.color = Color.red;
        }

        if (pakButton != null)
        {
            pakButton.onClick.RemoveAllListeners();
            pakButton.onClick.AddListener(OnPakKlik);
        }

        Debug.Log($"[BlokjeRijItem] Setup: {part.part_num} - {part.name}");
    }

    /// <summary>
    /// Configureert dezelfde rij-prefab voor de inventaris. De pakactie wordt vervangen door
    /// een selectiecallback en afbeeldingen worden alleen geladen wanneer dat expliciet aan staat.
    /// </summary>
    public void SetupVoorInventaris(LegoPart part, string zoekTerm, bool laadAfbeelding, Action<LegoPart> onSelecteer)
    {
        if (part == null) return;

        huidigPart = part;
        FindUIReferences();

        if (titelText != null)
        {
            MatchKwaliteit kwal = ZoekHelper.Kwaliteit(part, zoekTerm);
            string hex = ColorUtility.ToHtmlStringRGB(ZoekHelper.KleurVoor(kwal));
            titelText.text = $"<color=#{hex}>{ZoekHelper.SymboolVoor(kwal)}</color> {part.part_num} - {part.name}";
        }

        if (laadAfbeelding)
        {
            LaadAfbeelding(part);
        }
        else if (iconImage != null)
        {
            huidigeAfbeeldingUrl = "";
            iconImage.sprite = null;
            iconImage.color = new Color(1f, 1f, 1f, 0.12f);
        }

        if (kleurDropdown != null)
            kleurDropdown.gameObject.SetActive(false);

        if (statusText != null)
        {
            statusText.text = part.TotaalAantal() > 0
                ? "Beschikbaar: " + part.TotaalAantal()
                : "Niet op voorraad";
            statusText.color = part.TotaalAantal() > 0 ? Color.green : Color.red;
        }

        if (pakButton != null)
        {
            pakButton.onClick.RemoveAllListeners();
            pakButton.interactable = true;
            pakButton.onClick.AddListener(() => onSelecteer?.Invoke(part));
        }
    }

    /// <summary>
    /// Maakt deze bestaande Pak-Blokjes-rij geschikt voor de brede inventarisresultatenlijst.
    /// De prefab blijft dezelfde, maar de onderdelen gebruiken hier proportionele anchors zodat
    /// de volledige breedte van de actuele inventarisrij wordt benut.
    /// </summary>
    public void StelInventarisLayoutIn(float rijHoogte)
    {
        FindUIReferences();

        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(0f, rijHoogte);
            root.localScale = Vector3.one;
        }

        if (iconImage != null)
        {
            RectTransform rt = iconImage.rectTransform;
            rt.anchorMin = new Vector2(0.02f, 0.12f);
            rt.anchorMax = new Vector2(0.18f, 0.88f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            iconImage.preserveAspect = true;
        }

        if (titelText != null)
        {
            RectTransform rt = titelText.rectTransform;
            rt.anchorMin = new Vector2(0.20f, 0.52f);
            rt.anchorMax = new Vector2(0.72f, 0.92f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            titelText.fontSize = Mathf.Clamp(rijHoogte * 0.24f, 18f, 30f);
            titelText.enableAutoSizing = true;
            titelText.fontSizeMin = 14f;
            titelText.fontSizeMax = Mathf.Clamp(rijHoogte * 0.24f, 18f, 30f);
            titelText.alignment = TextAlignmentOptions.MidlineLeft;
            titelText.textWrappingMode = TextWrappingModes.NoWrap;
            titelText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (statusText != null)
        {
            RectTransform rt = statusText.rectTransform;
            rt.anchorMin = new Vector2(0.20f, 0.08f);
            rt.anchorMax = new Vector2(0.72f, 0.48f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            statusText.fontSize = Mathf.Clamp(rijHoogte * 0.16f, 14f, 22f);
            statusText.enableAutoSizing = true;
            statusText.fontSizeMin = 12f;
            statusText.fontSizeMax = Mathf.Clamp(rijHoogte * 0.16f, 14f, 22f);
            statusText.alignment = TextAlignmentOptions.MidlineLeft;
            statusText.textWrappingMode = TextWrappingModes.NoWrap;
            statusText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (kleurDropdown != null)
        {
            kleurDropdown.gameObject.SetActive(false);
        }

        if (pakButton != null)
        {
            RectTransform rt = pakButton.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.76f, 0.12f);
            rt.anchorMax = new Vector2(0.98f, 0.88f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            TMP_Text label = pakButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(8f, 4f);
                label.rectTransform.offsetMax = new Vector2(-8f, -4f);
                label.fontSize = Mathf.Clamp(rijHoogte * 0.20f, 16f, 26f);
                label.enableAutoSizing = true;
                label.fontSizeMin = 13f;
                label.fontSizeMax = Mathf.Clamp(rijHoogte * 0.20f, 16f, 26f);
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        LayoutRebuilder.MarkLayoutForRebuild(root);
    }

    private void OnKleurGekozen(int index)
    {
        if (huidigPart == null || huidigPart.beschikbareKleuren == null || index < 0 || index >= huidigPart.beschikbareKleuren.Count)
            return;

        geselecteerdeKleur = huidigPart.beschikbareKleuren[index];

        if (statusText != null)
        {
            if (geselecteerdeKleur.aantalInVoorraad > 0)
            {
                statusText.text = $"Op voorraad ({geselecteerdeKleur.aantalInVoorraad} stuks)";
                statusText.color = Color.green;
            }
            else
            {
                statusText.text = "Uitverkocht in deze kleur";
                statusText.color = Color.red;
            }
        }

        if (pakButton != null)
            pakButton.interactable = geselecteerdeKleur.aantalInVoorraad > 0;
    }

    private void OnPakKlik()
    {
        if (huidigPart == null)
            return;

        // Gebruikte zoekterm onthouden voor de zoekgeschiedenis / populaire termen
        PakBlokjesPanelManager pakManager = FindFirstObjectByType<PakBlokjesPanelManager>();
        if (pakManager != null)
        {
            pakManager.RegistreerHuidigeZoektermEnHerlaad();
        }

        if (geselecteerdeKleur != null && geselecteerdeKleur.aantalInVoorraad > 0)
        {
            // PAK-knop opent de aparte bevestigingspagina (blokje, kleur, aantal)
            BevestigPakPanel.Open(huidigPart, geselecteerdeKleur);
        }
    }

    private void LaadAfbeelding(LegoPart part)
    {
        if (iconImage == null) return;

        iconImage.preserveAspect = true;

        // 1) Eerst lokaal (top-N plaatjes die tijdens het bakken zijn gedownload)
        if (!string.IsNullOrEmpty(part.lokaalPad))
        {
            Sprite lokaal = Resources.Load<Sprite>(part.lokaalPad);
            if (lokaal != null)
            {
                huidigeAfbeeldingUrl = "";
                iconImage.sprite = lokaal;
                iconImage.color = Color.white;
                return;
            }
        }

        // 2) Anders via de Rebrickable URL
        if (string.IsNullOrEmpty(part.img_url))
        {
            huidigeAfbeeldingUrl = "";
            iconImage.sprite = null;
            iconImage.color = new Color(1f, 1f, 1f, 0.15f);
            return;
        }

        huidigeAfbeeldingUrl = part.img_url;
        StartCoroutine(DownloadAfbeelding(part.img_url));
    }

    private IEnumerator DownloadAfbeelding(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success || huidigeAfbeeldingUrl != url)
                yield break;

            Texture2D tex = DownloadHandlerTexture.GetContent(request);
            if (tex == null || iconImage == null)
                yield break;

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            iconImage.sprite = sprite;
            iconImage.color = Color.white;
        }
    }
}