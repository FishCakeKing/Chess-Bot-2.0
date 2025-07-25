using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Engine : MonoBehaviour
{
    public GameObject boardHandlerObject;
    public GameObject rulesHandlerObject;
    PieceHandler[,] board;
    BoardHandler boardHandler;
    RulesHandler rulesHandler;
    public char activePlayer;
    public char enginePlayer;
    private bool isWhite;

    void Start()
    {
        boardHandler = boardHandlerObject.GetComponent<BoardHandler>();
        board = boardHandler.GetBoard();
        rulesHandler = rulesHandlerObject.GetComponent<RulesHandler>();
        activePlayer = rulesHandler.GetActivePlayer();
        isWhite = rulesHandler.IsWhite(enginePlayer);
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
        //print("Returning " + legalMoves[0]);
        return GetRandomMove(legalMoves);
    }

    private float EvaluateBoard()
    {
        float evaluation = 0.0f;



        return evaluation;
    }

    public void SetBoard(PieceHandler[,] newBoard) { board = newBoard; }

    private (int, int, string) GetRandomMove(List<(int, int, string)> legalMoves)
    {
        int index = Random.Range(0, legalMoves.Count);
        return legalMoves[index];
    }
}
