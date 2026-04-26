using UnityEngine;
using System.Collections.Generic;
//using Station;
//using SpaceStationPeices;
using System;
using System.Threading;

namespace SPanel
{
    public class SolarP : MonoBehaviour
    {
        public string type = "static";
        public string id = "S_PS";
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
            transform.position = new Vector3(-6f, 4f, 0f);
            hide();
        }
        void Update()
        {
            if(transform.position.x != Math.Round(transform.position.x) || transform.position.y != Math.Round(transform.position.y))
            {
                transform.position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0f);
            }
        }
    }
}