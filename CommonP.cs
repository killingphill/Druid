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
using System.Collections.Generic;

namespace ReBot{

// enumerators 

	public enum forms {
		



	}



	public abstract class CommonP : CombatRotation{
		// options
		
		[JsonProperty("GatheringSetup")]
		public bool GatheringSetup = false;

		[JsonProperty("PvP settings")]
		public bool PvPSettings = true;

		
		
		public bool hasCatForm() {
			return (HasAura("Cat Form") || HasAura("Claws of Shirvallah"));
		}
		
		public bool waitTillCombatOver = false;
		
		public bool doAfterCombat(){
			waitTillCombatOver = false;
			return false;
		}
		
		public bool CastCatForm() {
			if(hasCatForm() == false) {
				API.ExecuteMacro("/cast Cat Form");
				return true;
			}
			return false;
		}
		
		public bool doIHaveADot() {
			
			return false;
		}
		public bool doOutOfCombat(){
			//Heal and Rebuff after combat
			if (CastSelf("Rejuvenation", () => Me.HealthFraction <= 0.75 && !HasAura("Rejuvenation"))) return true; 
			if (CastSelfPreventDouble("Healing Touch", () => Me.HealthFraction <= 0.5)) return true ;
			if (CastSelf("Remove Corruption", () => Me.Auras.Any(x => x.IsDebuff && "Curse,Poison".Contains(x.DebuffType)))) return true;
			if (CastSelf("Mark of the Wild", () => !HasAura("Mark of the Wild") && !HasAura("Blessing of Kings"))) return true ;
			
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
	}





































}