using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script handles rules. 
// It needs to be fed the state of the board
// Given this, it can return the valid moves for any piece on the board
public class RulesHandler : MonoBehaviour
{
    public GameObject boardObject;
    public GameObject dummyPrefab;
    BoardHandler boardHandler;
    Piece[,] board;
    private string enPassantSquare;
    private int enPassantDir;

    private Piece whiteKing;
    private Piece blackKing;

    private List<Piece> whitePieces;
    private List<Piece> blackPieces;

    private short halfMoveClock;
    private short fullMoveCounter;

    private bool blackShortCastle;
    private bool blackLongCastle;    

    private bool whiteShortCastle;
    private bool whiteLongCastle;

    private bool gameOver;
    private char gameResult;
    private bool threeFoldRepetition;

    private string currentFEN;
    private List<string> gamePositionsFEN;
    private Dictionary<string, int> positionRepetitionCount;

    private (List<(int, int)>, (int, int)) enemyAttackedSquares;

    private char activePlayer;


    void Start()
    {
        // Load objects and variables
        boardHandler = boardObject.GetComponent<BoardHandler>();
        whitePieces = new List<Piece>();
        blackPieces = new List<Piece>();
        positionRepetitionCount = new Dictionary<string, int>();
        gamePositionsFEN = new List<string>();

        // Load and setup from first FEN
        board = boardHandler.GetBoard();
        currentFEN = boardHandler.GetFEN();
        LoadFromFEN();
        gameOver = false;
        threeFoldRepetition = false;
        gameResult = '-';
        gamePositionsFEN.Add(currentFEN);
        enemyAttackedSquares = EnemyAttackedSquares(IsWhite(activePlayer),board);
    }


    public bool IsMoveLegal(int fromx, int fromy, string move,Piece[,] otherBoard)
    {
        // Call the corresponding movement function
        // Making a non-move is not legal
        string from = GetSquareNotationFromCoords(fromx, fromy);
        if (from==move) return false;

        // Follow player order
        if (IsWhite(otherBoard[fromx, fromy]) && activePlayer == 'b' || 
           !IsWhite(otherBoard[fromx, fromy]) && activePlayer == 'w')
        {
            return false;
        }

        // This sometimes still happens after castling. Usually means that something was not assigned properly, or GetMovesOrAttacks was called with the wrong flag
        if(board[fromx,fromy] == null)
        {
            print("Tried to move invalid piece at " + fromx + " " + fromy);
            return false;
        }
        (int, int) coords = GetCoordsFromSquareNotation(move);

        // Okay, we're good to go!
        char piece = otherBoard[fromx, fromy].pieceName;
        var legalMoves = GetMovesOrAttacks(fromx, fromy,false, otherBoard);

        if(piece == 'p' || piece == 'P')
        {
            // Pawn move
            if (legalMoves.Contains(move))
            {
                // The desired move is legal. 
                if (enPassantSquare != "-" && enPassantSquare == move)
                {
                    boardHandler.didEnPassant = true;
                }
                if (System.Math.Abs(fromy - coords.Item2) == 2)
                {
                    // Moved two spaces. Can en passant
                    SetEnPassant(coords.Item1, fromy);
                }
                else
                {
                    SetEnPassantInvalid();
                }

                return true;
            }
            return false;
        }        
        
        if(legalMoves.Contains(move))
        {
            return true;
        }
        return false;
       
    }


    public List<string> GetMovesOrAttacks(int fromx, int fromy,bool onlyReturnAttacks,Piece[,] otherBoard)
    {
        if(halfMoveClock >= 50 || threeFoldRepetition) // Draw by 50 move-rule, or 3-fold repetition
        {
            if (halfMoveClock >= 50) print("Draw by 50 move rule!");
            gameResult = 'd'; // d as in draw
            gameOver = true;
            print("Its a draw!");
            return new List<string>();
        }
        //if(!onlyReturnAttacks)print("Looking for a piece at " + fromx + " " + fromy + " and only attacks is "+onlyReturnAttacks);
        //print("Gettin ready to check " + fromx + ":" + fromy);
        char piece = otherBoard[fromx, fromy].pieceName;
        if (!onlyReturnAttacks) print("We found a " + piece + " at "+fromx+":"+fromy);
        if (!onlyReturnAttacks) print("It beleives it is at "+ otherBoard[fromx,fromy].x+":" + otherBoard[fromx, fromy].y);
        switch (piece)
        {
            // Pawn moves
            case 'p':
            case 'P':
                return LegalMovesPawn(fromx, fromy,onlyReturnAttacks,otherBoard);
            // Rook moves
            case 'R':
            case 'r':
                return LegalMovesRook(fromx, fromy,onlyReturnAttacks,otherBoard);
            // Knight moves
            case 'N':
            case 'n':
                return LegalMovesKnight(fromx, fromy,onlyReturnAttacks,otherBoard);
            // Bishop moves
            case 'B':
            case 'b':
                return LegalMovesBishop(fromx, fromy,onlyReturnAttacks,otherBoard);
            // Queen moves
            case 'Q':
            case 'q':
                return LegalMovesQueen(fromx, fromy,onlyReturnAttacks,otherBoard);
            // King 
            case 'K':
            case 'k':
                return LegalMovesKing(fromx, fromy,onlyReturnAttacks,otherBoard);
            default:
                //Debug.LogError("Invalid piece moved! " + piece);
                return new List<string>();
        }
    }

    private bool IsSquareEmpty(int x, int y,Piece[,] otherBoard)
    {
        return otherBoard[x, y] == null;
    }

    // Does x,y contain a piece of opposite color?
    private bool SquareHasAnEnemy(Piece attacker, int x, int y,Piece[,] otherBoard)
    {
        if (otherBoard[x, y] == null) return false;

        return (IsWhite(attacker) != IsWhite(otherBoard[x, y]));
    }

    // Verifies if the desired move puts self into check. If so, it returns true, so that the move can be discarded as possible
    private bool MovePutsSelfInCheck(int fromx, int fromy, int tox, int toy,Piece[,]otherBoard)
    {        
        bool res = false;
        Piece backupOfAttacker = otherBoard[fromx,fromy];
        Piece backupOfDefender = otherBoard[tox,toy];

        Debug.Assert(backupOfAttacker != null);

        if(!((backupOfDefender != null && IsWhite(backupOfDefender) != IsWhite(backupOfAttacker)) || backupOfDefender == null))
        {
            Debug.Log("What!! We are trying to move from " + fromx + ":" + fromy + " with our " + otherBoard[fromx, fromy].pieceName + " onto " + tox + ":" + toy + " where there is a " + otherBoard[tox, toy].pieceName);
            Debug.Break();
        }
        Debug.Assert((backupOfDefender != null && IsWhite(backupOfDefender) != IsWhite(backupOfAttacker)) || backupOfDefender == null); // Trying to move piece onto a friendly piece
        Debug.Assert(otherBoard[fromx, fromy] != null);

        bool isWhite = IsWhite(backupOfAttacker);

        // Now, make the move, see what happens, and undo it again
        otherBoard[fromx, fromy] = null;
        otherBoard[tox, toy] = backupOfAttacker;

        // Check for check
        // Doing this operation is heavy, so we only do it iff:
        // 1. An enemy attacks this piece, or the square we move to. In this case a check might be blocked
        // 2. There is an enemy on the square we are looking at. This might can kill knights checking the king
        if(enemyAttackedSquares.Item1.Contains((fromx,fromy))|| enemyAttackedSquares.Item1.Contains((tox, toy)) || backupOfAttacker != null)
            enemyAttackedSquares = EnemyAttackedSquares(isWhite, otherBoard);
        (int, int) kingCoords = enemyAttackedSquares.Item2;

        if(backupOfAttacker.pieceName == 'k' || backupOfAttacker.pieceName=='K')
        {
            // Okay, we might be in check right now, but who cares?
            // Instead, check if the square we want to (escape) to is safe
            kingCoords.Item1 = tox;
            kingCoords.Item2 = toy;
        }

        foreach((int,int) square in enemyAttackedSquares.Item1)
        {
            if(square == kingCoords)
            {
                res = true; // Yep, that move would put us into check
                //print("We in check, we are white? "+isWhite);
                break;
            }
        }

        // Restore the board
        otherBoard[fromx, fromy] = backupOfAttacker;
        otherBoard[tox, toy] = backupOfDefender;
        
        return res;
    }


    // Below are all the functions that return all legal moves for any given piece on the board
    // Set the flag 'onlyReturnAttacked' to instead return all squares that piece is currently attacking. 
    private List<string> LegalMovesPawn(int fromx, int fromy,bool onlyReturnAttacked,Piece[,]otherBoard)
    {
        List<string> validSquares = new List<string>();
        bool isWhite = IsWhite(otherBoard[fromx, fromy]);

        int dir = 1;
        if (!IsWhite(otherBoard[fromx, fromy])) dir = -1;

        // A pawn can: 
        // 1. Move 1 step forward
        // 2. Move 2 steps forward, if it is on the base row
        // 3. Capture diagonally
        // 4. Capture en passant
        // 5. Promote

        // It is not possible to en passant into a promotion, nor is it possible to promote during the first double step

        // 1. Move 1 step forward
        // This does NOT include promotion, that is handled further down
        if (fromy + dir <= 7 && fromy + dir >= 2)
        {
            if(IsSquareEmpty(fromx, fromy + dir, otherBoard) && !onlyReturnAttacked)
            {
                AddMoveToList(fromx, fromy, fromx, fromy + dir, ref validSquares,otherBoard);
            }
        }

        // 2.Move 2 steps forward, if it is on the base row
        if (isWhite && fromy == 2 || !isWhite && fromy == 7)
        {
            if(IsSquareEmpty(fromx, fromy + dir, otherBoard) && IsSquareEmpty(fromx,fromy+dir*2, otherBoard) && !onlyReturnAttacked)
            {
                AddMoveToList(fromx, fromy, fromx, fromy + dir * 2, ref validSquares,otherBoard);
                enPassantDir = dir;
            }
        }

        // 3. Capture
        // Capture left
        if(IsSquareValid(fromx - 1, fromy + dir) && fromx > 1 && SquareHasAnEnemy(otherBoard[fromx,fromy],fromx-1,fromy+dir, otherBoard))
        {
            if(fromy+dir == 8 || fromy + dir == 1)
            {
                // Capture into promotion
                if(!onlyReturnAttacked)HandlePromotion(fromx,fromx-1, fromy, dir, isWhite, onlyReturnAttacked, ref validSquares,otherBoard);
                else validSquares.Add(GetSquareNotationFromCoords(fromx - 1, fromy + dir));

            }
            else
            {
                if (!onlyReturnAttacked) AddMoveToList(fromx, fromy, fromx - 1, fromy + dir, ref validSquares, otherBoard);
                else validSquares.Add(GetSquareNotationFromCoords(fromx - 1, fromy + dir));
            }

        }

        // Capture right
        if (IsSquareValid(fromx + 1, fromy + dir) && fromx < 8 && SquareHasAnEnemy(otherBoard[fromx, fromy], fromx + 1, fromy + dir, otherBoard))
        {
            if (fromy + dir == 8 || fromy + dir == 1)
            {
                // Capture into promotion
                if (!onlyReturnAttacked) HandlePromotion(fromx,fromx + 1, fromy, dir, isWhite, onlyReturnAttacked, ref validSquares, otherBoard);
                else validSquares.Add(GetSquareNotationFromCoords(fromx + 1, fromy + dir));
            }
            else
            {
                if (!onlyReturnAttacked) AddMoveToList(fromx, fromy, fromx + 1, fromy + dir, ref validSquares, otherBoard);
                else validSquares.Add(GetSquareNotationFromCoords(fromx + 1, fromy + dir));
            }
        }

        // 4. En passant
        // En passant left. During en passant, the pawns are on the same y-level. Since it is left, the square x - ours should be = -1
        if (fromx > 1 && SquareHasAnEnemy(otherBoard[fromx, fromy], fromx - 1, fromy, otherBoard))
        {
            if(enPassantSquare != "-" && GetCoordsFromSquareNotation(enPassantSquare).Item1-fromx == -1)
            {
                if (!onlyReturnAttacked) AddMoveToList(fromx, fromy, fromx - 1, fromy + dir, ref validSquares,otherBoard);
                else validSquares.Add(GetSquareNotationFromCoords(fromx - 1, fromy + dir));
            }
        }

        // En passant right
        if (fromx < 8 && SquareHasAnEnemy(otherBoard[fromx, fromy], fromx + 1, fromy, otherBoard))
        {
            if (enPassantSquare != "-" && GetCoordsFromSquareNotation(enPassantSquare).Item1 - fromx == 1)
            {
                if (!onlyReturnAttacked) AddMoveToList(fromx, fromy, fromx + 1, fromy + dir, ref validSquares,otherBoard);
                else validSquares.Add(GetSquareNotationFromCoords(fromx + 1, fromy + dir));
            }
        }

        // 5. Promotion stepping forward
        if(isWhite && fromy+dir == 8 || !isWhite && fromy+dir == 1)
        {
            if(IsSquareEmpty(fromx,fromy+dir, otherBoard))
                HandlePromotion(fromx,fromx, fromy, dir,isWhite, onlyReturnAttacked, ref validSquares, otherBoard);
        }


        return validSquares;
    }

    // This just removes the "not in check" criteria from the main code, for a bit of debloating
    private void AddMoveToList(int fromx,int fromy, int tox, int toy, ref List<string> validSquares,Piece[,] otherBoard)
    {
        Debug.Assert(IsSquareValid(tox, toy));
        if(!MovePutsSelfInCheck(fromx, fromy, tox,toy, otherBoard))
        {
            validSquares.Add(GetSquareNotationFromCoords(tox, toy));
        }
    }
    
    private void HandlePromotion(int fromx,int tox, int fromy,int dir ,bool isWhite,bool onlyReturnAttacked,ref List<string> validSquares,Piece[,] otherBoard)
    {
        if (!onlyReturnAttacked && !MovePutsSelfInCheck(fromx, fromy, tox, fromy + dir, otherBoard))
        {
            char bishop = isWhite ? 'B' : 'b';
            char queen = isWhite ? 'Q' : 'q';
            char knight = isWhite ? 'N' : 'n';
            char rook = isWhite ? 'R' : 'r';
            validSquares.Add(GetSquareNotationFromCoords(tox, fromy + dir) + "=" + bishop);
            validSquares.Add(GetSquareNotationFromCoords(tox, fromy + dir) + "=" + queen);
            validSquares.Add(GetSquareNotationFromCoords(tox, fromy + dir) + "=" + knight);
            validSquares.Add(GetSquareNotationFromCoords(tox, fromy + dir) + "=" + rook);
        }
    }

    private List<string> LegalMovesRook(int fromx, int fromy, bool onlyReturnAttacked, Piece[,] otherBoard)
    {
        List<string> validMoves = new List<string>();
        // A rook can:
        // 1. Go right up to 7 steps
        // 2. Go left up to 7 steps
        // 3. Go up up to 7 steps
        // 4. Go down up to 7 steps

        // 1. Go right
        for(int i = fromx+1; i<=8;i++)
        {
            if (!AddMoveInDirection(fromx, fromy, i, fromy, ref validMoves,onlyReturnAttacked, otherBoard)) break;           
        }
        // 2. Go left
        for (int i = fromx -1; i >= 1; i--)
        {
            if (!AddMoveInDirection(fromx, fromy, i, fromy, ref validMoves, onlyReturnAttacked,otherBoard)) break;
        }
        // 3. Go up
        for (int i = fromy +1; i <= 8; i++)
        {
            if (!AddMoveInDirection(fromx, fromy, fromx, i, ref validMoves, onlyReturnAttacked,otherBoard)) break;
        }
        // 4. Go down
        for (int i = fromy - 1; i >= 1; i--)
        {
            if (!AddMoveInDirection(fromx, fromy, fromx, i, ref validMoves, onlyReturnAttacked,otherBoard)) break;
        }

        return validMoves;
    }

    private List<string> LegalMovesBishop(int fromx, int fromy, bool onlyReturnAttacked,Piece[,] otherBoard)
    {
        List<string> validMoves = new List<string> ();
        Piece attacker = otherBoard[fromx, fromy];

        // A bishop can 
        // 1. Move up to seven steps up + right
        // 2. Up + left
        // 3. Down + right
        // 4. Down + left

        // 1. Up and right
        int j = 1;
        for(int i = fromx+1;i<=8;i++)
        {
            if (fromy + j > 8) break;
            // We can re-use the rook code! They are functionally the same, yay!
            // Why is this? Well, it moves in a certain direction until it a: hits a wall, b: hits an enemy, or c: hits a friendly piece
            if (!AddMoveInDirection(fromx, fromy, i, fromy + j, ref validMoves, onlyReturnAttacked, otherBoard)) break;
            j++;
        }

        // 2. Up and left
        j = 1;
        for (int i = fromx - 1; i >= 1; i--)
        {
            if (fromy + j > 8) break;
            if (!AddMoveInDirection(fromx, fromy, i, fromy + j, ref validMoves, onlyReturnAttacked,otherBoard)) break;
            j++;
        }

        // 3. Down and right
        j = -1;
        for (int i = fromx + 1; i <= 8; i++)
        {
            if (fromy + j < 1) break;
            if (!AddMoveInDirection(fromx, fromy, i, fromy + j, ref validMoves, onlyReturnAttacked,otherBoard)) break;
            j--;
        }

        // 4. Down and left
        j = -1;
        for (int i = fromx - 1; i >= 1; i--)
        {
            if (fromy + j < 1) break;
            if (!AddMoveInDirection(fromx, fromy, i, fromy + j, ref validMoves, onlyReturnAttacked,otherBoard)) break;
            j--;
        }
        return validMoves;
    }

    private List<string> LegalMovesQueen(int fromx, int fromy, bool onlyReturnAttacked,Piece[,] otherBoard)
    {
        // The queen is so simple. Just move like a rook and like a bishop :)
        List<string> validMoves = new List<string>();
        foreach(string move in LegalMovesBishop(fromx,fromy, onlyReturnAttacked, otherBoard))
        {
            validMoves.Add(move);
        }
        foreach (string move in LegalMovesRook(fromx, fromy, onlyReturnAttacked, otherBoard))
        {
            validMoves.Add(move);
        }
        return validMoves;
    }

    private List<string> LegalMovesKnight(int fromx, int fromy, bool onlyReturnAttacked,Piece[,] otherBoard)
    {
        List<string> validMoves = new List<string>();
        Piece attacker = otherBoard[fromx, fromy];
        print("Horse name is "+attacker.pieceName);
        AddValidMove(1, 2, attacker, ref validMoves, onlyReturnAttacked, otherBoard);
        AddValidMove(1, -2, attacker, ref validMoves,onlyReturnAttacked,otherBoard);
        AddValidMove(-1, 2, attacker, ref validMoves, onlyReturnAttacked,otherBoard);
        AddValidMove(-1, -2, attacker, ref validMoves, onlyReturnAttacked,otherBoard);
        AddValidMove(2, 1, attacker, ref validMoves,onlyReturnAttacked,otherBoard);
        AddValidMove(2, -1, attacker, ref validMoves,onlyReturnAttacked,otherBoard);
        AddValidMove(-2, 1, attacker, ref validMoves,onlyReturnAttacked,otherBoard);
        AddValidMove(-2, -1, attacker, ref validMoves, onlyReturnAttacked,otherBoard);

        return validMoves;
    }

    private List<string> LegalMovesKing(int fromx, int fromy, bool onlyReturnAttacked,Piece[,] otherBoard)
    {
        List<string> validMoves = new List<string>();

        // A king can:
        // 1: Move 1 step in any direction
        // 2: Castle, ugh

        // 1. Move in any direction
        if(!onlyReturnAttacked)
        {
            enemyAttackedSquares = EnemyAttackedSquares(IsWhite(otherBoard[fromx, fromy]), otherBoard);
        }

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (j == 0 && i == 0) continue;
                if (enemyAttackedSquares.Item1 == null) break;

                if (enemyAttackedSquares.Item1.Contains((fromx+i, fromy+j)) && !onlyReturnAttacked)
                {
                    continue; // Do not capture something that is defended
                }
                AddValidMove(i, j, board[fromx, fromy], ref validMoves, onlyReturnAttacked, otherBoard);

            }
        }
        if (onlyReturnAttacked) return validMoves; // Castled squares do not count as attacked squares
        
        // 2. Castling
        // This is dependent on color. Black cant castle unto whites castling square, nuh-uh!
        // You can't castle through pieces, and can't castle through (or into) check
        // It is also not possible to castle if the king has moved, or the corresponding rook. This is handled in the "MakeMove" function

        bool isWhite = IsWhite(board[fromx, fromy]);

        // Warning! Ternary operators incoming
        bool shortCastle = isWhite ? whiteShortCastle : blackShortCastle;
        bool longCastle  = isWhite ? whiteLongCastle  : blackLongCastle;

        if (!shortCastle && !longCastle) return validMoves; // We can not castle anymore. Do not check further.

        int ylevel = isWhite ? 1 : 8;

        (List<(int, int)> enemySquares, (int, int) kingPos) = EnemyAttackedSquares(isWhite, otherBoard);

        if (enemySquares.Contains(kingPos)) return validMoves; // You can not castle out of check, for some reason

        Debug.Assert(kingPos == (5, ylevel)); // If this is fails, the king has somehow moved without us knowing. How!?

        if(shortCastle)
        { 
            if(RowIsEmptyAndNotAttacked(6,7,ylevel, enemySquares, otherBoard))
            {
                validMoves.Add(GetSquareNotationFromCoords(7,ylevel));
            }
        }
        if (longCastle)
        {
            if (RowIsEmptyAndNotAttacked(3,4,ylevel, enemySquares, otherBoard))
            {
                validMoves.Add(GetSquareNotationFromCoords(3, ylevel));
            }
        }


        return validMoves;
    }

    private bool RowIsEmptyAndNotAttacked(int fromx, int tox, int y, List<(int, int)> enemyAttackedSquares,Piece[,] otherBoard)
    {
        for(int i = fromx; i<=tox;i++)
        {
            if (!SquareIsEmptyAndNotAttacked(i, y, enemyAttackedSquares, otherBoard)) return false;
        }
        return true;
    }

    private bool SquareIsEmptyAndNotAttacked(int x, int y, List<(int,int)> enemyAttackedSquares,Piece[,] otherBoard)
    {
        return IsSquareEmpty(x, y, otherBoard) && !enemyAttackedSquares.Contains((x, y));
    }

    // Adds a move to the list, if the given move is valid
    private void AddValidMove(int xOffset, int yOffset,Piece attacker, ref List<string> validMoves,bool onlyReturnAttacked,Piece[,] otherBoard)
    {
        // AttackableSquare also check for check, heh. A check-check
        if (AttackableSquare(attacker, attacker.x + xOffset, attacker.y + yOffset, onlyReturnAttacked, otherBoard))
        {
            validMoves.Add(GetSquareNotationFromCoords(attacker.x + xOffset, attacker.y + yOffset));
        }
    }

    // Adds a move to the rook in one direction, returns false once an obstacle is encountered
    private bool AddMoveInDirection(int fromx, int fromy, int xIter, int yIter ,ref List<string> validMoves, bool onlyReturnAttacked, Piece[,]otherBoard)
    {
        // Conditions: There is an enemy. If we are looking for moves, it must not put us into check. If we are checking attacks, ignore that condition.
        if (SquareHasAnEnemy(otherBoard[fromx, fromy], xIter, yIter, otherBoard))
        {
            // There is an enemy. We can capture it, but can go no further
            if(onlyReturnAttacked || !MovePutsSelfInCheck(fromx, fromy, xIter, yIter, otherBoard))
                validMoves.Add(GetSquareNotationFromCoords(xIter, yIter));
            return false;
        }
        else if (otherBoard[xIter, yIter] == null && (onlyReturnAttacked || !MovePutsSelfInCheck(fromx, fromy, xIter, yIter, otherBoard)))
        {
            // Nothing there
            validMoves.Add(GetSquareNotationFromCoords(xIter, yIter));
            return true;
        }
        else if(otherBoard[xIter,yIter] != null)
        {
            // There is a friendly piece there. Don't kill it
            // However! We are DEFENDING that piece, so a king must not capture it!
            if(onlyReturnAttacked)
            {
                //print("Adding "+ GetSquareNotationFromCoords(xIter, yIter));
                validMoves.Add(GetSquareNotationFromCoords(xIter, yIter));
            }
            return false;
        }
        else
        {
            // The square is empty, but moving there puts us into check
            // However, if we move further, we might be able to do something
            return true;
        }
    }

    private bool AttackableSquare(Piece attacker, int x, int y, bool onlyReturnAttacked,Piece[,] otherBoard)
    {
        // The square must be within bounds. 
        if (!IsSquareValid(x, y)) return false;

        // It can be empty, or have an enemy
        bool emptyOrEnemy = (IsSquareEmpty(x, y, otherBoard) || SquareHasAnEnemy(attacker, x, y, otherBoard));
        if (onlyReturnAttacked && emptyOrEnemy) return true;

        // And it must not put you into check
        return emptyOrEnemy && !MovePutsSelfInCheck(attacker.x, attacker.y, x, y, otherBoard);
    }

    private bool IsSquareValid(int x, int y)
    {
        return x <= 8 && x >= 1 && y <= 8 && y >= 1;
    }

    public bool IsWhite(Piece p)
    {
        if (p.pieceName == 'w') return true;
        if (p.pieceName < 97) return true;
        return false;
    }
    public bool IsWhite(char name)
    {
        if (name == 'w') return true;
        if (name < 97) return true;
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



    // This also returns the kings position, so we dont have to loop through the entire board again later
    // Since this only care about squares CURRENTLY attacked by the static enemy pieces, we do not need to worry about enemy promotion 
    // and thus we can use simple int coords
    public (List<(int, int)>,(int,int)) EnemyAttackedSquares(bool isWhite,Piece[,] otherBoard)
    {
        List<(int, int)> attackedSquares = new List<(int, int)>();
        (int, int) friendlyKingCoords = (0, 0);
        foreach (Piece p in board)
        {
            if (p == null) continue;
            if (otherBoard[p.x, p.y] == null) continue;
            if (IsWhite(p) != isWhite)
            {
                List<string> attacked = GetMovesOrAttacks(p.x, p.y, true, otherBoard);
                if (gameOver) return (new List<(int,int)>(),(-1,-1));
                foreach(string squareNotation in attacked)
                {
                    (int, int) square = GetCoordsFromSquareNotation(squareNotation);
                    attackedSquares.Add(square);
                }
            }
            else if (p.pieceName == 'k'|| p.pieceName == 'K')
            {
                friendlyKingCoords.Item1 = p.x;
                friendlyKingCoords.Item2 = p.y;
            }

        }
        return (attackedSquares,friendlyKingCoords);
    }

    // int fromx, int fromy, string movenotation
    // this format is easy to use in arrays, and allows to differentiate promotion moves
    // example 1: 5,7,e8=Q
    // example 2: 4,3,e5
    public List<(int, int, string)> GetAllValidMoves(char color,Piece[,] otherBoard)
    {
        bool isWhite = IsWhite(color);
        List<(int,int,string)> attackedSquares = new List<(int, int,string)>();

        if (gameOver) return attackedSquares; 

        foreach (Piece p in otherBoard)
        {
            if (p == null) continue;
            if (otherBoard[p.x, p.y] == null) continue;
            if (IsWhite(p) == isWhite)
            {
                List<string> attacked = GetMovesOrAttacks(p.x, p.y, false,otherBoard);
                foreach (string square in attacked)
                {
                    attackedSquares.Add((p.x,p.y,square));
                }
            }
        }



        if(attackedSquares.Count == 0)
        {
            gameOver = true;

            if(!enemyAttackedSquares.Item1.Contains(enemyAttackedSquares.Item2))
            {
                // Draw by stalemate!
                gameResult = 'd'; 
            }
            else
            {
                // Checkmate!
                gameResult = isWhite ? 'b' : 'w'; // If white has no moves, black won
            }

            print("Game is over, "+gameResult);
        }
        return (attackedSquares);
    }

    // Called after a move was made, to update everything accordingly.
    // Also handles castling
    public void MakeMove(int fromx, int fromy, char pieceType, bool capture, int tox,int toy)
    {
        board[fromx, fromy].x = tox;
        board[fromx, fromy].y = toy;
        if (board[fromx, fromy] == null) print("Nothing there");
        else print(board[fromx, fromy].pieceName + " is at " + fromx + ":" + fromy);
        board = boardHandler.GetBoard();
        if (board[fromx, fromy] == null) print("Nothing there");
        else print(board[fromx, fromy].pieceName + " is at " + fromx + ":" + fromy);
        print("At the destination there is a " + board[tox, toy].pieceName);


        fullMoveCounter += 1;        
        if (pieceType == 'p' || pieceType == 'P' || capture)
        {
            halfMoveClock = 0;
        }
        else halfMoveClock += 1;


        // If the king is moved, it can no longer castle.
        if(pieceType == 'k' || pieceType == 'K')
        {
            if(IsWhite(pieceType))
            {
                whiteLongCastle = false;
                whiteShortCastle = false;
            }
            else
            {
                blackLongCastle = false;
                blackShortCastle = false;
            }

            int ylevel = IsWhite(pieceType) ? 1 : 8;
            char rookChar = IsWhite(pieceType) ? 'R' : 'r';

            if(System.Math.Abs(fromx-tox) > 1)
            {
                // Did castle. The king was already moved by the player/engine, now we need to move the rook
                if(tox == 7)
                {
                    // Short castle
                    string move = GetSquareNotationFromCoords(6, ylevel);
                    board[8, ylevel].pieceObject.GetComponent<PieceHandler>().MoveNoVerification(move);
                    return;
                }

                if (tox == 3)
                {
                    // Long castle
                    string move = GetSquareNotationFromCoords(4, ylevel);

                    board[1, ylevel].pieceObject.GetComponent<PieceHandler>().MoveNoVerification(move);
                    return;
                }
                fullMoveCounter -= 1; // A castle is not 2 moves!
                halfMoveClock -= 1;
            }

        }

        // If the corresponding rook was moved, that also prevents castling
        if(pieceType == 'r')
        {
            // A black rook was moved. Where was it?
            if(fromx == 1 && fromy == 8) 
            {
                // It was the top left. 
                blackLongCastle = false;
            }
            if (fromx == 8 && fromy == 8)
            {
                // It was the top right. 
                blackShortCastle = false;
            }
        }
        else if (pieceType == 'R')
        {
            // White rook
            if (fromx == 1 && fromy == 1)
            { 
                whiteLongCastle = false;
            }
            if (fromx == 8 && fromy == 1)
            {
                whiteShortCastle = false;
            }
        }

        UpdateAndSaveFEN();

        // We have our new FEN, time to save it in a dict and check for 3-fold repetition

        // We can not store the counters etc, since they do not loop
        // This ugly row below just converts rN1k1br1/4p1pp/p1n2p1n/1pP1q3/3p4/3p1B2/PP1QbP1P/R1B3K1 b - - 2 29 into rN1k1br1/4p1pp/p1n2p1n/1pP1q3/3p4/3p1B2/PP1QbP1P/R1B3K1
        string position = currentFEN.Substring(0, currentFEN.Length - (currentFEN.Length-currentFEN.IndexOf(' ')));
        if(!positionRepetitionCount.ContainsKey(position))
        {
            positionRepetitionCount.Add(position, 1);
        }
        else
        {
            positionRepetitionCount[position] += 1;
            if (positionRepetitionCount[position] >= 3)
            {
                print("Draw by 3-fold repetition");
                threeFoldRepetition = true;
                gameOver = true;
                gameResult = 'd';
            }
        }
        enemyAttackedSquares = EnemyAttackedSquares(IsWhite(pieceType),board); // Call this only once per turn, it is quite heavy-weight
        //print("Enemy attacks " + enemyAttackedSquares.Item1.Count + " squares");
        TogglePlayer();


    }

    public bool ThreeFoldRepetition()
    {
        return threeFoldRepetition;
    }

    public string GetFENNotation() { return currentFEN; }

    // Called after a move was made, to save that move in the list.
    // Allows for checking for 3-fold repetition
    // https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation
    private void UpdateAndSaveFEN()
    {
        string fenPosition = "";
        for(int i = 8; i>=1;i--)
        {
            short count = 48; // '0' in ascii
            for(int j = 1;j<=8;j++)
            {
                if(board[j,i] != null)
                {
                    if(count != 48)
                    {
                        fenPosition += (char)count;
                        count = 48;
                    }
                    fenPosition += board[j, i].pieceName; // Ahh the beauty
                }
                else
                {
                    count += 1;
                }
            }
            if (count != 48)
            {
                fenPosition += (char)count;
                count = 48;
            }
            if(i != 1) fenPosition += '/';
        }
        fenPosition += " ";
        fenPosition += activePlayer;
        fenPosition += " ";
        fenPosition += GetCastlingString();
        fenPosition += " ";
        fenPosition += enPassantSquare;
        fenPosition += " ";
        fenPosition += halfMoveClock.ToString();
        fenPosition += " ";
        fenPosition += fullMoveCounter.ToString();
        currentFEN = fenPosition;
        boardHandler.SetFEN(currentFEN);
    }

    public (short,short) GetCounters() { return (halfMoveClock, fullMoveCounter); }
    public (bool,bool,bool,bool) GetCastlingRights() { return (whiteShortCastle, whiteLongCastle, blackShortCastle, blackLongCastle); }

    private void LoadFromFEN()
    {
        var FENparts = currentFEN.Split(' ');
        string FENcastling = FENparts[2];
        foreach (char c in FENcastling)
        {
            switch (c)
            {
                case 'K':
                    whiteShortCastle = true;
                    break;
                case 'k':
                    blackShortCastle = true;
                    break;
                case 'Q':
                    whiteLongCastle = true;
                    break;
                case 'q':
                    blackLongCastle = true;
                    break;
                default:
                    break;
            }
        }
        activePlayer = (char)FENparts[1][0];
        enPassantSquare = FENparts[3];
        halfMoveClock = short.Parse(FENparts[4]);
        fullMoveCounter = short.Parse(FENparts[5]);
    }

    private string GetCastlingString()
    {
        string res = "";
        if (whiteShortCastle) res += "K";
        if (whiteLongCastle) res += "Q";
        if (blackShortCastle) res += "k";
        if (blackLongCastle) res += "q";
        if (res.Length == 0) res = "-";
        return res;
    }

    public bool IsGameOver()
    {
        return gameOver;
    }

    public char GameResult()
    {
        return gameResult;
    }

    private void TogglePlayer()
    {
        if (activePlayer == 'w') activePlayer = 'b';
        else activePlayer = 'w';
        boardHandler.SetActivePlayer(activePlayer); // Just for debuggin and seeing the state of the game
    }

    public char GetActivePlayer() { return activePlayer; }

    // Convert like a4 -> (1,4)
    public (int, int) GetCoordsFromSquareNotation(string not)
    {
        Debug.Assert(not.Length == 2 || not.Length == 4 && not[2] == '='); // Normal move, or promotion
        int x = (int)(not[0] - 96);
        int y = (int)(not[1] - 48);
        return (x, y);
    }

    private string GetSquareNotationFromCoords(int fromx, int fromy)
    {
        char square = (char)(fromx + 96);
        char number = (char)(fromy + 48);
        return square.ToString() + number.ToString();
    }

    public void ResetVariables()
    {
        whitePieces = new List<Piece>();
        blackPieces = new List<Piece>();
        positionRepetitionCount = new Dictionary<string, int>();
        gamePositionsFEN = new List<string>();
        board = boardHandler.GetBoard();
        currentFEN = boardHandler.GetFEN();
        LoadFromFEN();
        gameOver = false;
        threeFoldRepetition = false;
        gameResult = '-';
        gamePositionsFEN.Add(currentFEN);
        enemyAttackedSquares = EnemyAttackedSquares(IsWhite(activePlayer), board);
    }


}
