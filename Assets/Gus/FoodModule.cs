using Station;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace FoodModule
{
    public class Food : MonoBehaviour
    {
        public int UpCost = 2500;
        public int level = 1;
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
        public string type = "functional";
        public string id = "F_FM";
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
            transform.position = new Vector3(-6f, 2f, 0f);
            hide();
        }
        // Update is called once per frame   
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
            // The level influences the number of resources needed to refine into 50 food
            if ((SpaceStation.resources[(int)Resources.Carbon].amount >= 1 + Mathf.Round(100 * Mathf.Pow(1 + 0.05f, 7 - level))) && (SpaceStation.resources[(int)Resources.H2O].amount >= 1 + Mathf.Round(100 * Mathf.Pow(1 + 0.05f, 7 - level)))) // refines Carbon and H2O into Food
            {
                SpaceStation.resources[(int)Resources.Carbon].amount -= Mathf.RoundToInt(100 * Mathf.Pow(1 + 0.05f, 7 - level));
                SpaceStation.resources[(int)Resources.H2O].amount -= Mathf.RoundToInt(100 * Mathf.Pow(1 + 0.05f, 7 - level));
                SpaceStation.resources[(int)Resources.Food].amount += 50;
            }
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