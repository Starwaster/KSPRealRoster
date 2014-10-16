using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using Toolbar;

namespace RealRoster
{
    public class Settings
    {
        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Settings();
                    _instance.Load();
                }
                return _instance;
            }
        }
        
        private static Settings _instance;
        protected String filePath = KSPUtil.ApplicationRootPath + "GameData/Enneract/RealRoster/Plugins/RealRoster.cfg";
        [Persistent]
        public bool crewAssignment;
        [Persistent]
        public bool crewRandomization;
        [Persistent]
        public List<String> blackList;
        //[Persistent]
        public bool eventRegistered;

        public Settings()
        {
            crewAssignment = true;
            crewRandomization = true;
            eventRegistered = false;

            blackList = new List<String>();
            blackList.Add("Bob Kerman");
            blackList.Add("Bill Kerman");
            blackList.Add("Jebediah Kerman");
        }

        public void Reset()
        {
            crewAssignment = true;
            crewRandomization = true;

            blackList.Clear();
            blackList.Add("Bob Kerman");
            blackList.Add("Bill Kerman");
            blackList.Add("Jebediah Kerman");
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
