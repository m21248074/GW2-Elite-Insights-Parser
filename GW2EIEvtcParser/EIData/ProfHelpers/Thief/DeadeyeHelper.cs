﻿using System.Collections.Generic;
using System.Linq;
using GW2EIEvtcParser.ParsedData;
using GW2EIEvtcParser.ParserHelpers;
using static GW2EIEvtcParser.ArcDPSEnums;
using static GW2EIEvtcParser.EIData.Buff;
using static GW2EIEvtcParser.EIData.DamageModifiersUtils;
using static GW2EIEvtcParser.ParserHelper;
using static GW2EIEvtcParser.SkillIDs;

namespace GW2EIEvtcParser.EIData
{
    internal static class DeadeyeHelper
    {

        internal static readonly List<InstantCastFinder> InstantCastFinder = new List<InstantCastFinder>()
        {
            new EffectCastFinderByDst(Mercy, EffectGUIDs.DeadeyeMercy)
                .UsingDstSpecChecker(Spec.Deadeye), // Needs more testing to check for collisions
        };

        internal static readonly List<DamageModifierDescriptor> OutgoingDamageModifiers = new List<DamageModifierDescriptor>
        {
            new BuffOnActorDamageModifier(NumberOfBoons, "Premeditation", "1% per boon",DamageSource.NoPets, 1.0, DamageType.Strike, DamageType.All, Source.Deadeye, ByStack, BuffImages.Premeditation, DamageModifierMode.All).WithBuilds(GW2Builds.StartOfLife, GW2Builds.August2022Balance),
            new BuffOnActorDamageModifier(NumberOfBoons, "Premeditation", "1% per boon",DamageSource.NoPets, 1.0, DamageType.Strike, DamageType.All, Source.Deadeye, ByStack, BuffImages.Premeditation, DamageModifierMode.sPvPWvW).WithBuilds(GW2Builds.August2022Balance, GW2Builds.July2023BalanceAndSilentSurfCM),
            new BuffOnActorDamageModifier(NumberOfBoons, "Premeditation", "1.5% per boon",DamageSource.NoPets, 1.5, DamageType.Strike, DamageType.All, Source.Deadeye, ByStack, BuffImages.Premeditation, DamageModifierMode.PvE).WithBuilds(GW2Builds.August2022Balance, GW2Builds.July2023BalanceAndSilentSurfCM),
            new BuffOnActorDamageModifier(NumberOfBoons, "Premeditation", "1% per boon",DamageSource.NoPets, 1.0, DamageType.Strike, DamageType.All, Source.Deadeye, ByStack, BuffImages.Premeditation, DamageModifierMode.All).WithBuilds( GW2Builds.July2023BalanceAndSilentSurfCM),
            //
            new BuffOnActorDamageModifier(DeadeyesGaze, "Iron Sight", "10% to marked target", DamageSource.NoPets, 10.0, DamageType.Strike, DamageType.All, Source.Deadeye, ByPresence, BuffImages.IronSight, DamageModifierMode.All).UsingChecker((x, log) => {
                AbstractBuffEvent effectApply = log.CombatData.GetBuffDataByIDByDst(DeadeyesGaze, x.From).Where(y => y is BuffApplyEvent).LastOrDefault(y => y.Time <= x.Time);
                if (effectApply != null)
                {
                    return x.To == effectApply.By.GetMainAgentWhenAttackTarget(log, x.Time);
                }
                return false;
            }).WithBuilds(GW2Builds.StartOfLife, GW2Builds.August2022Balance),
            new BuffOnActorDamageModifier(DeadeyesGaze, "Iron Sight", "10% to marked target", DamageSource.NoPets, 10.0, DamageType.Strike, DamageType.All, Source.Deadeye, ByPresence, BuffImages.IronSight, DamageModifierMode.sPvPWvW).UsingChecker((x, log) => {
                AbstractBuffEvent effectApply = log.CombatData.GetBuffDataByIDByDst(DeadeyesGaze, x.From).Where(y => y is BuffApplyEvent).LastOrDefault(y => y.Time <= x.Time);
                if (effectApply != null)
                {
                    return x.To == effectApply.By.GetMainAgentWhenAttackTarget(log, x.Time);
                }
                return false;
            }).WithBuilds(GW2Builds.August2022Balance, GW2Builds.July2023BalanceAndSilentSurfCM),
            new BuffOnActorDamageModifier(DeadeyesGaze, "Iron Sight", "15% to marked target", DamageSource.NoPets, 15.0, DamageType.Strike, DamageType.All, Source.Deadeye, ByPresence, BuffImages.IronSight, DamageModifierMode.PvE).UsingChecker((x, log) => {
                AbstractBuffEvent effectApply = log.CombatData.GetBuffDataByIDByDst(DeadeyesGaze, x.From).Where(y => y is BuffApplyEvent).LastOrDefault(y => y.Time <= x.Time);
                if (effectApply != null)
                {
                    return x.To == effectApply.By.GetMainAgentWhenAttackTarget(log, x.Time);
                }
                return false;
            }).WithBuilds(GW2Builds.August2022Balance, GW2Builds.July2023BalanceAndSilentSurfCM),

            new BuffOnActorDamageModifier(DeadeyesGaze, "Iron Sight", "10% to marked target", DamageSource.NoPets, 10.0, DamageType.Strike, DamageType.All, Source.Deadeye, ByPresence, BuffImages.IronSight, DamageModifierMode.All).UsingChecker((x, log) => {
                AbstractBuffEvent effectApply = log.CombatData.GetBuffDataByIDByDst(DeadeyesGaze, x.From).Where(y => y is BuffApplyEvent).LastOrDefault(y => y.Time <= x.Time);
                if (effectApply != null)
                {
                    return x.To == effectApply.By.GetMainAgentWhenAttackTarget(log, x.Time);
                }
                return false;
            }).WithBuilds(GW2Builds.July2023BalanceAndSilentSurfCM),
        };

        internal static readonly List<DamageModifierDescriptor> IncomingDamageModifiers = new List<DamageModifierDescriptor>
        {
            new BuffOnActorDamageModifier(DeadeyesGaze, "Iron Sight", "-10% from marked target", DamageSource.NoPets, -10.0, DamageType.Strike, DamageType.All, Source.Deadeye, ByPresence, BuffImages.IronSight, DamageModifierMode.All).UsingChecker((x, log) => {
                AbstractBuffEvent effectApply = log.CombatData.GetBuffDataByIDByDst(DeadeyesGaze, x.To).Where(y => y is BuffApplyEvent).LastOrDefault(y => y.Time <= x.Time);
                if (effectApply != null)
                {
                    return x.From == effectApply.By.GetMainAgentWhenAttackTarget(log, x.Time);
                }
                return false;
            }).WithBuilds(GW2Builds.StartOfLife, GW2Builds.August2022Balance),
            new BuffOnActorDamageModifier(DeadeyesGaze, "Iron Sight", "-10% from marked target", DamageSource.NoPets, -10.0, DamageType.Strike, DamageType.All, Source.Deadeye, ByPresence, BuffImages.IronSight, DamageModifierMode.sPvPWvW).UsingChecker((x, log) => {
                AbstractBuffEvent effectApply = log.CombatData.GetBuffDataByIDByDst(DeadeyesGaze, x.To).Where(y => y is BuffApplyEvent).LastOrDefault(y => y.Time <= x.Time);
                if (effectApply != null)
                {
                    return x.From == effectApply.By.GetMainAgentWhenAttackTarget(log, x.Time);
                }
                return false;
            }).WithBuilds(GW2Builds.August2022Balance),
            new BuffOnActorDamageModifier(DeadeyesGaze, "Iron Sight", "-15% from marked target", DamageSource.NoPets, -15.0, DamageType.Strike, DamageType.All, Source.Deadeye, ByPresence, BuffImages.IronSight, DamageModifierMode.PvE).UsingChecker((x, log) => {
                AbstractBuffEvent effectApply = log.CombatData.GetBuffDataByIDByDst(DeadeyesGaze, x.To).Where(y => y is BuffApplyEvent).LastOrDefault(y => y.Time <= x.Time);
                if (effectApply != null)
                {
                    return x.From == effectApply.By.GetMainAgentWhenAttackTarget(log, x.Time);
                }
                return false;
            }).WithBuilds(GW2Builds.August2022Balance),
        };


        internal static readonly List<Buff> Buffs = new List<Buff>
        {
            new Buff("Kneeling", Kneeling, Source.Deadeye, BuffClassification.Other, BuffImages.Kneel).WithBuilds(GW2Builds.StartOfLife, GW2Builds.SOTOBetaAndSilentSurfNM),
            new Buff("Deadeye's Gaze", DeadeyesGaze, Source.Deadeye, BuffClassification.Other, BuffImages.DeadeyesMark),
        };

        private static HashSet<int> Minions = new HashSet<int>()
        {
            (int)MinionID.DeadeyeSylvari1,
            (int)MinionID.DeadeyeHuman1,
            (int)MinionID.DeadeyeCharr1,
            (int)MinionID.DeadeyeSylvari2,
            (int)MinionID.DeadeyeAsura1,
            (int)MinionID.DeadeyeNorn1,
            (int)MinionID.DeadeyeNorn2,
            (int)MinionID.DeadeyeHuman2,
            (int)MinionID.DeadeyeCharr2,
            (int)MinionID.DeadeyeAsura2,
        };
        internal static bool IsKnownMinionID(int id)
        {
            return Minions.Contains(id);
        }

    }
}
