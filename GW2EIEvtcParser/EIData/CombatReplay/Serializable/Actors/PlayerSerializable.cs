﻿using System.Collections.Generic;
using System.Linq;

namespace GW2EIEvtcParser.EIData
{
    public class PlayerSerializable : AbstractSingleActorSerializable
    {
        public int Group { get; }

        internal PlayerSerializable(AbstractPlayer player, ParsedEvtcLog log, CombatReplayMap map, CombatReplay replay) : base(player, log, map, replay, !log.FriendlyAgents.Contains(player.AgentItem) ? "TargetPlayer" : "Player")
        {
            Group = player.Group;
            SetStatus(log, player);
        }

    }
}
