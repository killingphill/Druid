//
//
//
//
//
//
//

using Geometry;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ReBot.API;
using ReBot;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Collections;
using System.Diagnostics;

namespace ReBot{

	public abstract class CommonP : CombatRotation{
		// options
		
		// <summary> 
		// Debugg
		// </summary>
		[JsonProperty("DeveloperDebugMode")]
        public bool developerDebugMode = false;
		
		[JsonProperty("GatheringSetup")]
		public bool GatheringSetup = false;

		[JsonProperty("PvP settings")]
		public bool PvPSettings = true;
		
		[JsonProperty("Cyclone Healer at target health percent")]
		public int CycloneHealerValue = 70;
		
		[JsonProperty("Arena Size"), JsonConverter(typeof(StringEnumConverter))]							
		public ArenaType ArenaSize  = ArenaType.Twos;
		

		public enum ArenaType
		{
			Twos ,
			Threes ,
			Fives ,
			NoArena ,
		}
		
		
		public int Arenavalue()
		{
			if(ArenaSize == ArenaType.Twos)
			{
				return 2;
			}
			if(ArenaSize == ArenaType.Threes)
			{
				return 3;
			}
			if(ArenaSize == ArenaType.Fives)
			{
				return 5;
			}
			return 10;
		}
		
		public bool hasCatForm() {
			return (HasAura("Cat Form") || HasAura("Claws of Shirvallah"));
		}
		
		public bool waitTillCombatOver = false;
		
		public bool doAfterCombat(){
			waitTillCombatOver = false;
			return false;
		}
		
		private bool FocusOn = false;
		public bool inArena = false;
		public int[] DRCounter = new int[10];
		public Stopwatch[] DRTimer = new Stopwatch[10];
		
		public bool CastCatForm() {
			if(hasCatForm() == false) {
				API.ExecuteMacro("/cast Cat Form");
				return true;
			}
			return false;
		}
		
		public bool DRTracker(int Victim)
		{

			DebugWrite(Victim.ToString());
			if(DRTimer[Victim].ElapsedMilliseconds > 20000)
			{
				DRCounter[Victim] = 0;
			}
			
			if( DRCounter[Victim] < 2)
			{
				return true;
			}
			else
			{
				return false;
			}

		}
		
		public PlayerObject[] SetArenaTargets() {
			var players = API.Players.Where(u => u.IsEnemy).ToArray();
			DebugWrite("Inside of SetArenaTarget");
			if (players.Length > 1)
			{
				HealerFocus(players);
			}
			return players;
		}
		
		public void HealerFocus(PlayerObject[] players) 
		{
			DebugWrite("Inside of HealerFocus");
			for(int i =0; i < players.Length; i++)
			{
				if(players[i].IsHealer){
					if (Me.Focus == null) {
						Me.SetFocus(players[i]);
						FocusOn = true;
					}
				}
			} 
			if (Me.Focus == null)
			{
				if (Target == players[0])
				{
					if(players.Length >1)
					{
						Me.SetFocus(players[1]);
					}
					FocusOn = true;
				}else{
					Me.SetFocus(players[0]);
					FocusOn = true;
				}
			}
		
		}
		
		
		public bool Cyclone(PlayerObject[] players)
		{
			for(int i=0; i < players.Length; i++)
			{
				if(players.Length >= 3)
				{
					if (players[i] == Me.Focus)
					{
						if (Target.HealthFraction <= CycloneHealerValue / 100f)
						{
							//If Target i Has DR it moves on
							if(DRCounter[i] < 2) {
								return CycloneTarget(players[i], i);
							}
							
						}
					}
					if (players[i] != Target)
					{
						//If Target i Has DR it moves on
						if(DRCounter[i] < 2) {
							return CycloneTarget(players[i], i);
						}
					}
				}
				else if(players.Length >1)
				{
					if (players[i] == Me.Focus)
					{
						if (players[i].IsHealer) 
						{
							if (Target.HealthFraction <= CycloneHealerValue / 100f)
							{
								return CycloneTarget(players[i], i);
							}
						}
						return CycloneTarget(players[i], i);			
					}
				}
			}
			return false;
		}
		
		public bool CycloneTarget(PlayerObject Victim, int Identifier)
		{
			DebugWrite("Inside of CycloneTarget");
			if(DRTracker(Identifier)){
				if(Victim.HasAura("Cyclone")){
					if(Victim.AuraTimeRemaining("Cyclone") <= 1.5f)
					{
						if (Cast("Cyclone", () => Victim.IsInCombatRangeAndLoS))
						{
							DRCounter[Identifier] += 1;
							DRTimer[Identifier].Restart();
							return true;
						}
					}
				}
				else
				{
					if ( Cast("Cyclone", () => Victim.IsInCombatRangeAndLoS))
					{
						DRCounter[Identifier] += 1;
						DRTimer[Identifier].Restart();
						return true;
					}
				}
			}
			return false;
		}
		
		public bool doIHaveADot() {
			
			return false;
		}
		public bool doOutOfCombat(){
			//Heal and Rebuff after combat
			inArena = false;
			if(Me.Focus == null) {
				FocusOn = false;
			}
			if (CastSelf("Rejuvenation", () => Me.HealthFraction <= 0.75 && !HasAura("Rejuvenation"))) return true; 
			if (CastSelfPreventDouble("Healing Touch", () => Me.HealthFraction <= 0.5)) return true ;
			if (CastSelf("Remove Corruption", () => Me.Auras.Any(x => x.IsDebuff && "Curse,Poison".Contains(x.DebuffType)))) return true;
			if (CastSelf("Mark of the Wild", () => !HasAura("Mark of the Wild") && !HasAura("Blessing of Kings"))) return true ;
			if (hasCatForm())
			{
				Cast("Prowl", () => !HasAura("Prowl"));
			}
			
			//If Gathering is setup it will prowl around gathering and not mounting
			if(GatheringSetup){
				if(!Me.PreventMounting || !Me.PreventFlightMounting){
					Me.PreventMounting = true;
					Me.PreventFlightMounting = true;
				}
				CastCatForm();
				Cast("Prowl", () => !HasAura("Prowl") && !HasAura("Travel Form") && !doIHaveADot() && Me.DistanceTo(API.GetNaviTarget()) > 30);
				if (CastSelf("Dash", () => HasAura("Prowl") && Me.DistanceTo(API.GetNaviTarget()) > 50))return true;
			}else {
				Me.PreventMounting = false;
				Me.PreventFlightMounting = false;
			}
			return false;
		}
		public void DebugWrite(string s) {if (developerDebugMode){API.Print(s);}}

	}
}