﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GW2EIEvtcParser.EncounterLogic;
using GW2EIEvtcParser.Interfaces;
using GW2EIEvtcParser.ParsedData;
using static GW2EIEvtcParser.ArcDPSEnums;
using static GW2EIEvtcParser.EIData.DamageModifiersUtils;
using static GW2EIEvtcParser.ParserHelper;
using static GW2EIEvtcParser.SkillIDs;

namespace GW2EIEvtcParser.EIData
{
    public abstract class DamageModifier : IVersionable
    {

        private DamageType _compareType { get; }
        private DamageType _srcType { get; }
        private DamageSource _dmgSrc { get; }
        protected double GainPerStack { get; }
        internal GainComputer GainComputer { get; }
        private ulong _minBuild { get; set; } = GW2Builds.StartOfLife;
        private ulong _maxBuild { get; set; } = GW2Builds.EndOfLife;
        public bool Multiplier => GainComputer.Multiplier;
        public bool SkillBased => GainComputer.SkillBased;

        public bool Approximate { get; protected set; } = false;
        public ParserHelper.Source Src { get; }
        public string Icon { get; }
        public string Name { get; }
        public int ID { get; }
        public string Tooltip { get; protected set; }

        internal DamageModifierMode Mode { get; } = DamageModifierMode.All;
        private List<DamageLogChecker> _dlCheckers { get; set; }

        internal DamageModifier(string name, string tooltip, DamageSource damageSource, double gainPerStack, DamageType srctype, DamageType compareType, ParserHelper.Source src, string icon, GainComputer gainComputer, DamageModifierMode mode)
        {
            Tooltip = tooltip;
            Name = name;
            ID = Name.GetHashCode();
            _dmgSrc = damageSource;
            GainPerStack = gainPerStack;
            if (GainPerStack == 0.0)
            {
                throw new InvalidOperationException("Gain per stack can't be 0");
            }
            _compareType = compareType;
            _srcType = srctype;
            Src = src;
            Icon = icon;
            GainComputer = gainComputer;
            Mode = mode;
            switch (_dmgSrc)
            {
                case DamageSource.All:
                    Tooltip += "<br>Actor + Minions";
                    break;
                case DamageSource.NoPets:
                    Tooltip += "<br>No Minions";
                    break;
            }
            if (_srcType == DamageType.All)
            {
                throw new InvalidDataException("No known damage modifier that modifies every outgoing damage");
            } 
            Tooltip += "<br>Applied on " + _srcType.DamageTypeToString();
            Tooltip += "<br>Compared against " + _compareType.DamageTypeToString();
            if (!Multiplier)
            {
                Tooltip += "<br>Non multiplier";
            }
            _dlCheckers = new List<DamageLogChecker>();
        }

        internal DamageModifier WithBuilds(ulong minBuild, ulong maxBuild = GW2Builds.EndOfLife)
        {
            _minBuild = minBuild;
            _maxBuild = maxBuild;
            return this;
        }

        internal virtual DamageModifier UsingChecker(DamageLogChecker dlChecker)
        {
            _dlCheckers.Add(dlChecker);
            return this;
        }

        protected bool CheckCondition(AbstractHealthDamageEvent dl, ParsedEvtcLog log)
        {
            return _dlCheckers.All(checker => checker(dl, log));
        }

        public bool Available(CombatData combatData)
        {
            ulong gw2Build = combatData.GetBuildEvent().Build;
            return gw2Build < _maxBuild && gw2Build >= _minBuild;
        }

        internal virtual bool Keep(FightLogic.ParseMode mode, EvtcParserSettings parserSettings)
        {
            // Remove approx based damage mods from PvP contexts
            if (Approximate)
            {
                if (mode == FightLogic.ParseMode.WvW || mode == FightLogic.ParseMode.sPvP)
                {
                    return false;
                }
            }
            if (Mode == DamageModifierMode.All)
            {     
                return true;
            }
            switch (mode)
            {
                case FightLogic.ParseMode.Unknown:
                case FightLogic.ParseMode.OpenWorld:
                    return Mode == DamageModifierMode.PvE;
                case FightLogic.ParseMode.FullInstance:
                case FightLogic.ParseMode.Instanced5:
                case FightLogic.ParseMode.Instanced10:
                case FightLogic.ParseMode.Benchmark:
                    return Mode == DamageModifierMode.PvE || Mode == DamageModifierMode.PvEInstanceOnly || Mode == DamageModifierMode.PvEWvW;
                case FightLogic.ParseMode.WvW:
                    return (Mode == DamageModifierMode.WvW || Mode == DamageModifierMode.sPvPWvW || Mode == DamageModifierMode.PvEWvW);
                case FightLogic.ParseMode.sPvP:
                    return Mode == DamageModifierMode.sPvP || Mode == DamageModifierMode.sPvPWvW;
            }
            return false;
        }

        internal DamageModifier UsingApproximate(bool approximate)
        {
            if (!Approximate && approximate)
            {
                Tooltip += "<br>Approximate";
            }
            else if (Approximate && !approximate)
            {
                Tooltip = Tooltip.Replace("<br>Approximate", "");
            }
            Approximate = approximate;
            return this;
        }

        public int GetTotalDamage(AbstractSingleActor actor, ParsedEvtcLog log, AbstractSingleActor t, long start, long end)
        {
            FinalDPS damageData = actor.GetDPSStats(t, log, start, end);
            switch (_compareType)
            {
                case DamageType.All:
                    return _dmgSrc == DamageSource.All ? damageData.Damage : damageData.ActorDamage;
                case DamageType.Condition:
                    return _dmgSrc == DamageSource.All ? damageData.CondiDamage : damageData.ActorCondiDamage;
                case DamageType.Power:
                    return _dmgSrc == DamageSource.All ? damageData.PowerDamage : damageData.ActorPowerDamage;
                case DamageType.LifeLeech:
                    return _dmgSrc == DamageSource.All ? damageData.LifeLeechDamage : damageData.ActorLifeLeechDamage;
                case DamageType.Strike:
                    return _dmgSrc == DamageSource.All ? damageData.StrikeDamage : damageData.ActorStrikeDamage;
                case DamageType.StrikeAndCondition:
                    return _dmgSrc == DamageSource.All ? damageData.StrikeDamage + damageData.CondiDamage : damageData.ActorStrikeDamage + damageData.ActorCondiDamage;
                case DamageType.StrikeAndConditionAndLifeLeech:
                    return _dmgSrc == DamageSource.All ? damageData.StrikeDamage + damageData.CondiDamage + damageData.LifeLeechDamage : damageData.ActorStrikeDamage + damageData.ActorCondiDamage + damageData.ActorLifeLeechDamage;
                default:
                    throw new NotImplementedException("Not implemented damage type " + _compareType);
            }
        }

        public IReadOnlyList<AbstractHealthDamageEvent> GetHitDamageEvents(AbstractSingleActor actor, ParsedEvtcLog log, AbstractSingleActor t, long start, long end)
        {
            return _dmgSrc == DamageSource.All ? actor.GetHitDamageEvents(t, log, start, end, _srcType) : actor.GetJustActorHitDamageEvents(t, log, start, end, _srcType);
        }

        internal abstract List<DamageModifierEvent> ComputeDamageModifier(AbstractSingleActor actor, ParsedEvtcLog log);

    }
}
