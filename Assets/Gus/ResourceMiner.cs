using Station;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ResourceMiner//This entire namespace works for all of the pieces, just change the Activation()
{
    public class ResourceMine : MonoBehaviour
    {
        public int level = 1;
        public int UpCost = 2500;
        public Dictionary<int, int> time = new Dictionary<int, int>()
        {
            {1, 5000},
            {2, 4500},
            {3, 4000},
            {4, 3500},
            {5, 3000},
            {6, 2500},
            {7, 2000}
        };
        // ect. Decreasing by 500 each time until it reaches 2000 (max level)


        public string type = "functional";
        public string ID = "F_RM";
        int X = 0;
        int Y = 0;
        public Vector3 setPosition = new Vector3(130f, 182f, 0.0f);
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
            transform.position = new Vector3(-6f, -2f, 0f);
            hide();
        }
        void Update()
        {
            if (transform.position.x != Math.Round(transform.position.x) || transform.position.y != Math.Round(transform.position.y))
            {
                transform.position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0f);
            }
        }
        void Activation()//on build.cs or RootSpaceStation.cs call Structual_piece.Activation(); to start the function
        {
            //control for the piece
            Thread.Sleep(time[level]);
            SpaceStation.resources[(int)Resources.Ore].amount += 10;
            SpaceStation.resources[(int)Resources.H2O].amount += 10;
            SpaceStation.resources[(int)Resources.Carbon].amount += 100;
            SpaceStation.resources[(int)Resources.Credits].amount += 25;
        }
        void upgrade() // in space station.cs 
        {
            if (time[level] < 7)
            {
                if (SpaceStation.resources[(int)Resources.Credits].amount >= UpCost)
                {
                    SpaceStation.resources[(int)Resources.Credits].amount -= UpCost;
                    level += 1;
                    UpCost += 2500;
                }
            }
        }
    }
}