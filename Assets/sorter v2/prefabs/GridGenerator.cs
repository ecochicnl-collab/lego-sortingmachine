using UnityEngine;

public class GridGenerator : MonoBehaviour
{

    public GameObject vakjePrefab;
    public int aantalVakjes = 500;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        for (int i = 0; i < aantalVakjes; i++)
        {
            GameObject nieuwVakje = Instantiate(vakjePrefab, this.transform);
            nieuwVakje.name = "Vakje_" + (i + 1);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
