//using JSONspacestation;
//using DrawConnecter;
using FoodModule;
using OreRefiner;
using ResourceMiner;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


//if money >= 1000 and resources >= 500 then proceed and subtract the resources and money
namespace Station
{
    public class SpaceStation : MonoBehaviour // add to unity project
    {
        public interface IActivatable
        {
            string type { get; set; }
            string id { get; set; }
            void Activation();
            void upgrade();
        }
        public static int credits;
        public int people = 50;
        public int peoplelimit = 100;
        public static List<GameObject> ATpiece = new List<GameObject>();
        public static List<Resource> resources = new List<Resource>()
        {
            new Resource
            {
                type = Resources.Ore,
                amount = 0
            },
            new Resource
            {
                type = Resources.Ice,
                amount = 0
            },
            new Resource
            {
                type = Resources.Carbon,
                amount = 0
            },
            new Resource
            {
                type = Resources.RefinedMetal,
                amount = 0
            },
            new Resource
            {
                type = Resources.H2O,
                amount = 0
            },
            new Resource
            {
                type = Resources.Food,
                amount = 0
            },
            new Resource
            {
                type = Resources.EnergyCells,
                amount = 0
            },

            new Resource
            {
                type = Resources.Population,
                amount = 0
            },

            new Resource
            {
                type = Resources.Credits,
                amount = 0
            }
        };
        //public static Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>()
        //{   
        //    {Resources.O2, 0}, //oxygen 
        //    {ResourceType.Ore, 0}, //ore -> metals 
        //    {ResourceType.H2O, 0}, //water
        //    {ResourceType.Carbon, 0}, //carbon
        //    {ResourceType.Food, 0}, //food
        //    {ResourceType.Metals, 0}, // Refined Metal
        //    {ResourceType.People, 50},
        //    {ResourceType.Credits, 0}, // Credits, affected in ResorceMiner and Hanger/trade modules
        //    {ResourceType.PowerCells, 5} // power, 1 solar panel array gives + 10 power, each module gives -1 power and you can transmit power to other stations
        //};
        //public static List<GameObject> ATpiece; // make 
        void createstation()
        {
            if (resources[(int)Resources.Credits].amount >= 1000 && resources[(int)Resources.H2O].amount >= 500 && resources[(int)Resources.Carbon].amount >= 500) // check if the player has enough resources to create the station
            {
                credits -= 1000; // subtract the credits
                resources[(int)Resources.H2O].amount -= 500; // subtract the O2
                resources[(int)Resources.Carbon].amount -= 500; // subtract the Carbon
                people -= 50; // subtract the people
                //resources = the resources in the loaded station
                //change script names and function names to actual names
                // DrawConnecter.cs had a function that draws the connecter (drawConnecter()) and JSONsaving.cs just saves the 
                //positions of each piece if it isn't directly attached
                //JSONspacestation.loadstation(The station being loaded); <- all that's needed
            }
        }

        void Update()
        {
            resources[(int)Resources.Credits].amount = credits;
            resources[(int)Resources.Population].amount = people;
            /*
            foreach (var p in ATpiece)
            {
                //if(p.id == "S-SP")
                //{
                    //resources["power"] += 10; // or just do: add a int count +=1 every time and power = count*10
                //}
                //if(p.id == "")
                IActivatable piece = p.GetComponent<IActivatable>();
                if (piece != null && piece.type == "functional")
                {
                    piece.Activation();
                }
            }
            */
            if (resources[(int)Resources.Food].amount >= 100) // refines food into people
            {
                if (people < peoplelimit)
                {
                    resources[(int)Resources.Food].amount -= 100;
                    people += 5;
                }
            }
        }
    }
}
