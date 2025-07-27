using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece
{
    public int x, y;
    public char pieceName;
    public GameObject pieceObject;
    public Piece(int x, int y, char name,GameObject pieceObject = null)
    {
        this.x = x;
        this.y = y;
        pieceName = name;
        this.pieceObject = pieceObject;
    }

}
