using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Assets.Scripts.Miscellaneous;

namespace Assets.Scripts.WorldMap
{
    internal class CanvasManager : MonoBehaviour
    {
        public GridManager gridManager;

        public Text fpsText;

        public InputField xInput;
        public InputField yInput;


        public Button genButton;

        public Text genTime;
        
        private void Awake()
        {
            Application.targetFrameRate = -1;
            
            genButton.onClick.AddListener(GenClick);

            xInput.OnSelect(null);
        }

        private void Update()
        {
            CountFrame();
        }


        public float updateInterval = 0.5f; // Time interval to update the frame rate
        private float accumulatedFrames = 0;
        private float timeLeft;
        void CountFrame()
        {
            timeLeft -= Time.deltaTime;
            accumulatedFrames++;

            if (timeLeft <= 0)
            {
                float framesPerSecond = accumulatedFrames / updateInterval;
                fpsText.text = framesPerSecond.ToString("F2");

                accumulatedFrames = 0;
                timeLeft = updateInterval;
            }
        }
        public void GenClick()
        {
            genTime.text = "Generating...";
            int x = int.Parse(xInput.text);
            int y = int.Parse(yInput.text);

            gridManager.planetGenerator.MainPlanet.PlanetSize = new Vector2Int(x, y);
            
            gridManager.GenerateGridChunks();

            string parseTime = ExtensionMethods.ParseLogTimer("", gridManager.time);

            genTime.text = parseTime;
        }
  
    }
}
