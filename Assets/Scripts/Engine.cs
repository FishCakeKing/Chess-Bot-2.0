using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Engine : MonoBehaviour
{
    // Less greedy checkmate in n engine, at halfmove depth = 2
    // Now avoids sacrifices that brings it down in material, as far as the depth can see
    // So, no more capturing a protected pawn with a queen

    // Result vs CheckmateInOne engine:
    // 63 wins 3 losses 36 draws

    // Vs human:
    // 0 wins 1 loss


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
        int depth = 2; // half moves
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
        if (legalMoves.Count < 10) depth += 1;
        // Okay, we have a list of all possible moves.
        // Time to think
        (int, int, string) bestMove = GetRandomMove(legalMoves);
        float bestMoveScore = EvaluateMove(bestMove,depth,board);
        //print("# of moves" + legalMoves.Count);
        foreach(var move in legalMoves)
        {
            //print("evaluating " + move);
            float moveScore = EvaluateMove(move,depth,board);
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

    private float EvaluateBoard(Piece[,] testBoard,int depth)
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
        //print("Evaluation is " + evaluation);
        // Go a step deeper: is this position actually good? What can the opponent do next?
        depth -= 1;
        float bestMoveScore = 0f;
        if (depth > 0 && enemyMoves.Count > 0)
        {
            var bestMove = GetRandomMove(enemyMoves);
            bestMoveScore = EvaluateMove(bestMove, depth,testBoard);

            // Find all opponent moves, return the highest scoring move, use that as our score instead (but negative)
            //print("# of moves" + enemyMoves.Count);
            foreach (var move in enemyMoves)
            {
                float moveScore = EvaluateMove(move, depth,testBoard);
                if(moveScore != 7f)
                if (moveScore < bestMoveScore)
                {
                    bestMoveScore = moveScore;
                    bestMove = move;
                }
            }

            return bestMoveScore;
        }
        return evaluation;
    }

    public float EvaluateMove((int,int,string) move,int depth,Piece[,] otherBoard)
    {
        //if (move.Item1 == 2) print("Rook move found "+move.Item3);
        Piece[,] testBoard = new Piece[9,9];
        for (int i = 1; i <= 8; i++)
        {
            for (int j = 1; j <= 8; j++)
            {
                if(otherBoard[i,j] != null)
                    testBoard[i, j] = new Piece(i,j, otherBoard[i, j].pieceName);
            }
        }
        testBoard = MakeMove(move, testBoard);
        //if(testBoard[2,1]!=null) print("On testboard at 2 1 there is a " + testBoard[2, 1].pieceName);
        return EvaluateBoard(testBoard,depth);
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

        
        var tmp = testBoard[move.Item1, move.Item2];
        testBoard[move.Item1, move.Item2] = null;
        (int, int) newCoords = GetCoordsFromSquareNotation(move.Item3);
        if(testBoard[newCoords.Item1, newCoords.Item2] != null && testBoard[newCoords.Item1, newCoords.Item2].pieceName == 'Q')
        print("and At " + newCoords.Item1 + " :" + newCoords.Item2 + " there is a  " + testBoard[newCoords.Item1, newCoords.Item2].pieceName);

        if (move.Item3.Length == 2)
        {
            testBoard[newCoords.Item1, newCoords.Item2] = new Piece(newCoords.Item1, newCoords.Item2, tmp.pieceName);
        }
        else
        {
            // Promotion move
            testBoard[newCoords.Item1, newCoords.Item2] = new Piece(newCoords.Item1, newCoords.Item2, move.Item3[3]);
        }

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
