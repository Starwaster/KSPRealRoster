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
        
        // These flags control actions to be taken on the update following editor modification.
        private bool firstPodFlag = false;
        private bool dirtyFrame = false;
        private bool partsUpdated = false;

        // This holds a reference to the first pod placed. This one gets automatically crewed, and this value should go null when it is removed.
        // (Thanks Unity!)
        private Part firstPod = null;

        // This structure holds an association between Parts and their Crew configurations which the user has written
        private Dictionary<Part , ProtoCrewMember[]> PartCrewDictionary = new Dictionary<Part, ProtoCrewMember[]>();

        // Limits on the number of times we spam expensive operations
        private int framecounter = 0;
        private static readonly int FRAME_INTERVAL = 20;

        // This happens when the behaviour is loaded.
        void Awake()
        {
            CommonLogic.DebugMessage(TAG, "Awake()");
            GameEvents.onEditorShipModified.Add(onEditorShipModified);
        }

        // This triggers every frame.
        void Update()
        {
            if (firstPodFlag)
            {
                CommonLogic.DebugMessage(TAG, "Update()");
                CommonLogic.getDefaultManifest();
                firstPodFlag = false;
                updatePartsDictionary();
            }
            else if (EditorLogic.fetch.editorScreen == EditorLogic.EditorScreen.Parts && dirtyFrame)
            {
                dirtyFrame = !dirtyFrame;
                fixManifest();
                partsUpdated = false;
            }
            else if (EditorLogic.fetch.editorScreen == EditorLogic.EditorScreen.Crew && !partsUpdated)
            {
                updatePartsDictionary();
                partsUpdated = true;
            } 
            else if (EditorLogic.fetch.editorScreen == EditorLogic.EditorScreen.Crew)
            {
                updateCrewDictionary();
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
            else
            {
                dirtyFrame = !dirtyFrame;
            }            
        }

        void fixManifest()
        {
            CommonLogic.DebugMessage(TAG, "fixManifest()");
            foreach (PartCrewManifest pcm in CMAssignmentDialog.Instance.GetManifest())
            {
                if (pcm.PartInfo.partPrefab.CrewCapacity > 0)
                {
                    ProtoCrewMember[] crewArray = pcm.GetPartCrew();
                    for (int i = 0; i < crewArray.Length; i++)
                    {
                        if (crewArray[i] != null)
                        {
                            pcm.RemoveCrewFromSeat(i);
                        }
                    }
                }
            }

            // Place correct Crew back
            List<ProtoCrewMember> addedList = new List<ProtoCrewMember>();

            foreach (Part part in PartCrewDictionary.Keys)
            {
                // We can't touch pods which are not attached
                if (EditorLogic.fetch.ship.Contains(part))
                {
                    ProtoCrewMember[] group = PartCrewDictionary[part];

                    //Make sure that this Kerbal isn't being cloned
                    for (int idx = 0; idx < group.Length; idx++)
                    {
                        if (addedList.Contains(group[idx]))
                        {
                            group[idx] = null;
                        }
                        else
                        {
                            // Add this kerb to our list of people who already have a seat
                            addedList.Add(group[idx]);
                        }
                    }
                    // Perform the crewing.
                    crewPartWithAssignment(part, group);
                }
            }
        }

        // Returns the PartCrewManifest that is owned by the specified Part.
        // If the Part is not attached to the ShipConstruct, null will be returned.
        // **REFACTOR ME?**
        PartCrewManifest findManifestByPart(Part part)
        {
            ShipConstruct ship = EditorLogic.fetch.ship;
            for (int idx = 0; idx < ship.Count; idx++)
            {
                if (part == ship[idx])
                {
                    return ShipConstruction.ShipManifest[idx];
                }
            }
            return null;
        }

        // Fills the part's crew capacity with the specified crew array.
        void crewPartWithAssignment(Part part, ProtoCrewMember[] crew)
        {
            PartCrewManifest manifest = findManifestByPart(part);

            int limit = (part.CrewCapacity < crew.Length) ? part.CrewCapacity : crew.Length;
            for (int idx = 0; idx < limit; idx++)
            {
                if (crew[idx] != null)
                {
                    manifest.AddCrewToSeat(crew[idx], idx);
                }
            }
        }      

        /* This method should do the initial population of Parts and Crews in PartCrewDictionary.
         * It should only be called once per time the user switches to the Crew tab, as it is
         * relatively expensive. */
        void updatePartsDictionary()
        {
            CommonLogic.DebugMessage(TAG, "updatePartsDictionary()");
            // Remove parts which don't exist in the scene anymore.
            // This really shouldn't work at all, because Dictionaries
            // arent's supposed to be able to have null keys.
            // But they do! (Thanks, Unity!)
            PartCrewDictionary = (from k in PartCrewDictionary where k.Key != null select k).ToDictionary(k => k.Key, k => k.Value);

            // Scan for crewable Parts not yet in the Dictionary 
            foreach (var part in EditorLogic.fetch.ship.Select((value, index) => new {index, value }))
            {
                if (part.value.CrewCapacity > 0 && !PartCrewDictionary.Keys.Contains(part.value))
                {                    
                    PartCrewDictionary.Add(part.value, CMAssignmentDialog.Instance.GetManifest()[part.index].GetPartCrew());
                }
            }
        }

        /* This method should update the Values (Crew) in the PartCrewDictionary. It does not look for new
         * Parts, which should not be added or removed during the course of the session in the Crew tab. */
        void updateCrewDictionary()
        {
            CommonLogic.DebugMessage(TAG, "updateCrewDictionary()");
            // Scan for crewable parts not yet in our dictionary.
            foreach (var part in EditorLogic.fetch.ship.Select((value, index) => new {index, value}))
            {                
                if (PartCrewDictionary.Keys.Contains(part.value))
                {                    
                    PartCrewDictionary[part.value] = CMAssignmentDialog.Instance.GetManifest()[part.index].GetPartCrew();
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
            CMAssignmentDialog.Instance.RefreshCrewLists(vcm, true, true);
        }
    }
}