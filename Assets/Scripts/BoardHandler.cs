using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardHandler : MonoBehaviour
{
    public GameObject blackPawnPrefab;
    public GameObject rulesHandler;

    void Start()
    {
        GameObject b = Instantiate(blackPawnPrefab, new Vector2(4, 4), Quaternion.identity,this.transform);
        b.GetComponent<PieceHandler>().rulesHandlerGameObject = rulesHandler;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
