﻿using System.Collections.Generic;

namespace GW2EIEvtcParser.EIData
{
    internal class ActorOrientationDecoration : FacingDecoration
    {

        public ActorOrientationDecoration((int start, int end) lifespan, AgentConnector connector, IReadOnlyList<ParametricPoint3D> facings) : base(lifespan, connector, facings)
        {
        }

        //

        public override GenericDecorationCombatReplayDescription GetCombatReplayDescription(CombatReplayMap map, ParsedEvtcLog log)
        {
            return new ActorOrientationDecorationCombatReplayDescription(log, this, map);
        }

        public override GenericAttachedDecoration UsingSkillMode(AbstractSingleActor owner, SkillModeCategory category = SkillModeCategory.NotApplicable, ParserHelper.Spec Spec = ParserHelper.Spec.Unknown, long skillID = 0)
        {
            return this;
        }
    }
}
