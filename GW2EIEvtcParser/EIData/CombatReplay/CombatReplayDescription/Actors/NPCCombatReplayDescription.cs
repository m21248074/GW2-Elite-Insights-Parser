﻿using System.Linq;
using GW2EIEvtcParser.ParsedData;

namespace GW2EIEvtcParser.EIData
{
    internal class NPCCombatReplayDescription : AbstractSingleActorCombatReplayDescription
    {
        public int MasterID { get; }

        internal NPCCombatReplayDescription(NPC npc, ParsedEvtcLog log, CombatReplayMap map, CombatReplay replay) : base(npc, log, map, replay)
        {
            if (log.FriendlyAgents.Contains(npc.AgentItem))
            {
                SetStatus(log, npc);
            }
            SetBreakbarStatus(log, npc);
            AgentItem master = npc.AgentItem.GetFinalMaster();
            // Don't put minions of NPC or unknown minions into the minion display system
            if (master != npc.AgentItem && master.IsPlayer && ParserHelper.IsKnownMinionID(npc.AgentItem, master.Spec))
            {
                MasterID = master.UniqueID;
            }
        }
    }
}
