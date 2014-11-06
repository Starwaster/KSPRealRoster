using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RealRoster
{
    // Toolbar should be available via KSC, hence the 'everyscene'
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class RealRoster : MonoBehaviour
    {
        // If this value is true, print debug messages
        static bool debug = true;

        // Instance of this singleton
        public static RealRoster instance = null;

        // Reference to settings scenario
        private RealRosterSettings settings = null;

        // List of registered CrewSelectionModes
        public List<ICrewSelectionMode> modes = new List<ICrewSelectionMode>();

        public void Awake()
        {
            DebugMessage("Awake()");
            instance = this;
        }

        public void Start()
        {
            DebugMessage("Start()");

            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Add(onEditorShipModified);
                DebugMessage("Successfully registered onEditorShipModified");
            }
        }
        
        // Occurs on every frame
        public void Update()
        {
            if (RealRosterSettings.instance != null && settings == null)
            {
                settings = RealRosterSettings.instance;
                DebugMessage("Obtained reference to settings.");
            }
        }

        public void OnDestroy()
        {
            DebugMessage("OnDestroy()");
            instance = null;
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
        }

        // Occurs every time any part is 'clicked' in the editor.
        void onEditorShipModified(ShipConstruct ship)
        {
            
        }

        public void registerCrewSelectionMode(ICrewSelectionMode mode)
        {
            if (!modes.Contains(mode))
            {
                DebugMessage("Registering CrewSelectionMode: " + mode.ToString());
                modes.Add(mode);
            }
        }

        // Conditionally prints a debug message.
        public static void DebugMessage(string message)
        {
            if (debug)
            {
                print("RealRoster: " + message);
            }
        }
    }

    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] { GameScenes.EDITOR })]
    public class RealRosterSettings : ScenarioModule
    {
        // Reference to the singleton of this class
        public static RealRosterSettings instance = null;

        [KSPField(isPersistant=true)]
        public bool allowCustomCrewing = true;
        [KSPField(isPersistant = true)]
        public string selectionModeName = RealRoster.instance.modes.FirstOrDefault().CleanName;
        [KSPField(isPersistant = true)]
        public List<string> crewBlackList = new List<string>();

        // Constructor
        public void Start()
        {
            instance = this;
        }

        public void onDestroy()
        {
            instance = null;
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class RealRosterGUI : MonoBehaviour 
    {
        RealRosterSettings settings = null;

        private IButton button = null;
        public static readonly String RESOURCE_PATH = "Enneract/RealRoster/Resources/";

        Dictionary<String, bool> tempBlackList = new Dictionary<String, bool>();

        protected Rect windowPos = new Rect(Screen.width / 4, Screen.height / 4, 10f, 10f);
        public bool settingWindowActive = false;
        private GUIStyle _windowStyle, _labelStyle, _buttonStyle, _toggleStyle, _scrollStyle, _hscrollBarStyle, _vscrollBarStyle;
        public Vector2 scrollPosition, scrollPosition2;

        public void Awake()
        {
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


            RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI));
        }

        public void Update()
        {
            if (RealRosterSettings.instance != null && settings == null)
            {
                settings = RealRosterSettings.instance;
            }

            if (settings && button == null)
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

                    button.Visibility = new GameScenesVisibility(GameScenes.EDITOR);
                }
            }
        }

        private void mainGUI(int windowID)
        {

            GUI.skin.verticalScrollbarThumb = HighLogic.Skin.verticalScrollbarThumb;
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            settings.allowCustomCrewing = GUILayout.Toggle(settings.allowCustomCrewing, "Custom Default Crewing", _toggleStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            GUILayout.Label("Blacklist: (Click to Remove)", _labelStyle);
            GUILayout.EndHorizontal();

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, _hscrollBarStyle, _vscrollBarStyle, _scrollStyle);

            foreach (String kerbal in settings.crewBlackList)
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                if (GUILayout.Button(kerbal))
                {
                    settings.crewBlackList.Remove(kerbal);

                }
                GUILayout.EndHorizontal();

            }
         
            GUILayout.EndScrollView();

            // Iterate through all Kerbals (including those not on a mission).
            List<ProtoCrewMember> roster = HighLogic.CurrentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Crew, ProtoCrewMember.RosterStatus.Available).ToList();
            GUILayout.Label("Crew: (Click to add to Blacklist)", _labelStyle);
            scrollPosition2 = GUILayout.BeginScrollView(scrollPosition2, GUILayout.ExpandWidth(true), GUILayout.Height(100));
            foreach (ProtoCrewMember kerbal in roster)
            {
                if (!settings.crewBlackList.Contains(kerbal.name))
                {
                    if (GUILayout.Button(kerbal.name))
                    {
                        settings.crewBlackList.Add(kerbal.name);
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void drawGUI()
        {
            if (settingWindowActive)
            {
                windowPos = GUILayout.Window(0, windowPos, mainGUI, "RealRoster Settings", _windowStyle);
            }
        }
        
    }
}