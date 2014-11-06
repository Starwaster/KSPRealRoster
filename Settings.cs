using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RealRoster
{
    // This gets loaded after KSPAddons are
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] { GameScenes.EDITOR, GameScenes.SPACECENTER })]
    class SettingsModule : ScenarioModule
    {
        // Statics
        private static readonly string RESOURCE_PATH = "Enneract/RealRoster/Resources/";
        private static readonly string ICON_ON = RESOURCE_PATH + "IconOn";
        private static readonly string ICON_OFF = RESOURCE_PATH + "IconOff";
        private static readonly string TAG = "SettingsModule";
        private static readonly string CAPTION = "RealRoster";

        //Automatic Persistent Fields
        [KSPField(isPersistant = true)]
        public bool AllowCustomCrew;
        [KSPField(isPersistant = true)]
        public string CrewSelectionMode;

        //Manual Persistent Fields
        public List<string> BlackList { get { return new List<string>(privateBlackList); } }
        private List<string> privateBlackList;

        //GUI Fields
        private IButton SettingsButton;
        private bool SettingsWindowVisible;
        
        
        void Awake()
        {
            CommonLogic.DebugMessage(TAG, "Awake...");

            SettingsWindowVisible = false;
        }

        void Start()
        {
            CommonLogic.DebugMessage(TAG, "Start...");
            if (ToolbarManager.ToolbarAvailable)
            {
                CommonLogic.DebugMessage(TAG, "Toolbar is available.");

                // This should be the last statement of this method.
                // Do not initialize the Button until everything else is set up.
                SettingsButton = ToolbarManager.Instance.add(CAPTION, CAPTION);
                if (SettingsButton != null)
                {
                    SettingsButton.TexturePath = RESOURCE_PATH + "IconOff";
                    SettingsButton.ToolTip = CAPTION;

                    SettingsButton.OnClick += (e) =>
                    {
                        SettingsButton.TexturePath = SettingsWindowVisible ? ICON_ON : ICON_OFF;
                        SettingsWindowVisible = !SettingsWindowVisible;
                    };

                    SettingsButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.SPH, GameScenes.SPACECENTER);
                }

            }
        }

        override void OnSave(ConfigNode config)
        {
            CommonLogic.DebugMessage(TAG, "Saving...");
            base.OnSave(config);

            ConfigNode blacklistNode = new ConfigNode("BLACKLIST_NODE");
            foreach (string name in BlackList)
            {
                CommonLogic.DebugMessage(TAG, "Writing '" + name + "' to blacklist node");
                blacklistNode.AddValue("name", name);
            }
            config.AddNode(blacklistNode);
        }

        override void OnLoad(ConfigNode config)
        {
            CommonLogic.DebugMessage(TAG, "Loading...");
            base.OnLoad(config);
            if (config.HasNode("BLACKLIST_NODE"))
            {
                ConfigNode blacklistNode = config.GetNode("BLACKLIST_NODE");
                foreach(string name in blacklistNode.GetValues("name")) 
                {
                    CommonLogic.DebugMessage(TAG, "Found blacklist member - " + name);
                    if (!privateBlackList.Contains(name))
                    {
                        privateBlackList.Add(name);
                    }
                }
            }
        }
    }
}
