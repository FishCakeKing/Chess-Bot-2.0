using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RulesHandler : MonoBehaviour
{
    public GameObject boardObject;
    BoardHandler boardHandler;
    PieceHandler[,] board;
    private string enPassantSquare;
    private int enPassantDir;

    void Start()
    {
        boardHandler = boardObject.GetComponent<BoardHandler>();
        board = boardHandler.GetBoard();
        enPassantSquare = "-";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool IsMoveLegal(int fromx, int fromy, int tox, int toy)
    {
        // Call the corresponding movement function
        char piece = board[fromx, fromy].pieceName;
        var legalMoves = GetLegalMoves(fromx, fromy);
        switch (piece)
        {
            // Pawn moves
            case 'p':
            case 'P':
                if (legalMoves.Contains((tox, toy)))
                {
                    // The desired move is legal. 
                    if (enPassantSquare != "-" && GetCoordsFromSquareNotation(enPassantSquare) == (tox, toy))
                    {
                        boardHandler.didEnPassant = true;
                    }
                    if (System.Math.Abs(fromy-toy) == 2) 
                    {
                        // Moved two spaces. Can en passant
                        SetEnPassant(tox, fromy);
                    }
                    else
                    {
                        SetEnPassantInvalid();
                    }

                    return true;
                }
                return false;
            default:
                Debug.Log("Invalid piece moved! " + piece + " at " + fromx + "," + fromy);
                return false;
        }
    }

    public List<(int, int)> GetLegalMoves(int fromx, int fromy)
    {
        char piece = board[fromx, fromy].pieceName;
        switch (piece)
        {
            // Pawn moves
            case 'p':
            case 'P':
                return LegalMovesPawn(fromx, fromy);
            default:
                return new List<(int, int)>();
        }
    }

    private bool IsSquareEmpty(int x, int y)
    {
        return board[x, y] == null;
    }

    // Does x,y contain a piece of opposite color?
    private bool SquareHasAnEnemy(PieceHandler attacker, int x, int y)
    {
        if (board[x, y] == null) return false;

        return (IsWhite(attacker) != IsWhite(board[x, y]));
    }

    private bool MovePutsSelfInCheck(int fromx, int fromy, int tox, int toy)
    {
        return false;
    }

    // Below are all the functions that return all legal moves for any given piece on the board
    private List<(int,int)> LegalMovesPawn(int fromx, int fromy)
    {
        List<(int, int)> validSquares = new List<(int, int)>();
        bool isWhite = IsWhite(board[fromx, fromy]);

        int dir = 1;
        if (!IsWhite(board[fromx, fromy])) dir = -1;

        // A pawn can: 
        // 1. Move 1 step forward
        // 2. Move 2 steps forward, if it is on the base row
        // 3. Capture diagonally
        // 4. Capture en passant
        // 5. Promote

        // 1. Move 1 step forward
        // This does NOT include promotion, that is handled further down
        if (fromy + dir <= 7 && fromy + dir >= 2)
        {
            if(IsSquareEmpty(fromx, fromy + dir))
            {
                validSquares.Add((fromx, fromy + dir));
            }
        }

        // 2.Move 2 steps forward, if it is on the base row
        if (isWhite && fromy == 2 || !isWhite && fromy == 7)
        {
            if(IsSquareEmpty(fromx,fromy+dir*2))
            {
                validSquares.Add((fromx, fromy + dir*2));
                enPassantDir = dir;
            }
        }

        // 3. Capture
        // Capture left
        if(fromx > 1 && SquareHasAnEnemy(board[fromx,fromy],fromx-1,fromy+dir))
        {
            validSquares.Add((fromx - 1, fromy + dir));
        }

        // Capture right
        if (fromx < 8 && SquareHasAnEnemy(board[fromx, fromy], fromx + 1, fromy + dir))
        {
            validSquares.Add((fromx + 1, fromy + dir));
        }

        // 4. En passant
        // En passant left. During en passant, the pawns are on the same y-level
        if (fromx > 1 && SquareHasAnEnemy(board[fromx, fromy], fromx - 1, fromy))
        {
            if(enPassantSquare != "-" && GetCoordsFromSquareNotation(enPassantSquare).Item1-fromx == -1)
                validSquares.Add((fromx - 1, fromy + dir));
        }

        // En passant right
        if (fromx < 8 && SquareHasAnEnemy(board[fromx, fromy], fromx + 1, fromy))
        {
            if (enPassantSquare != "-" && GetCoordsFromSquareNotation(enPassantSquare).Item1 - fromx == 1)
                validSquares.Add((fromx + 1, fromy + dir));
        }

        return validSquares;
    }

    private bool IsWhite(PieceHandler p)
    {
        if (p.pieceName < 97) return true;
        return false;
    }

    private void SetEnPassant(int tox,int fromy)
    {
        enPassantSquare = boardHandler.GetSquareNotation(tox, fromy + enPassantDir);
        boardHandler.SetEnPassant(enPassantSquare);
    }
    private void SetEnPassantInvalid()
    {
        enPassantSquare = "-";
        boardHandler.SetEnPassant(enPassantSquare);
    }

    public (int, int) GetCoordsFromSquareNotation(string not)
    {
        Debug.Assert(not.Length == 2);
        int x = (int)(not[0] - 96);
        int y = (int)(not[1] - 48);
        return (x, y);
    }
}
