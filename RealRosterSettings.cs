using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RealRoster
{
    // This gets loaded after KSPAddons are
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] { GameScenes.EDITOR, GameScenes.SPH, GameScenes.SPACECENTER })]
    public class RealRosterSettings : ScenarioModule
    {
        // Statics
        private static readonly string RESOURCE_PATH = "Enneract/RealRoster/Resources/";
        private static readonly string ICON_ON = RESOURCE_PATH + "IconOn";
        private static readonly string ICON_OFF = RESOURCE_PATH + "IconOff";
        private static readonly string TAG = "SettingsModule";
        private static readonly string CAPTION = "RealRoster";

        //Automatic Persistent Fields
        [KSPField(isPersistant = true)]
        public string CrewSelectionMode = CrewSelectionModeLoader.Instance.LoadedModes.FirstOrDefault().CleanName;

        //Manual Persistent Fields
        public List<string> BlackList { get { return new List<string>(privateBlackList); } }
        private List<string> privateBlackList = new List<string>();

        // Returns the index of the active CSM
        private int CrewSelectionModeIndex
        {
            get
            {
                return Array.IndexOf(ModeTextArray, CrewSelectionMode);
            }
            set
            {
                CrewSelectionMode = ModeTextArray[value];
            }
        }

        // Returns the reference to an instance of the selected CrewSelectionMode
        ICrewSelectionMode ActiveCSM
        {
            get
            {
                return CrewSelectionModeLoader.Instance.LoadedModes[CrewSelectionModeIndex];
            }
        }

        //GUI Fields
        private IButton SettingsButton;
        private bool SettingsWindowVisible;
        private GUIStyle _windowStyle, _labelStyle, _buttonStyle, _toggleStyle, _scrollStyle, _hscrollBarStyle, _vscrollBarStyle;
        protected Rect WindowPosition = new Rect(Screen.width / 4, Screen.height / 4, 10f, 10f);
        protected string[] ModeTextArray;
        public Vector2 RosterScrollPosition, BlackListScrollPosition;

        void Start()
        {
            CommonLogic.DebugMessage(TAG, "Start...");

            ModeTextArray = new string[CrewSelectionModeLoader.Instance.LoadedModes.Count];
            foreach (var mode in CrewSelectionModeLoader.Instance.LoadedModes.Select((value, i) => new { i, value }))
            {
                ModeTextArray[mode.i] = mode.value.CleanName;
            }

            if (ToolbarManager.ToolbarAvailable)
            {
                CommonLogic.DebugMessage(TAG, "Toolbar is available.");

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

                RenderingManager.AddToPostDrawQueue(3, new Callback(guiProxy));

                // This should be the last statement of this method.
                // Do not initialize the Button until everything else is set up.
                SettingsButton = ToolbarManager.Instance.add(CAPTION, CAPTION);
                if (SettingsButton != null)
                {
                    SettingsButton.TexturePath = ICON_OFF;
                    SettingsButton.ToolTip = CAPTION;

                    SettingsButton.OnClick += (e) =>
                    {
                        SettingsButton.TexturePath = SettingsWindowVisible ? ICON_OFF : ICON_ON;
                        SettingsWindowVisible = !SettingsWindowVisible;
                    };

                    SettingsButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.SPH, GameScenes.SPACECENTER);
                }
            }
            else
            {
                CommonLogic.DebugMessage(TAG, "Toolbar is not available, disabling functionality");
            }
        }

        void guiProxy()
        {
            if (SettingsWindowVisible)
                WindowPosition = GUILayout.Window(0, WindowPosition, drawWindow, "RealRoster Settings", _windowStyle);
        }

        void drawWindow(int WindowID)
        {
            CommonLogic.DebugMessage(TAG, CrewSelectionModeIndex + " " + ModeTextArray.Length);
            CrewSelectionModeIndex = GUILayout.SelectionGrid(CrewSelectionModeIndex, ModeTextArray, 1);

            GUILayout.Label("Crew: (Click to add to Blacklist)", _labelStyle);
            RosterScrollPosition = GUILayout.BeginScrollView(RosterScrollPosition, GUILayout.ExpandWidth(true), GUILayout.Height(100));
            foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Crew, ProtoCrewMember.RosterStatus.Available).ToList())
            {
                if (!privateBlackList.Contains(kerbal.name))
                {
                    if (GUILayout.Button(kerbal.name))
                    {
                        privateBlackList.Add(kerbal.name);
                    }
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Label("Blacklist: (Click to Remove)", _labelStyle);
            BlackListScrollPosition = GUILayout.BeginScrollView(BlackListScrollPosition, GUILayout.ExpandWidth(true), GUILayout.Height(100));
            foreach (string kerbal in privateBlackList)
            {               
                if (GUILayout.Button(kerbal))
                {
                    privateBlackList.Remove(kerbal);
                }
            }
            GUILayout.EndScrollView();

            GUI.DragWindow();        
        }

        public override void OnSave(ConfigNode config)
        {
            CommonLogic.DebugMessage(TAG, "Saving...");
            base.OnSave(config);

            ConfigNode blacklistNode = new ConfigNode("BLACKLIST_NODE");
            foreach (string name in BlackList)
            {
                CommonLogic.DebugMessage(TAG, "Writing '" + name + "' to blacklist node");
                blacklistNode.AddValue("kerbal", name);
            }
            config.AddNode(blacklistNode);

            foreach (ICrewSelectionMode mode in CrewSelectionModeLoader.Instance.LoadedModes)
            {
                mode.OnSave(config);
            }
        }

        public override void OnLoad(ConfigNode config)
        {
            CommonLogic.DebugMessage(TAG, "Loading...");
            base.OnLoad(config);
            if (config.HasNode("BLACKLIST_NODE"))
            {
                ConfigNode blacklistNode = config.GetNode("BLACKLIST_NODE");
                foreach (string name in blacklistNode.GetValues("kerbal")) 
                {
                    CommonLogic.DebugMessage(TAG, "Found blacklist member - " + name);
                    if (!privateBlackList.Contains(name))
                    {
                        privateBlackList.Add(name);
                    }
                }
            }

            foreach (ICrewSelectionMode mode in CrewSelectionModeLoader.Instance.LoadedModes)
            {
                mode.OnLoad(config);
            }
        }
    }
}
