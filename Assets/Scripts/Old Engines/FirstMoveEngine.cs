using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstMoveEngine : MonoBehaviour
{
    public GameObject boardHandlerObject;
    public GameObject rulesHandlerObject;
    Piece[,] board;
    BoardHandler boardHandler;
    RulesHandler rulesHandler;
    public char activePlayer;
    public char enginePlayer;
    void Start()
    {
        boardHandler = boardHandlerObject.GetComponent<BoardHandler>();
        board = boardHandler.GetBoard();
        rulesHandler = rulesHandlerObject.GetComponent<RulesHandler>();
        activePlayer = rulesHandler.GetActivePlayer();
    }


    void Update()
    {

    }

    // int fromx, int fromy, string move
    public (int, int, string) GetNextMove()
    {
        List<(int, int, string)> legalMoves = new List<(int, int, string)>();
        activePlayer = rulesHandler.GetActivePlayer();
        if (activePlayer == enginePlayer)
            legalMoves = rulesHandler.GetAllValidMoves(enginePlayer,board);
        else
        {
            print("Called at the wrong time");
        }
        if (legalMoves.Count == 0)
        {
            // No legal moves. Checkmate!
            return (-1, -1, "-");
        }
        //print("Returning " + legalMoves[0]);
        return legalMoves[0];
    }
    public void SetBoard(Piece[,] newBoard) { board = newBoard; }
}
