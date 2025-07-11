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

        if(piece == 'p' || piece == 'P')
        {
            // Pawn move
            if (legalMoves.Contains((tox, toy)))
            {
                // The desired move is legal. 
                if (enPassantSquare != "-" && GetCoordsFromSquareNotation(enPassantSquare) == (tox, toy))
                {
                    boardHandler.didEnPassant = true;
                }
                if (System.Math.Abs(fromy - toy) == 2)
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
        }        
        
        if(legalMoves.Contains((tox,toy)))
        {
            return true;
        }
        return false;
       
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
            // Rook moves
            case 'R':
            case 'r':
                return LegalMovesRook(fromx, fromy);
            // Knight moves
            case 'N':
            case 'n':
                return LegalMovesKnight(fromx, fromy);
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
        if (fromy + dir <= 8 && fromy + dir >= 1)
        {
            if(IsSquareEmpty(fromx, fromy + dir))
            {
                AddMoveToList(fromx, fromy, fromx, fromy + dir, ref validSquares);
            }
        }

        // 2.Move 2 steps forward, if it is on the base row
        if (isWhite && fromy == 2 || !isWhite && fromy == 7)
        {
            if(IsSquareEmpty(fromx,fromy+dir*2))
            {
                AddMoveToList(fromx, fromy, fromx, fromy + dir * 2, ref validSquares);
                enPassantDir = dir;
            }
        }

        // 3. Capture
        // Capture left
        if(IsSquareValid(fromx - 1, fromy + dir) && fromx > 1 && SquareHasAnEnemy(board[fromx,fromy],fromx-1,fromy+dir))
        {
            AddMoveToList(fromx, fromy, fromx - 1, fromy + dir, ref validSquares);
        }

        // Capture right
        if (IsSquareValid(fromx + 1, fromy + dir) && fromx < 8 && SquareHasAnEnemy(board[fromx, fromy], fromx + 1, fromy + dir) && IsSquareValid(fromx+1,fromy+dir))
        {
            AddMoveToList(fromx, fromy, fromx + 1, fromy + dir, ref validSquares);
        }

        // 4. En passant
        // En passant left. During en passant, the pawns are on the same y-level. Since it is left, the square x - ours should be = -1
        if (fromx > 1 && SquareHasAnEnemy(board[fromx, fromy], fromx - 1, fromy))
        {
            if(enPassantSquare != "-" && GetCoordsFromSquareNotation(enPassantSquare).Item1-fromx == -1)
                AddMoveToList(fromx, fromy, fromx - 1, fromy + dir, ref validSquares);
        }

        // En passant right
        if (fromx < 8 && SquareHasAnEnemy(board[fromx, fromy], fromx + 1, fromy))
        {
            if (enPassantSquare != "-" && GetCoordsFromSquareNotation(enPassantSquare).Item1 - fromx == 1)
                AddMoveToList(fromx, fromy, fromx + 1, fromy + dir, ref validSquares);
        }

        return validSquares;
    }

    // This just removes the "not in check" criteria from the main code, for a bit of debloating
    private void AddMoveToList(int fromx,int fromy, int tox, int toy, ref List<(int,int)> validSquares)
    {
         if(!MovePutsSelfInCheck(fromx, fromy, tox,toy))
        {
            validSquares.Add((tox, toy));
        }
    }

    private List<(int,int)> LegalMovesRook(int fromx, int fromy)
    {
        List<(int, int)> validMoves = new List<(int, int)>();
        // A rook can:
        // 1. Go right up to 7 steps
        // 2. Go left up to 7 steps
        // 3. Go up up to 7 steps
        // 4. Go down up to 7 steps

        // 1. Go right
        for(int i = fromx+1; i<=8;i++)
        {
            if (!AddMoveRook(fromx, fromy, i, fromy, ref validMoves)) break;           
        }
        // 2. Go left
        for (int i = fromx -1; i >= 1; i--)
        {
            if (!AddMoveRook(fromx, fromy, i, fromy, ref validMoves)) break;
        }
        // 3. Go up
        for (int i = fromy +1; i <= 8; i++)
        {
            if (!AddMoveRook(fromx, fromy, fromx, i, ref validMoves)) break;
        }
        // 4. Go down
        for (int i = fromy - 1; i >= 1; i--)
        {
            if (!AddMoveRook(fromx, fromy, fromx, i, ref validMoves)) break;
        }

        return validMoves;
    }

    private List<(int,int)> LegalMovesKnight(int fromx, int fromy)
    {
        List<(int, int)> validMoves = new List<(int, int)>();
        PieceHandler attacker = board[fromx, fromy];

        AddMoveHorse(1, 2, attacker, ref validMoves);
        AddMoveHorse(1, -2, attacker, ref validMoves);
        AddMoveHorse(-1, 2, attacker, ref validMoves);
        AddMoveHorse(-1, -2, attacker, ref validMoves);
        AddMoveHorse(2, 1, attacker, ref validMoves);
        AddMoveHorse(2, -1, attacker, ref validMoves);
        AddMoveHorse(-2, 1, attacker, ref validMoves);
        AddMoveHorse(-2, -1, attacker, ref validMoves);

        return validMoves;
    }

    // Adds a move to the knight, if the given move is valid
    private void AddMoveHorse(int xOffset, int yOffset,PieceHandler attacker, ref List<(int,int)> validMoves)
    {
        if (AttackableSquare(attacker, attacker.x + xOffset, attacker.y + yOffset) && ! MovePutsSelfInCheck(attacker.x,attacker.y,attacker.x+xOffset,attacker.y+yOffset))
        {
            validMoves.Add((attacker.x + xOffset, attacker.y + yOffset));
        }
    }

    // Adds a move to the rook in one direction, returns false once an obstacle is encountered
    private bool AddMoveRook(int fromx, int fromy, int xIter, int yIter ,ref List<(int,int)> validMoves)
    {
        if (SquareHasAnEnemy(board[fromx, fromy], xIter, yIter) && ! MovePutsSelfInCheck(fromx,fromy,xIter,yIter))
        {
            // There is an enemy. We can capture it, but can go no further
            validMoves.Add((xIter, yIter));
            return false;
        }
        else if (board[xIter, yIter] == null && !MovePutsSelfInCheck(fromx, fromy, xIter, yIter))
        {
            // Nothing there
            validMoves.Add((xIter, yIter));
            return true;
        }
        else
        {
            // There is a friendly piece there. Don't kill it
            return false;
        }
    }

    private bool AttackableSquare(PieceHandler attacker, int x, int y)
    {
        // The square must be within bounds. 
        // It can be empty, or it can have an enemy
        // And it must not put you into check
        return (IsSquareValid(x,y) && (IsSquareEmpty(x, y) || SquareHasAnEnemy(attacker, x, y))) && !MovePutsSelfInCheck(attacker.x, attacker.y, x, y);
    }

    private bool IsSquareValid(int x, int y)
    {
        return x <= 8 && x >= 1 && y <= 8 && y >= 1;
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
