using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RealRoster
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class RealRoster : MonoBehaviour
    {
        public static readonly string TAG = "Base";

        // Instance of this singleton
        public static RealRoster instance = null;

        // Reference to settings scenario

        // List of registered CrewSelectionModes
        public List<ICrewSelectionMode> modes = new List<ICrewSelectionMode>();

        public void Awake()
        {
            CommonLogic.DebugMessage("Awake()");
            instance = this;
        }

        public void Start()
        {
            CommonLogic.DebugMessage("Start()");

            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Add(onEditorShipModified);
                CommonLogic.DebugMessage("Successfully registered onEditorShipModified");
            }
        }
        
        // Occurs on every frame
        public void Update()
        {
            
        }

        public void OnDestroy()
        {
            CommonLogic.DebugMessage("OnDestroy()");
            instance = null;
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
        }

        // Occurs every time any part is 'clicked' in the editor.
        void onEditorShipModified(ShipConstruct ship)
        {
            
        }

        public void registerCrewSelectionMode(ICrewSelectionMode mode)
        {
            if (!modes.Contains(mode))
            {
                CommonLogic.DebugMessage("Registering CrewSelectionMode: " + mode.ToString());
                modes.Add(mode);
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    class SpaceCenterModule : MonoBehaviour
    {
        public void Awake()
        {
            GameEvents.onGUILaunchScreenVesselSelected.Add(onVesselSelected);
        }

        private void onVesselSelected(ShipTemplate shipTemplate)
        {
            CMAssignmentDialog dialog = CMAssignmentDialog.Instance;
            VesselCrewManifest vcm = dialog.GetManifest();

            foreach (PartCrewManifest pcm in vcm)
            {
                for (int i = 0; i < pcm.GetPartCrew().Length; i++)
                {
                    pcm.RemoveCrewFromSeat(i);
                }
            }

            dialog.RefreshCrewLists(vcm, false, true);
        }

        public void Destroy()
        {
            GameEvents.onGUILaunchScreenVesselSelected.Remove(onVesselSelected);
        }
    }
   
    public static class CommonLogic
    {
        // If this value is true, print debug messages
        private static bool debug = (true && CommonLogic.globalDebug);
        public static bool globalDebug = true;

        // Conditionally prints a debug message.
        public static void DebugMessage(string tag, string message)
        {
            if (debug)
            {
                UnityEngine.Debug.Log("RealRoster: " + tag + ": " + message);
            }
        }
    }
}