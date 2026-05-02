using UnityEngine;
using System.Collections.Generic;
using ResourceMiner;
//using JSONspacestation;
//using DrawConnecter;
using FoodModule;
using OreRefiner;
using Unity.VisualScripting;
using Ra;


//if money >= 1000 and resources >= 500 then proceed and subtract the resources and money
namespace StationO
{
    public class SpaceStation : MonoBehaviour // add to unity project
    {
        public static int credits;
        public static int people = 50;
        public int peoplelimit = 100;
        public RayController raycontroller;
        public static List<GameObject> ATpiece = new List<GameObject>();
        public static Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>()
        {   
            {ResourceType.O2, 0}, //oxygen 
            {ResourceType.Ore, 0}, //ore -> metals 
            {ResourceType.H2O, 0}, //water
            {ResourceType.Carbon, 0}, //carbon
            {ResourceType.Food, 0}, //food
            {ResourceType.Metals, 0}, // Refined Metal
            {ResourceType.People, 50},
            {ResourceType.Credits, 0}, // Credits, affected in ResorceMiner and Hanger/trade modules
            {ResourceType.PowerCells, 5} // power, 1 solar panel array gives + 10 power, each module gives -1 power and you can transmit power to other stations
        };
        //public static List<GameObject> ATpiece; // make 
        void createstation(Station station)
        { 
            if(resources[ResourceType.Credits] >= 1000 && resources[ResourceType.O2] >= 500 && resources[ResourceType.H2O] >= 500 && resources[ResourceType.Carbon] >= 500) // check if the player has enough resources to create the station
            {
                credits -= 1000; // subtract the credits
                resources[ResourceType.O2] -= 500; // subtract the O2
                resources[ResourceType.H2O] -= 500; // subtract the H2O
                resources[ResourceType.Carbon] -= 500; // subtract the Carbon
                people -= 50; // subtract the people
                
            }
        }

        void Update()
        {
            resources[ResourceType.Credits] = credits;
            resources[ResourceType.People] = people;
            if (resources[ResourceType.Food] >= 20) // refines food into people
            {
                if(people < peoplelimit)
                {
                    resources[ResourceType.Food] -= 20;
                    people += 1;
                }
            }
            foreach(var s in raycontroller.stations)
            {
                foreach (var u in s.pieces)
                {
                    IActivable e = u.GetComponent<IActivable>();
                    if(e.type == "functional")
                    {
                        e.Activation();
                    }
                }
            }
        }
    } 
}
