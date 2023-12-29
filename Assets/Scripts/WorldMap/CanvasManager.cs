using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Assets.Scripts.Miscellaneous;
using static Assets.Scripts.WorldMap.GridManager;

namespace Assets.Scripts.WorldMap
{
    internal class CanvasManager : MonoBehaviour
    {
        public GridSpawner gridSpawner;
        public MoveCamera moveCam;

        public Text fpsText;

        public InputField xInput;
        public InputField yInput;

        public InputField scrollSpeed;


        public Button genButton;

        public Text genTime;
        
        private void Awake()
        {
            Application.targetFrameRate = -1;
            
            genButton.onClick.AddListener(GenClick);

            xInput.OnSelect(null);

            scrollSpeed.onValueChanged.AddListener(delegate { ChangeScrollSpeed(); });
        }

        private void Update()
        {
            CountFrame();
        }

        private void ChangeScrollSpeed()
        {
            float speed = float.Parse(scrollSpeed.text);
            moveCam.zoomSpeed = speed;
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

            Vector2Int size = new Vector2Int(x, y);

            gridSpawner.CanvasGenerate(size);

            string parseTime = ExtensionMethods.ParseLogTimer("", gridSpawner.generateTime);

            genTime.text = parseTime;
        }
  
    }
}
