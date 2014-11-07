using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RealRoster
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class EditorBehaviour : MonoBehaviour
    {
        private static readonly string TAG = "EditorBehaviour";       
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
            CMAssignmentDialog dialog = CMAssignmentDialog.Instance;
            VesselCrewManifest vcm = dialog.GetManifest();

            CommonLogic.getDefaultManifest(vcm);

            dialog.RefreshCrewLists(vcm, false, true);
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

        public static void getDefaultManifest(VesselCrewManifest sourceVCM) 
        {
            CommonLogic.DebugMessage(TAG, "getDefaultManifest()");
            foreach (PartCrewManifest pcm in sourceVCM)
            {
                // Vital bits about the part.
                int capacity = pcm.PartInfo.partPrefab.CrewCapacity;

                if (capacity > 0 && pcm.GetPartCrew()[capacity - 1] != null)
                {
                    RealRosterSettings.ActiveCSM.fillPartCrewManifest(pcm);
                }
            }
        }
    }
}