﻿using System.Collections.Generic;
using GW2EIEvtcParser.ParsedData;

namespace GW2EIEvtcParser.EIData
{

    internal abstract class EnemyCastMechanic : CastMechanic
    {

        public EnemyCastMechanic(long mechanicID, string inGameName, MechanicPlotlySetting plotlySetting, string shortName, string description, string fullName, int internalCoolDown, CastChecker condition = null) : base(mechanicID, inGameName, plotlySetting, shortName, description, fullName, internalCoolDown, condition)
        {
            IsEnemyMechanic = true;
        }

        public EnemyCastMechanic(long[] mechanicIDs, string inGameName, MechanicPlotlySetting plotlySetting, string shortName, string description, string fullName, int internalCoolDown, CastChecker condition = null) : base(mechanicIDs, inGameName, plotlySetting, shortName, description, fullName, internalCoolDown, condition)
        {
            IsEnemyMechanic = true;
        }

        internal override void CheckMechanic(ParsedEvtcLog log, Dictionary<Mechanic, List<MechanicEvent>> mechanicLogs, Dictionary<int, AbstractSingleActor> regroupedMobs)
        {
            foreach (long mechanicID in MechanicIDs)
            {
                foreach (AbstractCastEvent c in log.CombatData.GetAnimatedCastData(mechanicID))
                {
                    AbstractSingleActor amp = null;
                    if (Keep(c, log))
                    {
                        amp = MechanicHelper.FindEnemyActor(log, c.Caster, regroupedMobs);
                    }
                    if (amp != null)
                    {
                        mechanicLogs[this].Add(new MechanicEvent(GetTime(c), this, amp));
                    }
                }
            }

        }
    }
}
