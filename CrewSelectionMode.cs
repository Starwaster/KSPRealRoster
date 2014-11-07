using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace RealRoster
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    class CrewSelectionModeLoader : MonoBehaviour
    {
        private static readonly string TAG = "CrewSelectionModeLoader";

        public static CrewSelectionModeLoader Instance = null;
        public List<ICrewSelectionMode> LoadedModes;

        void Awake() 
        {
            CommonLogic.DebugMessage(TAG, "Awake...");
            DontDestroyOnLoad(this);
            LoadedModes = new List<ICrewSelectionMode>();
            Instance = this;

            // examine our assembly for loaded types
            foreach (var type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
            {
                Type[] interfaces = type.GetInterfaces();
                if (interfaces.Contains(typeof(ICrewSelectionMode)) && type.IsClass)
                {
                    CommonLogic.DebugMessage(TAG, "found Mode called: " + type.Name);
                    object obj = Activator.CreateInstance(type);
                    LoadedModes.Add((ICrewSelectionMode)obj);
                }
            }
        }
    }

    interface ICrewSelectionMode
    {
        string CleanName { get; }
    }

    class NullSelectionMode : ICrewSelectionMode
    {
        private static readonly string TAG = "NullSelectionMode";

        public string CleanName { get { return "No Crew"; } }

        public NullSelectionMode()
        {
            CommonLogic.DebugMessage(TAG, "Loading NullSelectionMode");
        }
    }

    class RandomSelectionModule : ICrewSelectionMode
    {
        private static readonly string TAG = "RandomSelectionMode";

        public string CleanName { get { return "Randomize Crew"; } }

        public RandomSelectionModule()
        {
            CommonLogic.DebugMessage(TAG, "Loading RandomSelectionMode");
        }
    }
}
