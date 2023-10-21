﻿using System;
using System.Collections.Generic;
using System.Linq;
using GW2EIEvtcParser.EIData;
using GW2EIEvtcParser.Exceptions;
using GW2EIEvtcParser.ParsedData;
using GW2EIEvtcParser.ParserHelpers;
using static GW2EIEvtcParser.ParserHelper;
using static GW2EIEvtcParser.SkillIDs;
using static GW2EIEvtcParser.EncounterLogic.EncounterLogicUtils;
using static GW2EIEvtcParser.EncounterLogic.EncounterLogicPhaseUtils;
using static GW2EIEvtcParser.EncounterLogic.EncounterLogicTimeUtils;
using static GW2EIEvtcParser.EncounterLogic.EncounterImages;

namespace GW2EIEvtcParser.EncounterLogic
{
    internal class KainengOverlook : EndOfDragonsStrike
    {
        public KainengOverlook(int triggerID) : base(triggerID)
        {
            MechanicList.AddRange(new List<Mechanic>
            {
                new PlayerDstHitMechanic(new long[] { DragonSlashWaveNM, DragonSlashWaveCM }, "Dragon Slash - Wave", new MechanicPlotlySetting(Symbols.TriangleLeft, Colors.DarkRed), "Wave.H", "Hit by Wave", "Wave Hit", 150),
                new PlayerDstHitMechanic(new long[] { DragonSlashBurstNM, DragonSlashBurstCM }, "Dragon Slash - Burst", new MechanicPlotlySetting(Symbols.TriangleUp, Colors.DarkRed), "Burst.H", "Hit by Burst", "Burst Hit", 150),
                new PlayerDstHitMechanic(new long[] { DragonSlashRushNM1, DragonSlashRushNM2, DragonSlashRush1CM, DragonSlashRush2CM }, "Dragon Slash - Rush", new MechanicPlotlySetting(Symbols.TriangleDown, Colors.DarkRed), "Rush.H", "Hit by Rush", "Rush Hit", 150),
                new PlayerDstHitMechanic(new long[] { EnforcerRushingJusticeNM, EnforcerRushingJusticeCM }, "Rushing Justice", new MechanicPlotlySetting(Symbols.Square, Colors.Orange), "Flames.S", "Stood in Flames", "Stood in Flames", 150),
                new PlayerDstHitMechanic(new long[] { StormOfSwords1, StormOfSwords2, StormOfSwords3, StormOfSwords4, StormOfSwords5, StormOfSwords6, StormOfSwords7, StormOfSwords8, StormOfSwords9, StormOfSwords10 }, "Storm of Swords", new MechanicPlotlySetting(Symbols.Circle, Colors.Pink), "Storm.H", "Hit by bladestorm", "Bladestorm Hit", 150),
                new PlayerDstHitMechanic(new long[] { DragonSlashWaveNM, DragonSlashWaveCM, DragonSlashRushNM1, DragonSlashRushNM2, DragonSlashRush1CM, DragonSlashRush2CM }, "A Test of Your Reflexes", new MechanicPlotlySetting(Symbols.Diamond, Colors.Red), "TextReflx.Achiv", "Achievement Eligibility: A Test of Your Reflexes", "Achiv Test Reflexes", 150).UsingAchievementEligibility(true).UsingEnable((log) => log.FightData.IsCM),
                new PlayerDstHitMechanic(new long[] { ExplosiveUppercutNM, ExplosiveUppercutCM }, "Explosive Uppercut", new MechanicPlotlySetting(Symbols.TriangleNE, Colors.Pink), "ExpUpper.H", "Hit by Explosive Uppercut", "Explosive Uppercut Hit", 150),
                new PlayerDstHitMechanic(new long[] { FallOfTheAxeSmallConeNM, FallOfTheAxeSmallConeCM }, "Fall of the Axe", new MechanicPlotlySetting(Symbols.TriangleRight, Colors.LightGrey), "FallAxe.S.H", "Hit by Mech Rider Small Cone", "Mech Rider Small Cone Hit", 150),
                new PlayerDstHitMechanic(new long[] { FallOfTheAxeBigConeNM, FallOfTheAxeBigConeCM }, "Fall of the Axe", new MechanicPlotlySetting(Symbols.TriangleLeft, Colors.LightGrey), "FallAxe.B.H", "Hit by Mech Rider Big Cone", "Mech Rider Small Big Hit", 150),
                new PlayerDstHitMechanic(new long[] { ElectricRainNM, ElectricRainCM }, "Electric Rain", new MechanicPlotlySetting(Symbols.StarDiamond, Colors.LightOrange), "ElecRain.H", "Hit by Electric Rain (Set of 5 AoEs by Mech Rider)", "Electic Rain Hit", 150),
                new PlayerDstHitMechanic(BoomingCommandOverlap, "Booming Command", new MechanicPlotlySetting(Symbols.Circle, Colors.Red), "Red.O", "Red circle overlap", "Red Circle", 150),
                new PlayerDstHitMechanic(JadeBusterCannonMechRider, "Jade Buster Cannon", new MechanicPlotlySetting(Symbols.TriangleRight, Colors.Orange), "Laser.H", "Hit by Big Laser", "Laser Hit", 150),
                new PlayerDstSkillMechanic(new long[] { TargetedExpulsion, TargetedExpulsionCM }, "Targeted Expulsion", new MechanicPlotlySetting(Symbols.Square, Colors.Purple), "Bomb.D", "Downed by Bomb", "Bomb Downed", 150).UsingChecker((ahde, log) => ahde.HasDowned),
                new PlayerDstNoSkillMechanic(new long[] { EnhancedDestructiveAuraSkill1, EnhancedDestructiveAuraSkill2 }, "The Path of Most Resistance", new MechanicPlotlySetting(Symbols.DiamondWide, Colors.Purple), "MostResi.Achiv", "Achievement Eligibility: The Path of Most Resistance", "Achiv Most Resistance", 150).UsingAchievementEligibility(true).UsingEnable(x => x.FightData.IsCM),
                new PlayerDstBuffApplyMechanic(new long [] { TargetOrder1, TargetOrder2, TargetOrder3, TargetOrder4, TargetOrder5 }, "Target Order", new MechanicPlotlySetting(Symbols.Star, Colors.LightOrange), "Targ.Ord.A", "Received Target Order", "Target Order Application", 0),
                new PlayerDstBuffApplyMechanic(FixatedAnkkaKainengOverlook, "Fixated (Mindblade)", new MechanicPlotlySetting(Symbols.Circle, Colors.Purple), "Fixated.M", "Fixated by The Mindblade", "Fixated Mindblade", 150).UsingChecker((bae, log) => bae.CreditedBy.IsAnySpecies(new List<ArcDPSEnums.TrashID> { ArcDPSEnums.TrashID.TheMindblade, ArcDPSEnums.TrashID.TheMindbladeCM })),
                new PlayerDstBuffApplyMechanic(FixatedAnkkaKainengOverlook, "Fixated (Enforcer)", new MechanicPlotlySetting(Symbols.Circle, Colors.DarkPurple), "Fixated.E", "Fixated by The Enforcer", "Fixated Enforcer", 150).UsingChecker((bae, log) => bae.CreditedBy.IsAnySpecies(new List<ArcDPSEnums.TrashID> { ArcDPSEnums.TrashID.TheEnforcer, ArcDPSEnums.TrashID.TheEnforcerCM })),
                new PlayerDstEffectMechanic(EffectGUIDs.KainengOverlookSharedDestructionGreen, "Shared Destruction",  new MechanicPlotlySetting(Symbols.CircleOpen, Colors.Green), "Green", "Selected for Green", "Green", 150),
                new PlayerDstEffectMechanic(EffectGUIDs.KainengOverlookSharedDestructionGreenSuccess, "Shared Destruction",  new MechanicPlotlySetting(Symbols.Circle, Colors.Green), "Green.Succ", "Successful Green", "Successful Green", 150),
                new PlayerDstEffectMechanic(EffectGUIDs.KainengOverlookSharedDestructionGreenFailure, "Shared Destruction",  new MechanicPlotlySetting(Symbols.CircleCrossOpen, Colors.DarkGreen), "Green.Fail", "Failed Green", "Failed Green", 150),
                new PlayerDstEffectMechanic(EffectGUIDs.KainengOverlookSniperRicochetBeamCM, "Ricochet", new MechanicPlotlySetting(Symbols.CircleXOpen, Colors.Red), "Sniper.T", "Targetted by Sniper Ricochet", "Ricochet Target", 150),
                new PlayerDstEffectMechanic(EffectGUIDs.KainengOverlookMindbladeRainOfBladesFirstOrangeAoEOnPlayer, "Rain of Blades", new MechanicPlotlySetting(Symbols.TriangleUp, Colors.LightPurple), "RainBlad.T", "Targetted by Rain of Blades", "Rain of Blades Target", 150),
                new EnemyDstBuffApplyMechanic(EnhancedDestructiveAuraBuff, "Enhanced Destructive Aura", new MechanicPlotlySetting(Symbols.TriangleUpOpen, Colors.Purple), "DescAura", "Enhanced Destructive Aura", "Powered Up 2", 150),
                new EnemyDstBuffApplyMechanic(DestructiveAuraBuff, "Destructive Aura", new MechanicPlotlySetting(Symbols.TriangleUp, Colors.Purple), "Pwrd.Up2", "Powered Up (Split 2)", "Powered Up 2", 150),
                new EnemyDstBuffApplyMechanic(LethalInspiration, "Lethal Inspiration", new MechanicPlotlySetting(Symbols.TriangleUp, Colors.DarkGreen), "Pwrd.Up1", "Powered Up (Split 1)", "Powered Up 1", 150),
            }
            );
            Icon = EncounterIconKainengOverlook;
            Extension = "kaiover";
            EncounterCategoryInformation.InSubCategoryOrder = 2;
            EncounterID |= 0x000003;
        }

        protected override CombatReplayMap GetCombatMapInternal(ParsedEvtcLog log)
        {
            return new CombatReplayMap(CombatReplayKainengOverlook,
                            (1803, 1918),
                            (-24798, -18014, -18164, -10932)/*,
                            (-15360, -36864, 15360, 39936),
                            (3456, 11012, 4736, 14212)*/);
        }

        protected override List<int> GetTargetsIDs()
        {
            return new List<int>
            {
                (int)ArcDPSEnums.TargetID.MinisterLi,
                (int)ArcDPSEnums.TargetID.MinisterLiCM,
                (int)ArcDPSEnums.TrashID.TheEnforcer,
                (int)ArcDPSEnums.TrashID.TheMindblade,
                (int)ArcDPSEnums.TrashID.TheMechRider,
                (int)ArcDPSEnums.TrashID.TheRitualist,
                (int)ArcDPSEnums.TrashID.TheSniper,
                (int)ArcDPSEnums.TrashID.TheEnforcerCM,
                (int)ArcDPSEnums.TrashID.TheMindbladeCM,
                (int)ArcDPSEnums.TrashID.TheMechRiderCM,
                (int)ArcDPSEnums.TrashID.TheRitualistCM,
                (int)ArcDPSEnums.TrashID.TheSniperCM,
            };
        }

        protected override List<int> GetSuccessCheckIDs()
        {
            return new List<int>
            {
                (int)ArcDPSEnums.TargetID.MinisterLi,
                (int)ArcDPSEnums.TargetID.MinisterLiCM,
            };
        }

        internal override string GetLogicName(CombatData combatData, AgentData agentData)
        {
            return "Kaineng Overlook";
        }

        protected override List<ArcDPSEnums.TrashID> GetTrashMobsIDs()
        {
            return new List<ArcDPSEnums.TrashID>
            {
                ArcDPSEnums.TrashID.SpiritOfDestruction,
                ArcDPSEnums.TrashID.SpiritOfPain,
            };
        }

        protected override HashSet<int> GetUniqueNPCIDs()
        {
            return new HashSet<int>
            {
                (int)ArcDPSEnums.TargetID.MinisterLi,
                (int)ArcDPSEnums.TargetID.MinisterLiCM,
            };
        }

        internal override List<InstantCastFinder> GetInstantCastFinders()
        {
            return new List<InstantCastFinder>()
            {
                new DamageCastFinder(DestructiveAuraSkill, DestructiveAuraSkill),
                new DamageCastFinder(EnhancedDestructiveAuraSkill1, EnhancedDestructiveAuraSkill1),
                new DamageCastFinder(EnhancedDestructiveAuraSkill2, EnhancedDestructiveAuraSkill2),
            };
        }

        private static void AddSplitPhase(List<PhaseData> phases, IReadOnlyList<AbstractSingleActor> targets, AbstractSingleActor ministerLi, ParsedEvtcLog log, int phaseID)
        {
            if (targets.All(x => x != null))
            {
                EnterCombatEvent cbtEnter = null;
                foreach (NPC target in targets)
                {
                    cbtEnter = log.CombatData.GetEnterCombatEvents(target.AgentItem).LastOrDefault();
                    if (cbtEnter != null)
                    {
                        break;
                    }
                }
                if (cbtEnter != null)
                {
                    AbstractBuffEvent nextPhaseStartEvt = log.CombatData.GetBuffData(ministerLi.AgentItem).FirstOrDefault(x => x is BuffRemoveAllEvent && x.BuffID == Determined762 && x.Time > cbtEnter.Time);
                    long phaseEnd = nextPhaseStartEvt != null ? nextPhaseStartEvt.Time : log.FightData.FightEnd;
                    var addPhase = new PhaseData(cbtEnter.Time, phaseEnd, "Split Phase " + phaseID);
                    addPhase.AddTargets(targets);
                    phases.Add(addPhase);
                }
            }
        }

        private AbstractSingleActor GetMinisterLi(FightData fightData)
        {
            return Targets.FirstOrDefault(x => x.IsSpecies(fightData.IsCM ? (int)ArcDPSEnums.TargetID.MinisterLiCM : (int)ArcDPSEnums.TargetID.MinisterLi));
        }

        internal override List<PhaseData> GetPhases(ParsedEvtcLog log, bool requirePhases)
        {
            List<PhaseData> phases = GetInitialPhase(log);
            AbstractSingleActor ministerLi = GetMinisterLi(log.FightData);
            if (ministerLi == null)
            {
                throw new MissingKeyActorsException("Minister Li not found");
            }
            phases[0].AddTarget(ministerLi);
            if (!requirePhases)
            {
                return phases;
            }
            List<PhaseData> subPhases = GetPhasesByInvul(log, Determined762, ministerLi, false, true);
            for (int i = 0; i < subPhases.Count; i++)
            {
                subPhases[i].Name = "Phase " + (i + 1);
                subPhases[i].AddTarget(ministerLi);
            }
            // when wiped during a split phase, Li's LastAware is well before fight end
            subPhases.RemoveAll(x => (x.End + x.Start) / 2 > ministerLi.LastAware + ServerDelayConstant);
            phases.AddRange(subPhases);
            //
            AbstractSingleActor enforcer = Targets.LastOrDefault(x => x.IsSpecies(log.FightData.IsCM ? (int)ArcDPSEnums.TrashID.TheEnforcerCM : (int)ArcDPSEnums.TrashID.TheEnforcer));
            AbstractSingleActor mindblade = Targets.LastOrDefault(x => x.IsSpecies(log.FightData.IsCM ? (int)ArcDPSEnums.TrashID.TheMindbladeCM : (int)ArcDPSEnums.TrashID.TheMindblade));
            AbstractSingleActor mechRider = Targets.LastOrDefault(x => x.IsSpecies(log.FightData.IsCM ? (int)ArcDPSEnums.TrashID.TheMechRiderCM : (int)ArcDPSEnums.TrashID.TheMechRider));
            AbstractSingleActor sniper = Targets.LastOrDefault(x => x.IsSpecies(log.FightData.IsCM ? (int)ArcDPSEnums.TrashID.TheSniperCM : (int)ArcDPSEnums.TrashID.TheSniper));
            AbstractSingleActor ritualist = Targets.LastOrDefault(x => x.IsSpecies(log.FightData.IsCM ? (int)ArcDPSEnums.TrashID.TheRitualistCM : (int)ArcDPSEnums.TrashID.TheRitualist));
            AddSplitPhase(phases, new List<AbstractSingleActor>() { enforcer, mindblade, ritualist }, ministerLi, log, 1);
            AddSplitPhase(phases, new List<AbstractSingleActor>() { mechRider, sniper }, ministerLi, log, 2);
            return phases;
        }

        internal override void CheckSuccess(CombatData combatData, AgentData agentData, FightData fightData, IReadOnlyCollection<AgentItem> playerAgents)
        {
            AbstractSingleActor ministerLi = GetMinisterLi(fightData);
            if (ministerLi == null)
            {
                throw new MissingKeyActorsException("Minister Li not found");
            }
            var buffApplies = combatData.GetBuffData(Resurrection).OfType<BuffApplyEvent>().Where(x => x.To == ministerLi.AgentItem).ToList();
            if (buffApplies.Any())
            {
                fightData.SetSuccess(true, buffApplies[0].Time);
            }
        }

        internal override FightData.EncounterMode GetEncounterMode(CombatData combatData, AgentData agentData, FightData fightData)
        {
            AbstractSingleActor ministerLiCM = Targets.FirstOrDefault(x => x.IsSpecies(ArcDPSEnums.TargetID.MinisterLiCM));
            return ministerLiCM != null ? FightData.EncounterMode.CM : FightData.EncounterMode.Normal;
        }

        internal override void ComputePlayerCombatReplayActors(AbstractPlayer p, ParsedEvtcLog log, CombatReplay replay)
        {
            // Target Order
            replay.AddOverheadIcons(p.GetBuffStatus(log, TargetOrder1, log.FightData.LogStart, log.FightData.LogEnd).Where(x => x.Value > 0), p, ParserIcons.TargetOrder1Overhead);
            replay.AddOverheadIcons(p.GetBuffStatus(log, TargetOrder2, log.FightData.LogStart, log.FightData.LogEnd).Where(x => x.Value > 0), p, ParserIcons.TargetOrder2Overhead);
            replay.AddOverheadIcons(p.GetBuffStatus(log, TargetOrder3, log.FightData.LogStart, log.FightData.LogEnd).Where(x => x.Value > 0), p, ParserIcons.TargetOrder3Overhead);
            replay.AddOverheadIcons(p.GetBuffStatus(log, TargetOrder4, log.FightData.LogStart, log.FightData.LogEnd).Where(x => x.Value > 0), p, ParserIcons.TargetOrder4Overhead);
            replay.AddOverheadIcons(p.GetBuffStatus(log, TargetOrder5, log.FightData.LogStart, log.FightData.LogEnd).Where(x => x.Value > 0), p, ParserIcons.TargetOrder5Overhead);
            // Fixation
            replay.AddOverheadIcons(p.GetBuffStatus(log, FixatedAnkkaKainengOverlook, log.FightData.LogStart, log.FightData.LogEnd).Where(x => x.Value > 0), p, ParserIcons.FixationPurpleOverhead);
            List<AbstractBuffEvent> fixationEvents = GetFilteredList(log.CombatData, FixatedAnkkaKainengOverlook, p, true, true);
            replay.AddTether(fixationEvents, "rgba(255, 0, 255, 0.5)");

            // Shared Destruction (Green)
            int greenDuration = 6250;
            if (log.CombatData.TryGetEffectEventsBySrcWithGUIDs(p.AgentItem,
                new string[] { EffectGUIDs.KainengOverlookSharedDestructionGreenSuccess, EffectGUIDs.KainengOverlookSharedDestructionGreenFailure },
                out IReadOnlyList<EffectEvent> greenEndEffectEvents))
            {
                foreach (EffectEvent effect in greenEndEffectEvents)
                {
                    bool isSuccess = log.CombatData.GetEffectGUIDEvent(effect.EffectID).ContentGUID == EffectGUIDs.KainengOverlookSharedDestructionGreenSuccess;
                    AddSharedDestructionDecoration(p, replay, ((int)effect.Time - greenDuration, (int)effect.Time), isSuccess);
                }
            }
            else
            {
                greenEndEffectEvents = new List<EffectEvent>();
            }
            if (log.CombatData.TryGetEffectEventsBySrcWithGUID(p.AgentItem, EffectGUIDs.KainengOverlookSharedDestructionGreen, out IReadOnlyList<EffectEvent> greenApplyEffectEvents))
            {
                foreach (EffectEvent effect in greenApplyEffectEvents)
                {
                    // Check if any green effect event happens within 200 ms from another successful or failed green.
                    // If the green mechanic targets the same player twice at the same time (meaning only one green appears in game), the second effect gets queued up 6.5 seconds later.
                    // This prevents the late green effect from appearing in the combat replay, since it doesn't exist in game.
                    if (!greenEndEffectEvents.Any(x => Math.Abs(x.Time - effect.Time) < 200 || Math.Abs(x.Time - greenDuration - effect.Time) < 200))
                    {
                        AddSharedDestructionDecoration(p, replay, ProfHelper.ComputeEffectLifespan(log, effect, greenDuration), true);
                    }
                }
            }

            // Sniper Ricochet Tether & AoE - CM
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.KainengOverlookSniperRicochetBeamCM, out IReadOnlyList<EffectEvent> sniperBeamsCM))
            {
                foreach (EffectEvent effect in sniperBeamsCM.Where(x => x.Dst == p.AgentItem))
                {
                    // Check if any effect event exists before the current one within a 20 seconds time span
                    // This is to fix the beam duration incorrectly logged
                    // The first shot happens after 10 seconds, the following ones after 5 seconds
                    int correctedDuration = sniperBeamsCM.Where(x => x.Time > effect.Time - 20000 && x.Time != effect.Time && x.Time < effect.Time).Any() ? 5000 : 10000;
                    // Correct the life span for the circle decoration
                    (int, int) lifespan = ((int)effect.Time, (int)effect.Time + correctedDuration);

                    // Tether Sniper to Player
                    replay.AddTetherByEffectGUID(log, effect, "rgba(255, 200, 0, 0.3)", correctedDuration, true);

                    // Circle around the player
                    replay.Decorations.Add(new CircleDecoration(false, 0, 500, lifespan, "rgba(250, 50, 0, 0.2)", new AgentConnector(p)));
                }
            }

            // Targeted Expulsion - Orange spread AoEs CM
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.KainengOverlookTargetedExpulsion, out IReadOnlyList<EffectEvent> spreads))
            {
                foreach (EffectEvent effect in spreads.Where(x => x.Dst == p.AgentItem))
                {
                    (int, int) lifespan = ProfHelper.ComputeEffectLifespan(log, effect, 5000);
                    var connector = new AgentConnector(p);
                    replay.Decorations.Add(new CircleDecoration(true, 0, 230, lifespan, "rgba(200, 120, 0, 0.2)", connector));
                    replay.Decorations.Add(new CircleDecoration(true, lifespan.Item2, 230, lifespan, "rgba(200, 120, 0, 0.2)", connector));
                }
            }

            // Rain Of Blades - Mindblade AoE on players - Orange circle (first)
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.KainengOverlookMindbladeRainOfBladesFirstOrangeAoEOnPlayer, out IReadOnlyList<EffectEvent> mindbladeAoEOnPlayers))
            {
                foreach (EffectEvent effect in mindbladeAoEOnPlayers.Where(x => x.Dst == p.AgentItem))
                {
                    (int, int) lifespan = ProfHelper.ComputeEffectLifespan(log, effect, 8000);
                    var connector = new AgentConnector(p);
                    replay.Decorations.Add(new CircleDecoration(true, 0, 240, lifespan, "rgba(200, 120, 0, 0.2)", connector));
                    replay.Decorations.Add(new CircleDecoration(true, lifespan.Item2, 240, lifespan, "rgba(200, 120, 0, 0.2)", connector));
                }
            }

            // Rain Of Blades - Mindblade AoE on players - Orange circle (consecutives)
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.KainengOverlookMindbladeRainOfBladesConsecutiveOrangeAoEOnPlayer, out IReadOnlyList<EffectEvent> mindbladeAoEOnPlayers4))
            {
                foreach (EffectEvent effect in mindbladeAoEOnPlayers4.Where(x => x.Dst == p.AgentItem))
                {
                    (int, int) lifespan = ProfHelper.ComputeEffectLifespan(log, effect, 2000);
                    var connector = new AgentConnector(p);
                    replay.Decorations.Add(new CircleDecoration(true, 0, 240, lifespan, "rgba(200, 120, 0, 0.2)", connector));
                    replay.Decorations.Add(new CircleDecoration(true, lifespan.Item2, 240, lifespan, "rgba(200, 120, 0, 0.2)", connector));
                }
            }

            // Heaven's Palm - AoE on players
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.KainengOverlookEnforcerHeavensPalmAoE, out IReadOnlyList<EffectEvent> heavensPalm))
            {
                foreach (EffectEvent effect in heavensPalm.Where(x => x.Dst == p.AgentItem))
                {
                    (int, int) lifespan = ProfHelper.ComputeEffectLifespan(log, effect, 5000);
                    var connector = new AgentConnector(p);
                    replay.Decorations.Add(new CircleDecoration(true, 0, 280, lifespan, "rgba(200, 120, 0, 0.2)", connector));
                    replay.Decorations.Add(new CircleDecoration(true, lifespan.Item2, 280, lifespan, "rgba(200, 120, 0, 0.2)", connector));
                }
            }
        }

        internal override void ComputeNPCCombatReplayActors(NPC target, ParsedEvtcLog log, CombatReplay replay)
        {
            IReadOnlyList<AbstractCastEvent> casts = target.GetCastEvents(log, log.FightData.FightStart, log.FightData.FightEnd);

            switch (target.ID)
            {
                case (int)ArcDPSEnums.TargetID.MinisterLi:
                    break;
                case (int)ArcDPSEnums.TargetID.MinisterLiCM:
                    break;
                case (int)ArcDPSEnums.TrashID.TheMechRider:
                case (int)ArcDPSEnums.TrashID.TheMechRiderCM:
                    // Small Cone
                    var fallOfTheAxeSmall = casts.Where(x => x.SkillId == FallOfTheAxeSmallConeNM || x.SkillId == FallOfTheAxeSmallConeCM).ToList();
                    foreach (AbstractCastEvent c in fallOfTheAxeSmall)
                    {
                        int durationCastTime = 965;
                        (int, int) lifespan = ((int)c.Time, (int)c.Time + durationCastTime);
                        var facingDirection = Point3D.GetFacingPoint3D(replay, c, durationCastTime);
                        if (facingDirection == null)
                        {
                            continue;
                        }
                        AddFallOfTheAxeDecoration(target, replay, new AngleConnector(facingDirection), lifespan, 35);
                    }

                    // Big Cone
                    var fallOfTheAxeBig = casts.Where(x => x.SkillId == FallOfTheAxeBigConeNM || x.SkillId == FallOfTheAxeBigConeCM).ToList();
                    foreach (AbstractCastEvent c in fallOfTheAxeBig)
                    {
                        int durationCastTime = 1030;
                        (int, int) lifespan = ((int)c.Time, (int)c.Time + durationCastTime);
                        var facingDirection = Point3D.GetFacingPoint3D(replay, c, durationCastTime);
                        if (facingDirection == null)
                        {
                            continue;
                        }
                        AddFallOfTheAxeDecoration(target, replay, new AngleConnector(facingDirection), lifespan, 75);
                    }

                    // Jade Buster Cannon
                    var cannon = casts.Where(x => x.SkillId == JadeBusterCannonMechRider).ToList();
                    int warningDuration = 2800;
                    // Warning decoration
                    if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.KainengOverlookJadeBusterCannonWarning, out IReadOnlyList<EffectEvent> warningRectangle))
                    {
                        foreach (EffectEvent effect in warningRectangle)
                        {
                            (int, int) lifespanWarning = ProfHelper.ComputeEffectLifespan(log, effect, warningDuration);
                            var connector = new AgentConnector(target);
                            replay.Decorations.Add(new RectangleDecoration(false, 0, 375, 3000, lifespanWarning, "rgba(250, 50, 0, 0.2)", connector.WithOffset(new Point3D(0, -1350), true)));
                            replay.Decorations.Add(new RectangleDecoration(true, 0, 375, 3000, lifespanWarning, "rgba(200, 120, 0, 0.2)", connector.WithOffset(new Point3D(0, -1350), true)));
                        }
                    }
                    // Damage decoration
                    foreach (AbstractCastEvent c in cannon)
                    {
                        int durationCastTime = 10367;
                        (int, int) lifespan = ((int)c.Time + warningDuration, (int)c.Time + durationCastTime - warningDuration);
                        var connector = new AgentConnector(target);
                        replay.Decorations.Add(new RectangleDecoration(false, 0, 375, 3000, lifespan, "rgba(250, 50, 0, 0.2)", connector.WithOffset(new Point3D(0, -1350), true)));
                        replay.Decorations.Add(new RectangleDecoration(true, 0, 375, 3000, lifespan, "rgba(30, 120, 40, 0.4)", connector.WithOffset(new Point3D(0, -1350), true)));
                    }
                    break;
                case (int)ArcDPSEnums.TrashID.TheEnforcer:
                case (int)ArcDPSEnums.TrashID.TheEnforcerCM:
                    // Blue tether from Enforcer to Mindblade when they're close to each other
                    List<AbstractBuffEvent> enforcerInspiration = GetFilteredList(log.CombatData, LethalInspiration, target, true, true);
                    replay.AddTether(enforcerInspiration, "rgba(0, 0, 255, 0.1)");
                    break;
                case (int)ArcDPSEnums.TrashID.TheMindblade:
                case (int)ArcDPSEnums.TrashID.TheMindbladeCM:
                    // Blue tether from Mindblade to Enforcer when they're close to each other
                    List<AbstractBuffEvent> mindbladeInspiration = GetFilteredList(log.CombatData, LethalInspiration, target, true, true);
                    replay.AddTether(mindbladeInspiration, "rgba(0, 0, 255, 0.1)");
                    break;
                case (int)ArcDPSEnums.TrashID.TheRitualist:
                case (int)ArcDPSEnums.TrashID.TheRitualistCM:
                    break;
                case (int)ArcDPSEnums.TrashID.SpiritOfPain:
                    // Volatile Expulsion - Orange AoE around the spirit
                    if (log.CombatData.TryGetEffectEventsBySrcWithGUID(target.AgentItem, EffectGUIDs.KainengOverlookVolatileExpulsionAoE, out IReadOnlyList<EffectEvent> volatileExpulsion))
                    {
                        foreach (EffectEvent effect in volatileExpulsion)
                        {
                            (int, int) lifespan = ProfHelper.ComputeEffectLifespan(log, effect, 5500);
                            var connector = new AgentConnector(target);
                            replay.Decorations.Add(new CircleDecoration(true, 0, 380, lifespan, "rgba(200, 120, 0, 0.2)", connector));
                            replay.Decorations.Add(new CircleDecoration(true, lifespan.Item2, 380, lifespan, "rgba(200, 120, 0, 0.2)", connector));
                        }
                    }
                    break;
                case (int)ArcDPSEnums.TrashID.SpiritOfDestruction:
                    // Volatile Burst - Orange AoE around the spirit with safe zone in the center
                    if (log.CombatData.TryGetEffectEventsBySrcWithGUID(target.AgentItem,EffectGUIDs.KainengOverlookVolatileBurstAoE, out IReadOnlyList<EffectEvent> volatileBurst))
                    {
                        foreach (EffectEvent effect in volatileBurst)
                        {
                            (int, int) lifespan = ProfHelper.ComputeEffectLifespan(log, effect, 5500);
                            var connector = new AgentConnector(target);
                            replay.Decorations.Add(new DoughnutDecoration(true, 0, 100, 500, lifespan, "rgba(200, 120, 0, 0.2)", connector));
                            replay.Decorations.Add(new DoughnutDecoration(true, lifespan.Item2, 100, 500, lifespan, "rgba(200, 120, 0, 0.2)", connector));
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        internal override void ComputeEnvironmentCombatReplayDecorations(ParsedEvtcLog log)
        {
            base.ComputeEnvironmentCombatReplayDecorations(log);

            // Dragon Slash Burst - Red AoE Puddles - CM
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.KainengOverlookDragonSlashBurstRedAoE1, out IReadOnlyList<EffectEvent> smolReds))
            {
                foreach (EffectEvent effect in smolReds)
                {
                    (int, int) lifespan = ProfHelper.ComputeEffectLifespan(log, effect, 20800);
                    var connector = new PositionConnector(effect.Position);
                    int damageDelay = 1610;
                    int warningEnd = lifespan.Item1 + damageDelay;
                    EnvironmentDecorations.Add(new CircleDecoration(true, 0, 80, (lifespan.Item1, warningEnd), "rgba(250, 50, 0, 0.2)", connector));
                    EnvironmentDecorations.Add(new CircleDecoration(true, warningEnd, 80, (lifespan.Item1, warningEnd), "rgba(250, 50, 0, 0.2)", connector));
                    EnvironmentDecorations.Add(new CircleDecoration(true, 0, 80, (warningEnd, lifespan.Item2), "rgba(250, 50, 0, 0.4)", connector));
                }
            }

            // Jade Mines
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.KainengOverlookJadeMine1, out IReadOnlyList<EffectEvent> mines))
            {
                foreach (EffectEvent effect in mines)
                {
                    (int, int) lifespan = ProfHelper.ComputeDynamicEffectLifespan(log, effect, 0);
                    var connector = new PositionConnector(effect.Position);
                    EnvironmentDecorations.Add(new CircleDecoration(true, 0, 80, lifespan, "rgba(250, 50, 0, 0.4)", connector));
                }
            }

            // Electric Rain - 5 AoEs in sequence up to 5
            // Jade Lob - Small deathly AoE
            // Small Orange AoEs
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.KainengOverlookSmallOrangeAoE, out IReadOnlyList<EffectEvent> electricRain))
            {
                foreach (EffectEvent effect in electricRain)
                {
                    (int, int) lifespan = ProfHelper.ComputeEffectLifespan(log, effect, 2400);
                    var connector = new PositionConnector(effect.Position);
                    EnvironmentDecorations.Add(new CircleDecoration(true, 0, 100, lifespan, "rgba(200, 120, 0, 0.2)", connector));
                    EnvironmentDecorations.Add(new CircleDecoration(true, lifespan.Item2, 100, lifespan, "rgba(200, 120, 0, 0.2)", connector));
                }
            }

            // Jade Lob - Small deathly AoE
            // Pulsing Green Effect
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.KainengOverlookJadeLobPulsingGreen, out IReadOnlyList<EffectEvent> jadeLob))
            {
                foreach (EffectEvent effect in jadeLob)
                {
                    (int, int) lifespan = ProfHelper.ComputeEffectLifespan(log, effect, 1500);
                    var connector = new PositionConnector(effect.Position);
                    EnvironmentDecorations.Add(new CircleDecoration(false, 0, 100, lifespan, "rgba(250, 50, 0, 0.2)", connector));
                    EnvironmentDecorations.Add(new CircleDecoration(true, 0, 100, lifespan, "rgba(0, 200, 0, 0.2)", connector));
                }
            }

            // Enforcer Orbs AoE
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.KainengOverlookEnforcerOrbsAoE, out IReadOnlyList<EffectEvent> enforcerOrbsAoEs))
            {
                foreach (EffectEvent effect in enforcerOrbsAoEs)
                {
                    (int, int) lifespan = ProfHelper.ComputeEffectLifespan(log, effect, 2708);
                    var connector = new PositionConnector(effect.Position);
                    EnvironmentDecorations.Add(new CircleDecoration(false, 0, 100, lifespan, "rgba(250, 50, 0, 0.2)", connector));
                    EnvironmentDecorations.Add(new CircleDecoration(true, 0, 100, lifespan, "rgba(0, 0, 200, 0.2)", connector));
                }
            }

            // Rain Of Blades - Mindblade Red AoEs dropped by players
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.KainengOverlookMindbladeRainOfBladesRedAoECM, out IReadOnlyList<EffectEvent> mindbladeReds))
            {
                foreach (EffectEvent effect in mindbladeReds)
                {
                    (int, int) lifespan = ProfHelper.ComputeEffectLifespan(log, effect, 5000);
                    var connector = new PositionConnector(effect.Position);
                    int damageDelay = 2000;
                    int warningEnd = lifespan.Item1 + damageDelay;
                    EnvironmentDecorations.Add(new CircleDecoration(false, 0, 240, (lifespan.Item1, warningEnd), "rgba(250, 50, 0, 0.2)", connector));
                    EnvironmentDecorations.Add(new CircleDecoration(true, 0, 240, (lifespan.Item1, warningEnd), "rgba(250, 50, 0, 0.2)", connector));
                    EnvironmentDecorations.Add(new CircleDecoration(true, warningEnd, 240, (lifespan.Item1, warningEnd), "rgba(250, 50, 0, 0.2)", connector));
                    EnvironmentDecorations.Add(new CircleDecoration(true, 0, 240, (warningEnd, lifespan.Item2), "rgba(250, 50, 0, 0.4)", connector));
                }
            }

            // Rushing Justice - Enforcer Flames
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.KainengOverlookEnforcerRushingJusticeFlames, out IReadOnlyList<EffectEvent> rushingJustice))
            {
                foreach (EffectEvent effect in rushingJustice)
                {
                    (int, int) lifespan = ProfHelper.ComputeEffectLifespan(log, effect, 0);
                    var connector = new PositionConnector(effect.Position);
                    var rotationConnector = new AngleConnector(effect.Rotation.Z);
                    EnvironmentDecorations.Add(new RectangleDecoration(true, 0, 50, 145, lifespan, "rgba(250, 50, 0, 0.2)", connector).UsingRotationConnector(rotationConnector));
                }
            }

            // Spiritual Lightning AoEs - Ritualist
            if (log.CombatData.TryGetEffectEventsByGUID(EffectGUIDs.KainengOverlookRitualistSpiritualLightningAoE, out IReadOnlyList<EffectEvent> spiritualLightning))
            {
                foreach (EffectEvent effect in spiritualLightning)
                {
                    (int, int) lifespan = ProfHelper.ComputeEffectLifespan(log, effect, 2000);
                    var connector = new PositionConnector(effect.Position);
                    EnvironmentDecorations.Add(new CircleDecoration(true, 0, 90, lifespan, "rgba(200, 120, 0, 0.2)", connector));
                    EnvironmentDecorations.Add(new CircleDecoration(true, lifespan.Item2, 90, lifespan, "rgba(200, 120, 0, 0.2)", connector));
                }
            }
        }

        internal static void AddFallOfTheAxeDecoration(NPC target, CombatReplay replay, AngleConnector rotationConnector, (int, int) lifespan, int angle)
        {
            var connector = new AgentConnector(target);
            replay.Decorations.Add(new PieDecoration(false, 0, 480, angle, lifespan, "rgba(200, 120, 0, 0.2)", connector).UsingRotationConnector(rotationConnector));
            replay.Decorations.Add(new PieDecoration(true, 0, 480, angle, lifespan, "rgba(200, 120, 0, 0.2)", connector).UsingRotationConnector(rotationConnector));
            replay.Decorations.Add(new PieDecoration(true, lifespan.Item2, 480, angle, lifespan, "rgba(200, 120, 0, 0.2)", connector).UsingRotationConnector(rotationConnector));
        }

        private static void AddSharedDestructionDecoration(AbstractPlayer p, CombatReplay replay, (int, int) lifespan, bool isSuccessful)
        {
            string green = "rgba(0, 120, 0, 0.4)";
            string color = isSuccessful ? green : "rgba(120, 0, 0, 0.4)";
            var connector = new AgentConnector(p);
            replay.Decorations.Add(new CircleDecoration(true, lifespan.Item2, 180, lifespan, green, connector));
            replay.Decorations.Add(new CircleDecoration(true, 0, 180, lifespan, color, connector));
        }
    }
}
