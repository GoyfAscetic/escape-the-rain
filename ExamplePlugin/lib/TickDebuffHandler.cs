using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace ExamplePlugin
{
    public class TickDebuffHandler : DebuffHandler
    {
        public Dictionary<uint, List<DotController.DotIndex>> TickDebuffCache;

        public TickDebuffHandler()
        {
            TickDebuffCache = new Dictionary<uint, List<DotController.DotIndex>>();
        }

        public bool IsTickDamageEvent(DamageReportHandler damageReportHandler)
        {
            CharacterBody victim = damageReportHandler.Victim;
            EnsureIsCachedAndAlive(victim);

            if (!TickDebuffCache.ContainsKey(GetVictimNetworkUserId(victim)))
                return false;

            switch (damageReportHandler.TickingDebuffIndex)
            {
                case (DotController.DotIndex.Burn):
                    if (!victim.HasBuff(RoR2Content.Buffs.OnFire) && IsCached(victim, DotController.DotIndex.Burn))
                    {
                        RemoveDebuff(victim, DotController.DotIndex.Burn);
                        return false;
                    }
                    return CheckNew(victim, DotController.DotIndex.Burn);
                case (DotController.DotIndex.PercentBurn):
                    if (!victim.HasBuff(RoR2Content.Buffs.OnFire) && IsCached(victim, DotController.DotIndex.PercentBurn))
                    {
                        RemoveDebuff(victim, DotController.DotIndex.PercentBurn);
                        return false;
                    }
                    return CheckNew(victim, DotController.DotIndex.PercentBurn);
                case (DotController.DotIndex.Bleed):
                    if (!victim.HasBuff(RoR2Content.Buffs.Bleeding) && IsCached(victim, DotController.DotIndex.Bleed))
                    {
                        RemoveDebuff(victim, DotController.DotIndex.Bleed);
                        return false;
                    }
                    return CheckNew(victim, DotController.DotIndex.Bleed);
                case (DotController.DotIndex.Blight):
                    if (!victim.HasBuff(RoR2Content.Buffs.Blight) && IsCached(victim, DotController.DotIndex.Blight))
                    {
                        RemoveDebuff(victim, DotController.DotIndex.Blight);
                        return false;
                    }
                    return CheckNew(victim, DotController.DotIndex.Blight);
                case (DotController.DotIndex.Poison):
                    if (!victim.HasBuff(RoR2Content.Buffs.Poisoned) && IsCached(victim, DotController.DotIndex.Poison))
                    {
                        RemoveDebuff(victim, DotController.DotIndex.Poison);
                        return false;
                    }
                    return CheckNew(victim, DotController.DotIndex.Poison);
                default:
                    return false;
            }
        }

        public string ComposeLabel(DotController.DotIndex debuffIndex)
        {
            switch (debuffIndex)
            {
                case (DotController.DotIndex.Bleed):
                    return $"<color=#8A0303>Bleeding</color>";
                case (DotController.DotIndex.Burn):
                case (DotController.DotIndex.PercentBurn):
                    return $"<color=#E25822>Burning</color>";
                case (DotController.DotIndex.Blight):
                    return $"<color=#A13072>Blighted</color>";
                case (DotController.DotIndex.Poison):
                    return $"<color=#C9F24D>Poisoned</color>";
                default:
                    return null;
            }
        }

        private bool CheckNew(CharacterBody victim, DotController.DotIndex debuffIndex)
        {
            if (IsCached(victim, debuffIndex))
                return false;

            CacheDebuff(victim, debuffIndex);
            return true;
        }

        private bool IsCached(CharacterBody victim, DotController.DotIndex debuffIndex)
        {
            return TickDebuffCache[GetVictimNetworkUserId(victim)].IndexOf(debuffIndex) != -1;
        }

        private void CacheDebuff(CharacterBody victim, DotController.DotIndex debuffIndex)
        {
            int listIdx = TickDebuffCache[GetVictimNetworkUserId(victim)].IndexOf(debuffIndex);

            if (listIdx != -1)
                return;

            TickDebuffCache[GetVictimNetworkUserId(victim)].Add(debuffIndex);
        }

        private void RemoveDebuff(CharacterBody victim, DotController.DotIndex debuffIndex)
        {
            int listIdx = TickDebuffCache[GetVictimNetworkUserId(victim)].IndexOf(debuffIndex);

            if (listIdx == -1)
                return;

            TickDebuffCache[GetVictimNetworkUserId(victim)].RemoveAt(listIdx);
        }

        public override void EnsureIsCachedAndAlive(CharacterBody victim)
        {
            if (victim.isActiveAndEnabled)
            {
                if (!TickDebuffCache.ContainsKey(GetVictimNetworkUserId(victim)))
                    TickDebuffCache.Add(GetVictimNetworkUserId(victim), new List<DotController.DotIndex>());
            }
            else
            {
                TickDebuffCache.Remove(GetVictimNetworkUserId(victim));
            }
        }

        public override void ClearCache() => TickDebuffCache.Clear();
    }
}
