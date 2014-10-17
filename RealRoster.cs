using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using Toolbar;

namespace RealRoster
{
    // Toolbar should be available via KSC, hence the 'everyscene'
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class RealRoster : MonoBehaviour
    {

        // If this value is true, print debug messages
        bool debug = false;

        // This structure holds an association between Parts and their Crew configurations which the user has written
        Dictionary<Part, ProtoCrewMember[]> assignedCrewDictionary = new Dictionary<Part, ProtoCrewMember[]>();

        // This value indicates that the VesselCrewManifest has been regenerated, and action should
        // be taken next frame. Returns false if action does not need to be taken next frame.
        bool dirtyFlag;

        // This value indicates that the first pod eligible for auto-crewing has been placed, and action should
        // be taken next frame. Returns false if action has been taken or no pod has been placed.
        bool firstPodFlag;

        // Stores the reference to the first auto-crew pod. If this value is null, it has not been placed.
        Part firstPod = null;

        // Reference to singleton Settings class (Thanks Vendan!)
        Settings rrSettings = Settings.Instance;

        // Unity initialization call
        public void Awake()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {

                if (debug)
                {
                    DebugMessage("Plugin is initializing");
                    DebugMessage("crewAssignment: " + rrSettings.crewAssignment);
                    DebugMessage("crewRandomization: " + rrSettings.crewRandomization);

                    foreach (String name in rrSettings.blackList)
                    {
                        DebugMessage("BlackList: " + name);
                    }
                }
            }
        }

        public void Start()
        {
                rrGUI rrGUI = new rrGUI();
                rrGUI.init();
            //GameEvents.onEditorShipModified.Add(onEditorShipModified);
        }

        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                rrSettings.eventRegistered = false;
                DebugMessage("dESTROYYYYYY!!!");
                GameEvents.onEditorShipModified.Remove(onEditorShipModified);
            }
        }

        // Occurs on every frame
        public void Update()
        {

            if (HighLogic.LoadedSceneIsEditor)
            {
                if (!rrSettings.eventRegistered)
                {
                DebugMessage("EVENT REGISTERED");
                    GameEvents.onEditorShipModified.Add(onEditorShipModified);
                    rrSettings.eventRegistered = true;

                }

                // Kinda kludgey, but this is where the game stores the active Manifest. It isn't always valid, and our code assumes it is valid.
                // This might lose us a few update frames, but I couldn't find any bug introduced from it.
                if (ShipConstruction.ShipManifest != null)
                {
                    // If action needs to be taken because a pod has been placed for the first time.
                    // No reason to execute the other stages here, though!
                    if (firstPodFlag)
                    {
                        cleanUpManifest();
                        onFirstPod();
                        syncDictionary();
                    }
                    // If an editor update has happened, we need to clean up the mess
                    else if (dirtyFlag)
                    {
                        cleanUpManifest();
                    }
                    // Otherwise, just record the user input
                    else if (EditorLogic.fetch.editorScreen == EditorLogic.EditorScreen.Crew)
                    {
                        syncDictionary();
                    }
                }
            }
        }

        // Occurs every time any part is 'clicked' in the editor.
        void onEditorShipModified(ShipConstruct ship)
        {
            DebugMessage("EVENT FIRED!");
            // The main reason we hook this event is to notice that it happened. 
            // This flag primes our Update() method to fix the roster in the next frame.
            dirtyFlag = true;

            // If the ShipConstruct has parts (IE, has a root part been placed?)
            if (ship.Count > 0)
            {
                if (firstPod == null)
                {
                    // The natural ordering is what is important. Natural ordering is roughly 'distance from root', and is what
                    // most of the base game uses for locating parts. 
                    foreach (Part part in ship)
                    {
                        // We want a part that crew can control the ship from.
                        if (part.isControlSource && part.CrewCapacity > 0)
                        {
                            firstPod = part;
                            firstPodFlag = true;
                            DebugMessage("First Pod Placed!: " + firstPod.partInfo.title);
                            break; // We are not interested in the remainder of the iteration.
                        }
                    }
                }
            }
            // Otherwise, the root part was just deleted. This 'event' might be useful in the future, but currently this is dead code.
            else
            {
                DebugMessage("Root part has been deleted");
                onRootDeletion();
            }
        }

        // Occurs when the first pod eligible for crewing is placed.
        void onFirstPod()
        {
            // We are taking action, disable the action flag.
            firstPodFlag = false;
            // If the configuration plugin allows us to crew the first pod...
            if (rrSettings.crewAssignment)
            {
                EditorLogic editor = EditorLogic.fetch;
                VesselCrewManifest nextManifest = ShipConstruction.ShipManifest;

                crewPartWithAssignment(firstPod, getCrewForPod(firstPod.CrewCapacity));

                CMAssignmentDialog.Instance.RefreshCrewLists(nextManifest, false, true);
            }
        }

        // Occurs when the root part is deleted.
        // Possibly need to implement some reset code in here, but does not seem needed at this time.
        // Leaving code here in case it is needed, as finding the detection criteria was annoying.
        void onRootDeletion()
        {
            //ShipConstruction.ShipManifest = null;
        }

        // Returns the PartCrewManifest that is owned by the specified Part.
        // If the Part is not attached to the ShipConstruct, null will be returned.
        // **REFACTOR EXISTING CODE?**
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

        // Generates a random crew for the pod, based on settings.
        ProtoCrewMember[] getCrewForPod(int capacity)
        {
            ProtoCrewMember[] crew = new ProtoCrewMember[capacity];
            List<ProtoCrewMember> roster = HighLogic.CurrentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Crew, ProtoCrewMember.RosterStatus.Available).ToList();

            foreach (ProtoCrewMember kerbal in roster.ToList())
            {
                if (rrSettings.blackList.Contains(kerbal.name))
                {
                    roster.Remove(kerbal);
                }
            }

            while (roster.Count < capacity)
            {
                ProtoCrewMember newKerb = HighLogic.CurrentGame.CrewRoster.GetNewKerbal();
                DebugMessage(newKerb.name + " has been hired.");
                roster.Add(newKerb);
            }

            for (int idx = 0; idx < capacity; idx++)
            {
                if (rrSettings.crewRandomization)
                {
                    crew[idx] = roster[UnityEngine.Random.Range(0, (roster.Count - 1))];
                }
                else
                {
                    crew[idx] = roster.First();
                }
                roster.Remove(crew[idx]);
            }

            return crew;
        }

        // Deletes all crew from manifests, and restores saved crew configurations.
        // This occurs one frame after any editor update. It should be impossible for any
        // changes to be made by the user between such an update and this method occuring.
        void cleanUpManifest()
        {
            DebugMessage("Cleaning Up Vessel Manifest...");

            EditorLogic editor = EditorLogic.fetch;
            VesselCrewManifest nextManifest = ShipConstruction.ShipManifest;

            foreach (PartCrewManifest nextCrewManifest in nextManifest)
            {
                if (nextCrewManifest.PartInfo.partPrefab.CrewCapacity > 0)
                {
                    DebugMessage("Cleaning Up Part Manifest for: " + nextCrewManifest.PartInfo.title);
                    ProtoCrewMember[] crewArray = nextCrewManifest.GetPartCrew();
                    for (int i = 0; i < crewArray.Length; i++)
                    {
                        if (crewArray[i] != null)
                        {
                            nextCrewManifest.RemoveCrewFromSeat(i);
                        }
                    }
                }
            }

            // Place correct Crew back
            List<ProtoCrewMember> addedList = new List<ProtoCrewMember>();

            foreach (Part pod in assignedCrewDictionary.Keys)
            {
                // We can't touch pods which are not attached
                if (editor.ship.Contains(pod))
                {
                    ProtoCrewMember[] group = assignedCrewDictionary[pod];

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

            // reset dirty flag
            dirtyFlag = false;
            CMAssignmentDialog.Instance.RefreshCrewLists(nextManifest, false, true);
        }

        // This occurs on frames when there was no editor update.
        // There is no way to detect when a Kerbal is actually added to the roster manually, afaik.
        // **HIGHLY INEFFICENT, LOOK HERE FOR PERFORMANCE PROBLEMS**
        void syncDictionary()
        {
            EditorLogic editor = EditorLogic.fetch;
            VesselCrewManifest nextManifest = CMAssignmentDialog.Instance.GetManifest();

            // Scan for crewable parts not yet in our dictionary
            for (int i = 0; i < editor.ship.Count; i++)
            {
                Part current = editor.ship[i];
                if (current.CrewCapacity > 0 && !assignedCrewDictionary.Keys.Contains(current))
                {
                    //DebugMessage("Adding to dictionary, part: " + current.partInfo.title + " manifest: " + nextManifest[i].PartInfo.title);
                    assignedCrewDictionary.Add(current, nextManifest[i].GetPartCrew());
                }
            }

            // Look for parts with updated crews
            for (int i = 0; i < editor.ship.Count; i++)
            {
                Part shipPart = editor.ship[i];
                if (assignedCrewDictionary.ContainsKey(shipPart))
                {
                    //DebugMessage("Updating part in dictionary: " + nextManifest[i].PartInfo.title);
                    assignedCrewDictionary[shipPart] = nextManifest[i].GetPartCrew();
                }
            }

            // Remove parts which don't exist in the scene anymore.
            // the Part key has gone null. THIS IS WEIRD
            assignedCrewDictionary = (from kv in assignedCrewDictionary
                                      where kv.Key != null
                                      select kv).ToDictionary(kv => kv.Key, kv => kv.Value);

            ShipConstruction.ShipManifest = nextManifest;
        }

        // Conditionally prints a debug message.
        void DebugMessage(string message)
        {
            if (debug)
                print("RealRoster: " + message);
        }
    }
}