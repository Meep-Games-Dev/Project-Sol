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
        public static Dictionary<Resources, int> resources = new Dictionary<Resources, int>()
        {   
            {Resources.Ice, 0},
            {Resources.Ore, 0}, //ore -> metals 
            {Resources.H2O, 0}, //water
            {Resources.Carbon, 0}, //carbon
            {Resources.Food, 0}, //food
            {Resources.RefinedMetal, 0}, // Refined Metal
            {Resources.Population, 50},
            {Resources.Credits, 0}, // Credits, affected in ResorceMiner and Hanger/trade modules
            {Resources.EnergyCells, 5} // pow er, 1 solar panel array gives + 10 power, each module gives -1 power and you can transmit power to other stations
        };
        //public static List<GameObject> ATpiece; // make 
        void createstation(Station station)
        { 
            if(resources[Resources.Credits] >= 1000 && resources[Resources.H2O] >= 500 && resources[Resources.Carbon] >= 500) // check if the player has enough resources to create the station
            {
                credits -= 1000; // subtract the credits
                resources[Resources.H2O] -= 500; // subtract the H2O
                resources[Resources.Carbon] -= 500; // subtract the Carbon
                people -= 50; // subtract the people
                
            }
        }

        void Update()
        {
            resources[Resources.Credits] = credits;
            resources[Resources.Population] = people;
            if (resources[Resources.Food] >= 20) // refines food into people
            {
                if(people < peoplelimit)
                {
                    resources[Resources.Food] -= 20;
                    people += 1;
                }
            }
            foreach(var s in raycontroller.stations)
            {
                credits += s.resources[Resources.Credits];
                s.resources[Resources.Credits] = 0;
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
