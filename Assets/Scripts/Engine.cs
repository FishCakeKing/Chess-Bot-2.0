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

    // int fromx, int fromy, int tox, int toy
    public (int,int,int,int) GetNextMove()
    {
        print("Active is " + activePlayer);
        print("We is " + enginePlayer);
        List<(int, int, int, int)> legalMoves = new List<(int, int, int, int)>();
        float timeLimit = 1f; // needs to be implemented
        activePlayer = rulesHandler.GetActivePlayer();
        if(activePlayer == enginePlayer)
            legalMoves = rulesHandler.GetAllValidMoves(enginePlayer);
        else
        {
            print("Called at the wrong time");
        }
        print("Returning " + legalMoves[0]);
        return legalMoves[0];
    }
}
