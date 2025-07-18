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
    PieceHandler[,] board;
    private string enPassantSquare;
    private int enPassantDir;

    private PieceHandler whiteKing;
    private PieceHandler blackKing;

    private List<PieceHandler> whitePieces;
    private List<PieceHandler> blackPieces;

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

    private char activePlayer;


    void Start()
    {
        // Load objects and variables
        boardHandler = boardObject.GetComponent<BoardHandler>();
        whitePieces = new List<PieceHandler>();
        blackPieces = new List<PieceHandler>();
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
    }


    public bool IsMoveLegal(int fromx, int fromy, int tox, int toy)
    {
        // Call the corresponding movement function

        // Making a non-move is not legal
        if (fromx == tox && fromy == toy) return false;

        // Follow player order
        if (IsWhite(board[fromx, fromy]) && activePlayer == 'b' || 
           !IsWhite(board[fromx, fromy]) && activePlayer == 'w')
        {
            return false;
        }

        // This sometimes still happens after castling. Usually means that something was not assigned properly, or GetMovesOrAttacks was called with the wrong flag
        if(board[fromx,fromy] == null)
        {
            print("Tried to move invalid piece at " + fromx + " " + fromy);
            return false;
        }

        // Okay, we're good to go!
        char piece = board[fromx, fromy].pieceName;
        var legalMoves = GetMovesOrAttacks(fromx, fromy,false);

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


    public List<(int, int)> GetMovesOrAttacks(int fromx, int fromy,bool onlyReturnAttacks)
    {
        if(halfMoveClock == 50 || threeFoldRepetition) // Draw by 50 move-rule, or 3-fold repetition
        {
            gameResult = 'd'; // d as in draw
            return new List<(int, int)>();
        }
        char piece = board[fromx, fromy].pieceName;
        switch (piece)
        {
            // Pawn moves
            case 'p':
            case 'P':
                return LegalMovesPawn(fromx, fromy,onlyReturnAttacks);
            // Rook moves
            case 'R':
            case 'r':
                return LegalMovesRook(fromx, fromy,onlyReturnAttacks);
            // Knight moves
            case 'N':
            case 'n':
                return LegalMovesKnight(fromx, fromy,onlyReturnAttacks);
            // Bishop moves
            case 'B':
            case 'b':
                return LegalMovesBishop(fromx, fromy,onlyReturnAttacks);
            // Queen moves
            case 'Q':
            case 'q':
                return LegalMovesQueen(fromx, fromy,onlyReturnAttacks);
            // King 
            case 'K':
            case 'k':
                return LegalMovesKing(fromx, fromy,onlyReturnAttacks);
            default:
                Debug.LogError("Invalid piece moved! " + piece);
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

    // Verifies if the desired move puts self into check. If so, it returns true, so that the move can be discarded as possible
    private bool MovePutsSelfInCheck(int fromx, int fromy, int tox, int toy)
    {
        bool res = false;
        // Since PieceHandler is Monobehaviour we can not just create a 'PieceHandler tmp', we unfortunately need to instantiate a whole new prefab, and then use that PieceHandler
        GameObject backupOfAttackerObj = Instantiate(dummyPrefab);
        GameObject backupOfDefenderObj = Instantiate(dummyPrefab);

        PieceHandler backupOfAttacker = backupOfAttackerObj.GetComponent<PieceHandler>();
        PieceHandler backupOfDefender = backupOfDefenderObj.GetComponent<PieceHandler>();

        backupOfAttacker = board[fromx, fromy];
        backupOfDefender = board[tox, toy]; // This may very well be null

        bool isWhite = IsWhite(board[fromx, fromy]);

        // Now, make the move, see what happens, and undo it again
        Debug.Assert(backupOfAttacker != null);
        board[fromx, fromy] = null;
        board[tox, toy] = backupOfAttacker;

        // Check for check
        (List<(int, int)>,(int,int)) enemyAttacked = EnemyAttackedSquares(isWhite);
        (int, int) kingCoords = enemyAttacked.Item2;

        if(backupOfAttacker.pieceName == 'k' || backupOfAttacker.pieceName=='K')
        {
            // Okay, we might be in check right now, but who cares?
            // Instead, check if the square we want to (escape) to is safe
            kingCoords.Item1 = tox;
            kingCoords.Item2 = toy;
        }

        foreach((int,int) square in enemyAttacked.Item1)
        {
            if(square == kingCoords)
            {
                res = true; // Yep, that move would put us into check
                break;
            }
        }

        // Restore the board
        board[fromx, fromy] = backupOfAttacker;
        board[tox, toy] = backupOfDefender;

        // Clean up garbage
        Destroy(backupOfAttackerObj);
        Destroy(backupOfDefenderObj);

        
        return res;
    }


    // Below are all the functions that return all legal moves for any given piece on the board
    // Set the flag 'onlyReturnAttacked' to instead return all squares that piece is currently attacking. 
    private List<(int,int)> LegalMovesPawn(int fromx, int fromy,bool onlyReturnAttacked)
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
            if(IsSquareEmpty(fromx, fromy + dir) && !onlyReturnAttacked)
            {
                AddMoveToList(fromx, fromy, fromx, fromy + dir, ref validSquares);
            }
        }

        // 2.Move 2 steps forward, if it is on the base row
        if (isWhite && fromy == 2 || !isWhite && fromy == 7)
        {
            if(IsSquareEmpty(fromx, fromy + dir) && IsSquareEmpty(fromx,fromy+dir*2) && !onlyReturnAttacked)
            {
                AddMoveToList(fromx, fromy, fromx, fromy + dir * 2, ref validSquares);
                enPassantDir = dir;
            }
        }

        // 3. Capture
        // Capture left
        if(IsSquareValid(fromx - 1, fromy + dir) && fromx > 1 && SquareHasAnEnemy(board[fromx,fromy],fromx-1,fromy+dir))
        {
            if (!onlyReturnAttacked) AddMoveToList(fromx, fromy, fromx - 1, fromy + dir, ref validSquares);
            else validSquares.Add((fromx - 1, fromy + dir));
        }

        // Capture right
        if (IsSquareValid(fromx + 1, fromy + dir) && fromx < 8 && SquareHasAnEnemy(board[fromx, fromy], fromx + 1, fromy + dir))
        {
            if (!onlyReturnAttacked) AddMoveToList(fromx, fromy, fromx + 1, fromy + dir, ref validSquares);
            else validSquares.Add((fromx + 1, fromy + dir));
        }

        // 4. En passant
        // En passant left. During en passant, the pawns are on the same y-level. Since it is left, the square x - ours should be = -1
        if (fromx > 1 && SquareHasAnEnemy(board[fromx, fromy], fromx - 1, fromy))
        {
            if(enPassantSquare != "-" && GetCoordsFromSquareNotation(enPassantSquare).Item1-fromx == -1)
            {
                if (!onlyReturnAttacked) AddMoveToList(fromx, fromy, fromx - 1, fromy + dir, ref validSquares);
                else validSquares.Add((fromx - 1, fromy + dir));
            }
        }

        // En passant right
        if (fromx < 8 && SquareHasAnEnemy(board[fromx, fromy], fromx + 1, fromy))
        {
            if (enPassantSquare != "-" && GetCoordsFromSquareNotation(enPassantSquare).Item1 - fromx == 1)
            {
                if (!onlyReturnAttacked) AddMoveToList(fromx, fromy, fromx + 1, fromy + dir, ref validSquares);
                else validSquares.Add((fromx + 1, fromy + dir));
            }
        }

        return validSquares;
    }

    // This just removes the "not in check" criteria from the main code, for a bit of debloating
    private void AddMoveToList(int fromx,int fromy, int tox, int toy, ref List<(int,int)> validSquares)
    {
        Debug.Assert(IsSquareValid(tox, toy));
        if(!MovePutsSelfInCheck(fromx, fromy, tox,toy))
        {
            validSquares.Add((tox, toy));
        }
    }

    private List<(int,int)> LegalMovesRook(int fromx, int fromy, bool onlyReturnAttacked)
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
            if (!AddMoveInDirection(fromx, fromy, i, fromy, ref validMoves,onlyReturnAttacked)) break;           
        }
        // 2. Go left
        for (int i = fromx -1; i >= 1; i--)
        {
            if (!AddMoveInDirection(fromx, fromy, i, fromy, ref validMoves, onlyReturnAttacked)) break;
        }
        // 3. Go up
        for (int i = fromy +1; i <= 8; i++)
        {
            if (!AddMoveInDirection(fromx, fromy, fromx, i, ref validMoves, onlyReturnAttacked)) break;
        }
        // 4. Go down
        for (int i = fromy - 1; i >= 1; i--)
        {
            if (!AddMoveInDirection(fromx, fromy, fromx, i, ref validMoves, onlyReturnAttacked)) break;
        }

        return validMoves;
    }

    private List<(int,int)> LegalMovesBishop(int fromx, int fromy, bool onlyReturnAttacked)
    {
        List<(int, int)> validMoves = new List<(int, int)>();
        PieceHandler attacker = board[fromx, fromy];

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
            if (!AddMoveInDirection(fromx, fromy, i, fromy + j, ref validMoves, onlyReturnAttacked)) break;
            j++;
        }

        // 2. Up and left
        j = 1;
        for (int i = fromx - 1; i >= 1; i--)
        {
            if (fromy + j > 8) break;
            if (!AddMoveInDirection(fromx, fromy, i, fromy + j, ref validMoves, onlyReturnAttacked)) break;
            j++;
        }

        // 3. Down and right
        j = -1;
        for (int i = fromx + 1; i <= 8; i++)
        {
            if (fromy + j < 1) break;
            if (!AddMoveInDirection(fromx, fromy, i, fromy + j, ref validMoves, onlyReturnAttacked)) break;
            j--;
        }

        // 4. Down and left
        j = -1;
        for (int i = fromx - 1; i >= 1; i--)
        {
            if (fromy + j < 1) break;
            if (!AddMoveInDirection(fromx, fromy, i, fromy + j, ref validMoves, onlyReturnAttacked)) break;
            j--;
        }
        return validMoves;
    }

    private List<(int,int)> LegalMovesQueen(int fromx, int fromy, bool onlyReturnAttacked)
    {
        // The queen is so simple. Just move like a rook and like a bishop :)
        List<(int, int)> validMoves = new List<(int, int)>();
        foreach((int,int) move in LegalMovesBishop(fromx,fromy, onlyReturnAttacked))
        {
            validMoves.Add(move);
        }
        foreach ((int, int) move in LegalMovesRook(fromx, fromy, onlyReturnAttacked))
        {
            validMoves.Add(move);
        }
        return validMoves;
    }

    private List<(int,int)> LegalMovesKnight(int fromx, int fromy, bool onlyReturnAttacked)
    {
        List<(int, int)> validMoves = new List<(int, int)>();
        PieceHandler attacker = board[fromx, fromy];

        AddValidMove(1, 2, attacker, ref validMoves, onlyReturnAttacked);
        AddValidMove(1, -2, attacker, ref validMoves,onlyReturnAttacked);
        AddValidMove(-1, 2, attacker, ref validMoves, onlyReturnAttacked);
        AddValidMove(-1, -2, attacker, ref validMoves, onlyReturnAttacked);
        AddValidMove(2, 1, attacker, ref validMoves,onlyReturnAttacked);
        AddValidMove(2, -1, attacker, ref validMoves,onlyReturnAttacked);
        AddValidMove(-2, 1, attacker, ref validMoves,onlyReturnAttacked);
        AddValidMove(-2, -1, attacker, ref validMoves, onlyReturnAttacked);

        return validMoves;
    }

    private List<(int, int)> LegalMovesKing(int fromx, int fromy, bool onlyReturnAttacked)
    {
        List<(int, int)> validMoves = new List<(int, int)>();

        // A king can:
        // 1: Move 1 step in any direction
        // 2: Castle, ugh

        // 1. Move in any direction
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (j == 0 && i == 0) continue;
                AddValidMove(i, j, board[fromx, fromy], ref validMoves, onlyReturnAttacked);
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

        (List<(int, int)> enemyAttackedSquares, (int, int) kingPos) = EnemyAttackedSquares(isWhite);

        if (enemyAttackedSquares.Contains(kingPos)) return validMoves; // You can not castle out of check, for some reason

        Debug.Assert(kingPos == (5, ylevel)); // If this is fails, the king has somehow moved without us knowing. How!?

        if(shortCastle)
        { 
            if(RowIsEmptyAndNotAttacked(6,7,ylevel,enemyAttackedSquares))
            {
                validMoves.Add((7,ylevel));
            }
        }
        if (longCastle)
        {
            if (RowIsEmptyAndNotAttacked(3,4,ylevel,enemyAttackedSquares))
            {
                validMoves.Add((3, ylevel));
            }
        }


        return validMoves;
    }

    private bool RowIsEmptyAndNotAttacked(int fromx, int tox, int y, List<(int, int)> enemyAttackedSquares)
    {
        for(int i = fromx; i<=tox;i++)
        {
            if (!SquareIsEmptyAndNotAttacked(i, y, enemyAttackedSquares)) return false;
        }
        return true;
    }

    private bool SquareIsEmptyAndNotAttacked(int x, int y, List<(int,int)> enemyAttackedSquares)
    {
        return IsSquareEmpty(x, y) && !enemyAttackedSquares.Contains((x, y));
    }

    // Adds a move to the list, if the given move is valid
    private void AddValidMove(int xOffset, int yOffset,PieceHandler attacker, ref List<(int,int)> validMoves,bool onlyReturnAttacked)
    {
        // AttackableSquare also check for check, heh. A check-check
        if (AttackableSquare(attacker, attacker.x + xOffset, attacker.y + yOffset, onlyReturnAttacked))
        {
            validMoves.Add((attacker.x + xOffset, attacker.y + yOffset));
        }
    }

    // Adds a move to the rook in one direction, returns false once an obstacle is encountered
    private bool AddMoveInDirection(int fromx, int fromy, int xIter, int yIter ,ref List<(int,int)> validMoves, bool onlyReturnAttacked)
    {
        // Conditions: There is an enemy. If we are looking for moves, it must not put us into check. If we are checking attacks, ignore that condition.
        if (SquareHasAnEnemy(board[fromx, fromy], xIter, yIter))
        {
            // There is an enemy. We can capture it, but can go no further
            if(onlyReturnAttacked || !MovePutsSelfInCheck(fromx, fromy, xIter, yIter))
                validMoves.Add((xIter, yIter));
            return false;
        }
        else if (board[xIter, yIter] == null && (onlyReturnAttacked || !MovePutsSelfInCheck(fromx, fromy, xIter, yIter)))
        {
            // Nothing there
            validMoves.Add((xIter, yIter));
            return true;
        }
        else if(board[xIter,yIter] != null)
        {
            // There is a friendly piece there. Don't kill it
            return false;
        }else
        {
            // The square is empty, but moving there puts us into check
            // However, if we move further, we might be able to do something
            return true;
        }
    }

    private bool AttackableSquare(PieceHandler attacker, int x, int y, bool onlyReturnAttacked)
    {
        // The square must be within bounds. 
        if (!IsSquareValid(x, y)) return false;

        // It can be empty, or have an enemy
        bool emptyOrEnemy = (IsSquareEmpty(x, y) || SquareHasAnEnemy(attacker, x, y));
        if (onlyReturnAttacked && emptyOrEnemy) return true;

        // And it must not put you into check
        return emptyOrEnemy && !MovePutsSelfInCheck(attacker.x, attacker.y, x, y);
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
    private bool IsWhite(char name)
    {
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

    // Convert like a4 -> (1,4)
    public (int, int) GetCoordsFromSquareNotation(string not)
    {
        Debug.Assert(not.Length == 2);
        int x = (int)(not[0] - 96);
        int y = (int)(not[1] - 48);
        return (x, y);
    }

    // This also returns the kings position, so we dont have to loop through the entire board again later
    public (List<(int, int)>,(int,int)) EnemyAttackedSquares(bool isWhite)
    {
        List<(int, int)> attackedSquares = new List<(int, int)>();
        (int, int) friendlyKingCoords = (0, 0);
        foreach (PieceHandler p in board)
        {
            if (p == null) continue;
            if (IsWhite(p) != isWhite)
            {
                List<(int, int)> attacked = GetMovesOrAttacks(p.x, p.y, true);
                foreach((int,int) square in attacked)
                {
                    attackedSquares.Add(square);
                }
            }
            else if (p.pieceName == 'k' || p.pieceName == 'K')
            {
                friendlyKingCoords.Item1 = p.x;
                friendlyKingCoords.Item2 = p.y;
            }

        }
        return (attackedSquares,friendlyKingCoords);
    }

    public List<(int, int, int, int)> GetAllValidMoves(char color)
    {
        bool isWhite = IsWhite(color);
        List<(int,int,int, int)> attackedSquares = new List<(int, int, int, int)>();
        foreach (PieceHandler p in board)
        {
            if (p == null) continue;
            if (IsWhite(p) == isWhite)
            {
                List<(int, int)> attacked = GetMovesOrAttacks(p.x, p.y, false);
                foreach ((int, int) square in attacked)
                {
                    attackedSquares.Add((p.x,p.y,square.Item1,square.Item2));
                }
            }

        }
        return (attackedSquares);
    }

    // Called after a move was made, to update everything accordingly.
    // Also handles castling
    public void MakeMove(int fromx, int fromy, char pieceType, bool capture, int tox)
    {
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
                    board[8, ylevel].x = 6;
                    board[8, ylevel].transform.position = new Vector2(6,ylevel);
                    boardHandler.MovePiece(8, ylevel, 6, ylevel);
                    MakeMove(8, ylevel, rookChar, false, 6);
                    return;
                }

                if (tox == 3)
                {
                    // Long castle
                    board[1, ylevel].x = 4;
                    board[1, ylevel].transform.position = new Vector2(4, ylevel);
                    boardHandler.MovePiece(1, ylevel, 4, ylevel);
                    MakeMove(1, ylevel, rookChar, false, 4);
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
            if(positionRepetitionCount[position] >= 3)
            {
                threeFoldRepetition = true;
            }
        }

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
}
