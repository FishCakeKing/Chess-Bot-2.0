using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Engine : MonoBehaviour
{
    // Greedy capture engine!

    // Result vs random moves engine: 51 draws
    // After choosing random move and beeing greedy: 5 wins 46 draws

    public GameObject boardHandlerObject;
    public GameObject rulesHandlerObject;
    char[,] board;
    BoardHandler boardHandler;
    RulesHandler rulesHandler;
    public char activePlayer;
    public char enginePlayer;
    private bool isWhite;
    private Dictionary<char, float> pieceValues;

    void Start()
    {
        board = new char[9, 9];
        boardHandler = boardHandlerObject.GetComponent<BoardHandler>();
        SetBoard(boardHandler.GetBoard());
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
        List<(int, int, string)> legalMoves = new List<(int, int,string)>();
        float timeLimit = 1f; // needs to be implemented
        activePlayer = rulesHandler.GetActivePlayer();
        if(activePlayer == enginePlayer)
            legalMoves = rulesHandler.GetAllValidMoves(enginePlayer);
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

        return bestMove;
    }

    private float EvaluateBoard(char[,] testBoard)
    {
        float evaluation = 0.0f;
        float enemypoints = 0;
        float friendlypoints = 0;

        // Add up the values of all pieces
        foreach(char p in testBoard)
        {
            if (p == '-') continue;
            if(IsWhite(p) == IsWhite(enginePlayer))
            {
                friendlypoints += pieceValues[p];
            }else
            {
                enemypoints += pieceValues[p];
            }
        }

        evaluation = friendlypoints - enemypoints;

        return evaluation;
    }

    public float EvaluateMove((int,int,string) move)
    {
        char[,] testBoard = new char[9,9];
        for (int i = 1; i <= 8; i++)
        {
            for (int j = 1; j <= 8; j++)
            {
                testBoard[i, j] = board[i, j];
            }
        }
        testBoard = MakeMove(move, testBoard);
        return EvaluateBoard(testBoard);
    }


    public bool IsWhite(char name)
    {
        if (name == 'w') return true;
        if (name < 97) return true;
        return false;
    }

    private char[,] MakeMove((int,int,string) move,char[,] testBoard)
    {
        var tmp = testBoard[move.Item1, move.Item2];
        testBoard[move.Item1, move.Item2] = '-';
        (int, int) newCoords = GetCoordsFromSquareNotation(move.Item3);
        testBoard[newCoords.Item1, newCoords.Item2] = tmp;

        return testBoard;
    }

    public void SetBoard(PieceHandler[,] newBoard) 
    { 
        for(int i = 1;i<=8;i++)
        {
            for (int j = 1; j <= 8; j++)
            {
                if (newBoard[i, j] != null)
                    board[i, j] = newBoard[i, j].pieceName;
                else
                    board[i, j] = '-';
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
