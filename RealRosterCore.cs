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

        private Part firstPod = null;
        private bool firstPodFlag = false;

        void Awake()
        {
            CommonLogic.DebugMessage(TAG, "Awake()");
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
            CommonLogic.DebugMessage(TAG, "Destroy()");
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
        }

        void onEditorShipModified(ShipConstruct ship)
        {
            CommonLogic.DebugMessage(TAG, "OnEditorShipModified()");
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

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class PersistenceBehaviour : MonoBehaviour
    {
        private static readonly string TAG = "PersistenceBehaviour";

        // This value should be set to true when the user has modified a part, maybe!
        private bool dirtyFame = false;

        void Awake()
        {
            CommonLogic.DebugMessage(TAG, "Awake()");
            GameEvents.onEditorShipModified.Add(onEditorShipModified);
        }

        void Update()
        {            
            // User is modifying the crew! Watch for crew changes and record them.
            if (EditorLogic.fetch.editorScreen == EditorLogic.EditorScreen.Crew)
            {
                
            }
            // User is modifying the parts! Watch for part changes and record them.
            else if (EditorLogic.fetch.editorScreen == EditorLogic.EditorScreen.Parts && dirtyFame)
            {
                dirtyFame = !dirtyFame;
                
                // *TODO* Probably remove this for .90
                if (EditorLogic.fetch.CountAllSceneParts(true) == 0) 
                {
                    //ShipConstruction.ShipManifest = null;
                }
                
                cleanVesselManifest();
            }
        }

        void Destroy()
        {
            CommonLogic.DebugMessage(TAG, "Destroy()");
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
        }

        void onEditorShipModified(ShipConstruct ship)
        {
            CommonLogic.DebugMessage(TAG, "OnEditorShipModified()");          
            // This should prevent persistence module from stepping on 'allowed' crewing. 
            // *TODO* Probably remove this for .90
            if (CMAssignmentDialog.Instance.GetManifest() != null)
            {
                dirtyFame = !dirtyFame;
            }
            else
            {
                
            }
        }

        void cleanVesselManifest()
        {
            VesselCrewManifest manifest = CMAssignmentDialog.Instance.GetManifest();

            // Remove all currently assigned crew, they are incorrect
            foreach (PartCrewManifest pcm in manifest)
            {
                if (pcm.PartInfo.partPrefab.CrewCapacity > 0)
                {
                    ProtoCrewMember [] crew = pcm.GetPartCrew();
                    for (int i = 0; i < crew.Length; i++)
                    {
                        pcm.RemoveCrewFromSeat(i);
                    }
                }
            }

            CMAssignmentDialog.Instance.RefreshCrewLists(manifest, true, true);
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

            foreach (ProtoCrewMember k in RealRosterSettings.Instance.WhiteList)
            {
                CommonLogic.DebugMessage(TAG, "k: " + k.name);
            }

            foreach (PartCrewManifest pcm in vcm)
            {
                CommonLogic.DebugMessage(TAG, "Inspecting PCM for Part: " + pcm.PartInfo.partPrefab.partName);
                // Vital bits about the part.
                int capacity = pcm.PartInfo.partPrefab.CrewCapacity;

                if (capacity > 0)
                {
                    RealRosterSettings.ActiveCSM.fillPartCrewManifest(pcm);
                    break;
                }
            }
        }
    }
}