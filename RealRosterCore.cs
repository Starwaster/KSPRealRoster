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

        // This structure holds an association between Parts and their Crew configurations which the user has written
        Dictionary<Part, ProtoCrewMember[]> PartCrewDictionary = new Dictionary<Part, ProtoCrewMember[]>();

        // This value should be set to true when the user has modified a part, maybe!
        private bool dirtyFame = false;

        private int framecounter = 1;

        void Awake()
        {
            CommonLogic.DebugMessage(TAG, "Awake()");
            GameEvents.onEditorShipModified.Add(onEditorShipModified);
        }

        void Update()
        {            
            // User is modifying the crew! Watch for crew changes and record them.
            // Unfortunately, there is no event that we can hook here, so we get do perform costly operations
            // nearly every frame.
            if (EditorLogic.fetch.editorScreen == EditorLogic.EditorScreen.Crew)
            {
                if (++framecounter % 10 == 0)
                {
                    framecounter = 1;
                    updatePartCrewDictionary();
                }
            }
            // User is modifying the parts! Watch for part changes and record them.
            else if (EditorLogic.fetch.editorScreen == EditorLogic.EditorScreen.Parts && dirtyFame)
            {
                CommonLogic.DebugMessage(TAG, "On EditorScreen, dealing with a dirtyFrame");
                dirtyFame = !dirtyFame;

                if (ShipConstruction.ShipManifest != null)
                {
                    cleanVesselManifest();
                }

                if (EditorLogic.fetch.CountAllSceneParts(true) == 0)
                {
                    ShipConstruction.ShipManifest = null;
                }
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
            if (ShipConstruction.ShipManifest != null)
            {
                dirtyFame = !dirtyFame;
                updatePartCrewDictionary();
            }
        }

        void updatePartCrewDictionary()
        {
            EditorLogic editor = EditorLogic.fetch;
            VesselCrewManifest manifest = CMAssignmentDialog.Instance.GetManifest();

            // Scan for crewable parts not yet in our dictionary
            for (int i = 0; i < editor.ship.Count; i++)
            {
                Part current = editor.ship[i];
                if (current.CrewCapacity > 0 && !PartCrewDictionary.Keys.Contains(current))
                {
                    CommonLogic.DebugMessage(TAG,"Adding to dictionary, part: " + current.partInfo.title + " manifest: " + manifest[i].PartInfo.title);
                    PartCrewDictionary.Add(current, manifest[i].GetPartCrew());
                }
            }

            // Look for parts with updated crews
            for (int i = 0; i < editor.ship.Count; i++)
            {
                Part shipPart = editor.ship[i];
                if (PartCrewDictionary.ContainsKey(shipPart))
                {
                    CommonLogic.DebugMessage(TAG,"Updating part in dictionary: " + manifest[i].PartInfo.title);
                    PartCrewDictionary[shipPart] = manifest[i].GetPartCrew();
                }
            }

            // Remove parts which don't exist in the scene anymore.
            // the Part key has gone null. THIS IS WEIRD
            PartCrewDictionary = (from kv in PartCrewDictionary
                                      where kv.Key != null
                                      select kv).ToDictionary(kv => kv.Key, kv => kv.Value);

            ShipConstruction.ShipManifest = manifest;
        }

        void cleanVesselManifest()
        {
            CommonLogic.DebugMessage(TAG, "Cleaning vessel manifest");
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

            // Place correct Crew back
            List<ProtoCrewMember> addedList = new List<ProtoCrewMember>();

            foreach (Part pod in PartCrewDictionary.Keys)
            {
                // We can't touch pods which are not attached
                if (EditorLogic.fetch.ship.Contains(pod))
                {
                    ProtoCrewMember[] group = PartCrewDictionary[pod];

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
                    crewPartWithAssignment(pod, group);
                }
            }

            CMAssignmentDialog.Instance.RefreshCrewLists(manifest, true, true);
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