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

    public interface ICrewSelectionMode
    {
        // Human-readable name for this mode. (This is what is displayed in the settings menu).
        string CleanName { get; }

        // This method will be called when the settings are written to persistence.

        void OnLoad(ConfigNode config);
        // This method will be called when the settings are read from persistence.
        void OnSave(ConfigNode config);

        void fillPartCrewManifest(PartCrewManifest sourcePCM);
    }

    internal class DefaultSelectionMode : ICrewSelectionMode
    {
        private static readonly string TAG = "DefaultSelectionMode";

        public string CleanName { get { return "Default Crew"; } }

        public DefaultSelectionMode()
        {
            CommonLogic.DebugMessage(TAG, "Loading DefaultSelectionMode");
        }

        public void OnLoad(ConfigNode config) { } // This implementation does not write to the ConfigNode
        public void OnSave(ConfigNode config) { } // This implementation does not write to the ConfigNode

        public void fillPartCrewManifest(PartCrewManifest sourcePCM) 
        {
            int capacity = sourcePCM.PartInfo.partPrefab.CrewCapacity;

            // First pass removes everyone
            for (int i = 0; i < capacity; i++)
            {
                sourcePCM.RemoveCrewFromSeat(i);
            }

            // Second pass places back non-blacklisted kerbs. 
            List<ProtoCrewMember> roster = HighLogic.CurrentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Crew, ProtoCrewMember.RosterStatus.Available).ToList();
            ProtoCrewMember[] newRoster = new ProtoCrewMember[capacity];
            int count = 0;
            foreach (ProtoCrewMember kerb in roster)
            {
                if (!RealRosterSettings.Instance.BlackList.Contains(kerb.name))
                {
                    newRoster[count++] = kerb;
                }
                if (count > capacity)
                {
                    break;
                }
            }
            count = (count < capacity) ? count : capacity;
            for (int i = 0; i < count; i++)
            {
                sourcePCM.AddCrewToSeat(newRoster[i], i);
            }
        }
    }

    internal class NullSelectionMode : ICrewSelectionMode
    {
        private static readonly string TAG = "NullSelectionMode";

        public string CleanName { get { return "No Crew"; } }

        public NullSelectionMode()
        {
            CommonLogic.DebugMessage(TAG, "Loading NullSelectionMode");
        }

        public void OnLoad(ConfigNode config) { } // This implementation does not write to the ConfigNode
        public void OnSave(ConfigNode config) { } // This implementation does not write to the ConfigNode

        public void fillPartCrewManifest(PartCrewManifest sourcePCM)
        {
            int capacity = sourcePCM.PartInfo.partPrefab.CrewCapacity;
            for (int i = 0; i < capacity; i++)
            {
                sourcePCM.RemoveCrewFromSeat(i);
            }    
        }
    }

    internal class RandomSelectionMode : ICrewSelectionMode
    {
        private static readonly string TAG = "RandomSelectionMode";

        public string CleanName { get { return "Random Crew"; } }

        public RandomSelectionMode()
        {
            CommonLogic.DebugMessage(TAG, "Loading RandomSelectionMode");
        }

        public void OnLoad(ConfigNode config) { } // This implementation does not write to the ConfigNode
        public void OnSave(ConfigNode config) { } // This implementation does not write to the ConfigNode

        public void fillPartCrewManifest(PartCrewManifest sourcePCM) { }
    }
}
