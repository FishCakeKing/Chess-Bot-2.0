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
    public GameObject rulesHandlerObject;

    public GameObject engineObject;
    public GameObject engine2Object;

    public GameObject highLightPrefab;
    public GameObject highLight2Prefab;

    public GameObject promotionPaneWhitePrefab;
    public GameObject promotionPaneBlackPrefab;

    public List<GameObject> blackPieces;
    public List<GameObject> whitePieces;
    private List<GameObject> highLights;

    private List<char> whiteCaptures; // The black pieces that white has captured. 
    private List<char> blackCaptures; // No need to keep pieces here, this is just to calculate values

    private Dictionary<char, GameObject> pieceNamePrefabConvertion;

    public List<string> activePieceReachableSquares;

    public bool didEnPassant;

    [SerializeField]
    private string StartingFEN;

    public string FEN;

    private PieceHandler[,] board;

    private Engine engine;
    private RandomMoveEngine engine2;

    private int engineOneWins;
    private int engineTwoWins;
    private int draws;

    private RulesHandler rulesHandler;

    private GameObject promotionPane;

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
    [SerializeField]
    private bool enableEngines;

    [SerializeField]
    private char engine1Color;

    [SerializeField]
    private char engine2Color;

    private bool displayedGameResult;

    private bool displayedTournamentResults;

    private (int, int, string) nextMove;

    [SerializeField]
    private int tournamentGames;

    void Awake()
    {
        //FEN = "rN1k1br1/4p1pp/p1n2p1n/1pP1q3/3p4/3p1B2/PP1QbP1P/R1B3K1 b - - 2 29";
        FEN = StartingFEN;//"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        //FEN = "4B2N/8/8/k6B/6N1/3K4/8/1q6 w - - 0 1";
        blackPieces = new List<GameObject>();
        whitePieces = new List<GameObject>();
        highLights = new List<GameObject>();
        whiteCaptures = new List<char>();
        blackCaptures = new List<char>();
        pieceNamePrefabConvertion = new Dictionary<char, GameObject>();
        activePieceReachableSquares = new List<string>();
        engine = engineObject.GetComponent<Engine>();
        engine2 = engine2Object.GetComponent<RandomMoveEngine>();
        rulesHandler = rulesHandlerObject.GetComponent<RulesHandler>();

        engineOneWins = 0;
        engineTwoWins = 0;
        draws = 0;

        displayedGameResult = false;
        displayedTournamentResults = false;

        board = new PieceHandler[9, 9]; // Sorry, but when talking about rows 1-8, worrying about 0-indexing just causes confusion later down the line

        InitializePieceDict();

        PlaceFENNotation(FEN);


    }

    void Update()
    {
         if(rulesHandler.IsGameOver())
         {
             HandleGameOver();
             return;
         }
         
        if (enableEngines)
        {
            if (activeColor == engine1Color) // just for debug
            {
                engine.SetBoard(board);
                nextMove = engine.GetNextMove();                

            }
            
            else if(activeColor == engine2Color)
            {
                engine2.SetBoard(board);
                nextMove = engine2.GetNextMove();
            }

            if (rulesHandler.IsGameOver())
            {
                return;
            }
            //print(board[nextMove.Item1, nextMove.Item2].pieceName + " " + nextMove.Item3);
            board[nextMove.Item1, nextMove.Item2].Move(nextMove.Item3);
            FEN = GetFEN();
        }

    }

    GameObject PlacePiece(GameObject piecePrefab,char pieceName, int x, int y)
    {
        Debug.Assert(x >= 1 && y >= 1 && x <= 8 && y <= 8);
        if (x < 1 || y < 1 || x > 8 || y > 8) return null; // The user can never make this happen. If this happens, the code messed up somewhere and we're dead
        
        GameObject b = Instantiate(piecePrefab, new Vector2(x, y), Quaternion.identity,this.transform);
        PieceHandler p = b.GetComponent<PieceHandler>();
        p.pieceName = pieceName;
        p.rulesHandlerGameObject = rulesHandlerObject;
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

    // Move the piece and handle promotion
    public bool MovePiece(int fromx, int fromy, string move)
    {
        // Here we do not validate if this is a valid move! That is done when deciding to move the piece
        (int, int) newCoords = GetCoordsFromSquareNotation(move);
        bool capture = false;

        // Step 1: Handle promotion
        if (move.Length == 4)
        {
            // Promotion move
            Debug.Assert(move[2] == '=');

            // Destroy the pawn
            Destroy(board[fromx, fromy].gameObject);


            // Get new piece properties
            char desiredPiece = move[3];

            // Handle eventual capture
            if (board[newCoords.Item1, newCoords.Item2] != null)
            {
                Capture(board[newCoords.Item1, newCoords.Item2].gameObject);
                capture = true;
            }

            // Instantiate the correct prefab
            GameObject newPieceObj = PlacePiece(pieceNamePrefabConvertion[desiredPiece], desiredPiece, newCoords.Item1, newCoords.Item2);
            PieceHandler newPiece = newPieceObj.GetComponent<PieceHandler>();

            // Initialize and save
            board[newCoords.Item1, newCoords.Item2] = newPiece;

            return capture;
        }

        // Step 2: Handle all non-promotion moves
        PieceHandler tmp = board[fromx, fromy];
        board[fromx, fromy] = null;
        if (board[newCoords.Item1, newCoords.Item2] != null)
        {
            Capture(board[newCoords.Item1, newCoords.Item2].gameObject);
            capture = true;
        }
        else if(didEnPassant)
        {
            // Did en passant
            print("did en passee");
            didEnPassant = false;
            int dir = newCoords.Item2 - fromy;
            Debug.Assert(System.Math.Abs(dir) == 1);
            if(System.Math.Abs(dir) != 1)
            {
                print("Huh!? Tried to en passant from " + fromx + ":" + fromy + " to " + newCoords.Item1 + ":" + newCoords.Item2);
                Debug.Break();
            }
            Capture(board[newCoords.Item1, newCoords.Item2 - dir].gameObject);
            capture = true;
        }
        board[newCoords.Item1, newCoords.Item2] = tmp;
        board[newCoords.Item1, newCoords.Item2].MoveTo(newCoords.Item1, newCoords.Item2);

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

    void HandleTournament()
    {
        if(rulesHandler.IsGameOver() && ! displayedTournamentResults)
        {
            switch (rulesHandler.GameResult())
            {
                case 'b':
                    if (engine1Color == 'b') engineOneWins += 1;
                    else engineTwoWins += 1;
                    break;
                case 'w':
                    if (engine1Color == 'w') engineOneWins += 1;
                    else engineTwoWins += 1;
                    break;
                case 'd':
                    draws += 1;
                    break;
                default:
                    print("Unknown game result " + rulesHandler.GameResult());
                    break;
            }


            if(engineOneWins+engineTwoWins+draws <= tournamentGames)
            {
                RemoveHighlights();
                ClearBoard();
                PlaceFENNotation(StartingFEN);
                rulesHandler.ResetVariables();
            }
            else
            {
                displayedTournamentResults = true;
                print("Match results: ");
                print("Engine one wins: "+engineOneWins);
                print("Engine two wins: " + engineTwoWins);
                print("Draws: " + draws);
            }
        }
    }

    void ClearBoard()
    {
        foreach(PieceHandler p in board)
        {
            if (p != null) Destroy(p.gameObject);
        }
        board = new PieceHandler[9, 9];
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

    public Piece[,] GetBoard()
    {
        Piece[,] pieceBoard = new Piece[9, 9];
        for(int i = 1;i<=8;i++)
        {
            for(int j = 1; j<=8;j++)
            {
                if(board[i,j] != null)
                    pieceBoard[i, j] = new Piece(i, j,board[i,j].pieceName,board[i,j].p.pieceObject);
            }
        }
        return pieceBoard;
    }

    public PieceHandler[,] GetPHBoard()
    {
        return board;
    }

    // Convert something like 4,4 to d4
    public string GetSquareNotation(int x, int y)
    {
        return (char)(x+96) + y.ToString(); // Even more ascii magic. 'a' = 97
    }
    public (int, int) GetCoordsFromSquareNotation(string not)
    {
        Debug.Assert(not.Length == 2 || not.Length == 4 && not[2] == '='); // Normal move or promotion
        int x = (int)(not[0] - 96);
        int y = (int)(not[1] - 48);
        return (x, y);
    }

    public void HighLightSquares(List<string> squares)
    {
        foreach(string squareNotation in squares)
        {
            (int,int) square = GetCoordsFromSquareNotation(squareNotation);
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
        foreach(GameObject square in highLights)
        {
            if (square == null) continue;
            
            (int,int) position = ((int)square.transform.position.x,(int)square.transform.position.y);
            if(position == (x,y))
            {
                Destroy(g);
                return;
            }
        }
        highLights.Add(g);
    }

    public void RemoveHighlights()
    {
        foreach(var a in highLights)
        {
            Destroy(a.gameObject);
        }
        activePieceReachableSquares = new List<string>();
    }

    private void HandleGameOver()
    {
        if (!displayedGameResult)
        {
            displayedGameResult = true;
            switch (rulesHandler.GameResult())
            {
                case 'b':
                    print("Black wins!");
                    break;
                case 'w':
                    print("White wins!");
                    break;
                case 'd':
                    print("It's a draw!");
                    break;
                default:
                    print("Unknown game result " + rulesHandler.GameResult());
                    break;
            }
        }
        HandleTournament();
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

    public void SetActivePlayer(char p)
    {
        activeColor = p;
    }

    public void SetPromotionPane(GameObject g) { promotionPane = g; }
    public void DestroyPromotionPlane() { Destroy(promotionPane); }

    public void SetFEN(string fen) { FEN = fen; }


}
