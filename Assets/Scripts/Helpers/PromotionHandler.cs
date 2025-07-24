using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PromotionHandler : MonoBehaviour
{
    // This script is attached to the promotion pane.
    // It listens for which piece has been chosen and reports once one has been
    public GameObject queenButton;
    public GameObject rookButton;
    public GameObject knightButton;
    public GameObject bishopButton;
    List<Clickable> promotionPieces;
    bool selectedPiece;
    char selectedPieceName;
    void Start()
    {
        promotionPieces = new List<Clickable>();
        promotionPieces.Add(queenButton.GetComponent<Clickable>());
        promotionPieces.Add(rookButton.GetComponent<Clickable>());
        promotionPieces.Add(knightButton.GetComponent<Clickable>());
        promotionPieces.Add(bishopButton.GetComponent<Clickable>());        
        selectedPiece = false;
        selectedPieceName = '-';
    }

    void Update()
    {
        if(!selectedPiece)
        {
            foreach (Clickable c in promotionPieces)
            {
                if (c.WasClicked())
                {
                    selectedPiece = true;
                    selectedPieceName = c.gameObject.name[0];
                }
            }
        }
    }

    public bool HasSelectedPiece() { return selectedPiece; }
    public char GetSelectedPiece() { return selectedPieceName; }
}
