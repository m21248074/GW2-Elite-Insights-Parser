﻿using System.Collections.Generic;
using System.Linq;
using GW2EIEvtcParser.EIData;
using GW2EIEvtcParser.Exceptions;
using GW2EIEvtcParser.ParsedData;
using static GW2EIEvtcParser.EncounterLogic.EncounterImages;
using static GW2EIEvtcParser.EncounterLogic.EncounterLogicTimeUtils;
using static GW2EIEvtcParser.SkillIDs;

namespace GW2EIEvtcParser.EncounterLogic
{
    internal class StatueOfDeath : HallOfChains
    {
        
        public StatueOfDeath(int triggerID) : base(triggerID)
        {
            MechanicList.AddRange(new List<Mechanic>
            {
            new PlayerDstHitMechanic(HungeringMiasma, "Hungering Miasma", new MechanicPlotlySetting(Symbols.TriangleLeftOpen,Colors.DarkGreen), "Vomit","Hungering Miasma (Vomit Goo)", "Vomit Dmg",0),
            new PlayerDstBuffApplyMechanic(ReclaimedEnergyBuff, "Reclaimed Energy", new MechanicPlotlySetting(Symbols.Circle,Colors.Yellow), "Light Orb Collected","Applied when taking a light orb", "Light Orb",0),
            new PlayerCastStartMechanic(ReclaimedEnergySkill, "Reclaimed Energy Thrown", new MechanicPlotlySetting(Symbols.CircleOpen,Colors.Yellow), "Light Orb Thrown","Has thrown a light orb", "Light Orb Thrown",0)
                .UsingChecker((evt, log) =>
                {
                    return evt.Status != AbstractCastEvent.AnimationStatus.Interrupted;
                }),
            new PlayerDstBuffApplyMechanic(FracturedSpirit, "Fractured Spirit", new MechanicPlotlySetting(Symbols.Circle,Colors.Green), "Orb CD","Applied when taking green", "Green port",0),
            }
            );
            Extension = "souleater";
            Icon = EncounterIconStatueOfDeath;
            EncounterCategoryInformation.InSubCategoryOrder = 2;
            EncounterID |= 0x000004;
        }

        protected override CombatReplayMap GetCombatMapInternal(ParsedEvtcLog log)
        {
            return new CombatReplayMap(CombatReplayStatueOfDeath,
                            (710, 709),
                            (1306, -9381, 4720, -5968)/*,
                            (-21504, -12288, 24576, 12288),
                            (19072, 15484, 20992, 16508)*/);
        }
        internal override List<InstantCastFinder> GetInstantCastFinders()
        {
            return new List<InstantCastFinder>()
            {
                new DamageCastFinder(HungeringAura , HungeringAura ), // Hungering Aura
            };
        }
        protected override List<ArcDPSEnums.TrashID> GetTrashMobsIDs()
        {
            return new List<ArcDPSEnums.TrashID>
            {
                ArcDPSEnums.TrashID.OrbSpider,
                ArcDPSEnums.TrashID.SpiritHorde1,
                ArcDPSEnums.TrashID.SpiritHorde2,
                ArcDPSEnums.TrashID.SpiritHorde3,
                ArcDPSEnums.TrashID.GreenSpirit1,
                ArcDPSEnums.TrashID.GreenSpirit2
            };
        }

        internal override long GetFightOffset(EvtcVersionEvent evtcVersion, FightData fightData, AgentData agentData, List<CombatItem> combatData)
        {
            if (!agentData.TryGetFirstAgentItem(ArcDPSEnums.TargetID.EaterOfSouls, out AgentItem eaterOfSouls))
            {
                throw new MissingKeyActorsException("Eater of Souls not found");
            }
            long startToUse = GetGenericFightOffset(fightData);
            CombatItem logStartNPCUpdate = combatData.FirstOrDefault(x => x.IsStateChange == ArcDPSEnums.StateChange.LogNPCUpdate);
            if (logStartNPCUpdate != null)
            {
                var peasants = new List<AgentItem>(agentData.GetNPCsByID(ArcDPSEnums.TrashID.AscalonianPeasant1));
                peasants.AddRange(agentData.GetNPCsByID(ArcDPSEnums.TrashID.AscalonianPeasant2));
                if (peasants.Count != 0)
                {
                    startToUse = peasants.Max(x => x.LastAware);
                }
                else
                {
                    startToUse = GetFirstDamageEventTime(fightData, agentData, combatData, eaterOfSouls);
                }
            }
            return startToUse;
        }

        internal override void ComputeNPCCombatReplayActors(NPC target, ParsedEvtcLog log, CombatReplay replay)
        {
            IReadOnlyList<AbstractCastEvent> cls = target.GetCastEvents(log, log.FightData.FightStart, log.FightData.FightEnd);
            int start = (int)replay.TimeOffsets.start;
            int end = (int)replay.TimeOffsets.end;
            switch (target.ID)
            {
                case (int)ArcDPSEnums.TargetID.EaterOfSouls:
                    var breakbar = cls.Where(x => x.SkillId == Imbibe).ToList();
                    foreach (AbstractCastEvent c in breakbar)
                    {
                        start = (int)c.Time;
                        end = (int)c.EndTime;
                        var circle = new CircleDecoration(180, (start, end), Colors.LightBlue, 0.3, new AgentConnector(target));
                        replay.AddDecorationWithGrowing(circle, start + c.ExpectedDuration);
                    }
                    var vomit = cls.Where(x => x.SkillId == HungeringMiasma).ToList();
                    foreach (AbstractCastEvent c in vomit)
                    {
                        start = (int)c.Time + 2100;
                        int cascading = 1500;
                        int duration = 15000 + cascading;
                        end = start + duration;
                        uint radius = 900;
                        Point3D facing = target.GetCurrentRotation(log, start);
                        Point3D position = target.GetCurrentPosition(log, start);
                        if (facing != null && position != null)
                        {
                            replay.Decorations.Add(new PieDecoration(radius, 60, (start, end), Colors.GreenishYellow, 0.5, new PositionConnector(position)).UsingGrowingEnd(start + cascading).UsingRotationConnector(new AngleConnector(facing)));
                        }
                    }
                    var pseudoDeath = cls.Where(x => x.SkillId == PseudoDeathEaterOfSouls).ToList();
                    foreach (AbstractCastEvent c in pseudoDeath)
                    {
                        start = (int)c.Time;
                        //int duration = 900;
                        end = (int)c.EndTime; //duration;
                        //replay.Actors.Add(new CircleActor(true, 0, 180, (start, end), "rgba(255, 150, 255, 0.35)", new AgentConnector(target)));
                        replay.Decorations.Add(new CircleDecoration(180, (start, end), "rgba(255, 180, 220, 0.7)", new AgentConnector(target)).UsingGrowingEnd(end));
                    }
                    break;
                case (int)ArcDPSEnums.TrashID.GreenSpirit1:
                case (int)ArcDPSEnums.TrashID.GreenSpirit2:
                    var green = cls.Where(x => x.SkillId == GreensEaterofSouls).ToList();
                    foreach (AbstractCastEvent c in green)
                    {
                        int gstart = (int)c.Time + 667;
                        int gend = gstart + 5000;
                        var circle = new CircleDecoration(240, (gstart, gend), Colors.Green, 0.2, new AgentConnector(target));
                        replay.AddDecorationWithGrowing(circle, gend);
                    }
                    break;
                case (int)ArcDPSEnums.TrashID.SpiritHorde1:
                case (int)ArcDPSEnums.TrashID.SpiritHorde2:
                case (int)ArcDPSEnums.TrashID.SpiritHorde3:
                case (int)ArcDPSEnums.TrashID.OrbSpider:
                    break;
                default:
                    break;
            }

        }

        internal override void ComputeEnvironmentCombatReplayDecorations(ParsedEvtcLog log)
        {
            base.ComputeEnvironmentCombatReplayDecorations(log);
            // TODO check sizes
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.EaterOfSoulsSpiritOrbs, out IReadOnlyList<EffectEvent> orbEffectEvents))
            {
                foreach (EffectEvent effectEvent in orbEffectEvents)
                {
                    (long start, long end) lifespan = effectEvent.ComputeDynamicLifespan(log, 0);
                    EnvironmentDecorations.Add(new CircleDecoration(20, lifespan, Colors.Pink, 0.8, new PositionConnector(effectEvent.Position)));
                }
            }
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.EaterOfSoulsSpiderWeb, out IReadOnlyList<EffectEvent> webEffectEvents))
            {
                foreach (EffectEvent effectEvent in webEffectEvents)
                {
                    (long start, long end) lifespan = effectEvent.ComputeLifespan(log, effectEvent.Duration);
                    uint webRadius = 320;
                    var webIndicator = new CircleDecoration(webRadius, lifespan, Colors.Orange, 0.1, new PositionConnector(effectEvent.Position));
                    var web = new CircleDecoration(webRadius, (lifespan.end, lifespan.end + 750), Colors.Orange, 0.3, new PositionConnector(effectEvent.Position));
                    EnvironmentDecorations.Add(webIndicator);
                    EnvironmentDecorations.Add(webIndicator.GetBorderDecoration(Colors.Orange, 0.3));
                    EnvironmentDecorations.Add(webIndicator.Copy().UsingGrowingEnd(lifespan.end));
                    EnvironmentDecorations.Add(web);
                }
            }
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.EaterOfSoulsLightOrbOnGround, out IReadOnlyList<EffectEvent> orbOnGroundEffectEvents))
            {
                foreach (EffectEvent effectEvent in orbOnGroundEffectEvents)
                {
                    (long start, long end) lifespan = effectEvent.ComputeDynamicLifespan(log, 0);
                    EnvironmentDecorations.Add(new CircleDecoration(80, lifespan, Colors.Yellow, 0.6, new PositionConnector(effectEvent.Position)));
                }
            }
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.EaterOfSoulsLightOrbThrowHitGround, out IReadOnlyList<EffectEvent> orbThrowEffectEvents))
            {
                foreach (EffectEvent effectEvent in orbThrowEffectEvents)
                {
                    (long start, long end) lifespan = (effectEvent.Time, effectEvent.Time + 500);
                    EnvironmentDecorations.Add(new CircleDecoration(40, lifespan, Colors.Yellow, 0.6, new PositionConnector(effectEvent.Position)));
                }
            }
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.EaterOfSoulsSpiritShockwave2, out IReadOnlyList<EffectEvent> shockwaveEffectEvents))
            {
                foreach (EffectEvent effectEvent in shockwaveEffectEvents)
                {
                    (long start, long end) lifespan = effectEvent.ComputeLifespan(log, 3600);
                    EnvironmentDecorations.Add(new CircleDecoration(1400, lifespan, Colors.Red, 0.3, new PositionConnector(effectEvent.Position)).UsingFilled(false).UsingGrowingEnd(lifespan.end));
                }
            }
        }

        internal override void ComputePlayerCombatReplayActors(AbstractPlayer p, ParsedEvtcLog log, CombatReplay replay)
        {
            base.ComputePlayerCombatReplayActors(p, log, replay);
            var spiritTransform = log.CombatData.GetBuffDataByIDByDst(FracturedSpirit, p.AgentItem).Where(x => x is BuffApplyEvent).ToList();
            foreach (AbstractBuffEvent c in spiritTransform)
            {
                int duration = 30000;
                AbstractBuffEvent removedBuff = log.CombatData.GetBuffRemoveAllData(MortalCoilStatueOfDeath).FirstOrDefault(x => x.To == p.AgentItem && x.Time > c.Time && x.Time < c.Time + duration);
                int start = (int)c.Time;
                int end = start + duration;
                if (removedBuff != null)
                {
                    end = (int)removedBuff.Time;
                }
                var circle = new CircleDecoration(100, (start, end), "rgba(0, 50, 200, 0.3)", new AgentConnector(p));
                replay.AddDecorationWithGrowing(circle, start + duration);
            }
        }

        internal override void CheckSuccess(CombatData combatData, AgentData agentData, FightData fightData, IReadOnlyCollection<AgentItem> playerAgents)
        {
            NoBouncyChestGenericCheckSucess(combatData, agentData, fightData, playerAgents);
        }

        internal override string GetLogicName(CombatData combatData, AgentData agentData)
        {
            return "Statue of Death";
        }
    }
}
