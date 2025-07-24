using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clickable : MonoBehaviour
{
    bool clicked;
    private void Start()
    {
        clicked = false;
    }
    private void OnMouseDown()
    {
        clicked = true;
    }

    public bool WasClicked() { return clicked; }
}

