﻿using System.Collections.Generic;
using System.Linq;
using GW2EIEvtcParser.ParsedData;

namespace GW2EIEvtcParser.EIData
{
    public class FinalOffensiveStats
    {
        public int TotalDamageCount { get; }
        public int TotalDmg { get; }
        public int DirectDamageCount { get; }
        public int DirectDmg { get; }
        public int ConnectedDamageCount { get; }
        public int ConnectedDmg { get; }
        public int ConnectedDirectDamageCount { get; }
        public int ConnectedDirectDmg { get; }
        public int CritableDirectDamageCount { get; }
        public int CriticalCount { get; }
        public int CriticalDmg { get; }
        public int FlankingCount { get; }
        public int GlanceCount { get; }
        public int AgainstMovingCount { get; }
        public int Missed { get; }
        public int Blocked { get; }
        public int Evaded { get; }
        public int Interrupts { get; }
        public int Invulned { get; }
        public int Killed { get; }
        public int Downed { get; }

        public int AgainstDownedCount { get; }
        public int AgainstDownedDamage { get; }

        public int ConnectedPowerCount { get; }
        public int ConnectedPowerAbove90HPCount { get; }
        public int ConnectedConditionCount { get; }
        public int ConnectedConditionAbove90HPCount { get; }

        public int DownContribution { get; }

        public int AppliedCrowdControl { get; }
        public double AppliedCrowdControlDuration { get; }


        internal FinalOffensiveStats(ParsedEvtcLog log, long start, long end, AbstractSingleActor actor, AbstractSingleActor target)
        {
            IReadOnlyList<AbstractHealthDamageEvent> dls = actor.GetDamageEvents(target, log, start, end);
            foreach (AbstractHealthDamageEvent dl in dls)
            {
                if (dl.From == actor.AgentItem)
                {
                    if (!(dl is NonDirectHealthDamageEvent))
                    {
                        if (dl.HasHit)
                        {
                            if (SkillItem.CanCrit(dl.SkillId, log.LogData.GW2Build))
                            {
                                if (dl.HasCrit)
                                {
                                    CriticalCount++;
                                    CriticalDmg += dl.HealthDamage;
                                }
                                CritableDirectDamageCount++;
                            }
                            if (dl.IsFlanking)
                            {
                                FlankingCount++;
                            }

                            if (dl.HasGlanced)
                            {
                                GlanceCount++;
                            }
                            ConnectedDirectDamageCount++;
                            ConnectedDirectDmg += dl.HealthDamage;
                        }

                        if (dl.IsBlind)
                        {
                            Missed++;
                        }
                        if (dl.IsEvaded)
                        {
                            Evaded++;
                        }
                        if (dl.IsBlocked)
                        {
                            Blocked++;
                        }
                        if (!dl.DoubleProcHit)
                        {
                            DirectDamageCount++;
                            DirectDmg += dl.HealthDamage;
                        }
                    }
                    if (dl.IsAbsorbed)
                    {
                        Invulned++;
                    }
                    if (!dl.DoubleProcHit)
                    {
                        TotalDamageCount++;
                        TotalDmg += dl.HealthDamage;
                    }

                    if (dl.HasHit)
                    {
                        ConnectedDamageCount++;
                        ConnectedDmg += dl.HealthDamage;
                        // Derive down contribution from health updates as they are available after this build
                        if (log.LogData.EvtcBuild < ArcDPSEnums.ArcDPSBuilds.Last90BeforeDownRetired)
                        {
                            IReadOnlyList<Last90BeforeDownEvent> last90BeforeDownEvents = log.CombatData.GetLast90BeforeDownEvents(dl.To);
                            if (last90BeforeDownEvents.Any(x => dl.Time <= x.Time && dl.Time >= x.Time - x.TimeSinceLast90))
                            {
                                DownContribution += dl.HealthDamage;
                            }
                        }
                        else
                        {
                            if (dl.To.IsDownedBeforeNext90(log, dl.Time))
                            {
                                DownContribution += dl.HealthDamage;
                            }
                        }
                        if (dl.AgainstMoving)
                        {
                            AgainstMovingCount++;
                        }
                        if (dl.ConditionDamageBased(log))
                        {
                            ConnectedConditionCount++;
                            if (dl.IsOverNinety)
                            {
                                ConnectedConditionAbove90HPCount++;
                            }
                        }
                        else
                        {
                            ConnectedPowerCount++;
                            if (dl.IsOverNinety)
                            {
                                ConnectedPowerAbove90HPCount++;
                            }
                        }
                        if (dl.AgainstDowned)
                        {
                            AgainstDownedCount++;
                            AgainstDownedDamage += dl.HealthDamage;
                        }
                    }
                }

                if (!(dl is NonDirectHealthDamageEvent))
                {
                    if (dl.HasInterrupted)
                    {
                        Interrupts++;
                    }
                }
                if (dl.HasKilled)
                {
                    Killed++;
                }
                if (dl.HasDowned)
                {
                    Downed++;
                }
            }
            IReadOnlyList<CrowdControlEvent> ccs = actor.GetOutgoingCrowdControlEvents(target, log, start, end);
            foreach (CrowdControlEvent cc in ccs)
            {
                AppliedCrowdControl++;
                AppliedCrowdControlDuration += cc.Duration;
            }
        }
    }
}
