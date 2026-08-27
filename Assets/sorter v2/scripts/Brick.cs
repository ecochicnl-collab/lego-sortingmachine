using UnityEngine;
using System.Collections.Generic;

public class Brick : MonoBehaviour
{
    [System.Serializable]
    public class BlokLocatie
    {
        public string part_num;
        public int vakje;
        public string kant;
    }

    [System.Serializable]
    public class SorterIndeling
    {
        public List<BlokLocatie> locaties = new List<BlokLocatie>();
    }

    [System.Serializable]
    public class BrickData
    {
        public string part_num;
        public string name;
        public int part_cat_id;
    }

    [System.Serializable]
    public class BrickDataset
    {
        public List<BrickData> bricks;
    }
}
