﻿using System.Collections.Generic;
using System.Linq;
using GW2EIEvtcParser.EIData.Buffs;
using static GW2EIEvtcParser.ArcDPSEnums;
using static GW2EIEvtcParser.EIData.Buff;
using static GW2EIEvtcParser.ParserHelper;
using static GW2EIEvtcParser.SkillIDs;

namespace GW2EIEvtcParser.EIData
{
    internal static class DruidHelper
    {
        internal static readonly List<InstantCastFinder> InstantCastFinder = new List<InstantCastFinder>()
        {
            new BuffGainCastFinder(EnterCelestialAvatar,CelestialAvatar).UsingBeforeWeaponSwap(true), // Celestial Avatar
            new BuffLossCastFinder(ExitCelestialAvatar,CelestialAvatar).UsingBeforeWeaponSwap(true), // Release Celestial Avatar
            new DamageCastFinder(GlyphOfEquality, GlyphOfEquality).UsingEnable((combatData) => !combatData.HasEffectData), // Disable this one when effect events are present
            new EffectCastFinderByDst(GlyphOfEqualityCA, EffectGUIDs.DruidGlyphOfEqualityCA).UsingChecker((evt, combatData, agentData, skillData) => evt.Dst.Spec == Spec.Druid),
            new EffectCastFinder(GlyphOfEquality, EffectGUIDs.DruidGlyphOfEquality).UsingChecker((evt, combatData, agentData, skillData) => evt.Src.Spec == Spec.Druid)
        };

        private static readonly HashSet<long> _celestialAvatar = new HashSet<long>
        {
            EnterCelestialAvatar, ExitCelestialAvatar
        };

        public static bool IsCelestialAvatarTransform(long id)
        {
            return _celestialAvatar.Contains(id);
        }

        internal static readonly List<DamageModifier> DamageMods = new List<DamageModifier>
        {
        };

        internal static readonly List<Buff> Buffs = new List<Buff>
        {
            new Buff("Celestial Avatar", CelestialAvatar, Source.Druid, BuffClassification.Other, BuffImages.CelestialAvatar),
            new Buff("Ancestral Grace", AncestralGrace, Source.Druid, BuffClassification.Other, BuffImages.AncestralGrace),
            new Buff("Glyph of Empowerment", GlyphOfEmpowerment, Source.Druid, BuffClassification.Offensive, BuffImages.GlyphOfTheStars).WithBuilds(GW2Builds.StartOfLife, GW2Builds.April2019Balance),
            new Buff("Glyph of Unity", GlyphOfUnityEffect, Source.Druid, BuffClassification.Other, BuffImages.GlyphOfUnity),
            new Buff("Glyph of Unity (CA)", GlyphOfUnityEffectCA, Source.Druid, BuffClassification.Other, BuffImages.GlyphOfUnityCelestialAvatar),
            new Buff("Glyph of the Stars", GlyphOfTheStars, Source.Druid, BuffClassification.Defensive, BuffImages.GlyphOfTheStars).WithBuilds(GW2Builds.April2019Balance, GW2Builds.October2022Balance),
            new Buff("Glyph of the Stars (CA)", GlyphOfTheStarsCA, Source.Druid, BuffClassification.Defensive, BuffImages.GlyphOfEmpowermentCelestialAvatar).WithBuilds(GW2Builds.April2019Balance, GW2Builds.EndOfLife),
            new Buff("Natural Mender", NaturalMender, Source.Druid, BuffStackType.Stacking, 10, BuffClassification.Other, BuffImages.NaturalMender).WithBuilds(GW2Builds.StartOfLife, GW2Builds.October2022Balance),
            new Buff("Natural Mender", NaturalMender, Source.Druid, BuffClassification.Other, BuffImages.NaturalMender).WithBuilds(GW2Builds.October2022Balance),
            new Buff("Lingering Light", LingeringLight, Source.Druid, BuffClassification.Other, BuffImages.LingeringLight),
        };

    }
}
