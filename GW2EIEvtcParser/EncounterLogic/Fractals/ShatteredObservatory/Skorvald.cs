﻿using System;
using System.Collections.Generic;
using System.Linq;
using GW2EIEvtcParser.EIData;
using GW2EIEvtcParser.Exceptions;
using GW2EIEvtcParser.Extensions;
using GW2EIEvtcParser.ParsedData;
using static GW2EIEvtcParser.ParserHelper;
using static GW2EIEvtcParser.SkillIDs;
using static GW2EIEvtcParser.EncounterLogic.EncounterLogicUtils;
using static GW2EIEvtcParser.EncounterLogic.EncounterLogicPhaseUtils;
using static GW2EIEvtcParser.EncounterLogic.EncounterLogicTimeUtils;
using static GW2EIEvtcParser.EncounterLogic.EncounterImages;

namespace GW2EIEvtcParser.EncounterLogic
{
    internal class Skorvald : ShatteredObservatory
    {
        public Skorvald(int triggerID) : base(triggerID)
        {
            MechanicList.AddRange(new List<Mechanic>
            {
            new PlayerDstHitMechanic(new long[]{ CombustionRush1, CombustionRush2, CombustionRush3 }, "Combustion Rush", new MechanicPlotlySetting(Symbols.TriangleLeft,Colors.Magenta), "Charge","Combustion Rush", "Charge",0),
            new PlayerDstHitMechanic(new long[] { PunishingKickAnomaly, PunishingKickSkorvald }, "Punishing Kick", new MechanicPlotlySetting(Symbols.TriangleRightOpen,Colors.Magenta), "Add Kick","Punishing Kick (Single purple Line, Add)", "Kick (Add)",0),
            new PlayerDstHitMechanic(new long[] { CranialCascadeAnomaly,CranialCascade2 }, "Cranial Cascade", new MechanicPlotlySetting(Symbols.TriangleRightOpen,Colors.Yellow), "Add Cone KB","Cranial Cascade (3 purple Line Knockback, Add)", "Small Cone KB (Add)",0),
            new PlayerDstHitMechanic(new long[] { RadiantFurySkorvald, RadiantFury2 }, "Radiant Fury", new MechanicPlotlySetting(Symbols.Octagon,Colors.Red), "Burn Circle","Radiant Fury (expanding burn circles)", "Expanding Circles",0),
            new PlayerDstHitMechanic(FocusedAnger, "Focused Anger", new MechanicPlotlySetting(Symbols.TriangleDown,Colors.Orange), "Large Cone KB","Focused Anger (Large Cone Overhead Crosshair Knockback)", "Large Cone Knockback",0),
            new PlayerDstHitMechanic(new long[] { HorizonStrikeSkorvald1, HorizonStrikeSkorvald2 }, "Horizon Strike", new MechanicPlotlySetting(Symbols.Circle,Colors.LightOrange), "Horizon Strike","Horizon Strike (turning pizza slices)", "Horizon Strike",0), // 
            new PlayerDstHitMechanic(CrimsonDawn, "Crimson Dawn", new MechanicPlotlySetting(Symbols.Circle,Colors.DarkRed), "Horizon Strike End","Crimson Dawn (almost Full platform attack after Horizon Strike)", "Horizon Strike (last)",0),
            new PlayerDstHitMechanic(SolarCyclone, "Solar Cyclone", new MechanicPlotlySetting(Symbols.AsteriskOpen,Colors.DarkMagenta), "Cyclone","Solar Cyclone (Circling Knockback)", "KB Cyclone",0),
            new PlayerDstBuffApplyMechanic(Fear, "Fear", new MechanicPlotlySetting(Symbols.SquareOpen,Colors.Red), "Eye","Hit by the Overhead Eye Fear", "Eye (Fear)",0, (ba, log) => ba.AppliedDuration == 3000), //not triggered under stab, still get blinded/damaged, seperate tracking desired?
            new PlayerDstBuffApplyMechanic(FixatedBloom1, "Fixate", new MechanicPlotlySetting(Symbols.StarOpen,Colors.Magenta), "Bloom Fix","Fixated by Solar Bloom", "Bloom Fixate",0),
            new PlayerDstHitMechanic(BloomExplode, "Explode", new MechanicPlotlySetting(Symbols.Circle,Colors.Yellow), "Bloom Expl","Hit by Solar Bloom Explosion", "Bloom Explosion",0), //shockwave, not damage? (damage is 50% max HP, not tracked)
            new PlayerDstHitMechanic(SpiralStrike, "Spiral Strike", new MechanicPlotlySetting(Symbols.CircleOpen,Colors.DarkGreen), "Spiral","Hit after Warp (Jump to Player with overhead bomb)", "Spiral Strike",0),
            new PlayerDstHitMechanic(WaveOfMutilation, "Wave of Mutilation", new MechanicPlotlySetting(Symbols.TriangleSW,Colors.DarkGreen), "KB Jump","Hit by KB Jump (player targeted)", "Knockback jump",0),
            new PlayerDstBuffApplyMechanic(SkorvaldsIre, "Skorvald Fixate", new MechanicPlotlySetting(Symbols.CircleOpenDot, Colors.Purple), "Skor Fixate", "Fixated by Skorvald's Ire",  "Skorvald's Fixate", 0),
            });
            Extension = "skorv";
            Icon = EncounterIconSkorvald;
            EncounterCategoryInformation.InSubCategoryOrder = 0;
            EncounterID |= 0x000001;
        }

        protected override CombatReplayMap GetCombatMapInternal(ParsedEvtcLog log)
        {
            return new CombatReplayMap(CombatReplaySkorvald,
                            (987, 1000),
                            (-22267, 14955, -17227, 20735)/*,
                            (-24576, -24576, 24576, 24576),
                            (11204, 4414, 13252, 6462)*/);
        }

        internal override List<PhaseData> GetPhases(ParsedEvtcLog log, bool requirePhases)
        {
            // generic method for fractals
            List<PhaseData> phases = GetInitialPhase(log);
            AbstractSingleActor skorvald = Targets.FirstOrDefault(x => x.IsSpecies(ArcDPSEnums.TargetID.Skorvald));
            if (skorvald == null)
            {
                throw new MissingKeyActorsException("Skorvald not found");
            }
            phases[0].AddTarget(skorvald);
            if (!requirePhases)
            {
                return phases;
            }
            phases.AddRange(GetPhasesByInvul(log, Determined762, skorvald, true, true));
            for (int i = 1; i < phases.Count; i++)
            {
                PhaseData phase = phases[i];
                if (i % 2 == 0)
                {
                    phase.Name = "Split " + (i) / 2;
                    var ids = new List<int>
                    {
                        (int)ArcDPSEnums.TrashID.FluxAnomaly1,
                        (int)ArcDPSEnums.TrashID.FluxAnomaly2,
                        (int)ArcDPSEnums.TrashID.FluxAnomaly3,
                        (int)ArcDPSEnums.TrashID.FluxAnomaly4,
                    };
                    AddTargetsToPhaseAndFit(phase, ids, log);

                    // add anomaly numbers
                    int offset = 4 * (i / 2 - 1);
                    foreach (NPC target in phase.Targets)
                    {
                        switch (target.ID) {
                            case (int) ArcDPSEnums.TrashID.FluxAnomaly1:
                                target.OverrideName(target.Character + " " + (1 + offset));
                                break;
                            case (int) ArcDPSEnums.TrashID.FluxAnomaly2:
                                target.OverrideName(target.Character + " " + (2 + offset));
                                break;
                            case (int) ArcDPSEnums.TrashID.FluxAnomaly3:
                                target.OverrideName(target.Character + " " + (3 + offset));
                                break;
                            case (int) ArcDPSEnums.TrashID.FluxAnomaly4:
                                target.OverrideName(target.Character + " " + (4 + offset));
                                break;
                        } 
                    }
                }
                else
                {
                    phase.Name = "Phase " + (i + 1) / 2;
                    phase.AddTarget(skorvald);
                }
            }
            return phases;
        }

        internal override void EIEvtcParse(ulong gw2Build, FightData fightData, AgentData agentData, List<CombatItem> combatData, IReadOnlyDictionary<uint, AbstractExtensionHandler> extensions)
        {
            base.EIEvtcParse(gw2Build, fightData, agentData, combatData, extensions);
            AbstractSingleActor skorvald = Targets.FirstOrDefault(x => x.IsSpecies(ArcDPSEnums.TargetID.Skorvald));
            if (skorvald == null)
            {
                throw new MissingKeyActorsException("Skorvald not found");
            }
            skorvald.OverrideName("Skorvald");
        }

        internal override long GetFightOffset(FightData fightData, AgentData agentData, List<CombatItem> combatData)
        {
            CombatItem logStartNPCUpdate = combatData.FirstOrDefault(x => x.IsStateChange == ArcDPSEnums.StateChange.LogStartNPCUpdate);
            if (logStartNPCUpdate != null)
            {
                AgentItem skorvald = agentData.GetNPCsByID(ArcDPSEnums.TargetID.Skorvald).FirstOrDefault();
                if (skorvald == null)
                {
                    throw new MissingKeyActorsException("Skorvald not found");
                }
                // Skorvald may spawns with 0% hp
                CombatItem firstNonZeroHPUpdate = combatData.FirstOrDefault(x => x.IsStateChange == ArcDPSEnums.StateChange.HealthUpdate && x.SrcMatchesAgent(skorvald) && x.DstAgent > 0);
                CombatItem enterCombat = combatData.FirstOrDefault(x => x.IsStateChange == ArcDPSEnums.StateChange.EnterCombat && x.SrcMatchesAgent(skorvald) && x.Time <= logStartNPCUpdate.Time + ParserHelper.ServerDelayConstant);
                return firstNonZeroHPUpdate != null ? Math.Min(firstNonZeroHPUpdate.Time, enterCombat != null ? enterCombat.Time : long.MaxValue) : GetGenericFightOffset(fightData);
            }
            return GetGenericFightOffset(fightData);
        }

        internal override FightData.EncounterMode GetEncounterMode(CombatData combatData, AgentData agentData, FightData fightData)
        {
            AbstractSingleActor target = Targets.FirstOrDefault(x => x.IsSpecies(ArcDPSEnums.TargetID.Skorvald));
            if (target == null)
            {
                throw new MissingKeyActorsException("Skorvald not found");
            }
            if (combatData.GetBuildEvent().Build >= 106277)
            {
                // Agent check not reliable, produces false positives and regular false negatives
                /*if (agentData.GetNPCsByID(16725).Any() && agentData.GetNPCsByID(11245).Any())
                {
                    return FightData.CMStatus.CM;
                }*/
                // Check some CM skills instead, not perfect but helps, 
                // Solar Bolt is the first thing he tries to cast, that looks very consistent
                // If the phase 1 is super fast to the point skorvald does not cast anything, supernova should be there
                // Otherwise we are looking at a super fast phase 1 (< 7 secondes) where the team ggs just before supernova
                // Joining the encounter mid fight may also yield a false negative but at that point the log is incomplete already
                var cmSkills = new HashSet<long>
                {
                    SolarBoltCM,
                    SupernovaCM,
                };
                if (combatData.GetSkills().Intersect(cmSkills).Any())
                {
                    return FightData.EncounterMode.CM;
                }
                return FightData.EncounterMode.Normal;
            }
            else
            {
                return (target.GetHealth(combatData) == 5551340) ? FightData.EncounterMode.CM : FightData.EncounterMode.Normal;
            }
        }

        protected override List<int> GetTargetsIDs()
        {
            return new List<int>()
            {
                (int)ArcDPSEnums.TargetID.Skorvald,
                (int)ArcDPSEnums.TrashID.FluxAnomaly4,
                (int)ArcDPSEnums.TrashID.FluxAnomaly3,
                (int)ArcDPSEnums.TrashID.FluxAnomaly2,
                (int)ArcDPSEnums.TrashID.FluxAnomaly1,
            };
        }

        internal override void CheckSuccess(CombatData combatData, AgentData agentData, FightData fightData, IReadOnlyCollection<AgentItem> playerAgents)
        {
            base.CheckSuccess(combatData, agentData, fightData, playerAgents);
            // reward or death worked
            if (fightData.Success)
            {
                return;
            }
            AbstractSingleActor skorvald = Targets.FirstOrDefault(x => x.IsSpecies(ArcDPSEnums.TargetID.Skorvald));
            if (skorvald == null)
            {
                throw new MissingKeyActorsException("Skorvald not found");
            }
            AbstractHealthDamageEvent lastDamageTaken = combatData.GetDamageTakenData(skorvald.AgentItem).LastOrDefault(x => (x.HealthDamage > 0) && playerAgents.Contains(x.From.GetFinalMaster()));
            if (lastDamageTaken != null)
            {
                BuffApplyEvent invul895Apply = combatData.GetBuffData(Determined895).OfType<BuffApplyEvent>().Where(x => x.To == skorvald.AgentItem && x.Time > lastDamageTaken.Time - 500).LastOrDefault();
                if (invul895Apply != null)
                {
                    fightData.SetSuccess(true, Math.Min(invul895Apply.Time, lastDamageTaken.Time));
                }
            }
        }

        protected override List<ArcDPSEnums.TrashID> GetTrashMobsIDs()
        {
            return new List<ArcDPSEnums.TrashID>
            {
                ArcDPSEnums.TrashID.SolarBloom
            };
        }

        internal override void ComputeNPCCombatReplayActors(NPC target, ParsedEvtcLog log, CombatReplay replay)
        {
            IReadOnlyList<AbstractCastEvent> casts = target.GetCastEvents(log, log.FightData.FightStart, log.FightData.FightEnd);

            switch (target.ID)
            {
                case (int)ArcDPSEnums.TargetID.Skorvald:
                    // Horizon Strike
                    var horizonStrike = casts.Where(x => x.SkillId == HorizonStrikeSkorvald2 || x.SkillId == HorizonStrikeSkorvald4).ToList();
                    foreach (AbstractCastEvent c in horizonStrike)
                    {
                        int start = (int)c.Time + 100;
                        int duration = 3900;
                        int radius = 1200;
                        int angle = 70;
                        int shiftingAngle = 45;
                        int sliceSpawnInterval = 750;
                        int attackEnd = start + duration;
                        attackEnd = GetAttackEndByStunTime(log, target, c, duration, attackEnd);
                        attackEnd = GetAttackEndByDeterminedTime(log, target, c, duration, attackEnd);

                        Point3D facingDirection = GetFacingPoint3D(replay, c, duration);
                        if (facingDirection == null)
                        {
                            continue;
                        }
                        float degree = RadianToDegreeF(Math.Atan2(facingDirection.Y, facingDirection.X));

                        // Horizon Strike starting at Skorvald's facing point
                        if (c.SkillId == HorizonStrikeSkorvald4)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                AddHorizonStrikeDecoration(replay, target, start, attackEnd, degree, radius, angle);
                                start += sliceSpawnInterval;
                                attackEnd += sliceSpawnInterval;
                                degree -= shiftingAngle;
                            }
                        }
                        // Starting at Skorvald's 90° of facing point
                        if (c.SkillId == HorizonStrikeSkorvald2)
                        {
                            degree -= 90;
                            for (int i = 0; i < 4; i++)
                            {
                                AddHorizonStrikeDecoration(replay, target, start, attackEnd, degree, radius, angle);
                                start += sliceSpawnInterval;
                                attackEnd += sliceSpawnInterval;
                                degree += shiftingAngle;
                            }
                        }
                    }

                    // Crimson Dawn
                    IReadOnlyList<long> skillIds = new List<long>() { CrimsonDawnSkorvaldCM1, CrimsonDawnSkorvaldCM2, CrimsonDawnSkorvaldCM3, CrimsonDawnSkorvaldCM4 };
                    var crimsonDawn = casts.Where(x => skillIds.Contains(x.SkillId)).ToList();
                    foreach (AbstractCastEvent c in crimsonDawn)
                    {
                        int radius = 1200;
                        int angle = 295;
                        int start = (int)c.Time;
                        int duration = 3000;
                        int attackEnd = start + duration;
                        attackEnd = GetAttackEndByStunTime(log, target, c, duration, attackEnd);
                        attackEnd = GetAttackEndByDeterminedTime(log, target, c, duration, attackEnd);

                        Point3D facingDirection = GetFacingPoint3D(replay, c, duration);
                        if (facingDirection == null)
                        {
                            continue;
                        }
                        float degree = RadianToDegreeF(Math.Atan2(facingDirection.Y, facingDirection.X));

                        if (c.SkillId == CrimsonDawnSkorvaldCM2)
                        {
                            degree += 90;
                        }
                        if (c.SkillId == CrimsonDawnSkorvaldCM1)
                        {
                            degree += 270;
                        }
                        replay.Decorations.Add(new PieDecoration(true, 0, radius, degree, angle, (start, attackEnd), "rgba(250, 120, 0, 0.2)", new AgentConnector(target)));
                        replay.Decorations.Add(new PieDecoration(true, 0, radius, degree, angle, (attackEnd, attackEnd + 500), "rgba(255, 0, 0, 0.2)", new AgentConnector(target)));
                    }

                    // Punishing Kick
                    var punishingKick = casts.Where(x => x.SkillId == PunishingKickSkorvald).ToList();
                    foreach (AbstractCastEvent c in punishingKick)
                    {
                        int start = (int)c.Time;
                        int duration = 1850;
                        int translation = 150;
                        int cascadeCount = 4;
                        int attackEnd = start + duration;
                        attackEnd = GetAttackEndByStunTime(log, target, c, duration, attackEnd);
                        attackEnd = GetAttackEndByDeterminedTime(log, target, c, duration, attackEnd);

                        Point3D frontalPoint = GetFacingPoint3D(replay, c, duration);
                        if (frontalPoint == null)
                        {
                            continue;
                        }
                        float rotation = Point3D.GetRotationFromFacing(frontalPoint);

                        // Frontal
                        AddKickIndicatorDecoration(replay, target, start, attackEnd, rotation, translation, cascadeCount);
                    }

                    // Solar Bolt
                    EffectGUIDEvent solarBolt = log.CombatData.GetEffectGUIDEvent(EffectGUIDs.SolarBolt);
                    if (solarBolt != null)
                    {
                        var solarBoltEffects = log.CombatData.GetEffectEventsByEffectID(solarBolt.ContentID).ToList();
                        foreach (EffectEvent solarBoltEffect in solarBoltEffects)
                        {
                            int aoeRadius = 100;
                            int aoeTimeout = 12000;
                            int start = (int)solarBoltEffect.Time;
                            int attackEnd = start + aoeTimeout;
                            replay.Decorations.Add(new CircleDecoration(true, 0, aoeRadius, (start, attackEnd), "rgba(255, 0, 0, 0.2)", new PositionConnector(solarBoltEffect.Position)));
                        }
                    }

                    // Radiant Fury
                    var radiantFury = casts.Where(x => x.SkillId == RadiantFurySkorvald).ToList();
                    foreach (AbstractCastEvent c in radiantFury)
                    {
                        int start = (int)c.Time;
                        int duration = 2700;
                        int expectedHitTime = start + duration;
                        int attackEnd = start + duration;
                        int endWave = attackEnd + 900;
                        attackEnd = GetAttackEndByStunTime(log, target, c, duration, attackEnd);
                        attackEnd = GetAttackEndByDeterminedTime(log, target, c, duration, attackEnd);

                        if (expectedHitTime <= attackEnd)
                        {
                            // Shockwave
                            AddSolarDischargeDecoration(replay, target, attackEnd, endWave, 1200);
                        }
                    }

                    // Supernova - Phase Oneshot
                    var supernova = casts.Where(x => x.SkillId == SupernovaCM).ToList();
                    foreach (AbstractCastEvent c in supernova)
                    {
                        int start = (int)c.Time;
                        int duration = 75000;
                        int expectedHitTime = start + duration;
                        int attackEnd = start + duration;
                        attackEnd = GetAttackEndByStunTime(log, target, c, duration, attackEnd);

                        replay.Decorations.Add(new CircleDecoration(true, expectedHitTime, 1200, (start, attackEnd), "rgba(255, 0, 0, 0.2)", new AgentConnector(target)));
                        replay.Decorations.Add(new CircleDecoration(true, 0, 1200, (start, attackEnd), "rgba(255, 0, 0, 0.2)", new AgentConnector(target)));
                    }

                    // Solar Cyclone
                    EffectGUIDEvent kick = log.CombatData.GetEffectGUIDEvent(EffectGUIDs.KickGroundEffect);
                    if (kick != null)
                    {
                        IReadOnlyList<EffectEvent> kickEffects = log.CombatData.GetEffectEventsByEffectID(kick.ContentID);
                        foreach (EffectEvent kickEffect in kickEffects)
                        {
                            int start = (int)kickEffect.Time;
                            int end = start + 300;
                            replay.Decorations.Add(new RotatedRectangleDecoration(true, 0, 300, (int)target.HitboxWidth, RadianToDegreeF(kickEffect.Orientation.Z) - 90, 0, (start, end), "rgba(255, 0, 0, 0.2)", new PositionConnector(kickEffect.Position)));
                        }
                    }

                    // Cranial Cascade
                    var cranialCascadeSkorvald = casts.Where(x => x.SkillId == CranialCascadeSkorvald).ToList();
                    foreach (AbstractCastEvent c in cranialCascadeSkorvald)
                    {
                        int start = (int)c.Time;
                        int duration = 1750;
                        int angle = 35;
                        int cascadeCount = 4;
                        int translation = 150;
                        int attackEnd = start + duration;
                        attackEnd = GetAttackEndByStunTime(log, target, c, duration, attackEnd);
                        attackEnd = GetAttackEndByDeterminedTime(log, target, c, duration, attackEnd);

                        Point3D frontalPoint = GetFacingPoint3D(replay, c, duration);
                        if (frontalPoint == null)
                        {
                            continue;
                        }
                        float rotation = Point3D.GetRotationFromFacing(frontalPoint);

                        // Frontal
                        AddKickIndicatorDecoration(replay, target, start, attackEnd, rotation, translation, cascadeCount);
                        // Left
                        AddKickIndicatorDecoration(replay, target, start, attackEnd, rotation - angle, translation, cascadeCount);
                        // Right
                        AddKickIndicatorDecoration(replay, target, start, attackEnd, rotation + angle, translation, cascadeCount);
                    }
                    break;
                case (int)ArcDPSEnums.TrashID.FluxAnomaly1:
                case (int)ArcDPSEnums.TrashID.FluxAnomaly2:
                case (int)ArcDPSEnums.TrashID.FluxAnomaly3:
                case (int)ArcDPSEnums.TrashID.FluxAnomaly4:
                    // Solar Stomp
                    var solarStomp = casts.Where(x => x.SkillId == SolarStomp).ToList();
                    foreach (AbstractCastEvent c in solarStomp)
                    {
                        int radius = 280;
                        int castTime = 2250;
                        int start = (int)c.Time;
                        int attackEnd = start + castTime;
                        int endWave = attackEnd + castTime;

                        // Stomp
                        replay.Decorations.Add(new CircleDecoration(true, attackEnd, radius, (start, attackEnd), "rgba(250, 120, 0, 0.2)", new AgentConnector(target)));
                        replay.Decorations.Add(new CircleDecoration(true, 0, radius, (start, attackEnd), "rgba(250, 120, 0, 0.2)", new AgentConnector(target)));
                        // Shockwave
                        AddSolarDischargeDecoration(replay, target, attackEnd, endWave, 1200);
                    }

                    // Punishing Kick
                    var punishingKickAnomaly = casts.Where(x => x.SkillId == PunishingKickAnomaly).ToList();
                    foreach (AbstractCastEvent c in punishingKickAnomaly)
                    {
                        int start = (int)c.Time;
                        int duration = 1850;
                        int translation = 150;
                        int cascadeCount = 4;
                        int attackEnd = start + duration;

                        Point3D frontalPoint = GetFacingPoint3D(replay, c, duration);
                        if (frontalPoint == null)
                        {
                            continue;
                        }
                        float rotation = Point3D.GetRotationFromFacing(frontalPoint);

                        // Frontal
                        AddKickIndicatorDecoration(replay, target, start, attackEnd, rotation, translation, cascadeCount);
                    }

                    // Cranial Cascade
                    var cranialCascadeAnomaly = casts.Where(x => x.SkillId == CranialCascadeAnomaly).ToList();
                    foreach (AbstractCastEvent c in cranialCascadeAnomaly)
                    {
                        int start = (int)c.Time;
                        int duration = 1750;
                        int angle = 35;
                        int cascadeCount = 4;
                        int translation = 150;
                        int attackEnd = start + duration;

                        Point3D frontalPoint = GetFacingPoint3D(replay, c, duration);
                        if (frontalPoint == null)
                        {
                            continue;
                        }
                        float rotation = Point3D.GetRotationFromFacing(frontalPoint);

                        // Left
                        AddKickIndicatorDecoration(replay, target, start, attackEnd, rotation - angle, translation, cascadeCount);
                        // Right
                        AddKickIndicatorDecoration(replay, target, start, attackEnd, rotation + angle, translation, cascadeCount);
                    }

                    // Mist Smash
                    var mistSmash = casts.Where(x => x.SkillId == MistSmash).ToList();
                    foreach (AbstractCastEvent c in mistSmash)
                    {
                        int start = (int)c.Time;
                        int duration = 1933;
                        int radius = 160;
                        int attackEnd = start + duration;
                        int waveEnd = attackEnd + 2250;

                        replay.Decorations.Add(new CircleDecoration(true, attackEnd, radius, (start, attackEnd), "rgba(250, 120, 0, 0.2)", new AgentConnector(target)));
                        replay.Decorations.Add(new CircleDecoration(true, 0, radius, (start, attackEnd), "rgba(250, 120, 0, 0.2)", new AgentConnector(target)));
                        // Nightmare Discharge Shockwave
                        replay.Decorations.Add(new CircleDecoration(false, waveEnd, 1200, (attackEnd, waveEnd), "rgba(255, 200, 0, 0.3)", new AgentConnector(target)));
                    }

                    // Wave of Mutilation
                    var waveOfMutilation = casts.Where(x => x.SkillId == WaveOfMutilation).ToList();
                    foreach (AbstractCastEvent c in waveOfMutilation)
                    {
                        int start = (int)c.Time;
                        int duration = 1850;
                        int angle = 18;
                        int translation = 150;
                        int cascadeCount = 4;
                        int attackEnd = start + duration;

                        Point3D frontalPoint = GetFacingPoint3D(replay, c, duration);
                        if (frontalPoint == null)
                        {
                            continue;
                        }
                        float rotation = Point3D.GetRotationFromFacing(frontalPoint);

                        float startingDegree = rotation - angle * 2;
                        for (int i = 0; i < 5; i++)
                        {
                            AddKickIndicatorDecoration(replay, target, start, attackEnd, startingDegree, translation, cascadeCount);
                            startingDegree += angle;
                        }
                    }
                    break;
                case (int)ArcDPSEnums.TrashID.SolarBloom:
                    break;
                default:
                    break;
            }
        }

        internal override void ComputeEnvironmentCombatReplayDecorations(ParsedEvtcLog log)
        {
            base.ComputeEnvironmentCombatReplayDecorations(log);

            // Mist Bomb - Both for Skorvald and Flux Anomalies
            EffectGUIDEvent mistBomb = log.CombatData.GetEffectGUIDEvent(EffectGUIDs.MistBomb);
            if (mistBomb != null)
            {
                var mistBombEffects = log.CombatData.GetEffectEventsByEffectID(mistBomb.ContentID).ToList();
                foreach (EffectEvent mistBombEffect in mistBombEffects)
                {
                    int aoeRadius = 130;
                    int aoeTimeout = 300;
                    int start = (int)mistBombEffect.Time;
                    int attackEnd = start + aoeTimeout;
                    EnvironmentDecorations.Add(new CircleDecoration(true, 0, aoeRadius, (start, attackEnd), "rgba(250, 120, 0, 0.2)", new PositionConnector(mistBombEffect.Position)));
                }
            }
        }

        private static void AddHorizonStrikeDecoration(CombatReplay replay, AbstractSingleActor target, int start, int attackEnd, float degree, int radius, int angle)
        {
            float front = degree;
            float flip = degree + 180;

            // Indicator
            replay.Decorations.Add(new PieDecoration(true, 0, radius, front, angle, (start, attackEnd), "rgba(250, 120, 0, 0.2)", new AgentConnector(target)));
            replay.Decorations.Add(new PieDecoration(true, 0, radius, flip, angle, (start, attackEnd), "rgba(250, 120, 0, 0.2)", new AgentConnector(target)));
            // Attack hit
            replay.Decorations.Add(new PieDecoration(true, attackEnd + 300, radius, front, angle, (attackEnd, attackEnd + 300), "rgba(255, 0, 0, 0.2)", new AgentConnector(target)));
            replay.Decorations.Add(new PieDecoration(true, attackEnd + 300, radius, flip, angle, (attackEnd, attackEnd + 300), "rgba(255, 0, 0, 0.2)", new AgentConnector(target)));
        }

        private static void AddSolarDischargeDecoration(CombatReplay replay, AbstractSingleActor target, int start, int attackEnd, int radius)
        {
            replay.Decorations.Add(new CircleDecoration(false, attackEnd, radius, (start, attackEnd), "rgba(120, 0, 0, 0.6)", new AgentConnector(target)));
        }

        private static int GetAttackEndByStunTime(ParsedEvtcLog log, AbstractSingleActor target, AbstractCastEvent c, int duration, int attackEnd)
        {
            Segment stun = target.GetBuffStatus(log, Stun, c.Time, c.Time + duration).FirstOrDefault(x => x.Value > 0);
            return stun != null ? (int)Math.Min(stun.Start, attackEnd) : attackEnd;
        }

        private static int GetAttackEndByDeterminedTime(ParsedEvtcLog log, AbstractSingleActor target, AbstractCastEvent c, int duration, int attackEnd)
        {
            Segment det = target.GetBuffStatus(log, Determined762, c.Time, c.Time + duration).FirstOrDefault(x => x.Value > 0);
            return det != null ? (int)Math.Min(det.Start, attackEnd) : attackEnd;
        }

        private static Point3D GetFacingPoint3D(CombatReplay replay, AbstractCastEvent c, int duration)
        {
            IReadOnlyList<ParametricPoint3D> list = replay.PolledRotations;
            ParametricPoint3D facingDirection = list.FirstOrDefault(x => x.Time > c.Time + 100 && x.Time < c.Time + 100 + duration); // 200 for turning delay event
            if (facingDirection != null)
            {
                return new Point3D(facingDirection.X, facingDirection.Y);
            }
            // Last facing direction polled
            ParametricPoint3D lastDirection = list.LastOrDefault(x => x.Time < c.Time);
            if (lastDirection != null)
            {
                return new Point3D(lastDirection.X, lastDirection.Y);
            }
            return null;
        }

        private static void AddKickIndicatorDecoration(CombatReplay replay, AbstractSingleActor target, int start, int attackEnd, float rotation, int translation, int cascadeCount)
        {
            replay.Decorations.Add(new RotatedRectangleDecoration(true, attackEnd, 300, (int)target.HitboxWidth, rotation, translation, (start, attackEnd), "rgba(250, 120, 0, 0.2)", new AgentConnector(target)));
            replay.Decorations.Add(new RotatedRectangleDecoration(true, 0, 300, (int)target.HitboxWidth, rotation, translation, (start, attackEnd), "rgba(250, 120, 0, 0.2)", new AgentConnector(target)));

            for (int i = 0; i < cascadeCount; i++)
            {
                replay.Decorations.Add(new RotatedRectangleDecoration(true, 0, 300, (int)target.HitboxWidth, rotation, translation, (attackEnd, attackEnd + 300), "rgba(255, 0, 0, 0.2)", new AgentConnector(target)));
                attackEnd += 300;
                translation += 300;
            }
        }
    }
}
