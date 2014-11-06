using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RealRoster
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] { GameScenes.EDITOR, GameScenes.SPACECENTER })]
    class Settings : ScenarioModule
    {
        public static readonly String RESOURCE_PATH = "Enneract/RealRoster/Resources/";
        public static readonly string TAG = "Settings";

        public bool settingWindowActive;

        protected Rect windowPos;
        protected int selectedMode;
        
        private IButton button = null;
        private GUIStyle _windowStyle, _labelStyle, _buttonStyle, _toggleStyle, _scrollStyle, _hscrollBarStyle, _vscrollBarStyle, _thumbStyle;
        private string[] modeText;

        [KSPField(isPersistant = true)]
        public bool AllowCustomCrewing;
        [KSPField(isPersistant = true)]
        public string SelectionModeName;

        private List<string> BlackList;
        
        public void Awake()
        {
            windowPos = new Rect(Screen.width / 4, Screen.height / 4, 10f, 10f);
            selectedMode = 0;

            _windowStyle = new GUIStyle(HighLogic.Skin.window);
            _windowStyle.fixedWidth = 250f;
            _windowStyle.fixedHeight = 400f;

            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _labelStyle.stretchWidth = true;

            _buttonStyle = new GUIStyle(HighLogic.Skin.button);
            _toggleStyle = new GUIStyle(HighLogic.Skin.toggle);

            _scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);
            _vscrollBarStyle = new GUIStyle(HighLogic.Skin.verticalScrollbar);
            _hscrollBarStyle = new GUIStyle(HighLogic.Skin.horizontalScrollbar);
            _thumbStyle = new GUIStyle(HighLogic.Skin.verticalScrollbarThumb);

            RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI));
        }

        public void Update()
        {
            if (button == null)
            {
                String iconOff = RESOURCE_PATH + "IconOff";
                button = ToolbarManager.Instance.add("RealRoster", "button");
                if (button != null)
                {
                    button.TexturePath = iconOff;
                    button.ToolTip = "RealRoster Settings";
                    button.OnClick += (e) =>
                    {
                        settingWindowActive = !settingWindowActive;
                    };

                    modeText = new string[CrewSelectionModeLoader.Instance.LoadedModes.Count];

                    button.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.SPACECENTER);
                }
            }
        }

        private void mainGUI(int windowID)
        {
            // Allow Custom Crewing
            AllowCustomCrewing = GUILayout.Toggle(AllowCustomCrewing, "Custom Crewing", _toggleStyle);

            // Selection Mode Label
            GUILayout.Label("Custom Crew Selection Mode", _labelStyle);
            foreach (var item in CrewSelectionModeLoader.Instance.LoadedModes.Select((value, i) => new { i, value }))
            {
                modeText[item.i] = item.value.CleanName;
                if (modeText[item.i].Equals(SelectionModeName, StringComparison.Ordinal))
                {
                    selectedMode = item.i; 
                }
            }

            selectedMode = GUILayout.SelectionGrid(selectedMode, modeText, 1);

            // Blacklist Label
            GUILayout.Label("Blacklist: (Click to Remove)", _labelStyle);

            // Build list of crew currently on blacklist.
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

            DebugMessage("Generating Roster");
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
            DebugMessage("RealRosterSettings: Saving Configuration...");
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
            DebugMessage("RealRosterSettings: Loading Configuration...");

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


        // If this value is true, print debug messages
        private static bool debug = (true && CommonLogic.globalDebug);

        // Conditionally prints a debug message.
        public static void DebugMessage(string message)
        {
            if (debug)
            {
                UnityEngine.Debug.Log("RealRoster: " + TAG + ": " + message);
            }
        }
    }
}
