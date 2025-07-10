using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardHandler : MonoBehaviour
{
    public GameObject blackPawnPrefab;
    public GameObject whitePawnPrefab;
    public GameObject blackRookPrefab;
    public GameObject whiteRookPrefab;
    public GameObject blackKnightPrefab;
    public GameObject whiteKnightPrefab;
    public GameObject blackBishopPrefab;
    public GameObject whiteBishopPrefab;
    public GameObject blackQueenPrefab;
    public GameObject whiteQueenPrefab;
    public GameObject blackKingPrefab;
    public GameObject whiteKingPrefab;
    public GameObject rulesHandler;

    void Start()
    {
        PlacePiece(blackPawnPrefab, 'p', 4, 4);
        PlacePiece(blackPawnPrefab, 'p', 5, 4);
        PlacePiece(blackPawnPrefab, 'p', 6, 4);
        PlacePiece(whiteBishopPrefab, 'p', 3, 3);
        PlacePiece(blackKingPrefab, 'p', 1, 1);
        PlacePiece(whiteRookPrefab, 'p', 2, 7);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void PlacePiece(GameObject piecePrefab,char pieceName, int x, int y)
    {
        Debug.Assert(x >= 1 && y >= 1 && x <= 8 && y <= 8);
        if (x < 1 || y < 1 || x > 8 || y > 8) return; // The user can never make this happen. If this happens, the code messed up somewhere
        
        GameObject b = Instantiate(piecePrefab, new Vector2(x, y), Quaternion.identity);
        PieceHandler p = b.GetComponent<PieceHandler>();
        p.pieceName = pieceName;
        p.rulesHandlerGameObject = rulesHandler;
        p.Init();
    }
}
