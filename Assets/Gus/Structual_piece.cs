using UnityEngine;
using System.Collections.Generic;
//using OGPC-2025-2026.Assets.Gus;
//using OGPC BUGGY CODE TEST.scripts;

public class Structual_piece : MonoBehaviour
{
    void Start(){
        int X = 0;
        int Y = 0;
        int R = 0;
    }
    void Update(){
        if (Input.GetMouseButtonDown(0)){ //probably not needed
            Vector3 MousePos = Input.mousePosition;
            transform.Translate(Vector3.mousePosition);

    }
}