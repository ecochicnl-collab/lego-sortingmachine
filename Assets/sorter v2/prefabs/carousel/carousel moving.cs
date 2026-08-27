using UnityEngine;
using UnityEngine.InputSystem;

public class carouselmoving : MonoBehaviour
{

    public Rigidbody basisRigidbody;
    public Rigidbody[] bakjes;
    public float snelheid = 5f;
    private float doelBasisY;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        doelBasisY = 0f;
    }

    public Vector3 basisDraaiAs = new Vector3(0, 1, 0);
    public Vector3 bakjeKantelAs = new Vector3(0, 0, 1);
    public Vector3 bakjePositieOffset = new Vector3(0, 0, 0);

    // Update is called once per frame
    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
        {
            doelBasisY += 90f;
        }
        if (UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
        {
            
        }
    }
    private void FixedUpdate()
    {
        Quaternion huidigeRotatieBasis = basisRigidbody.rotation;
        Quaternion doelRotatieBasis = Quaternion.Euler(basisDraaiAs * doelBasisY);
        basisRigidbody.rotation = Quaternion.Slerp(huidigeRotatieBasis, doelRotatieBasis, snelheid * Time.fixedDeltaTime);

        foreach (Rigidbody bakje in bakjes)
        {
            if (bakje == null) continue;

            Vector3 LokaleRotatie = bakje.transform.localEulerAngles;

            float wereldY = bakje.transform.eulerAngles.y;
            wereldY = Mathf.Repeat(wereldY, 360f);

            float doelZ = (wereldY < 3f || wereldY > 357f) ? -45f : 0f;

            float huidigeHoek = 0f;
            if (bakjeKantelAs.x > 0) huidigeHoek = LokaleRotatie.x;
            else if (bakjeKantelAs.y > 0) huidigeHoek = LokaleRotatie.y;
            else if (bakjeKantelAs.z > 0) huidigeHoek = LokaleRotatie.z;

            if (huidigeHoek > 180f) huidigeHoek -= 360f;
            float nieuweHoek = Mathf.Lerp(huidigeHoek, doelZ, snelheid * Time.fixedDeltaTime);
            float nieuweZ = Mathf.Lerp(huidigeHoek, doelZ, snelheid * Time.fixedDeltaTime);

            Vector3 targetLokaleEuler = new Vector3(
            bakjeKantelAs.x > 0 ? nieuweHoek : LokaleRotatie.x,
            bakjeKantelAs.y > 0 ? nieuweHoek : LokaleRotatie.y,
            bakjeKantelAs.z > 0 ? nieuweHoek : LokaleRotatie.z
            );

            Quaternion nieuweLokaleRotatie = Quaternion.Euler(targetLokaleEuler);
            Vector3 doelWereldPositie = basisRigidbody.position + basisRigidbody.rotation * bakjePositieOffset;
            bakje.MovePosition(doelWereldPositie);
            bakje.MoveRotation(basisRigidbody.rotation * nieuweLokaleRotatie);
        }
    }
}
