using RoR2;
using UnityEngine;

namespace ExamplePlugin
{
    public class DamageReportHandler
    {
        public CharacterBody Victim { get; }
        public CharacterBody Attacker { get; }
        public string AttackerName { get; }
        public DotController.DotIndex TickingDebuffIndex { get; }
        public TeamIndex VictimTeamIndex { get; }
        public TeamIndex AttackerTeamIndex { get; }
        public DamageType DamageTypeIndex { get; }
        public int Damage { get; }

        public DamageReportHandler(DamageReport damageReport)
        {
            Victim = damageReport.victimBody;
            Attacker = damageReport.attackerBody;
            if (damageReport.attacker != null)
                AttackerName = damageReport.attacker.name;
            else
                AttackerName = "";
            Damage = (int)Mathf.Round(damageReport.damageDealt);
            TickingDebuffIndex = damageReport.damageInfo.dotIndex;
            VictimTeamIndex = damageReport.victimTeamIndex;
            AttackerTeamIndex = damageReport.attackerTeamIndex;
            DamageTypeIndex = damageReport.damageInfo.damageType;
        }

        public bool VictimIsInPlayerTeam()
        {
            return VictimTeamIndex == TeamIndex.Player;
        }

        public bool AttackIsFriendlyFire()
        {
            return AttackerTeamIndex == VictimTeamIndex && AttackerTeamIndex > TeamIndex.None;
        }
        
        public bool DamageIsAboveHitPointsThreshold(int hitPointsPercentage)
        {
            return (int)((Victim.maxHealth / 100 * hitPointsPercentage) - Damage) <= 0;
        }

        public bool CheckIfDamageBroadcast(int hitPointsPercentage)
        {
            return Damage > 0 && DamageIsAboveHitPointsThreshold(hitPointsPercentage)
                && Attacker != null;
        }

        public bool CheckIfFallDamageBroadcast(int hitPointsPercentage)
        {
            Inventory inventory = Victim.inventory;
            return DamageIsAboveHitPointsThreshold(hitPointsPercentage)
                && (inventory ? inventory.GetItemCount(RoR2Content.Items.FallBoots) : 0) <= 0
                && (Victim.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage) == CharacterBody.BodyFlags.None
                && (DamageTypeIndex & DamageType.FallDamage) > DamageType.Generic;
        }

        public bool CheckIfShrineBloodDamageBroadcast(int hitPointsPercentage)
        {
            return DamageIsAboveHitPointsThreshold(hitPointsPercentage)
                && (DamageTypeIndex & DamageType.BypassArmor) > DamageType.Generic
                && AttackerName.Contains("ShrineBlood");
        }
    }
}