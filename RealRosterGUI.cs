using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RealRoster
{
    class RealRosterGUI
    {
        RealRosterSettings settings = RealRosterSettings.instance;
        private IButton button;
        public static readonly String RESOURCE_PATH = "Enneract/RealRoster/Resources/";

        Dictionary<String, bool> tempBlackList = new Dictionary<String, bool>();

        protected Rect windowPos = new Rect(Screen.width / 4, Screen.height / 4, 10f, 10f);
        public bool settingWindowActive = false;
        private GUIStyle _windowStyle, _labelStyle, _buttonStyle, _toggleStyle, _scrollStyle, _hscrollBarStyle, _vscrollBarStyle;

        public void init()
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

            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                AddToolbarButton();
            }
        }
        private void AddToolbarButton()
        {
            String iconOn = RESOURCE_PATH + "IconOn";
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

                button.Visibility = new GameScenesVisibility(GameScenes.SPACECENTER, GameScenes.EDITOR);
            }

        }

        //Scroll window!
        public Vector2 scrollPosition, scrollPosition2;
        //public Vector2 scrollPosition;

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
            GUILayout.EndHorizontal();

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
