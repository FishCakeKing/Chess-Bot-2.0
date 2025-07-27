using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script manages everything the piece does and is.
// It stores coordinates and piece type, and has functions to move the piece around
public class PieceHandler : MonoBehaviour
{
    // GUI variables
    bool isDragging; // Is the player dragging around the piece with their mouse?
    Vector3 offset; // This makes sure the piece does not always snap to the mouse coords when we pick it up

    public GameObject rulesHandlerGameObject; // Poll this to make sure a move is legal, before allowing dropping the piece
    private RulesHandler rulesHandler;

    public GameObject boardHandlerObject;
    private BoardHandler boardHandler;

    // Piece properties
    public int x, y;
    public char pieceName; // Q = white queen, q = black queen etc

    public Piece p;

    private bool choosingPromotion;

    private GameObject promotionPane;

    public void Init()
    {
        isDragging = false;
        x = (int)transform.position.x;
        y = (int)transform.position.y;
        p = new Piece(x, y, pieceName,this.gameObject);
        rulesHandler = rulesHandlerGameObject.GetComponent<RulesHandler>();
        boardHandler = boardHandlerObject.GetComponent<BoardHandler>();
        choosingPromotion = false;
    }

    
    void Update()
    {
        if(choosingPromotion)
        {
            if (promotionPane == null)
            {
                transform.position = new Vector2(x, y);
                choosingPromotion = false;
                return;
            }
            if(promotionPane.GetComponent<PromotionHandler>().HasSelectedPiece())
            {
                char chosenPiece = promotionPane.GetComponent<PromotionHandler>().GetSelectedPiece();
                var newCoords = GetCoordinates();
                string move = boardHandler.GetSquareNotation(newCoords.Item1, newCoords.Item2) + "="+chosenPiece;
                if (rulesHandler.IsMoveLegal(x, y, move))
                {
                    Move(move);
                }
                // Here the piece is destroyed
            }
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
    
    // When the piece is dropped in a valid location, it should snap to whatever square it is over.
    private void snapToGrid()
    {
        // World coordinates are the same as grid coordinates
        float newx = (float)System.Math.Round(transform.position.x);
        float newy = (float)System.Math.Round(transform.position.y);

        // Clamp x and y to be within the actual board
        newx = System.Math.Max(1, newx);
        newx = System.Math.Min(8, newx);
        newy = System.Math.Min(8, newy);
        newy = System.Math.Max(1, newy);


        transform.position = new Vector2(newx, newy);

        x = (int)newx;
        y = (int)newy;
    }

    private void OnMouseUp()
    {        
        isDragging = false;
        var newCoords = GetCoordinates();
        // Here we need to spawn an object to choose promotion piece and send that in together with the move
        string move = boardHandler.GetSquareNotation(newCoords.Item1, newCoords.Item2);
        print("Released " + pieceName);
        if(pieceName == 'p' && newCoords.Item2 == 1 || pieceName == 'P' && newCoords.Item2 == 8)
        {
            choosingPromotion = true;
            if (pieceName < 97)
            {
                promotionPane = Instantiate(boardHandler.promotionPaneWhitePrefab, new Vector3(x, y, 0), Quaternion.identity, this.transform);
                boardHandler.SetPromotionPane(promotionPane);
            }
            else
            {
                promotionPane = Instantiate(boardHandler.promotionPaneBlackPrefab, new Vector3(x, y, 0), Quaternion.identity, this.transform);
                boardHandler.SetPromotionPane(promotionPane);
            }
            return;
        }

        if (rulesHandler.IsMoveLegal(x,y,move))
        {
            Move(move);
        }
        else
        {
            // Illegal move, ignore it
        }
        transform.position = new Vector2(x, y);
        print(pieceName + " fully released");


    }

    public void Move(string move)
    {
        if (rulesHandler.IsMoveLegal(x, y, move))
        {
            (int, int) newCoords = boardHandler.GetCoordsFromSquareNotation(move);
            bool capture = boardHandler.MovePiece(x, y, move); // Tell the board handler that we moved
            rulesHandler.MakeMove(x, y, pieceName, capture, newCoords.Item1, newCoords.Item2);
            boardHandler.SetCounters(rulesHandler.GetCounters()); // Just for debugging
            boardHandler.SetCastling(rulesHandler.GetCastlingRights());
            snapToGrid();
        }
    }

    public void MoveNoVerification(string move)
    {
        (int, int) newCoords = boardHandler.GetCoordsFromSquareNotation(move);
        bool capture = boardHandler.MovePiece(x, y, move); // Tell the board handler that we moved
        rulesHandler.MakeMove(x, y, pieceName, capture, newCoords.Item1, newCoords.Item2);
        boardHandler.SetCounters(rulesHandler.GetCounters()); // Just for debugging
        boardHandler.SetCastling(rulesHandler.GetCastlingRights());
        snapToGrid();
    }


        private void OnMouseDown()
    {
        if (Input.GetMouseButton(0))
        {
            print("Selected " + pieceName);
            isDragging = true;
            offset = transform.position - GetMouseWorldPosition();
            boardHandler.RemoveHighlights();
            
            boardHandler.activePieceReachableSquares = rulesHandler.GetMovesOrAttacks(x,y,false);
            print("Attacks gotten");
            boardHandler.HighLightSquares(boardHandler.activePieceReachableSquares);
            boardHandler.HighlightSquare(x, y,true);
            boardHandler.DestroyPromotionPlane();
            print(pieceName + " fully selected");
        }
    }

    private void OnMouseDrag()
    {
        if (isDragging)
        {
            transform.position = GetMouseWorldPosition() + offset;
        }
    }
    
    private (int,int) GetCoordinates()
    {
        return ((int)System.Math.Round(transform.position.x), (int)System.Math.Round(transform.position.y));
    }

    // Called when non-humans need to move pieces
    public void MoveTo(int tox, int toy)
    {
        transform.position = new Vector2(tox, toy);
    }

}
