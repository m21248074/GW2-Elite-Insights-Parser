/*jshint esversion: 6 */
"use strict";

function healingGraphTypeEnumToString(mode, healingMode) {
    var name = "";
    switch (mode) {
        case GraphType.DPS:
            name = healingMode === HealingType.Barrier ? "秒屏障" : "秒治療";
            break;
        case GraphType.CenteredDPS:
            name = healingMode === HealingType.Barrier ? "中位秒屏障" : "中位秒治療";
            break;
        case GraphType.Damage:
            name = healingMode === HealingType.Barrier ? "屏障" : "治療";
            break;
        default:
            break;
    }
    return name;
}

function healingTypeEnumToString(mode) {
    var name = "";
    switch (mode) {
        case HealingType.All:
            name = "全部";
            break;
        case HealingType.HealingPower:
            name = "治療效果";
            break;
        case HealingType.Conversion:
            name = "轉換(傷害->治療)";
            break;
        case HealingType.Hybrid:
            name = "治療效果 或 轉換(傷害->治療)";
            break;
        case HealingType.Downed:
            name = "倒地治療";
            break;
        case HealingType.Barrier:
            name = "治療效果";
            break;
        default:
            break;
    }
    return name;
}

function getHPSGraphCacheID(hpsmode, healingmode, graphmode, activetargets, phaseIndex, extra) {
    return "hps" + hpsmode + '-'+ healingmode + '-' + graphmode + '-' + getTargetCacheID(activetargets) + '-' + phaseIndex + (extra !== null ? '-' + extra : '');
}

function getHealingGraphName(healingMode, graphMode) {
    return healingTypeEnumToString(healingMode) + " " + healingGraphTypeEnumToString(graphMode, healingMode) + " 圖";
}

function computePlayersHealingGraphData(graph, data, yaxis) {
    var offset = 0;
    for (var i = 0; i < logData.players.length; i++) {
        var player = logData.players[i];
        if (player.isFake) {
            continue;
        }
        offset += computePlayerHealthData(graph.players[i].healthStates, player, data, yaxis)
        offset += computePlayerBarrierData(graph.players[i].barrierStates, player, data, yaxis)
    }
    return offset;
}