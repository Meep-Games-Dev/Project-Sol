using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class Root // might not matter bc Root
// is an unmovable object with the sole purpose of linking together a space station, not moving!
{
    public int X;
    public int Y;
    public int R;
}


public class RootSpaceStationPeice : MonoBehaviour
{

    public OtherSpaceStationPeices referance;
    
    var pieces = 0;
    void Start()
    {
        X = 0;
        Y = 0;
        Z = 0;
    }
    void Disp(msg, txtsize, x, y)//display a message the the screen
    {
        // do the UI rendering "fun" where the msg fades after 2.5 seconds
    }

    void Update()
    {
        if (X != 0 || Y != 0 || R != 0)
        {
            X = 0;
            Y = 0;
            Z = 0;
        }
        if (OtherSpaceStationPeices.SelectedPiece /*Is touching root*/)
        {
            pieces += 1;
            //detect where the piece is touching and append it to that area.
        }
        if (pieces == 0)
        {
            Disp("Attach a piece to the root piece to get started!", 15, 0, 5);
        }
        if (pieces == 64) // to prevent crashing the game and messing up storage later update to a better #
        {
            Disp("Error, you cannot have more than 64 pieces on the space station at a time", 20, 0, 0);
            //Don't allow anymore pieces to be added
        }
    }
}
