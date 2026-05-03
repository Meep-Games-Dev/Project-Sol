using StationO;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;


namespace HangerModule//This entire namespace works for all of the pieces, just change the Activation()
{
    public class Hanger : MonoBehaviour
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

        public InputAction rightMouse;
        public PlayerInput input;
        public string type = "functional";
        public string ID = "F_HM";
        bool isEnabled = false;
        public Vector3 setPosition = new Vector3(-7f, 6f, 0f);
        [SerializeField] public GameObject objectToShow;
        public void OnEnable()
        {
            rightMouse.Enable();
        }
        public void OnDisable()
        {
            rightMouse.Disable();
        }
        void Awake()
        {

            input = new PlayerInput();
            rightMouse = input.Main.MouseClickRight;
            transform.position = new Vector3(-7f, 6f, 0f);
            hide();
        }
        public void show()
        {
            /*
            if (objectToShow != null)
            {
                objectToShow.SetActive(true);
            }
            */
        }
        public void hide()
        {
            /*
            if (objectToShow != null)
            {
                objectToShow.SetActive(false);
            }
            */
        }
        void Update()
        {
            if (transform.position.x != Math.Round(transform.position.x) || transform.position.y != Math.Round(transform.position.y))
            {
                transform.position = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0f);
            }
        }
        void BuyColonyShip()
        {
            SpaceStation.credits -= 1500;
            SpaceStation.people -= 50;
        }
        void BuyLightFighter()
        {
            SpaceStation.credits -= 100;
            SpaceStation.people -= 5;
        }
        void BuyTransportShip()
        {
            SpaceStation.credits -= 300;
            SpaceStation.people -= 5;
        }
    }
}