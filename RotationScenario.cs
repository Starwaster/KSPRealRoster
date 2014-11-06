using KSP;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RealRoster
{
	[KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[]{
		GameScenes.SPACECENTER,
		GameScenes.FLIGHT,
		GameScenes.EDITOR,
		GameScenes.SPH,
		GameScenes.TRACKSTATION
	})]

	public class RotationScenario : ScenarioModule
	{
		public static RotationScenario Instance;
		
		private List<ProtoCrewMember> crewRotationPool;

		//private List<ProtoCrewMember> _crewRotationPool;

		public IEnumerable<ProtoCrewMember> roster = HighLogic.CurrentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Crew, ProtoCrewMember.RosterStatus.Available).ToList();

		public List<ProtoCrewMember> CrewRotationPool
		{
			get
			{
                //RealRoster.DebugMessage("RealRoster.RRScenario.CrewRotationPool() sending " + this.crewRotationPool.Count.ToString() + " element list. (original: " + this.crewRotationPool.Count.ToString() + ")");
				return new List<ProtoCrewMember>(crewRotationPool);
			}
		}



		public double CRPTimeStamp;

		bool scenarioInitialized;

		RealRosterSettings rrSettings = RealRosterSettings.Instance;

		public RotationScenario ()
		{
			scenarioInitialized = false;
			CRPTimeStamp = 0.0;
		}
		
		public override void OnAwake ()
		{
			//RealRoster.DebugMessage ("RealRoster.RRScenario.OnAwake()");
			GameEvents.onKerbalAdded.Add(AddKerbal);
			GameEvents.onKerbalRemoved.Add(RemoveKerbal);
			GameEvents.onKerbalTypeChange.Add(KerbalTypeChanged);
			GameEvents.onKerbalStatusChange.Add(KerbalStatusChanged);
			RotationScenario.Instance = this;
		}

		public void OnStart()
		{
			//RealRoster.DebugMessage ("RealRoster.RRScenario.RRScenario.OnStart()");
		}


		private void AddKerbal(ProtoCrewMember kerbal)
		{
			//RealRoster.DebugMessage ("RealRoster.RRScenario.GameEvent.onKerbalAdded: kerbal = " + kerbal.name);
			if ((object)kerbal != null && kerbal.name != "")
			{
				if (kerbal.type == ProtoCrewMember.KerbalType.Crew && kerbal.rosterStatus == ProtoCrewMember.RosterStatus.Available)
				{
					crewRotationPool.Add (kerbal);
					CRPTimeStamp = Planetarium.GetUniversalTime ();
				}
			}
		}
		
		private void RemoveKerbal(ProtoCrewMember kerbal)
		{
			//RealRoster.DebugMessage ("RealRoster.RRScenario.GameEvent.onKerbalRemoved: kerbal = " + kerbal.name);
			if ((object)kerbal != null && kerbal.type == ProtoCrewMember.KerbalType.Crew)
			{
				crewRotationPool.Remove (kerbal);
				CRPTimeStamp = Planetarium.GetUniversalTime ();
			}
		}

		private void KerbalTypeChanged(ProtoCrewMember kerbal, ProtoCrewMember.KerbalType oldtype, ProtoCrewMember.KerbalType newtype)
		{
			if ((object)kerbal != null && kerbal.name != "" && oldtype != newtype)
			{
				//RealRoster.DebugMessage ("RealRoster.RRScenario.GameEvent.onKerbalTypeChanged: kerbal = " + kerbal.name + ", oldtype = " + oldtype.ToString () + ", newtype = " + newtype.ToString ());
				if (newtype != ProtoCrewMember.KerbalType.Crew && crewRotationPool.Contains (kerbal))
				{
					crewRotationPool.Remove (kerbal);
					CRPTimeStamp = Planetarium.GetUniversalTime ();
				}
				// Was this necessary? I don't know! Thinking about Kerbal Recovery missions. Don't know if OnKerbalAdded is called for them.
				else if (newtype == ProtoCrewMember.KerbalType.Crew && kerbal.rosterStatus == ProtoCrewMember.RosterStatus.Available && !crewRotationPool.Contains (kerbal))
				{
					crewRotationPool.Add (kerbal);
					CRPTimeStamp = Planetarium.GetUniversalTime ();
				}
			}
		}

		private void KerbalStatusChanged(ProtoCrewMember kerbal, ProtoCrewMember.RosterStatus oldstatus, ProtoCrewMember.RosterStatus newstatus)
		{
			if ((object)kerbal != null && kerbal.name != "" && oldstatus != newstatus)
			{
				//RealRoster.DebugMessage ("RealRoster.RRScenario.GameEvent.onKerbalStatusChanged: kerbal = " + kerbal.name + ", oldstatus = " + oldstatus.ToString () + ", newstatus = " + newstatus.ToString ());
				if (newstatus != ProtoCrewMember.RosterStatus.Available && crewRotationPool.Contains (kerbal))
				{
					crewRotationPool.Remove (kerbal);
					CRPTimeStamp = Planetarium.GetUniversalTime ();
				}
				else if (newstatus == ProtoCrewMember.RosterStatus.Available && !crewRotationPool.Contains (kerbal))
				{
					crewRotationPool.Add (kerbal);
					CRPTimeStamp = Planetarium.GetUniversalTime ();
				}
			}
		}

		// Don't use.... doesn't deal with moving the candidate crewmember and may be obsolete
		public ProtoCrewMember GetNextAvailable()
		{
			if (crewRotationPool.Count == 0)
				return null;
			ProtoCrewMember candidate;
			candidate = crewRotationPool.FirstOrDefault();

			return candidate;
		}
		
		public override void OnSave(ConfigNode node)
		{
			//RealRoster.DebugMessage ("RealRoster: RRScenario.OnSave()");

			// Don't bother declaring that we're initialized if we don't even have a valid crew roster yet.
			if ((object)crewRotationPool == null)
				//RealRoster.DebugMessage ("RealRoster.RRScenario.OnSave(): null crewRotationPool error");
			if ((object)node == null)
			{
				//RealRoster.DebugMessage ("RealRoster.RRScenario.OnSave(): null ConfigNode node - ABORTING");
				return;
			}
			if (crewRotationPool.Count > 0)
			{
				scenarioInitialized = true;
				CRPTimeStamp = Planetarium.GetUniversalTime();
			}

			//RealRoster.DebugMessage ("RealRoster.RRScenario.OnSave(): crewRotationPool.Count = " + crewRotationPool.Count.ToString ());
			ConfigNode crewRotationNode = new ConfigNode("CREW_ROTATION_NODE");
			node.AddValue ("scenarioInitialized", scenarioInitialized);
			if ((object)crewRotationNode == null)
			{
				//RealRoster.DebugMessage ("RealRoster.RRScenario.OnSave(): ERROR. CREW_ROTATION_NODE is null!");
			}
			crewRotationNode.AddValue ("timestamp", CRPTimeStamp);
			//RealRoster.DebugMessage ("RealRoster.RRScenario.OnSave(): saving crewRotationPool data to CREW_ROTATION_NODE, timestamp: " + CRPTimeStamp.ToString ());
			foreach (ProtoCrewMember kerbal in crewRotationPool)
			{
				crewRotationNode.AddValue("kerbal", kerbal.name);
				//RealRoster.DebugMessage ("RealRoster.RRScenario.OnSave(): Added " + kerbal.name + " to CREW_ROTATION_NODE");
			}
			//RealRoster.DebugMessage ("RealRoster.RRScenario.OnSave(): Saving CREW_ROTATION_NODE to savegame.");
			node.AddNode (crewRotationNode);
		}

		public override void OnLoad(ConfigNode node)
		{
			//RealRoster.DebugMessage ("RealRoster: RRScenario.OnLoad()");

			double loadedCRPTimeStamp = 0.0;

			List<ProtoCrewMember> loadedCrewRotationPool = new List<ProtoCrewMember>();

			if (node.HasValue ("scenarioInitialized"))
				bool.TryParse (node.GetValue ("scenarioInitialized"), out scenarioInitialized);

			if (!scenarioInitialized)
			{
				crewRotationPool = HighLogic.CurrentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Crew, ProtoCrewMember.RosterStatus.Available).ToList();
				CRPTimeStamp = Planetarium.GetUniversalTime();
			}
			else if (node.HasNode ("CREW_ROTATION_NODE"))
			{
				//RealRoster.DebugMessage ("Found CREW_ROTATION_NODE");
				ConfigNode crewRotationNode = node.GetNode ("CREW_ROTATION_NODE");
				if (crewRotationNode.HasValue ("timestamp"))
					double.TryParse (crewRotationNode.GetValue ("timestamp"), out loadedCRPTimeStamp);
				foreach (string kerbalName in crewRotationNode.GetValues("kerbal"))
				{
					ProtoCrewMember candidate = HighLogic.CurrentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Crew, ProtoCrewMember.RosterStatus.Available).FirstOrDefault(kerbal => kerbal.name == kerbalName);
					if ((object)candidate != null && !loadedCrewRotationPool.Contains (candidate))
					{
						loadedCrewRotationPool.Add (candidate);
						//RealRoster.DebugMessage ("Adding Kerbal " + candidate.name + " to Crew Rotation Pool buffer");
					}
				}
				if (loadedCrewRotationPool.Count != roster.Count ())
				{
					foreach(ProtoCrewMember candidate2 in roster)
					{
						if (!loadedCrewRotationPool.Contains (candidate2))
						{
							// Failsafe in case Kerbals go awol from list. Shouldn't happen except during development of this code
							loadedCrewRotationPool.Add (candidate2);
						}
					}
				}
				this.crewRotationPool = loadedCrewRotationPool;
				CRPTimeStamp = loadedCRPTimeStamp;
				//RealRoster.DebugMessage ("crewRotationPool count = " + this.crewRotationPool.Count().ToString ());
			}
		}
	}
}
