using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RealRoster
{
    public class RealRosterSettings
    {
        public static RealRosterSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RealRosterSettings();
                    _instance.Load();
                }
                return _instance;
            }
        }
        
        private static RealRosterSettings _instance;
        protected String filePath = KSPUtil.ApplicationRootPath + "GameData/Enneract/RealRoster/Plugins/RealRoster.cfg";
        [Persistent]
        public bool crewAssignment;
        [Persistent]
        public bool crewRandomization;
        [Persistent]
        public List<String> blackList;
		//[Persistent]
		//public bool useCrewRotation;

        //[Persistent]
        public bool eventRegistered;

        public RealRosterSettings()
        {
            crewAssignment = true;
            crewRandomization = true;
            eventRegistered = false;

            blackList = new List<String>();
        }

        public void Reset()
        {
            crewAssignment = true;
            crewRandomization = true;

            blackList.Clear();
        }

        public void Load()
        {
            if (System.IO.File.Exists(filePath))
            {
                ConfigNode cnToLoad = ConfigNode.Load(filePath);
                ConfigNode.LoadObjectFromConfig(this, cnToLoad);
            }
            else
            {
                this.Save();
            }
        }

        public void Save()
        {
            eventRegistered = false;
            ConfigNode cnTemp = ConfigNode.CreateConfigFromObject(this, new ConfigNode());
            cnTemp.Save(filePath);
        }
    }
}
