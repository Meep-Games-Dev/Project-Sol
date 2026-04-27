using UnityEngine;
using System.Collections.Generic;
using Station;
using System;
using System.Threading;

namespace FoodModule
{
    public class Food : MonoBehaviour
    {
        public int UpCost = 2500;
        public int level = 1;
        public Dictionary<int, int> time = new Dictionary<int, int> ()
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
        int X = 0;
        int Y = 0;
        int R = 0;
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
            if(transform.position.x != Math.Round(transform.position.x) || transform.position.y != Math.Round(transform.position.y))
            {
                transform.position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0f);
            }
        }   
        void Activation()//on build.cs or RootSpaceStation.cs call Structual_piece.Activation(); to start the function
        {
            //control for the piece
            // The level influences the number of resources needed to refine into 50 food
            if ((SpaceStation.resources[ResourceType.Carbon] >= 1 + Mathf.Round(100 * Mathf.Pow(1 + 0.05f, 7 - level))) && (SpaceStation.resources[ResourceType.H2O] >= 1 + Mathf.Round(100 * Mathf.Pow(1 + 0.05f, 7 - level)))) // refines Carbon and H2O into Food
            {
                SpaceStation.resources[ResourceType.Carbon] -= Mathf.RoundToInt(100 * Mathf.Pow(1 + 0.05f, 7 - level));
                SpaceStation.resources[ResourceType.H2O] -= Mathf.RoundToInt(100 * Mathf.Pow(1 + 0.05f, 7 - level));
                SpaceStation.resources[ResourceType.Food] += 50;
            }
        }
        void upgrade() // in space station.cs 
        {
            if (time[level] < 7)
            {
                if (SpaceStation.resources[ResourceType.Credits] >= UpCost)
                {
                    SpaceStation.resources[ResourceType.Credits] -= UpCost;
                    level += 1;
                    UpCost += 2500;
                }
            }
        }       
    }
}