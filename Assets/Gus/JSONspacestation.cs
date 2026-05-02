using System.Collections.Generic;
using System.IO;
using UnityEngine;


// SAVE/LOAD STATION PROCESS: Build the station -> save (Convert to JSON) -> load (Convert from JSON to station)

namespace Savee
{
    public class mainS
    {
        Dictionary<string, string> Saves = new Dictionary<string, string>()
        {
            {"StarterStation", "StarterStation.json"},
            {"Station1", "spacestation1.json"}, //Rename "station1" to the actual name of the save file
            {"Station2", "spacestation2.json"}, //Rename "station2" to the actual name of the save file
            {"Station3", "spacestation3.json"}, //Rename "station3" to the actual name of the save file
            {"Station4", "spacestation4.json"}, //Rename "station4" to the actual name of the save file
            {"Station5", "spacestation5.json"} //Rename "station5" to the actual name of the save file
        };
public Dictionary<string, (int X, int Y)> APIECE = new Dictionary<string, (int X, int Y)>();
public static Dictionary<GameObject, Vector3> save = new Dictionary<GameObject, Vector3>();
    public List<dynamic> saves = new List<dynamic>() // this might replace the Dictionary Saves because you can do saves[0] for indexing and do
    {
        "spacestation1.json",
        "spacestation2.json",
        "spacestation3.json",
        "spacestation4.json",
        "spacestation5.json"
    };

public int O2 = 0;
public int Ore = 0;
public int H2O = 0;
public int Carbon = 0;
public int Food = 0;
public int Metals = 0;
public int energy = 0;
        public void SaveStation(int station, Dictionary<string, object> saveData) // this would be SaveStation(int station) and make it so the pieces are the piece IDs
        {
            if(station > 5)
            {
                station = 5;
            }
            if(station < 1)
            {
                station = 1;
            }
            string jsonData = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(Application.persistentDataPath + $"/spacestation{station}.json", jsonData);
            Debug.Log(jsonData);
        }

public void LoadStation(int station) // LoadStation(0) to load station1.json
        {
if(station > 5)
{
station = 5;
}
if(station < 1)
{
station = 1;
}

        }
    }
}