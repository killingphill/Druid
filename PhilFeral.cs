using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;
using Geometry;
using ReBot.API;


namespace ReBot
{
    // WoD Patch 6.0.2
	[Rotation("PhilFeral", "Phil", WoWClass.Druid, Specialization.DruidFeral, 5, 25)]
	public class PhilFeral : CommonP
	{
	
	//Should it heal its party members
	[JsonProperty("PvP Healing")]
		public bool PvPHealing = false;
		
	//Health Percentage it starts casting Rejuvenation and Cenarion Ward	
	[JsonProperty("Cast Rejuvenation And Cenarion Ward")]
		public int HealingPercent = 80;
		
		// This is important, some mobs can't get the rake debuff. If this is missing the bot would always try rake...
		AutoResetDelay rakeDelay = new AutoResetDelay(7000);
		public bool mountup = false; // Has the bot not mount up
		
		public PhilFeral()
		{
			GroupBuffs = new[]
			{
				"Mark of the Wild"
			};
			PullSpells = new[]
			{
				"Moonfire"
			};
		}

		public override bool OutOfCombat()
		{
			if (doAfterCombat()) {
				return true;
			} 
			if (doOutOfCombat()) {
				return true;
			}
			return false;
			
		
		}

		public bool Burst()
		{
			if(HasAura("Savage Roar"))
			{
				Cast("Berserk", () => Target.IsInCombatRange);
				Cast("Incarnation: King of the Jungle");
				return true;
			}else {
				if(Me.GetPower(WoWPowerType.Energy) >= 25 && Me.ComboPoints >= 1) {
					Cast("Savage Roar");
					return false;
				}else {
					return false;
				}
			
			}
		}
		
		public bool IsStealthy()
		{
			if(HasAura("Prowl")) return true;
			if(HasAura("Incarnation: King of the Jungle")) return true;
			return false;
		
		}
		

		
		public override void Combat()
		{
			
			//<HEALING SECTION!!!!>
			//Get a List of all Party Members
			List<PlayerObject> members = Group.GetGroupMemberObjects();
			//Sort the Group by Health Percent
			List<PlayerObject> HPSort = members.OrderBy(x => x.HealthFraction).ToList();
			//If I can Instant Cast Healing Touch
			if (HasAura("Predatory Swiftness")) {
				if (Me.HealthFraction < HPSort[0].HealthFraction) {
					if (CastSelf("Healing Touch", () => Me.HealthFraction <= (HealingPercent/ 100f) && HasAura("Predatory Swiftness"))) return;
				}else {
					Cast("Healing Touch", () => HPSort[0].HealthFraction <= (HealingPercent/ 100f) && HasAura("Predatory Swiftness") && PvPHealing && HPSort[0].IsInCombatRangeAndLoS);
				}
			}
			if (CastSelf("Rejuvenation", () => Me.HealthFraction <= (HealingPercent/ 100f) && !HasAura("Rejuvenation") && PvPHealing)) return;
			if (CastSelf("Cenarion Ward", () => Me.HealthFraction <= (HealingPercent/ 100f))) return;
			
			//Throws Rejuvenation on all members below 90%
			for (int i =0; i <= HPSort.Count; i++) 
			{
				//Removes Players that are out of Range or Dead
				PlayerObject HealingTarget = HPSort[i];
				if(HealingTarget.IsDead == false && HealingTarget.IsInCombatRangeAndLoS) {
					Cast("Rejuvenation", () => HealingTarget.HealthFraction < (HealingPercent/ 100f) && !HealingTarget.HasAura("Rejuvenation"));
					Cast("Cenarion Ward", () => HealingTarget.HealthFraction <(HealingPercent/ 100f));
				}
			}
			//</Healing Section!!!!>
			
			
			
			// Interrupt using Mighty Bash
			if (Cast("Mighty Bash", () => Target.CanParticipateInCombat && Target.IsCastingAndInterruptible())) return;
			if (hasCatForm())
			{
				//interrupt wtih Wild Charge and Skull Bash
				Cast("Skull Bash", () => Target.IsCastingAndInterruptible());
				Cast("Wild Charge", () => Target.IsCastingAndInterruptible());
				Cast("Rake", () => Target.IsCasting && IsStealthy() );
				Cast("Maim", () => Target.IsCasting && Me.ComboPoints >= 3);

				//Try and prevent Rogues and Priests from going invisible
				if (Cast("Faerie Swarm", () => Target.IsPlayer && Target.Class == WoWClass.Rogue && !Target.HasAura("Faerie Swarm"))) return;
				if (Cast("Faerie Swarm", () => Target.IsPlayer && Target.Class == WoWClass.Priest && !Target.HasAura("Faerie Swarm"))) return;
		

				if (CastSelf("Tiger's Fury", () => Me.GetPower(WoWPowerType.Energy) <=50))
					CastSelf("Berserk", () => Me.HpLessThanOrElite(0.6) && !PvPSettings);

				if (Cast("Wild Charge", () => Target.HealthFraction > 0.25 || Me.HealthFraction < 0.4 || Target.IsElite() || Target.IsCastingAndInterruptible())) return;
				if (Cast("Typhoon", () => Target.IsInCombatRange && Me.HealthFraction < 0.5)) return;

				if (Cast("Savage Roar", () => Me.GetPower(WoWPowerType.Energy) >= 25 && !HasAura("Savage Roar") && (Me.ComboPoints >= 4))) return;
				if (Cast("Rip", () => Me.GetPower(WoWPowerType.Energy) >= 30 && !Target.HasAura("Rip", true) && Me.ComboPoints >= 4)) return;
				if (Cast("Ferocious Bite", () => Me.GetPower(WoWPowerType.Energy) >= 25 && Me.ComboPoints >= 4)) return;

				if (Cast("Thrash", () => HasAura("Clearcasting") && !Target.HasAura("Thrash", true))) return;

				if (Cast("Rake", () => Me.GetPower(WoWPowerType.Energy) >= 35 && !Target.HasAura("Rake", true) && rakeDelay.IsReady)) return;

				if (Adds.Count > 2 && Adds.Count(x => x.DistanceSquared < 8 * 8) > 2)
				{
					if (Cast("Berserk", () => Adds.Count >3 )) return;
					if (Cast("Swipe", () => Me.GetPower(WoWPowerType.Energy) >= 45 || HasAura("Clearcasting"))) return;
					if (Adds.Count > 10 && Adds.Count(x => x.DistanceSquared < 8*8) > 10)
					return; // only do swipe
				    
				}
				if (Cast("Shred", () => Me.GetPower(WoWPowerType.Energy) >= 40 || HasAura("Clearcasting"))) return;
			}
			else
			{
				if (CastSelf("Rejuvenation", () => Me.HealthFraction <= 0.75 && !HasAura("Rejuvenation"))) return;
				if (Cast("Moonfire", () => Target.HealthFraction <= 0.1 && !Target.IsElite())) return;
				CastCatForm();
			}
		}
	}
}