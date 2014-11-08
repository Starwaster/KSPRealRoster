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
				return new List<ProtoCrewMember>(crewRotationPool);
				//this._crewRotationPool = new List<ProtoCrewMember>(crewRotationPool);
				Debug.Log ("RealRoster.RRScenario.CrewRotationPool() sending " + this.crewRotationPool.Count.ToString() + " element list. (original: " + this.crewRotationPool.Count.ToString () + ")");
				//return this._crewRotationPool;
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
			Debug.Log ("RealRoster.RRScenario.OnAwake()");
			GameEvents.onKerbalAdded.Add(AddKerbal);
			GameEvents.onKerbalRemoved.Add(RemoveKerbal);
			GameEvents.onKerbalTypeChange.Add(KerbalTypeChanged);
			GameEvents.onKerbalStatusChange.Add(KerbalStatusChanged);
			RotationScenario.Instance = this;
		}

		public void OnStart()
		{
			Debug.Log ("RealRoster.RRScenario.RRScenario.OnStart()");
		}


		private void AddKerbal(ProtoCrewMember kerbal)
		{
			Debug.Log ("RealRoster.RRScenario.GameEvent.onKerbalAdded: kerbal = " + kerbal.name);
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
			Debug.Log ("RealRoster.RRScenario.GameEvent.onKerbalRemoved: kerbal = " + kerbal.name);
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
				Debug.Log ("RealRoster.RRScenario.GameEvent.onKerbalTypeChanged: kerbal = " + kerbal.name + ", oldtype = " + oldtype.ToString () + ", newtype = " + newtype.ToString ());
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
				Debug.Log ("RealRoster.RRScenario.GameEvent.onKerbalStatusChanged: kerbal = " + kerbal.name + ", oldstatus = " + oldstatus.ToString () + ", newstatus = " + newstatus.ToString ());
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
			Debug.Log ("RealRoster: RRScenario.OnSave()");

			// Don't bother declaring that we're initialized if we don't even have a valid crew roster yet.
			if ((object)crewRotationPool == null)
				Debug.Log ("RealRoster.RRScenario.OnSave(): null crewRotationPool error");
			if ((object)node == null)
			{
				Debug.Log ("RealRoster.RRScenario.OnSave(): null ConfigNode node - ABORTING");
				return;
			}
			if (crewRotationPool.Count > 0)
			{
				scenarioInitialized = true;
				CRPTimeStamp = Planetarium.GetUniversalTime();
			}

			Debug.Log ("RealRoster.RRScenario.OnSave(): crewRotationPool.Count = " + crewRotationPool.Count.ToString ());
			ConfigNode crewRotationNode = new ConfigNode("CREW_ROTATION_NODE");
			node.AddValue ("scenarioInitialized", scenarioInitialized);
			if ((object)crewRotationNode == null)
			{
				Debug.Log ("RealRoster.RRScenario.OnSave(): ERROR. CREW_ROTATION_NODE is null!");
			}
			crewRotationNode.AddValue ("timestamp", CRPTimeStamp);
			Debug.Log ("RealRoster.RRScenario.OnSave(): saving crewRotationPool data to CREW_ROTATION_NODE, timestamp: " + CRPTimeStamp.ToString ());
			foreach (ProtoCrewMember kerbal in crewRotationPool)
			{
				crewRotationNode.AddValue("kerbal", kerbal.name);
				Debug.Log ("RealRoster.RRScenario.OnSave(): Added " + kerbal.name + " to CREW_ROTATION_NODE");
			}
			Debug.Log ("RealRoster.RRScenario.OnSave(): Saving CREW_ROTATION_NODE to savegame.");
			node.AddNode (crewRotationNode);
		}

		public override void OnLoad(ConfigNode node)
		{
			Debug.Log ("RealRoster: RRScenario.OnLoad()");

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
				Debug.Log ("Found CREW_ROTATION_NODE");
				ConfigNode crewRotationNode = node.GetNode ("CREW_ROTATION_NODE");
				if (crewRotationNode.HasValue ("timestamp"))
					double.TryParse (crewRotationNode.GetValue ("timestamp"), out loadedCRPTimeStamp);
				foreach (string kerbalName in crewRotationNode.GetValues("kerbal"))
				{
					ProtoCrewMember candidate = HighLogic.CurrentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Crew, ProtoCrewMember.RosterStatus.Available).FirstOrDefault(kerbal => kerbal.name == kerbalName);
					if ((object)candidate != null && !loadedCrewRotationPool.Contains (candidate))
					{
						loadedCrewRotationPool.Add (candidate);
						Debug.Log ("Adding Kerbal " + candidate.name + " to Crew Rotation Pool buffer");
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
				Debug.Log ("crewRotationPool count = " + this.crewRotationPool.Count().ToString ());
			}
		}
	}
}
