using UnityEngine;

public class CarrouselController : MonoBehaviour
{
    public float draaiSnelheid = 180f;
    public Collider detectieCollider;
    public LayerMask blokjesLayer;
    
    public bool IsBusy { get; private set; }
    private float doelHoek;

    void Update()
    {
        if (IsBusy)
        {
            float huidigeHoekY = transform.eulerAngles.y;
            float stap = draaiSnelheid * Time.deltaTime;
            float nieuweHoekY = Mathf.MoveTowardsAngle(huidigeHoekY, doelHoek, stap);
            transform.rotation = Quaternion.Euler(0, nieuweHoekY, 0);

            if (Mathf.Abs(Mathf.DeltaAngle(nieuweHoekY, doelHoek)) < 0.1f)
            {
                transform.rotation = Quaternion.Euler(0, doelHoek, 0);
                IsBusy = false; 

                // AANGEPAST: De draai is klaar, update de posities van de blokjes!
                CheckBlokjesPosities();
            }
        }
        else if (IsBlokjeAanwezig())
        {
            doelHoek = Mathf.Round((transform.eulerAngles.y + 90f) / 90f) * 90f;
            IsBusy = true;
        }
    }

    private bool IsBlokjeAanwezig()
    {
        if (detectieCollider == null) return false;
        Collider[] hits = Physics.OverlapBox(detectieCollider.bounds.center, detectieCollider.bounds.extents, transform.rotation, blokjesLayer);
        return hits.Length > 0;
    }

    private void CheckBlokjesPosities()
    {
        // Loop door alle objecten die aan de carrousel vastzitten (de childs)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            
            // Check of het object wel in de blokjes layer zit
            if (((1 << child.gameObject.layer) & blokjesLayer) != 0)
            {
                // We misbruiken een onzichtbare teller (of component) om de stappen te tellen.
                // Om het script compact te houden, slaan we de teller op in een unieke tag of een kleine custom script-check.
                // Maar het allermakkelijkst zonder extra scripts is een teller-component zoeken of toevoegen:
                BlokjeStappen teller = child.GetComponent<BlokjeStappen>();
                if (teller == null)
                {
                    teller = child.gameObject.AddComponent<BlokjeStappen>();
                }

                teller.stappengedaan++;

                // Als hij 3 posities heeft gehad, verdwijnt hij!
                if (teller.stappengedaan >= 3)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }
}

// Een super klein hulp-script dat automatisch op het blokje wordt gezet om te tellen
public class BlokjeStappen : MonoBehaviour
{
    public int stappengedaan = 0;
}