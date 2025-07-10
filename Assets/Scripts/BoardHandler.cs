using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script manages the board. 
// It places and removes pieces
// It has no intelligence regarding chess rules
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

    private List<GameObject> blackPieces;
    private List<GameObject> whitePieces;

    private List<char> whiteCaptures; // The black pieces that white has captured. 
    private List<char> blackCaptures; // No need to keep pieces here, this is just to calculate values

    private Dictionary<char, GameObject> pieceNamePrefabConvertion;


    void Start()
    {
        blackPieces = new List<GameObject>();
        whitePieces = new List<GameObject>();
        whiteCaptures = new List<char>();
        blackCaptures = new List<char>();
        pieceNamePrefabConvertion = new Dictionary<char, GameObject>();

        InitializePieceDict();

        PlaceFENNotation("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    GameObject PlacePiece(GameObject piecePrefab,char pieceName, int x, int y)
    {
        Debug.Assert(x >= 1 && y >= 1 && x <= 8 && y <= 8);
        if (x < 1 || y < 1 || x > 8 || y > 8) return null; // The user can never make this happen. If this happens, the code messed up somewhere and we're dead
        
        GameObject b = Instantiate(piecePrefab, new Vector2(x, y), Quaternion.identity);
        PieceHandler p = b.GetComponent<PieceHandler>();
        p.pieceName = pieceName;
        p.rulesHandlerGameObject = rulesHandler;
        p.Init();

        // In ASCII, the BIG LETTERS come before the small ones. The small ones start with 'a' = 97
        // Furthermore, it follows from standard FEN-notation that white uses upper case letters
        if (pieceName < 97) 
        {
            whitePieces.Add(b);
        }
        else
        {
            blackPieces.Add(b);
        }

        return b;
    }

    // https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation
    public void PlaceFENNotation(string notation)
    {
        int x = 1;
        int y = 8;
        foreach(char c in notation)
        {
            if (c == ' ') break;
            if(c == '/')
            {
                // New row
                y -= 1;
                x = 1;
            }
            else if(char.IsNumber(c))
            {
                x += (int)c - 48; // 48 is ASCCI magic from ascii number to actual value
            }
            else
            {
                PlacePiece(pieceNamePrefabConvertion[c], c, x, y);
                x = x + 1;
            }
        }
    }

    void Capture(GameObject capturee)
    {
        // This function only removes the capturee and saves the captured value
        // Moving the attacker must be managed in piece logic, not here
        char pieceName = capturee.GetComponent<PieceHandler>().pieceName;
        if (pieceName < 97)
        {
            // A white piece was captured. Only black can do this!
            blackPieces.Remove(capturee);
            blackCaptures.Add(pieceName);
        }
        else
        {
            whitePieces.Remove(capturee);
            whiteCaptures.Add(pieceName);
        }
        Destroy(capturee); // The capturee is puree
    }

    void ClearBoard()
    {
        foreach (GameObject p in blackPieces)
        {
            Destroy(p);
        }
        foreach (GameObject p in whitePieces)
        {
            Destroy(p);
        }
    }

    void InitializePieceDict()
    {
        pieceNamePrefabConvertion.Add('P', whitePawnPrefab);
        pieceNamePrefabConvertion.Add('p', blackPawnPrefab);
        pieceNamePrefabConvertion.Add('N', whiteKnightPrefab);
        pieceNamePrefabConvertion.Add('n', blackKnightPrefab);
        pieceNamePrefabConvertion.Add('K', whiteKingPrefab);
        pieceNamePrefabConvertion.Add('k', blackKingPrefab);
        pieceNamePrefabConvertion.Add('Q', whiteQueenPrefab);
        pieceNamePrefabConvertion.Add('q', blackQueenPrefab);
        pieceNamePrefabConvertion.Add('B', whiteBishopPrefab);
        pieceNamePrefabConvertion.Add('b', blackBishopPrefab);
        pieceNamePrefabConvertion.Add('R', whiteRookPrefab);
        pieceNamePrefabConvertion.Add('r', blackRookPrefab);
    }
}
