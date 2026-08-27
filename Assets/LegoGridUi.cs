using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class LegoGridUi : MonoBehaviour
{
    public GameObject knopPrefab; 
    public search legoSearchScript; 

    private int geselecteerdeBreedte = -1;
    private int geselecteerdeLengte = -1;

    void Start()
    {
        if (legoSearchScript == null)
        {
            Debug.LogError("[GridUI] Sleep het GameObject met het 'search' script in de LegoGridUi component!");
            return;
        }

        if (knopPrefab == null)
        {
            Debug.LogError("[GridUI] knopPrefab ontbreekt. Sleep de prefab in de inspector!");
            return;
        }

        GridLayoutGroup grid = GetComponent<GridLayoutGroup>();
        if (grid == null) 
        {
            grid = gameObject.AddComponent<GridLayoutGroup>();
        }

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;
        grid.cellSize = new Vector2(100f, 100f);
        grid.spacing = new Vector2(12f, 12f);

        Debug.Log("[GridUI] Grid wordt opgebouwd met 8 kolommen");

        for (int breedte = 1; breedte <= 8; breedte++)
        {
            for (int lengte = 1; lengte <= 8; lengte++)
            {
                int b = breedte;
                int l = lengte;

                GameObject nieuweKnop = Instantiate(knopPrefab, transform);
                nieuweKnop.name = b + "x" + l;

                TMP_Text tmpTekst = nieuweKnop.GetComponentInChildren<TMP_Text>();
                if (tmpTekst != null) tmpTekst.text = b + "x" + l;

                Button btn = nieuweKnop.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => OnGridKnopGeklikt(b, l));
                }
            }
        }

        Debug.Log($"[GridUI] Grid klaar: {transform.childCount} knoppen gegenereerd");
    }

    public void OnGridKnopGeklikt(int breedte, int lengte)
    {
        if (legoSearchScript == null) return;

        if (geselecteerdeBreedte == breedte && geselecteerdeLengte == lengte)
        {
            geselecteerdeBreedte = -1;
            geselecteerdeLengte = -1;
            
            legoSearchScript.ResetGrid(); 
            Debug.Log("[GridUI] Grid filter uitgeschakeld. Database toont weer alles.");
        }
        else
        {
            geselecteerdeBreedte = breedte;
            geselecteerdeLengte = lengte;
            
            legoSearchScript.SetGridMaat(breedte, lengte); 
            Debug.Log($"[GridUI] Grid filter ingesteld op database voor naam-maat: {breedte} x {lengte}");
        }
    }
}