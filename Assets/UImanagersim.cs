using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class UImanagersim : MonoBehaviour
{

    [Header("Start de sim")]
    [SerializeField] private ronddraaien draaiScript1;
    [SerializeField] private ronddraaien draaiScript2;
    [SerializeField] private ronddraaien draaiScript3;
    [SerializeField] private spawner spawnerScript;
    
    private bool ispressed = false;

    [Header("Slider Elementen")]
    [SerializeField] private Slider sliderChannel1;
    [SerializeField] private Slider sliderChannel2;
    [SerializeField] private Slider sliderChannel3;
    [SerializeField] private Slider sliderBlokjesvallen;

    [Header("c-channel ui teksten")]
    [SerializeField] private TextMeshProUGUI channel1Tekst;
    [SerializeField] private TextMeshProUGUI channel2Tekst;
    [SerializeField] private TextMeshProUGUI channel3Tekst;
    [SerializeField] private TextMeshProUGUI blokjesvallenTekst;
    [SerializeField] private TextMeshProUGUI spawnertext;
    public Button startstim;
    public Image Knopafbeelding;

    public Color startColor = Color.green;
    public Color klikColor = Color.red;

    private bool isRood = false;


    public static float SnelheidChannel1 = 90f;
    public static float SnelheidChannel2 = 90f;
    public static float SnelheidChannel3 = 90f;
    public static float blokjelatenvallen = 1f;

    public float maxSnelheid = 200f;

    public void pasblokjestijd(float nieuweSnelheid)
    {
        blokjelatenvallen = Mathf.Round(nieuweSnelheid * 100f) / 100f;
        PlayerPrefs.SetFloat("blokjelatenvallen", blokjelatenvallen);
        PlayerPrefs.Save();

        if (blokjesvallenTekst != null)
        {
            blokjesvallenTekst.text = "tijd om blokjes te laten vallen is " + nieuweSnelheid.ToString("F2") + "s";
        }
    }
    public void PasSnelheidChannel1(float nieuweSnelheid)
    {
        SnelheidChannel1 = nieuweSnelheid;
        PlayerPrefs.SetFloat("SnelheidChannel1", nieuweSnelheid);
        PlayerPrefs.Save();
        if (channel1Tekst != null)
        {
            channel1Tekst.text = "C1 Snelheid " + nieuweSnelheid.ToString("F0");
        }
    }

    public void PasSnelheidChannel2(float nieuweSnelheid)
    {
        SnelheidChannel2 = nieuweSnelheid;
        PlayerPrefs.SetFloat("SnelheidChannel2", nieuweSnelheid);
        PlayerPrefs.Save();
        if (channel2Tekst != null)
        {
            channel2Tekst.text = "C2 Snelheid " + nieuweSnelheid.ToString("F0");
        }
    }

    public void PasSnelheidChannel3(float nieuweSnelheid)
    {
        SnelheidChannel3 = nieuweSnelheid;
        PlayerPrefs.SetFloat("SnelheidChannel3", nieuweSnelheid);
        PlayerPrefs.Save();
        if (channel3Tekst != null)
        {
            channel3Tekst.text = "C3 Snelheid " + nieuweSnelheid.ToString("F0");
            PlayerPrefs.SetFloat("SnelheidChannel3", nieuweSnelheid);
        }
    }

    




    
    
    private void Start()
    {

        
        SnelheidChannel1 = PlayerPrefs.GetFloat("SnelheidChannel1", 0);
        SnelheidChannel2 = PlayerPrefs.GetFloat("SnelheidChannel2", 0);
        SnelheidChannel3 = PlayerPrefs.GetFloat("SnelheidChannel3", 0);
        blokjelatenvallen = PlayerPrefs.GetFloat("blokjelatenvallen", 0);
        InstalleerSliders();
        if (startstim != null)
        {
        startstim.onClick.AddListener(wisselkleur);
        startstim.Select();
        }

        if (Knopafbeelding != null)
        {
            Knopafbeelding.color = Color.green;
        }
        if (startstim != null)
        {
            startstim.onClick.AddListener(wisselkleur);
        }
    }
    void wisselkleur()
    {
        if (Knopafbeelding == null) return;
        if (isRood)
        {
            Knopafbeelding.color = startColor;
            isRood = false;
        }
        else
        {
            Knopafbeelding.color = klikColor;
            isRood = true;
        }
    }

    private void InstalleerSliders()
    {

        if (sliderChannel1 != null)
        {
        sliderChannel1.maxValue = maxSnelheid;
        sliderChannel1.value = Mathf.Clamp(SnelheidChannel1, sliderChannel1.minValue, sliderChannel1.maxValue);
        sliderChannel1.onValueChanged.RemoveAllListeners();
        sliderChannel1.onValueChanged.AddListener(PasSnelheidChannel1);
        PasSnelheidChannel1(sliderChannel1.value);
        }
        if (sliderChannel2 != null)
        {
        sliderChannel2.maxValue = maxSnelheid;
        sliderChannel2.value = Mathf.Clamp(SnelheidChannel2, sliderChannel2.minValue, sliderChannel2.maxValue);
        sliderChannel2.onValueChanged.RemoveAllListeners();
        sliderChannel2.onValueChanged.AddListener(PasSnelheidChannel2);
        PasSnelheidChannel2(sliderChannel2.value);
        }

        if (sliderChannel3 != null)
        {
            sliderChannel3.maxValue = maxSnelheid;
            sliderChannel3.value = Mathf.Clamp(SnelheidChannel3, sliderChannel3.minValue, sliderChannel3.maxValue);
            sliderChannel3.onValueChanged.RemoveAllListeners();
            sliderChannel3.onValueChanged.AddListener(PasSnelheidChannel3);
            PasSnelheidChannel3(sliderChannel3.value);
        }

        if (sliderBlokjesvallen != null)
        {
            sliderBlokjesvallen.minValue = 0f;
            sliderBlokjesvallen.maxValue = 2f;
            sliderBlokjesvallen.wholeNumbers = false;
            sliderBlokjesvallen.value = Mathf.Clamp(blokjelatenvallen, sliderBlokjesvallen.minValue, sliderBlokjesvallen.maxValue);
            sliderBlokjesvallen.onValueChanged.RemoveAllListeners();
            sliderBlokjesvallen.onValueChanged.AddListener(pasblokjestijd);
            pasblokjestijd(sliderBlokjesvallen.value);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartDeSimulatieKnop()
{
    if (ispressed == false)
    {
        ispressed = true;
        wisselkleur();


        if (spawnertext != null)
        {
            spawnertext.text = "stop spawner";
        }


        
        if (sliderChannel1 != null) PasSnelheidChannel1(sliderChannel1.value);
        if (sliderChannel2 != null) PasSnelheidChannel2(sliderChannel2.value);
        if (sliderChannel3 != null) PasSnelheidChannel3(sliderChannel3.value);
        if (sliderBlokjesvallen != null) pasblokjestijd(sliderBlokjesvallen.value);
        

        if(spawnerScript != null) spawnerScript.canrun = true;
        if(draaiScript1 != null) draaiScript1.simstarted = true;
        if(draaiScript2 != null) draaiScript2.simstarted = true;
        if(draaiScript3 != null) draaiScript3.simstarted = true;
        return;
    }
    
    if (ispressed == true)
    {
        wisselkleur();
        if (spawnertext != null)
        {
            spawnertext.text = "start spawner";
        }
        ispressed = false;
        if(spawnerScript != null) spawnerScript.canrun = false;
        if(draaiScript1 != null) draaiScript1.simstarted = false;
        if(draaiScript2 != null) draaiScript2.simstarted = false;
        if(draaiScript3 != null) draaiScript3.simstarted = false;
        return;
    }
}
}
