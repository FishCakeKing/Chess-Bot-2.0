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

    public GameObject highLightPrefab;
    public GameObject highLight2Prefab;

    public List<GameObject> blackPieces;
    public List<GameObject> whitePieces;
    private List<GameObject> highLights;

    private List<char> whiteCaptures; // The black pieces that white has captured. 
    private List<char> blackCaptures; // No need to keep pieces here, this is just to calculate values

    private Dictionary<char, GameObject> pieceNamePrefabConvertion;

    public List<(int, int)> activePieceReachableSquares;

    public bool didEnPassant;

    private string FEN;

    private PieceHandler[,] board;

    [SerializeField]
    private char activeColor;
    [SerializeField]
    private string castlingRights;
    [SerializeField]
    private string enPassantSquare;
    [SerializeField]
    private short halfmoveClock;
    [SerializeField]
    private short fullmoveNumber;


    void Awake()
    {
        //FEN = "rN1k1br1/4p1pp/p1n2p1n/1pP1q3/3p4/3p1B2/PP1QbP1P/R1B3K1 b - - 2 29";
        FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        blackPieces = new List<GameObject>();
        whitePieces = new List<GameObject>();
        highLights = new List<GameObject>();
        whiteCaptures = new List<char>();
        blackCaptures = new List<char>();
        pieceNamePrefabConvertion = new Dictionary<char, GameObject>();
        activePieceReachableSquares = new List<(int, int)>();

        board = new PieceHandler[9, 9]; // Sorry, but when talking about rows 1-8, worrying about 0-indexing just causes confusion later down the line

        InitializePieceDict();

        PlaceFENNotation(FEN);

    }

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
        p.boardHandlerObject = this.gameObject;
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

    public bool MovePiece(int fromx, int fromy, int tox, int toy)
    {
        // Here we do not validate if this is a valid move! That is done when deciding to move the piece
        PieceHandler tmp = board[fromx, fromy];
        board[fromx, fromy] = null;
        bool capture = false;
        if (board[tox, toy] != null)
        {
            Capture(board[tox, toy].gameObject);
            capture = true;
        }
        else if(didEnPassant)
        {
            // Did en passant
            didEnPassant = false;
            int dir = toy - fromy;
            Debug.Assert(System.Math.Abs(dir) == 1);
            Capture(board[tox, toy - dir].gameObject);
            capture = true;
        }
        board[tox, toy] = tmp;

        RemoveHighlights();
        return capture;
    }

    // https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation
    public void PlaceFENNotation(string notation)
    {
        string[] parts = notation.Split(' ');

        Debug.Assert(parts.Length == 6); // Pieces, active color, castling, en passant, clock, count

        int x = 1;
        int y = 8;
        foreach(char c in parts[0])
        {
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
                GameObject p = PlacePiece(pieceNamePrefabConvertion[c], c, x, y);
                board[x, y] = p.GetComponent<PieceHandler>();
                x = x + 1;
            }
        }
        Debug.Assert(parts[1].Length == 1); // If this is not 1 the FEN notation is incorrect
        activeColor = (char)parts[1][0];

        castlingRights = parts[2];
        enPassantSquare = parts[3];
        halfmoveClock = short.Parse(parts[4]);
        fullmoveNumber = short.Parse(parts[5]);

        FEN = notation;
    }
    public string GetFEN()
    {
        return FEN;
    }
    public void SetEnPassant(string square)
    {
        enPassantSquare = square;
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
            PieceHandler piece = p.GetComponent<PieceHandler>();
            board[piece.x, piece.y] = null;
            Destroy(p);
        }
        foreach (GameObject p in whitePieces)
        {
            PieceHandler piece = p.GetComponent<PieceHandler>();
            board[piece.x, piece.y] = null;
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

    public PieceHandler[,] GetBoard()
    {
        return board;
    }

    // Convert something like 4,4 to d4
    public string GetSquareNotation(int x, int y)
    {
        return (char)(x+96) + y.ToString(); // Even more ascii magic. 'a' = 97
    }

    public void HighLightSquares(List<(int,int)> squares)
    {
        foreach((int,int) square in squares)
        {
            HighlightSquare(square.Item1, square.Item2);
        }
    }

    public void HighlightSquare(int x, int y,bool secondaryColor = false)
    {
        GameObject g;
        if(!secondaryColor)
        {
            g = Instantiate(highLightPrefab, new Vector2(x + 0.0f, y + 0.0f), Quaternion.identity, this.transform);
        }
        else
        {
            g = Instantiate(highLight2Prefab, new Vector2(x + 0.0f, y + 0.0f), Quaternion.identity, this.transform);
        }
        highLights.Add(g);
    }

    public void RemoveHighlights()
    {
        foreach(var a in highLights)
        {
            Destroy(a.gameObject);
        }
        activePieceReachableSquares = new List<(int, int)>();
    }
    // These two setters (counters and castling) are just for debug, so that we can see what is going on in one place    
    public void SetCounters((short,short)counters)
    {
        halfmoveClock = counters.Item1;
        fullmoveNumber = counters.Item2;
    }
    public void SetCastling((bool,bool,bool,bool) rights)
    {
        //  (whiteShortCastle, whiteLongCastle, blackShortCastle, blackLongCastle)
        string newRights = "";
        if (rights.Item1) newRights += "K";
        if (rights.Item2) newRights += "Q";
        if (rights.Item3) newRights += "k";
        if (rights.Item4) newRights += "q";
        if (newRights.Length == 0) newRights += "-";
        castlingRights = newRights;
    }
    public short GetHalfMoveClock() { return halfmoveClock; }
    public short GetFullMoveCounter() { return fullmoveNumber; }

    public string GetCastlingRights() { return castlingRights; }

}
