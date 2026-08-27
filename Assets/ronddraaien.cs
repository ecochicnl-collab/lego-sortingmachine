using UnityEngine;

public class ronddraaien : MonoBehaviour
{
    


    private Rigidbody rb;
    public bool simstarted = false;

    [Header("Blokkering Instellingen")]
    public Collider detectieCollieder;
    public LayerMask blokjesLayer;

    [Header("kanaal instelling 1 2 of 3")]
    [SerializeField] private int channelNummer = 1;
    [SerializeField] private float echtedraaiSnelheid = 10f;

    [SerializeField] public Vector3 draaiSnelheid = new Vector3(0f, 10f, 0f);


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void FixedUpdate()
    {
        if (!simstarted) return;

        UpdateSnelheid();

        if (IsBlokjeAanwezig())
        {
            return;
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null) return;
        }

        Quaternion extraRotatie = Quaternion.Euler(draaiSnelheid * Time.fixedDeltaTime);
        rb.MoveRotation(rb.rotation * extraRotatie);
    }

    private void UpdateSnelheid()
    {
        switch (channelNummer)
        {
            case 1:
                echtedraaiSnelheid = UImanagersim.SnelheidChannel1;
                break;
            case 2:
                echtedraaiSnelheid = UImanagersim.SnelheidChannel2;
                break;
            case 3:
                echtedraaiSnelheid = UImanagersim.SnelheidChannel3;
                break;
            default:
                echtedraaiSnelheid = 10f;
                break;
        }

        draaiSnelheid.y = echtedraaiSnelheid;
    }

    private bool IsBlokjeAanwezig()
    {
        if (detectieCollieder == null) return false;
        
        Collider[] overlappers = Physics.OverlapBox(
            detectieCollieder.bounds.center, 
            detectieCollieder.bounds.extents, 
            detectieCollieder.transform.rotation, 
            blokjesLayer
        );

        foreach (var col in overlappers)
        {
            // Check 1: Het mag niet de detectiecollider zelf zijn
            if (col == detectieCollieder) continue;

            // Check 2: Controleren of het object écht in de blokjesLayer zit
            if (((1 << col.gameObject.layer) & blokjesLayer) != 0)
            {
                return true;
            }
        }
        return false;
    }
}