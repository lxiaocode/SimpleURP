using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabGrid : MonoBehaviour
{
    public GameObject Prefab;
    public float Spacing = 1f;
    public int Rows = 10;
    public int Columns = 10;
    public int Layers = 10;

    void Start()
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                for (int layer = 0; layer < Layers; layer++)
                {
                    Vector3 position = new Vector3(col * Spacing, layer * Spacing, row * Spacing);
                    Instantiate(Prefab, position, Quaternion.identity, transform);
                }
            }
        }
    }
}
