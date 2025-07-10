using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceHandler : MonoBehaviour
{
    // GUI variables
    bool isDragging; // Is the player dragging around the piece with their mouse?
    Vector3 offset; // This makes sure the piece does not always snap to the mouse coords when we pick it up

    public GameObject rulesHandlerGameObject; // Poll this to make sure a move is legal, before allowing dropping the piece
    private RulesHandler rulesHandler;
    // Piece properties
    public int x, y;
    public char pieceName; // Q = white queen, q = black queen etc

    void Start()
    {
        isDragging = false;
        x = (int)transform.position.x;
        y = (int)transform.position.y;
        rulesHandler = rulesHandlerGameObject.GetComponent<RulesHandler>();
    }

    
    void Update()
    {
        
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

        // The piece did not move
        if (newCoords.Item1 == x && newCoords.Item2 == y) return;

        if(rulesHandler.IsMoveLegal(x,y,newCoords.Item1,newCoords.Item2))
        {
            snapToGrid();
        }

        transform.position = new Vector2(x, y);

    }

        private void OnMouseDown()
    {
        if (Input.GetMouseButton(0))
        {
            isDragging = true;
            offset = transform.position - GetMouseWorldPosition();
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
        return ((int)System.Math.Round(transform.position.x), (int)System.Math.Round(transform.position.x));
    }

}
