using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RealRoster
{
    public interface ICrewSelectionMode
    {
        string CleanName { get; }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class NullSelectionModule : MonoBehaviour, ICrewSelectionMode
    {
        public string CleanName { get { return "No Crew"; } }

        public void Start()
        {
            RealRoster.instance.registerCrewSelectionMode(this);
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class RandomSelectionModule : MonoBehaviour, ICrewSelectionMode
    {
        public string CleanName { get { return "Randomize Crew"; } }

        public void Start()
        {
            RealRoster.instance.registerCrewSelectionMode(this);
        }
    }
}
