﻿using System;
using System.Security.Cryptography.X509Certificates;

namespace GW2EIEvtcParser.EIData
{
    internal abstract class GenericAttachedDecoration : GenericDecoration
    {

        public Connector ConnectedTo { get; }
        public RotationConnector RotationConnectedTo { get; protected set; }
        public SkillConnector Owner;
        public SkillModeDescriptor Skill;

        protected GenericAttachedDecoration((int start, int end) lifespan, Connector connector) : base(lifespan)
        {
            ConnectedTo = connector;
        }

        /// <summary>Creates a new line towards the other decoration</summary>
        public LineDecoration LineTo(GenericAttachedDecoration other, int growing, string color)
        {
            int start = Math.Max(Lifespan.start, other.Lifespan.start);
            int end = Math.Min(Lifespan.end, other.Lifespan.end);
            return new LineDecoration(growing, (start, end), color, ConnectedTo, other.ConnectedTo);
        }

        public virtual GenericAttachedDecoration UsingRotationConnector(RotationConnector rotationConnectedTo)
        {
            RotationConnectedTo = rotationConnectedTo;
            return this;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="skill">Skill information</param>
        /// <returns></returns>
        public virtual GenericAttachedDecoration UsingSkillMode(SkillModeDescriptor skill)
        {
            Skill = skill;
            Owner = new SkillConnector(skill.Owner);
            return this;
        }
    }
}
