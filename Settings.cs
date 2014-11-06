using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RealRoster
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] { GameScenes.EDITOR, GameScenes.SPACECENTER })]
    class SettingsModule : ScenarioModule
    {
        private static readonly string RESOURCE_PATH = "Enneract/RealRoster/Resources/";
        private static readonly string TAG = "SettingsModule";
        
        void Awake()
        {
            CommonLogic.DebugMessage(TAG, "Awake...");
        }

        void Start()
        {
            CommonLogic.DebugMessage(TAG, "Start...");
            if (ToolbarManager.ToolbarAvailable)
            {
                CommonLogic.DebugMessage(TAG, "Toolbar is available.");
            }
        }

        private void mainGUI(int windowID)
        {
            // Allow Custom Crewing
            CommonLogic.DebugMessage(TAG, "Generating Toggle 'Custom Crewing'");
            AllowCustomCrewing = GUILayout.Toggle(AllowCustomCrewing, "Custom Crewing", _toggleStyle);

            // Selection Mode Label
            CommonLogic.DebugMessage(TAG, "Generating Label 'Custom Crew Selection Mode'");
            GUILayout.Label("Custom Crew Selection Mode", _labelStyle);
            CommonLogic.DebugMessage(TAG, "Generating Text for 'Custom Crew Selection Mode'");
            foreach (var item in CrewSelectionModeLoader.Instance.LoadedModes.Select((value, i) => new { i, value }))
            {
                if (modeText[item.i].Equals(SelectionModeName, StringComparison.Ordinal))
                {
                    selectedMode = item.i; 
                }
            }

            CommonLogic.DebugMessage(TAG, "Generating SelectionGrid 'Custom Crew Selection Mode'");
            selectedMode = GUILayout.SelectionGrid(selectedMode, modeText, 1);

            // Blacklist Label
            CommonLogic.DebugMessage(TAG, "Generating Label 'BlackList'");
            GUILayout.Label("Blacklist: (Click to Remove)", _labelStyle);

            // Build list of crew currently on blacklist.
            CommonLogic.DebugMessage(TAG, "Generating Buttons 'BlackList'");
            foreach (String kerbal in BlackList)
            {
                if (GUILayout.Button(kerbal))
                {
                    BlackList.Remove(kerbal);
                }
            }

            // Iterate through all Kerbals (including those not on a mission).
            List<ProtoCrewMember> roster = HighLogic.CurrentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Crew, ProtoCrewMember.RosterStatus.Available).ToList();

            // Crew pool label
            GUILayout.Label("Crew: (Click to add to Blacklist)", _labelStyle);

            CommonLogic.DebugMessage(TAG, "Generating Buttons 'Roster'");
            foreach (ProtoCrewMember kerbal in roster)
            {
                if (!BlackList.Contains(kerbal.name))
                {
                    if (GUILayout.Button(kerbal.name))
                    {
                        BlackList.Add(kerbal.name);
                    }
                }
            }

            GUI.DragWindow();
        }

        private void drawGUI()
        {
            if (settingWindowActive)
            {
                windowPos = GUILayout.Window(0, windowPos, mainGUI, "RealRoster Settings", _windowStyle);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            
            base.OnSave(node);

            ConfigNode blackListNode = new ConfigNode("BLACKLIST");
            foreach (string kerbal in BlackList)
            {
                blackListNode.AddValue("kerbal", kerbal);
            }

            node.AddNode(blackListNode);
        }

        public override void OnLoad(ConfigNode node)
        {
            
            AllowCustomCrewing = true;
            SelectionModeName = "No Crew";
            BlackList = new List<string>();

            base.OnLoad(node);

            if (node.HasNode("BLACKLIST"))
            {
                ConfigNode blacklistNode = node.GetNode("BLACKLIST");
                foreach (string kerbal in blacklistNode.GetValues("kerbal"))
                {
                    BlackList.Add(kerbal);
                }
            }
        }
    }
}
