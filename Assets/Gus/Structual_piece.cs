using UnityEngine;
using System.Collections.Generic;
using System;
//using Station;
//using SpaceStationPeices;

namespace Structual_piece //The piece is a 90 degree connector - block straight-line and replace with straight-line-90 degree connector
//Make another piece that is just a strightline connecter.
{
    public class Structual : MonoBehaviour
    {
        public string type = "static"; // no function 
        public string id = "S_SP";
        public Vector3 setPosition = new Vector3(0f, 6f, 0.0f);
        public GameObject objectToShow; 
        public void show()
        {
            if (objectToShow != null)
            {
                objectToShow.GetComponent<RectTransform>().position = setPosition; 
                objectToShow.SetActive(true);
            }
        }
        public void hide()
        {
            if (objectToShow != null)
            {
                objectToShow.GetComponent<RectTransform>().position = setPosition;
                objectToShow.SetActive(false);
            }
        }
        
        void Awake()
        {
            transform.position = new Vector3(-7f, -4f, 0f);
            hide();
        }
        void Update() // the rounding for the piece is only activated when the piece is placed, not when it is being dragged
        {
            if(transform.position.x != Math.Round(transform.position.x) || transform.position.y != Math.Round(transform.position.y))
            {
                transform.position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0f);
            }
        }
    }
}