using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace RealRoster
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    internal class CrewSelectionModeLoader : MonoBehaviour
    {
        public static CrewSelectionModeLoader Instance = null;
        public List<ICrewSelectionMode> LoadedModes;

        public void Awake() 
        {
            DontDestroyOnLoad(this);
            LoadedModes = new List<ICrewSelectionMode>();
            Instance = this;

            // examine our assembly for loaded types
            foreach (var type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
            {
                Type[] interfaces = type.GetInterfaces();
                if (interfaces.Contains(typeof(ICrewSelectionMode)) && type.IsClass)
                {
                    Debug.Log("CrewSelectionModeLoader Found Mode at: " + type.FullName);
                    object instance = Activator.CreateInstance(type);

                    LoadedModes.Add((ICrewSelectionMode)instance);
                }
            }
        }
    }


    public interface ICrewSelectionMode
    {
        string CleanName { get; }
    }

    class NullSelectionModule : ICrewSelectionMode
    {
        public string CleanName { get { return "No Crew"; } }

        public NullSelectionModule()
        {
            Debug.Log("Loading NullSelectionModule");
        }
    }

    class RandomSelectionModule : ICrewSelectionMode
    {
        public string CleanName { get { return "Randomize Crew"; } }

        public RandomSelectionModule()
        {
            Debug.Log("Loading RandomSelectionModule");
        }
    }
}
