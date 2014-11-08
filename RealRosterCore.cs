using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RealRoster
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class PersistenceBehaviour : MonoBehaviour
    {

    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class EditorBehaviour : MonoBehaviour
    {
        private static readonly string TAG = "EditorBehaviour";

        private Part firstPod = null;
        private bool firstPodFlag = false;

        void Awake()
        {
            GameEvents.onEditorShipModified.Add(onEditorShipModified);
        }

        void Update()
        {
            if (firstPodFlag)
            {
                CommonLogic.getDefaultManifest();
                firstPodFlag = false;
            }
        }

        void Destroy()
        {
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
        }

        void onEditorShipModified(ShipConstruct ship)
        {
            if (ship.Count > 0 && firstPod == null)
            {
                foreach (Part part in ship)
                {
                    if (part.isControlSource && part.CrewCapacity > 0)
                    {
                        firstPod = part;
                        firstPodFlag = true;
                        break;
                    }
                }
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    class SpaceCenterBehaviour : MonoBehaviour
    {
        private static readonly string TAG = "SpaceCenterBehaviour";

        public void Awake()
        {
            GameEvents.onGUILaunchScreenVesselSelected.Add(onVesselSelected);
        }

        private void onVesselSelected(ShipTemplate shipTemplate)
        {
            CommonLogic.DebugMessage(TAG, "onVesselSelected()");
            CommonLogic.getDefaultManifest();
        }

        public void Destroy()
        {
            GameEvents.onGUILaunchScreenVesselSelected.Remove(onVesselSelected);
        }
    }
   
    public static class CommonLogic
    {
        private static readonly string TAG = "CommonLogic";

        // If this value is true, print debug messages
        private static bool debug = true;

        // Conditionally prints a debug message.
        public static void DebugMessage(string tag, string message)
        {
            if (debug)
            {
                UnityEngine.Debug.Log("RealRoster: " + tag + ": " + message);
            }
        }

        public static void getDefaultManifest() 
        {
            VesselCrewManifest vcm = CMAssignmentDialog.Instance.GetManifest();
            CommonLogic.DebugMessage(TAG, "getDefaultManifest()");
            foreach (PartCrewManifest pcm in vcm)
            {
                // Vital bits about the part.
                int capacity = pcm.PartInfo.partPrefab.CrewCapacity;

                if (capacity > 0 && pcm.GetPartCrew()[capacity - 1] != null)
                {
                    RealRosterSettings.ActiveCSM.fillPartCrewManifest(pcm);
                }
            }
            CMAssignmentDialog.Instance.RefreshCrewLists(vcm, false, true);
        }
    }
}