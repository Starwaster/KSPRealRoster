using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RealRoster
{
    // This gets loaded after KSPAddons are, so this has more or less become the 'central' module of the mod.
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] { GameScenes.EDITOR, GameScenes.SPH, GameScenes.SPACECENTER })]
    public class RealRosterSettings : ScenarioModule
    {
        // Statics
        private static readonly string RESOURCE_PATH = "Enneract/RealRoster/Resources/";
        private static readonly string ICON_ON = RESOURCE_PATH + "IconOn";
        private static readonly string ICON_OFF = RESOURCE_PATH + "IconOff";
        private static readonly string TAG = "SettingsModule";
        private static readonly string CAPTION = "RealRoster";

        // Misc
        public List<ProtoCrewMember> FullRoster
        {
            get
            {
                return HighLogic.CurrentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Crew, ProtoCrewMember.RosterStatus.Available).ToList();
            }
        }

        public List<ProtoCrewMember> WhiteList
        {
            get
            {
                return FullRoster.Except(BlackList).ToList();
            }
        }

        public List<ProtoCrewMember> BlackList
        { 
            get
            {
                return FullRoster.Where(kerb => privateBlackList.Contains(kerb.name)).ToList();
            }
        }

        private List<string> privateBlackList = new List<string>();

        public static RealRosterSettings Instance = null;

        //Automatic Persistent Fields

        // This section syncronizes a string value which gets written to persistence the numeric index of that mode.
        [KSPField(isPersistant = true)]
        public string CrewSelectionMode = CrewSelectionModeLoader.Instance.LoadedModes.FirstOrDefault().CleanName;

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

        internal static ICrewSelectionMode ActiveCSM
        {
            get
            {
                return CrewSelectionModeLoader.Instance.LoadedModes[Instance.CrewSelectionModeIndex];
            }
        }

        //GUI Fields
        private IButton SettingsButton;
        private bool SettingsWindowVisible;
        private GUIStyle _windowStyle, _labelStyle, _buttonStyle, _scrollStyle;
        protected Rect WindowPosition = new Rect((Screen.width / 4)*3, Screen.height / 10, 10f, 10f);
        protected string[] ModeTextArray;
        public Vector2 RosterScrollPosition, BlackListScrollPosition;

        void Start()
        {
            CommonLogic.DebugMessage(TAG, "Start...");

            foreach (ProtoCrewMember kerb in BlackList)
            {
                CommonLogic.DebugMessage(TAG, kerb.name);
            }
            Instance = this;

            ModeTextArray = new string[CrewSelectionModeLoader.Instance.LoadedModes.Count];
            foreach (var mode in CrewSelectionModeLoader.Instance.LoadedModes.Select((value, i) => new { i, value }))
            {
                ModeTextArray[mode.i] = mode.value.CleanName;
            }

            if (ToolbarManager.ToolbarAvailable)
            {
                CommonLogic.DebugMessage(TAG, "Toolbar is available.");

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
            _windowStyle = new GUIStyle(HighLogic.Skin.window);
            _windowStyle.fixedWidth = 250f;

            if (SettingsWindowVisible)
                WindowPosition = GUILayout.Window(9001, WindowPosition, drawWindow, "RealRoster Settings", _windowStyle);
        }

        void drawWindow(int WindowID)
        {
            GUI.skin = HighLogic.Skin;

            if (SettingsWindowVisible)
            {
                if (WindowPosition.Contains(Input.mousePosition) && InputLockManager.GetControlLock(CAPTION) == ControlTypes.None)
                {
                    InputLockManager.SetControlLock(ControlTypes.All, CAPTION);
                }
                else if (!WindowPosition.Contains(Input.mousePosition) && InputLockManager.GetControlLock(CAPTION) != ControlTypes.None)
                {
                    InputLockManager.RemoveControlLock(CAPTION);
                }
            }

            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.stretchWidth = true;

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.padding = new RectOffset(3, 3, 3, 3);

            _scrollStyle = new GUIStyle(GUI.skin.scrollView);

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            GUILayout.Label("Automatic Crew Mode", _labelStyle);
            GUILayout.EndHorizontal();

            CrewSelectionModeIndex = GUILayout.SelectionGrid(CrewSelectionModeIndex, ModeTextArray, 1, _buttonStyle);

            GUILayout.Label("Crew: (Click to add to Blacklist)", _labelStyle);
            RosterScrollPosition = GUILayout.BeginScrollView(RosterScrollPosition, GUILayout.ExpandWidth(true), GUILayout.Height(200));
            foreach (ProtoCrewMember kerbal in WhiteList)
            {
                if (GUILayout.Button(kerbal.name, _buttonStyle))
                {
                    privateBlackList.Add(kerbal.name);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Label("Blacklist: (Click to Remove)", _labelStyle);
            BlackListScrollPosition = GUILayout.BeginScrollView(BlackListScrollPosition, GUILayout.ExpandWidth(true), GUILayout.Height(200));
            foreach (ProtoCrewMember kerbal in BlackList)
            {
                if (GUILayout.Button(kerbal.name, _buttonStyle))
                {
                    privateBlackList.Remove(kerbal.name);
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
            foreach (ProtoCrewMember kerb in BlackList)
            {
                CommonLogic.DebugMessage(TAG, "Writing '" + kerb.name + "' to blacklist node");
                blacklistNode.AddValue("kerbal", kerb.name);
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
