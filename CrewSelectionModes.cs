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

        void fillPartCrewManifest(PartCrewManifest part);
    }

    internal class DefaultSelectionMode : ICrewSelectionMode
    {
        private static readonly string TAG = "DefaultSelectionMode";

        public string CleanName { get { return "Default Crew"; } }

        public DefaultSelectionMode()
        {
            CommonLogic.DebugMessage(TAG, "Loading " + TAG);
        }

        public void OnLoad(ConfigNode config) { } // This implementation does not write to the ConfigNode
        public void OnSave(ConfigNode config) { } // This implementation does not write to the ConfigNode

        public void fillPartCrewManifest(PartCrewManifest part)
        {
            int capacity = part.PartInfo.partPrefab.CrewCapacity;
            int assigned = 0;

            // First pass removes everyone
            for (int i = 0; i < capacity; i++)
            {
                part.RemoveCrewFromSeat(i);
            }

            // Second pass places back non-blacklisted kerbs. 
            foreach (ProtoCrewMember crew in RealRosterSettings.Instance.WhiteList)
            {
                CommonLogic.DebugMessage(TAG, "Assigning " + crew.name + " to the vessel.");
                part.AddCrewToSeat(crew, assigned++);
                if (assigned == capacity)
                {
                    break;
                }
            }
        }
    }

    internal class NullSelectionMode : ICrewSelectionMode
    {
        private static readonly string TAG = "NullSelectionMode";

        public string CleanName { get { return "No Crew"; } }

        public NullSelectionMode()
        {
            CommonLogic.DebugMessage(TAG, "Loading " + TAG);
        }

        public void OnLoad(ConfigNode config) { } // This implementation does not write to the ConfigNode
        public void OnSave(ConfigNode config) { } // This implementation does not write to the ConfigNode

        public void fillPartCrewManifest(PartCrewManifest part)
        {
            int capacity = part.PartInfo.partPrefab.CrewCapacity;
            for (int i = 0; i < capacity; i++)
            {
                part.RemoveCrewFromSeat(i);
            }
        }
    }

    internal class RandomSelectionMode : ICrewSelectionMode
    {
        private static readonly string TAG = "RandomSelectionMode";

        public string CleanName { get { return "Random Crew"; } }

        public RandomSelectionMode()
        {
            CommonLogic.DebugMessage(TAG, "Loading " + TAG);
        }

        public void OnLoad(ConfigNode config) { } // This implementation does not write to the ConfigNode
        public void OnSave(ConfigNode config) { } // This implementation does not write to the ConfigNode

        public void fillPartCrewManifest(PartCrewManifest part)
        {
            int capacity = part.PartInfo.partPrefab.CrewCapacity;

            // First pass removes everyone
            for (int i = 0; i < capacity; i++)
            {
                part.RemoveCrewFromSeat(i);
            }

            // Second pass places back non-blacklisted kerbs.
            List<ProtoCrewMember> tempWhitelist = RealRosterSettings.Instance.WhiteList;

            for (int i = 0; (i < capacity) && (tempWhitelist.Count > 0); i++)
            {
                ProtoCrewMember temp = tempWhitelist[UnityEngine.Random.Range(0, tempWhitelist.Count)];
                tempWhitelist.Remove(temp);
                part.AddCrewToSeat(temp, i);
            }
        }
    }

    internal class RotationSelectionMode : ICrewSelectionMode
    {
        private static readonly string TAG = "RotationSelectionMode";

        public string CleanName { get { return "Rotate Crew"; } }

        public RotationSelectionMode()
        {
            CommonLogic.DebugMessage(TAG, "Loading " + TAG);
        }

        public void OnLoad(ConfigNode config) { } 
        public void OnSave(ConfigNode config) { }

        public void fillPartCrewManifest(PartCrewManifest part) { }
    }
}
