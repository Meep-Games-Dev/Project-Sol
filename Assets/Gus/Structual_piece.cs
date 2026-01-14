using UnityEngine;
using System.Collections.Generic;
using SpaceStation;

namespace Structual_Piece
{
    public class Structual_piece : MonoBehaviour
    {
        int X = 0;
        int Y = 0;
        int R = 0;
        void update()
        {
            /* if (X > canvas_max_X || X < canvas_min_X || Y > canvas_max_Y || Y < canvas_min_Y)
            {
                X = 0;
                Y = 0;
            }
            */
        }
        void Activation()//on build.cs or RootSpaceStation.cs call Structual_piece.Activation(); to start the function
        {
            //control for the piece
        }
    }
}
