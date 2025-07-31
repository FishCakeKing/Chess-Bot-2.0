using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Engine : MonoBehaviour
{
    // Greedy capture engine!

    // Result vs random moves engine: 51 draws
    // After choosing random move and beeing greedy: 5 wins 46 draws
    // After fixing promotion logic: 8 wins 43 draws

    // After adding checkmate in 1 logic: 44 wins 0 loss 57 draws


    public GameObject boardHandlerObject;
    public GameObject rulesHandlerObject;
    Piece[,] board;
    BoardHandler boardHandler;
    RulesHandler rulesHandler;
    public char activePlayer;
    public char enginePlayer;
    private bool isWhite;
    private Dictionary<char, float> pieceValues;

    void Start()
    {
        boardHandler = boardHandlerObject.GetComponent<BoardHandler>();
        board = boardHandler.GetBoard();
        rulesHandler = rulesHandlerObject.GetComponent<RulesHandler>();
        activePlayer = rulesHandler.GetActivePlayer();
        isWhite = rulesHandler.IsWhite(enginePlayer);
        LoadPieceValues();
    }

    
    void Update()
    {
        
    }

    // int fromx, int fromy, string move
    public (int,int,string) GetNextMove()
    {
        board = boardHandler.GetBoard();
        List<(int, int, string)> legalMoves = new List<(int, int,string)>();
        float timeLimit = 1f; // needs to be implemented
        activePlayer = rulesHandler.GetActivePlayer();
        if(activePlayer == enginePlayer)
            legalMoves = rulesHandler.GetAllValidMoves(enginePlayer,board);
        else
        {
            print("Called at the wrong time");
        }
        if(legalMoves.Count == 0)
        {
            // No legal moves. Checkmate!
            return (-1, -1, "-");
        }

        // Okay, we have a list of all possible moves.
        // Time to think
        (int, int, string) bestMove = GetRandomMove(legalMoves);
        float bestMoveScore = EvaluateMove(bestMove);
        //print("# of moves" + legalMoves.Count);
        foreach(var move in legalMoves)
        {
            float moveScore = EvaluateMove(move);
            //print(move + " has score " + moveScore);
            if(moveScore > bestMoveScore)
            {
                //print("New best found! " + move + " with a score of " + moveScore);
                bestMoveScore = moveScore;
                bestMove = move;
            }
        }
        print("Best move is " + bestMove + " with score " + bestMoveScore);
        return bestMove;
    }

    private float EvaluateBoard(Piece[,] testBoard)
    {
        float evaluation = 0.0f;
        float enemypoints = 0;
        float friendlypoints = 0;
        // Add up the values of all pieces
        foreach(Piece p in testBoard)
        {
            if (p == null) continue;
            if (p.pieceName == '-') print(" WHAT!===========");
            if(IsWhite(p) == IsWhite(enginePlayer))
            {
                friendlypoints += pieceValues[p.pieceName];
            }else
            {
                enemypoints += pieceValues[p.pieceName];
            }
        }
        char enemyColor = enginePlayer == 'w' ? 'b':'w';
        var enemyMoves = rulesHandler.GetAllValidMoves(enemyColor, testBoard,true);
        evaluation = friendlypoints - enemypoints;
        if (enemyMoves.Count == 0) evaluation += 100; // VERY crude checkmate in 1 check, also finds draws



        return evaluation;
    }

    public float EvaluateMove((int,int,string) move)
    {
        //if (move.Item1 == 2) print("Rook move found "+move.Item3);
        Piece[,] testBoard = new Piece[9,9];
        for (int i = 1; i <= 8; i++)
        {
            for (int j = 1; j <= 8; j++)
            {
                if(board[i,j] != null)
                    testBoard[i, j] = new Piece(i,j,board[i, j].pieceName);
            }
        }
        testBoard = MakeMove(move, testBoard);
        //if(testBoard[2,1]!=null) print("On testboard at 2 1 there is a " + testBoard[2, 1].pieceName);
        return EvaluateBoard(testBoard);
    }


    public bool IsWhite(char name)
    {
        if (name == 'w') return true;
        if (name < 97) return true;
        return false;
    }
    public bool IsWhite(Piece p)
    {
        var name = p.pieceName;
        if (name == 'w') return true;
        if (name < 97) return true;
        return false;
    }

    private Piece[,] MakeMove((int,int,string) move,Piece[,] testBoard)
    {
        //print("At " + move.Item1 + " :" + move.Item2 + " there is a  " + testBoard[move.Item1, move.Item2].pieceName);
        var tmp = testBoard[move.Item1, move.Item2];
        testBoard[move.Item1, move.Item2] = null;
        (int, int) newCoords = GetCoordsFromSquareNotation(move.Item3);

        if(move.Item3.Length == 2)
        {
            testBoard[newCoords.Item1, newCoords.Item2] = tmp;
        }
        else
        {
            // Promotion move
            testBoard[newCoords.Item1, newCoords.Item2] = new Piece(move.Item1, move.Item2, move.Item3[3]);
        }
        testBoard[newCoords.Item1, newCoords.Item2].x = newCoords.Item1;
        testBoard[newCoords.Item1, newCoords.Item2].y = newCoords.Item2;
        // print("At " + newCoords.Item1+"("+testBoard[newCoords.Item1,newCoords.Item2].x+")" + ":" + newCoords.Item2 + "(" + testBoard[newCoords.Item1, newCoords.Item2].y + ")" + " there is a " + testBoard[newCoords.Item1, newCoords.Item2].pieceName);
        return testBoard;
    }

    public void SetBoard(PieceHandler[,] newBoard) 
    { 
        for(int i = 1;i<=8;i++)
        {
            for (int j = 1; j <= 8; j++)
            {
                if (newBoard[i, j] != null)
                    board[i, j] = new Piece(i, j,newBoard[i,j].pieceName);
                else
                    board[i, j] = null;
            }
        }
    }

    private (int, int, string) GetRandomMove(List<(int, int, string)> legalMoves)
    {
        int index = Random.Range(0, legalMoves.Count);
        return legalMoves[index];
    }

    public (int, int) GetCoordsFromSquareNotation(string not)
    {
        Debug.Assert(not.Length == 2 || not.Length == 4 && not[2] == '='); // Normal move, or promotion
        int x = (int)(not[0] - 96);
        int y = (int)(not[1] - 48);
        return (x, y);
    }

    private void LoadPieceValues()
    {
        pieceValues = new Dictionary<char, float>();
        pieceValues.Add('q', 9);
        pieceValues.Add('Q', 9);
        pieceValues.Add('b', 3);
        pieceValues.Add('B', 3);
        pieceValues.Add('n', 3);
        pieceValues.Add('N', 3);
        pieceValues.Add('r', 5);
        pieceValues.Add('R', 5);
        pieceValues.Add('p', 1);
        pieceValues.Add('P', 1);
        pieceValues.Add('k', 100);
        pieceValues.Add('K', 100);
        pieceValues.Add('-', 0);
        pieceValues.Add('\0', 0);
    }
}
