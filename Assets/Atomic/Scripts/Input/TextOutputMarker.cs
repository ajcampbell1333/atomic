using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Atomic.Input
{
    public class TextOutputMarker : Singleton<TextOutputMarker>
    {
        [HideInInspector] public List<Text> displays;

        public bool isActive
        {
            get
            {
                return displays.Count  > 0;
            }
        }

        private void Awake()
        {
            if (GetComponent<Text>().enabled)
            {
                // Used as a debugging console - if needed, toggle it on in Inspector
                Text output = GetComponent<Text>();
                RegisterOutput(ref output);
            }
        }

        /// <summary>
        /// Connect the hovered string var to the text output source
        /// </summary>
        public void RegisterOutput(ref Text _display)
        {
            displays.Add(_display);
        }

        /// <summary>
        /// Disconnect all connected string vars from the text output source
        /// </summary>
        public void UnregisterAll()
        {
            displays.Clear();
        }

        public string GetCurrentDisplayString()
        {
            if (displays.Count > 0)
                return displays[0].text;
            else return "";
        }

        public void UpdateOutputs(string newOutput)
        {
            foreach (Text display in displays)
                display.text = newOutput;
        }
    }
}


