using UnityEngine;
using System.Collections.Generic;
using StationO;
using System;
using System.Threading;
using UnityEngine.InputSystem;


namespace OreRefiner
{
    public class OreRefinerModule : MonoBehaviour
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
        public string id = "F_OR";

        private bool isDragging = false;
        private Camera mainCamera;
        private Vector3 offset;
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
            transform.position = new Vector3(-7f, 0f, 0f);
            // Hide the object initially
            hide();
        }

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
            if ((SpaceStation.resources[ResourceType.Ore] >= 1 + Mathf.Round(100 * Mathf.Pow(1 + 0.05f, 7 - level)))) // refines Carbon and H2O into Food
            {
                SpaceStation.resources[ResourceType.Ore] -= Mathf.RoundToInt(100 * Mathf.Pow(1 + 0.05f, 7 - level));
                SpaceStation.resources[ResourceType.Metals] += 15;
            }
        }
        void upgrade() // in space station.cs 
        {
            if (time[level] < 7)
            {
                if (SpaceStation.credits >= UpCost)
                {
                    SpaceStation.resources[ResourceType.Credits] -= UpCost;
                    level += 1;
                    UpCost += 2500;
                }
            }
        }      
    }
}