﻿using System.Collections.Generic;
using GW2EIEvtcParser.ParsedData;
using static GW2EIEvtcParser.EIData.Buff;

namespace GW2EIEvtcParser.EIData
{
    internal static class MirageHelper
    {

        internal static readonly List<InstantCastFinder> InstantCastFinder = new List<InstantCastFinder>()
        {
            new DamageCastFinder(45449, 45449, EIData.InstantCastFinder.DefaultICD), // Jaunt
            new BuffGainCastFinder(SkillIDs.MirageCloakDodge, 40408, EIData.InstantCastFinder.DefaultICD), // Mirage Cloak
        };

        internal static readonly List<DamageModifier> DamageMods = new List<DamageModifier>
        {
        };

        internal static readonly List<Buff> Buffs = new List<Buff>
        {
                new Buff("Mirage Cloak",40408, ParserHelper.Source.Mirage, BuffClassification.Other, "https://wiki.guildwars2.com/images/a/a5/Mirage_Cloak_%28effect%29.png"),
                new Buff("False Oasis",40802, ParserHelper.Source.Mirage, BuffClassification.Other, "https://wiki.guildwars2.com/images/3/32/False_Oasis.png"),
        };

        private static HashSet<long> Minions = new HashSet<long>();
        internal static bool IsKnownMinionID(long id)
        {
            return Minions.Contains(id);
        }
    }
}
