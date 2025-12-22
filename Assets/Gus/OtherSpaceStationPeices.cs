using System;
using UnityEngine;
using System.Collections.Generic;


public class Structual_piece
{
    public Structual_piece referance;
    public int X;
    public int Y;
    public int R;// rotation in 90 degree increments-starting at 0
}
public class ResourceMiner_piece
{
    public ResourceRefine referance;
    public int X;
    public int Y;
    public int R;
}
public class OtherSpaceStationPeices : MonoBehaviour
{

    public List<Item> pieces= new List<Item>() {Structual_Piece, ResourceMiner_piece};



    void Update()
    {
        for (int i = 0; i < pieces)
        {
            if (Input.GetKey(KeyCode.E))
            {
                var SelectedPiece = pieces(i);
                i += 1;
                if (i > pieces)
                {
                    i = 0;
                }
            }
        }
        if (Input.GetKey(KeyCode.R))
        {
            SelectedPiece.R += 90;
        }
    }
}
