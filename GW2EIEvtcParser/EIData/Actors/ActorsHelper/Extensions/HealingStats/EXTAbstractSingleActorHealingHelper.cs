﻿using System;
using System.Collections.Generic;
using System.Linq;
using GW2EIEvtcParser.Exceptions;
using GW2EIEvtcParser.Extensions;
using GW2EIEvtcParser.ParsedData;
using static GW2EIEvtcParser.EIData.Buff;
using static GW2EIEvtcParser.Extensions.ExtensionHelper;
using static GW2EIEvtcParser.ParserHelper;

namespace GW2EIEvtcParser.EIData
{
    public class EXTAbstractSingleActorHealingHelper : EXTActorHealingHelper
    {
        private AbstractSingleActor _actor { get; }
        private AgentItem _agentItem => _actor.AgentItem;

        private readonly Dictionary<EXTHealingType, CachingCollectionWithTarget<List<EXTAbstractHealingEvent>>> _typedSelfHealEvents = new Dictionary<EXTHealingType, CachingCollectionWithTarget<List<EXTAbstractHealingEvent>>>();

        private readonly Dictionary<EXTHealingType, CachingCollectionWithTarget<int[]>> _healing1S = new Dictionary<EXTHealingType, CachingCollectionWithTarget<int[]>>();

        internal EXTAbstractSingleActorHealingHelper(AbstractSingleActor actor) : base()
        {
            _actor = actor;
        }

        public override IReadOnlyList<EXTAbstractHealingEvent> GetOutgoingHealEvents(AbstractSingleActor target, ParsedEvtcLog log, long start, long end)
        {
            if (HealEvents == null)
            {
                HealEvents = new List<EXTAbstractHealingEvent>();
                HealEvents.AddRange(log.CombatData.EXTHealingCombatData.GetHealData(_agentItem).Where(x => x.ToFriendly));
                IReadOnlyDictionary<long, Minions> minionsList = _actor.GetMinions(log);
                foreach (Minions mins in minionsList.Values)
                {
                    HealEvents.AddRange(mins.EXTHealing.GetOutgoingHealEvents(null, log, 0, log.FightData.FightEnd));
                }
                HealEvents = HealEvents.OrderBy(x => x.Time).ToList();
                HealEventsByDst = HealEvents.GroupBy(x => x.To).ToDictionary(x => x.Key, x => x.ToList());
            }
            if (target != null)
            {
                if (HealEventsByDst.TryGetValue(target.AgentItem, out List<EXTAbstractHealingEvent> list))
                {
                    return list.Where(x => x.Time >= start && x.Time <= end).ToList();
                }
                else
                {
                    return new List<EXTAbstractHealingEvent>();
                }
            }
            return HealEvents.Where(x => x.Time >= start && x.Time <= end).ToList();
        }

        public override IReadOnlyList<EXTAbstractHealingEvent> GetIncomingHealEvents(AbstractSingleActor target, ParsedEvtcLog log, long start, long end)
        {
            if (HealReceivedEvents == null)
            {
                HealReceivedEvents = new List<EXTAbstractHealingEvent>();
                HealReceivedEvents.AddRange(log.CombatData.EXTHealingCombatData.GetHealReceivedData(_agentItem).Where(x => x.ToFriendly));
                IReadOnlyDictionary<long, Minions> minionsList = _actor.GetMinions(log);
                foreach (Minions mins in minionsList.Values)
                {
                    HealEvents.AddRange(mins.EXTHealing.GetIncomingHealEvents(null, log, 0, log.FightData.FightEnd));
                }
                HealReceivedEvents = HealReceivedEvents.OrderBy(x => x.Time).ToList();
                HealReceivedEventsByDst = HealReceivedEvents.GroupBy(x => x.To).ToDictionary(x => x.Key, x => x.ToList());
            }
            if (target != null)
            {
                if (HealReceivedEventsByDst.TryGetValue(target.AgentItem, out List<EXTAbstractHealingEvent> list))
                {
                    return list.Where(x => x.Time >= start && x.Time <= end).ToList();
                }
                else
                {
                    return new List<EXTAbstractHealingEvent>();
                }
            }
            return HealReceivedEvents.Where(x => x.Time >= start && x.Time <= end).ToList();
        }

        public IReadOnlyList<EXTAbstractHealingEvent> GetJustActorOutgoingHealEvents(AbstractSingleActor target, ParsedEvtcLog log, long start, long end)
        {
            return GetOutgoingHealEvents(target, log, start, end).Where(x => x.From == _agentItem).ToList();
        }

        internal IReadOnlyList<EXTAbstractHealingEvent> GetJustActorTypedOutgoingHealEvents(AbstractSingleActor target, ParsedEvtcLog log, long start, long end, EXTHealingType healingType)
        {
            if (!_typedSelfHealEvents.TryGetValue(healingType, out CachingCollectionWithTarget<List<EXTAbstractHealingEvent>> healEventsPerPhasePerTarget))
            {
                healEventsPerPhasePerTarget = new CachingCollectionWithTarget<List<EXTAbstractHealingEvent>>(log);
                _typedSelfHealEvents[healingType] = healEventsPerPhasePerTarget;
            }
            if (!healEventsPerPhasePerTarget.TryGetValue(start, end, target, out List<EXTAbstractHealingEvent> dls))
            {
                dls = GetTypedOutgoingHealEvents(target, log, start, end, healingType).Where(x => x.From == _agentItem).ToList();
                healEventsPerPhasePerTarget.Set(start, end, target, dls);
            }
            return dls;
        }

        public IReadOnlyList<int> Get1SHealingList(ParsedEvtcLog log, long start, long end, AbstractSingleActor target, EXTHealingType healingType = EXTHealingType.All )
        {
            if (!_healing1S.TryGetValue(healingType, out CachingCollectionWithTarget<int[]> graphs))
            {
                graphs = new CachingCollectionWithTarget<int[]>(log);
                _healing1S[healingType] = graphs;
            }
            if (!graphs.TryGetValue(start, end, target, out int[] graph))
            {
                int durationInMS = (int)(end - start);
                int durationInS = durationInMS / 1000;
                graph = durationInS * 1000 != durationInMS ? new int[durationInS + 2] : new int[durationInS + 1];
                // fill the graph
                int previousTime = 0;
                foreach (EXTAbstractHealingEvent dl in GetTypedOutgoingHealEvents(target, log, start, end, healingType))
                {
                    int time = (int)Math.Ceiling((dl.Time - start) / 1000.0);
                    if (time != previousTime)
                    {
                        for (int i = previousTime + 1; i <= time; i++)
                        {
                            graph[i] = graph[previousTime];
                        }
                    }
                    previousTime = time;
                    graph[time] += dl.HealingDone;
                }
                for (int i = previousTime + 1; i < graph.Length; i++)
                {
                    graph[i] = graph[previousTime];
                }
                //
                graphs.Set(start, end, target, graph);
            }
            return graph;
        }
    }
}
