using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class carouselmovingsecond : MonoBehaviour
{
    [System.Serializable]
    public struct BakjeInstelling
    {
        public Transform bakjeTransform;
        public Vector3 kantelAs;
        public float actieHoek;
    }

    [Header("Transforms")]
    public Transform basisTransform;
    
    [Header("Bakjes Configuratie")]
    public BakjeInstelling[] bakjes; 

    [Header("Settings")]
    public float snelheid = 5f;
    public Vector3 basisDraaiAs = new Vector3(0, 1, 0);

    [Header("Offsets")]
    public Vector3 basisPositieOffset;

    [Header("Blokjes Instellingen")]
    public LayerMask blokjesLayer;

    private float doelBasisRotatie;
    private Quaternion startrotatieBasis;
    private Vector3 startPositieBasis;
    private bool isDraaiingBezig = false;
    private int huidigePositieIndex = 0;
    private float blokjesaantalperuur = 0;
    private float timer = 0;
    
    public TextMeshProUGUI resultaattext;
    private float besteaantal = 0;

    public bool IsBusy => isDraaiingBezig;

    private void Start()
    {

        besteaantal = PlayerPrefs.GetFloat("bestetijd", 0);
        startrotatieBasis = basisTransform.rotation;
        startPositieBasis = basisTransform.position;
        doelBasisRotatie = 0f;

        foreach (BakjeInstelling instelling in bakjes)
        {
            if (instelling.bakjeTransform != null)
            {
                CarouselBakje trigger = instelling.bakjeTransform.gameObject.GetComponent<CarouselBakje>();
                if (trigger == null)
                {
                    trigger = instelling.bakjeTransform.gameObject.AddComponent<CarouselBakje>();
                }
                trigger.Setup(blokjesLayer, this);
            }
        }
    }

    private void Update()
    {

        timer += Time.deltaTime;

        if (timer >= 30)
        {
            timernaarnul();
        }
        if (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame && !isDraaiingBezig)
        {
            DraaiEenStap();
        }
    }

    private void timernaarnul()
    {
            timer = 0;


            
            berekentijd();


    }
    private void voegblokjestoe()
    {
        blokjesaantalperuur += 1;
    }
    private void berekentijd()
    {

        blokjesaantalperuur *= 120;
        
        if ( blokjesaantalperuur > besteaantal)
        {

                besteaantal = blokjesaantalperuur;
                PlayerPrefs.SetFloat("bestetijd", besteaantal);
                PlayerPrefs.Save();
        }

        resultaattext.text = "het aantal blokjes per uur is " + blokjesaantalperuur + " en het beste resultaat is " + besteaantal;

        blokjesaantalperuur = 0;
        

    }

    public void ActiveerDraaiVanafTrigger()
    {
        if (!isDraaiingBezig)
        {
            DraaiEenStap();
        }
    }

    public void OntvangDraaiSeintje()
    {
        if (!isDraaiingBezig)
        {
            DraaiEenStap();
        }
    }

    public void DraaiEenStap()
    {
        if (!isDraaiingBezig)
        {

            
        
            StartCoroutine(DraaiVolgordeRoutine());
        }
    }

    

    private System.Collections.IEnumerator DraaiVolgordeRoutine()
    {
        isDraaiingBezig = true;

        float tijdOmhoog = 0f;
        while (tijdOmhoog < 1f)
        {
            tijdOmhoog += Time.deltaTime * snelheid;
            RoteerAlleBakjesNaarHoek(0f, tijdOmhoog);
            yield return null;
        }
        RoteerAlleBakjesNaarHoek(0f, 1f);

        doelBasisRotatie += 90f;
        voegblokjestoe();
        Quaternion startRotatieVanDezenDraai = basisTransform.rotation;
        Quaternion doelRotatieVanDezenDraai = startrotatieBasis * Quaternion.Euler(basisDraaiAs * doelBasisRotatie);

        float tijdBasis = 0f;
        while (tijdBasis < 1f)
        {
            tijdBasis += Time.deltaTime * (snelheid * 0.5f);
            Vector3 doelPositieBasis = startPositieBasis + basisPositieOffset;
            basisTransform.position = doelPositieBasis;
            basisTransform.rotation = Quaternion.Slerp(startRotatieVanDezenDraai, doelRotatieVanDezenDraai, tijdBasis);
            yield return null;
        }
        basisTransform.rotation = doelRotatieVanDezenDraai;

        huidigePositieIndex = (huidigePositieIndex + 1) % 4;

        BakjeInstelling actieBakjeInstelling = default;
        bool bakjeGevonden = false;

        if (huidigePositieIndex == 0 && bakjes.Length > 0) { actieBakjeInstelling = bakjes[0]; bakjeGevonden = true; }
        else if (huidigePositieIndex == 1 && bakjes.Length > 3) { actieBakjeInstelling = bakjes[3]; bakjeGevonden = true; }
        else if (huidigePositieIndex == 2 && bakjes.Length > 2) { actieBakjeInstelling = bakjes[2]; bakjeGevonden = true; }
        else if (huidigePositieIndex == 3 && bakjes.Length > 1) { actieBakjeInstelling = bakjes[1]; bakjeGevonden = true; }

        if (bakjeGevonden && actieBakjeInstelling.bakjeTransform != null)
        {
            float tijdOmlaag = 0f;
            float doelHoek = actieBakjeInstelling.actieHoek; 

            while (tijdOmlaag < 1f)
            {
                tijdOmlaag += Time.deltaTime * snelheid;
                RoteerEnkelBakjeNaarHoek(actieBakjeInstelling, doelHoek, tijdOmlaag);
                yield return null;
            }
            RoteerEnkelBakjeNaarHoek(actieBakjeInstelling, doelHoek, 1f);
        }

        isDraaiingBezig = false;
    }

    private void RoteerAlleBakjesNaarHoek(float doelHoek, float tijd)
    {
        foreach (BakjeInstelling instelling in bakjes)
        {
            if (instelling.bakjeTransform == null) continue;
            RoteerEnkelBakjeNaarHoek(instelling, doelHoek, tijd);
        }
    }

    private void RoteerEnkelBakjeNaarHoek(BakjeInstelling instelling, float doelHoek, float tijd)
    {
        Transform bakje = instelling.bakjeTransform;
        Vector3 lokaleRotatie = bakje.localEulerAngles;
        Vector3 kantelAs = instelling.kantelAs;

        float huidigeHoek = 0f;
        if (kantelAs.x > 0) huidigeHoek = lokaleRotatie.x;
        else if (kantelAs.y > 0) huidigeHoek = lokaleRotatie.y;
        else if (kantelAs.z > 0) huidigeHoek = lokaleRotatie.z;

        if (huidigeHoek > 180f) huidigeHoek -= 360f;
        float nieuweHoek = Mathf.Lerp(huidigeHoek, doelHoek, tijd);

        Quaternion extraAsRotatie = Quaternion.identity;
        if (kantelAs.x > 0) extraAsRotatie = Quaternion.Euler(nieuweHoek, 0, 0);
        else if (kantelAs.y > 0) extraAsRotatie = Quaternion.Euler(0, nieuweHoek, 0);
        else if (kantelAs.z > 0) extraAsRotatie = Quaternion.Euler(0, 0, nieuweHoek);

        bakje.rotation = basisTransform.rotation * extraAsRotatie;
    }
}