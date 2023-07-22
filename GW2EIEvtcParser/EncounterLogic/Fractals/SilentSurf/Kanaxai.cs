﻿using System;
using System.Collections.Generic;
using System.Linq;
using GW2EIEvtcParser.EIData;
using GW2EIEvtcParser.Exceptions;
using GW2EIEvtcParser.Extensions;
using GW2EIEvtcParser.ParsedData;
using static GW2EIEvtcParser.SkillIDs;
using static GW2EIEvtcParser.ParserHelper;
using static GW2EIEvtcParser.EncounterLogic.EncounterCategory;
using static GW2EIEvtcParser.EncounterLogic.EncounterLogicUtils;
using static GW2EIEvtcParser.EncounterLogic.EncounterLogicPhaseUtils;
using static GW2EIEvtcParser.EncounterLogic.EncounterLogicTimeUtils;
using static GW2EIEvtcParser.EncounterLogic.EncounterImages;

namespace GW2EIEvtcParser.EncounterLogic
{
    internal class Kanaxai : SilentSurf
    {
        public Kanaxai(int triggerID) : base(triggerID)
        {
            MechanicList.AddRange(new List<Mechanic>
            {
                new PlayerDstHitMechanic(RendingStorm, "Rending Storm", new MechanicPlotlySetting(Symbols.CircleXOpen, Colors.Red), "RendStm.H", "Hit by Rending Storm (Axe AoE)", "Rending Storm Hit", 0),
                new PlayerDstHitMechanic(new long [] { HarrowshotDeath, HarrowshotExposure, HarrowshotFear, HarrowshotLethargy, HarrowshotTorment }, "Harrowshot", new MechanicPlotlySetting(Symbols.Circle, Colors.Orange), "Harrowshot.H", "Harrowshot (Lost all boons)", "Harrowshot (Boonstrip)", 0),
                new PlayerDstBuffApplyMechanic(ExtremeVulnerability, "Extreme Vulnerability", new MechanicPlotlySetting(Symbols.X, Colors.DarkRed), "ExtVuln.A", "Applied Extreme Vulnerability", "Extreme Vulnerability Application", 150),
                new PlayerDstBuffApplyMechanic(ExposedPlayer, "Exposed", new MechanicPlotlySetting(Symbols.TriangleRight, Colors.Pink), "Expo.A", "Applied Exposed", "Exposed Application (Player)", 0),
                new PlayerDstBuffApplyMechanic(Fear, "Fear", new MechanicPlotlySetting(Symbols.TriangleUp, Colors.Yellow), "Fear.A", "Fear Applied", "Fear Application", 150),
                new PlayerDstBuffApplyMechanic(Phantasmagoria, "Phantasmagoria", new MechanicPlotlySetting(Symbols.Diamond, Colors.Pink), "Phant.A", "Phantasmagoria Applied (Aspect visible on Island)", "Phantasmagoria Application", 150),
                new EnemyDstBuffApplyMechanic(Exposed31589, "Exposed", new MechanicPlotlySetting(Symbols.TriangleLeft, Colors.Pink), "Expo.A", "Applied Exposed to Kanaxai", "Exposed Application (Kanaxai)", 150),
            });
            Extension = "kanaxai";
            Icon = EncounterIconKanaxai;
            EncounterID |= 0x000001;
        }

        protected override CombatReplayMap GetCombatMapInternal(ParsedEvtcLog log)
        {
            return new CombatReplayMap(CombatReplayKanaxai,
                           (334, 370),
                           (-6195, -295, -799, 5685));
        }

        protected override HashSet<int> GetUniqueNPCIDs()
        {
            return new HashSet<int>
            {
                (int)ArcDPSEnums.TargetID.KanaxaiScytheOfHouseAurkusCM,
            };
        }

        protected override List<int> GetTargetsIDs()
        {
            return new List<int>
            {
                (int)ArcDPSEnums.TargetID.KanaxaiScytheOfHouseAurkusCM,
                (int)ArcDPSEnums.TrashID.AspectOfTorment,
                (int)ArcDPSEnums.TrashID.AspectOfLethargy,
                (int)ArcDPSEnums.TrashID.AspectOfExposure,
                (int)ArcDPSEnums.TrashID.AspectOfDeath,
                (int)ArcDPSEnums.TrashID.AspectOfFear,
            };
        }

        internal override FightData.EncounterMode GetEncounterMode(CombatData combatData, AgentData agentData, FightData fightData)
        {
            return FightData.EncounterMode.CMNoName;
        }

        internal override void EIEvtcParse(ulong gw2Build, FightData fightData, AgentData agentData, List<CombatItem> combatData, IReadOnlyDictionary<uint, AbstractExtensionHandler> extensions)
        {
            base.EIEvtcParse(gw2Build, fightData, agentData, combatData, extensions);
            var aspectCounts = new Dictionary<int, int>();
            foreach (AbstractSingleActor actor in Targets)
            {
                switch(actor.ID)
                {
                    case (int)ArcDPSEnums.TrashID.AspectOfTorment:
                    case (int)ArcDPSEnums.TrashID.AspectOfLethargy:
                    case (int)ArcDPSEnums.TrashID.AspectOfExposure:
                    case (int)ArcDPSEnums.TrashID.AspectOfDeath:
                    case (int)ArcDPSEnums.TrashID.AspectOfFear:
                        if (aspectCounts.TryGetValue(actor.ID, out int count))
                        {
                            actor.OverrideName(actor.Character + " " + count);
                            aspectCounts[actor.ID] = count + 1;
                        } 
                        else
                        {
                            actor.OverrideName(actor.Character + " 1");
                            aspectCounts[actor.ID] = 2;
                        }
                        break;
                }
            }
        }

        internal override List<PhaseData> GetPhases(ParsedEvtcLog log, bool requirePhases)
        {
            List<PhaseData> phases = GetInitialPhase(log);
            AbstractSingleActor kanaxai = Targets.FirstOrDefault(x => x.IsSpecies(ArcDPSEnums.TargetID.KanaxaiScytheOfHouseAurkusCM));
            if (kanaxai == null)
            {
                throw new MissingKeyActorsException("Kanaxai not found");
            }
            phases[0].AddTarget(kanaxai);
            if (!requirePhases)
            {
                return phases;
            }
            // Phases
            List<PhaseData> encounterPhases = GetPhasesByInvul(log, DeterminedToDestroy, kanaxai, true, true);

            var worldCleaverPhaseStarts = log.CombatData.GetBuffData(DeterminedToDestroy).OfType<BuffApplyEvent
                >().Where(x => x.To == kanaxai.AgentItem).Select(x => x.Time).ToList();
            int worldCleaverCount = 0;
            int repeatedCount = 0;
            var isRepeatedWorldCleaverPhase = new List<bool>();
            for (int i = 0; i < encounterPhases.Count; i++)
            {
                PhaseData curPhase = encounterPhases[i];
                if (worldCleaverPhaseStarts.Any(x => curPhase.Start == x))
                {
                    var baseName = "World Cleaver ";
                    long midPhase = (curPhase.Start + curPhase.End) / 2;
                    if (kanaxai.GetCurrentHealthPercent(log, midPhase) > 50)
                    {
                        if (repeatedCount == 0)
                        {
                            isRepeatedWorldCleaverPhase.Add(false);
                            curPhase.Name = baseName + (++worldCleaverCount);
                        } 
                        else
                        {
                            isRepeatedWorldCleaverPhase.Add(true);
                            curPhase.Name = baseName + (worldCleaverCount) + " Repeated " + repeatedCount;
                        }
                        repeatedCount++;
                    } 
                    else if (kanaxai.GetCurrentHealthPercent(log, midPhase) > 25)
                    {
                        if (worldCleaverCount == 1)
                        {
                            repeatedCount = 0;
                        }
                        if (repeatedCount == 0)
                        {
                            isRepeatedWorldCleaverPhase.Add(false);
                            curPhase.Name = baseName + (++worldCleaverCount);
                        }
                        else
                        {
                            isRepeatedWorldCleaverPhase.Add(true);
                            curPhase.Name = baseName + (worldCleaverCount) + " Repeated " + repeatedCount;
                        }
                        repeatedCount++;
                    } 
                    else
                    {
                        // No hp update events, buggy log
                        return phases;
                    }
                    foreach (AbstractSingleActor aspect in Targets)
                    {
                        switch (aspect.ID)
                        {
                            case (int)ArcDPSEnums.TrashID.AspectOfTorment:
                            case (int)ArcDPSEnums.TrashID.AspectOfLethargy:
                            case (int)ArcDPSEnums.TrashID.AspectOfExposure:
                            case (int)ArcDPSEnums.TrashID.AspectOfDeath:
                            case (int)ArcDPSEnums.TrashID.AspectOfFear:
                                if (log.CombatData.GetBuffRemoveAllData(Determined762).Any(x => x.To == aspect.AgentItem && x.Time >= curPhase.Start && x.Time <= curPhase.End))
                                {
                                    curPhase.AddTarget(aspect);
                                }
                                break;
                        }
                    }
                    curPhase.AddTarget(kanaxai);
                } 
                else
                {
                    isRepeatedWorldCleaverPhase.Add(false);
                }
            }
            // Handle main phases after world cleave phases as we need to know if it is a repeated phase
            int phaseCount = 0;
            for (int i = 0; i < encounterPhases.Count; i++)
            {
                PhaseData curPhase = encounterPhases[i];
                if (!worldCleaverPhaseStarts.Any(x => curPhase.Start == x))
                {
                    var baseName = "Phase ";
                    if (i < isRepeatedWorldCleaverPhase.Count - 1)
                    {
                        if (isRepeatedWorldCleaverPhase[i + 1])
                        {
                            curPhase.Name = baseName + (phaseCount) + " Repeated " + (++repeatedCount);
                        } 
                        else
                        {
                            curPhase.Name = baseName + (++phaseCount);
                            repeatedCount = 0;
                        }
                    } 
                    else
                    {
                        curPhase.Name = baseName + (++phaseCount);
                    }
                    curPhase.AddTarget(kanaxai);
                }
            }
            phases.AddRange(encounterPhases);

            return phases;
        }

        internal override void CheckSuccess(CombatData combatData, AgentData agentData, FightData fightData, IReadOnlyCollection<AgentItem> playerAgents)
        {
            AbstractSingleActor kanaxai = Targets.FirstOrDefault(x => x.IsSpecies(ArcDPSEnums.TargetID.KanaxaiScytheOfHouseAurkusCM));
            if (kanaxai == null)
            {
                throw new MissingKeyActorsException("Kanaxai not found");
            }
            BuffApplyEvent invul762Gain = combatData.GetBuffData(Determined762).OfType<BuffApplyEvent>().Where(x => x.To == kanaxai.AgentItem).FirstOrDefault();
            if (invul762Gain != null)
            {
                fightData.SetSuccess(true, invul762Gain.Time);
            }
        }

    }
}
