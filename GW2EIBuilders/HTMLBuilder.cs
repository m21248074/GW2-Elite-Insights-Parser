﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GW2EIBuilders.HtmlModels;
using GW2EIEvtcParser;
using GW2EIEvtcParser.EIData;
using GW2EIEvtcParser.ParsedData;
using Newtonsoft.Json;
using System.Linq;

namespace GW2EIBuilders
{
    public class HTMLBuilder
    {
        private static readonly UTF8Encoding NoBOMEncodingUTF8 = new UTF8Encoding(false);

        private readonly string _eiJS;
        private readonly string _eiCRJS;

        private readonly string _scriptVersion;
        private readonly int _scriptVersionRev;

        private readonly ParsedEvtcLog _log;
        private readonly Version _parserVersion;
        private readonly bool _cr;
        private readonly bool _light;
        private readonly bool _externalScripts;
        private readonly string _externalScriptsPath;
        private readonly string _externalScriptsCdn;

        private readonly string[] _uploadLink;

        private readonly Dictionary<long, Buff> _usedBuffs = new Dictionary<long, Buff>();
        private readonly HashSet<DamageModifier> _usedDamageMods = new HashSet<DamageModifier>();
        private readonly Dictionary<long, SkillItem> _usedSkills = new Dictionary<long, SkillItem>();

        public HTMLBuilder(ParsedEvtcLog log, HTMLSettings settings, HTMLAssets assets, Version parserVersion, string[] uploadString = null)
        {
            if (settings == null)
            {
                throw new InvalidDataException("Missing settings in HTMLBuilder");
            }
            _eiJS = assets.EIJavascriptCode;
            _eiCRJS = assets.EICRJavascriptCode;
            _parserVersion = parserVersion;
            _scriptVersion = parserVersion.Major + "." + parserVersion.Minor;
#if !DEBUG
            _scriptVersion += "." + parserVersion.Build;
#else
            _scriptVersion += "-debug";
#endif
            _scriptVersionRev = parserVersion.Revision;
            _log = log;

            _uploadLink = uploadString ?? new string[] { "", "", "" };

            _cr = _log.CanCombatReplay;
            _light = settings.HTMLLightTheme;
            _externalScripts = settings.ExternalHTMLScripts;
            _externalScriptsPath = settings.ExternalHtmlScriptsPath;
            _externalScriptsCdn = settings.ExternalHtmlScriptsCdn;
        }

        /// <summary>
        /// Create the damage taken distribution table for a given player
        /// </summary>
        /// <param name="p"></param>
        /// <param name="phaseIndex"></param>

        public void CreateHTML(StreamWriter sw, string path)
        {
            string html = Properties.Resources.tmplMain;
            _log.UpdateProgressWithCancellationCheck("HTML: replacing global variables");
            html = html.Replace("${bootstrapTheme}", !_light ? "slate" : "yeti");

            _log.UpdateProgressWithCancellationCheck("HTML: building CSS");
            html = html.Replace("<!--${Css}-->", BuildCss(path));
            _log.UpdateProgressWithCancellationCheck("HTML: building JS");
            html = html.Replace("<!--${Js}-->", BuildEIJs(path));
            _log.UpdateProgressWithCancellationCheck("HTML: building Combat Replay JS");
            html = html.Replace("<!--${CombatReplayJS}-->", BuildCombatReplayJS(path));

            html = html.Replace("'${logDataJson}'", ToJson(LogDataDto.BuildLogData(_log, _usedSkills, _usedBuffs, _usedDamageMods, _cr, _light, _parserVersion, _uploadLink)));

            _log.UpdateProgressWithCancellationCheck("HTML: building Graph Data");
            html = html.Replace("'${graphDataJson}'", ToJson(new ChartDataDto(_log)));

            sw.Write(html);
            return;
        }

        private string BuildCombatReplayJS(string path)
        {
            if (!_cr)
            {
                return "";
            }
            string scriptContent = _eiCRJS;
            if (_externalScripts && path != null)
            {
                string jsFileName = "EliteInsights-CR-" + _scriptVersion + ".js";
                string jsPath = Path.Combine(path, jsFileName);
                string jsPrePath = "./" + jsFileName;

                // Setting: External Scripts Path
                // overwrite jsPath (create directory) if files should be placed on different location
                // settings.externalHtmlScriptsPath is set by the user
                if (!string.IsNullOrWhiteSpace(_externalScriptsPath))
                {
                    string htmlExternalScriptsPath = _externalScriptsPath;
                    bool validPath = false;

                    if (!System.IO.Directory.Exists(htmlExternalScriptsPath))
                    {
                        try
                        {
                            System.IO.Directory.CreateDirectory(htmlExternalScriptsPath);
                            validPath = true;
                        }
                        catch
                        {
                            // something went wrong on creating the external folder (invalid chars?)      
                            // this will skip the saving in this path and continue with jsscript files in the root path for the report
                        }
                    }
                    else
                    {
                        validPath = true;
                    }

                    // if the creation of the folder did not fail or the folder already exists use it to include within the report
                    if (validPath)
                    {
                        jsPath = Path.Combine(htmlExternalScriptsPath, jsFileName);
                    }
                }


                try
                {
                    // if the source file already exists, skip creation
                    if (!System.IO.File.Exists(jsPath))
                    {
                        using (var fs = new FileStream(jsPath, FileMode.Create, FileAccess.Write))
                        using (var scriptWriter = new StreamWriter(fs, NoBOMEncodingUTF8))
                        {
                            scriptWriter.Write(scriptContent);
                        }
                    }
                }
                catch (IOException)
                {
                }

                // Setting: External Scripts CDN Url
                // If the user set a Cdn url we replace the externalScriptsPath with the proper cdn url
                if (!string.IsNullOrWhiteSpace(_externalScriptsCdn))
                {
                    jsPrePath = (_externalScriptsCdn.EndsWith("/") && _externalScriptsCdn.Length > 1 ? _externalScriptsCdn.Substring(0, _externalScriptsCdn.Length - 1) : _externalScriptsCdn) + "/" + jsFileName;
                }
                else if (!string.IsNullOrWhiteSpace(_externalScriptsPath))
                {
                    jsPrePath = _externalScriptsPath + @"\" + jsFileName;
                }

                string content = "<script src=\"" + jsPrePath + "?version=" + _scriptVersionRev + "\"></script>\n";
                return content;
            }
            else
            {
                return "<script>\r\n" + scriptContent + "\r\n</script>";
            }
        }

        private string BuildCss(string path)
        {
            string scriptContent = Properties.Resources.css;

            if (_externalScripts && path != null)
            {
                string cssFilename = "EliteInsights-" + _scriptVersion + ".css";
                string cssPath = Path.Combine(path, cssFilename);
                string cssPrePath = "./" + cssFilename;

                // Setting: External Scripts Path
                // overwrite cssPath (create directory) if files should be placed on different location
                // settings.externalHtmlScriptsPath is set by the user
                if (!string.IsNullOrWhiteSpace(_externalScriptsPath))
                {
                    string htmlExternalScriptsPath = _externalScriptsPath;
                    bool validPath = false;

                    if (!System.IO.Directory.Exists(htmlExternalScriptsPath))
                    {
                        try
                        {
                            System.IO.Directory.CreateDirectory(htmlExternalScriptsPath);
                            validPath = true;
                        }
                        catch
                        {
                            // something went wrong on creating the external folder (invalid chars?)      
                            // this will skip the saving in this path and continue with css files in the root path for the report
                        }
                    }
                    else
                    {
                        validPath = true;
                    }

                    // if the creation of the folder did not fail or the folder already exists use it to include within the report
                    if (validPath)
                    {
                        cssPath = Path.Combine(htmlExternalScriptsPath, cssFilename);
                    }
                }

                try
                {
                    // if the source file already exists, skip creation
                    if (!System.IO.File.Exists(cssPath))
                    {
                        using (var fs = new FileStream(cssPath, FileMode.Create, FileAccess.Write))
                        using (var scriptWriter = new StreamWriter(fs, NoBOMEncodingUTF8))
                        {
                            scriptWriter.Write(scriptContent);
                        }
                    }
                }
                catch (IOException)
                {
                }

                // Setting: External Scripts CDN Url
                // If the user set a Cdn url we replace the externalScriptsPath with the proper cdn url
                if (!string.IsNullOrWhiteSpace(_externalScriptsCdn))
                {
                    cssPrePath = (_externalScriptsCdn.EndsWith("/") && _externalScriptsCdn.Length > 1 ? _externalScriptsCdn.Substring(0, _externalScriptsCdn.Length - 1) : _externalScriptsCdn) + "/" + cssFilename;
                }
                else if(!string.IsNullOrWhiteSpace(_externalScriptsPath))
                {
                    cssPrePath = _externalScriptsPath + @"\" + cssFilename;
                }

                return "<link rel=\"stylesheet\" type=\"text/css\" href=\"" + cssPrePath + "?version=" + _scriptVersionRev + "\">";
            }
            else
            {
                return "<style type=\"text/css\">\r\n" + scriptContent + "\r\n</style>";
            }
        }

        private string BuildEIJs(string path)
        {
            string scriptContent = _eiJS;

            if (_externalScripts && path != null)
            {
                string scriptFilename = "EliteInsights-" + _scriptVersion + ".js";
                string scriptPath = Path.Combine(path, scriptFilename);
                string scriptPrePath = "./" + scriptFilename;

                // Setting: External Scripts Path
                // overwrite scriptPath (create directory) if files should be placed on different location
                // settings.externalHtmlScriptsPath is set by the user
                if (!string.IsNullOrWhiteSpace(_externalScriptsPath))
                {
                    string htmlExternalScriptsPath = _externalScriptsPath;
                    bool validPath = false;

                    if (!System.IO.Directory.Exists(htmlExternalScriptsPath))
                    {
                        try
                        {
                            System.IO.Directory.CreateDirectory(htmlExternalScriptsPath);
                            validPath = true;
                        }
                        catch
                        {
                            // something went wrong on creating the external folder (invalid chars?)      
                            // this will skip the saving in this path and continue with script files in the root path for the report
                        }
                    }
                    else
                    {
                        validPath = true;
                    }

                    // if the creation of the folder did not fail or the folder already exists use it to include within the report
                    if (validPath)
                    {
                        scriptPath = Path.Combine(htmlExternalScriptsPath, scriptFilename);
                    }
                }

                try
                {
                    // if the source file already exists, skip creation
                    if (!System.IO.File.Exists(scriptPath))
                    {
                        using (var fs = new FileStream(scriptPath, FileMode.Create, FileAccess.Write))
                        using (var scriptWriter = new StreamWriter(fs, NoBOMEncodingUTF8))
                        {
                            scriptWriter.Write(scriptContent);
                        }
                    }
                }
                catch (IOException)
                {
                }

                // Setting: External Scripts CDN Url
                // If the user set a Cdn url we replace the externalScriptsPath with the proper cdn url
                if (!string.IsNullOrWhiteSpace(_externalScriptsCdn))
                {
                    scriptPrePath = (_externalScriptsCdn.EndsWith("/") && _externalScriptsCdn.Length > 1 ? _externalScriptsCdn.Substring(0, _externalScriptsCdn.Length-1) : _externalScriptsCdn) + "/" + scriptFilename;
                }
                else if (!string.IsNullOrWhiteSpace(_externalScriptsPath))
                {
                    scriptPrePath = _externalScriptsPath + @"\" + scriptFilename;
                }

                return "<script src=\"" + scriptPrePath + "?version=" + _scriptVersionRev + "\"></script>";
            }
            else
            {
                return "<script>\r\n" + scriptContent + "\r\n</script>";
            }
        }

        private static string ToJson(object value)
        {
            var settings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = RawFormatBuilder.DefaultJsonContractResolver
            };
            return JsonConvert.SerializeObject(value, settings);
        }

        internal static string GetLink(string name)
        {
            switch (name)
            {
                case "Question":
                    return "https://wiki.guildwars2.com/images/d/de/Sword_slot.png";
                case "Sword":
                    return "https://wiki.guildwars2.com/images/0/07/Crimson_Antique_Blade.png";
                case "Axe":
                    return "https://wiki.guildwars2.com/images/d/d4/Crimson_Antique_Reaver.png";
                case "Dagger":
                    return "https://wiki.guildwars2.com/images/6/65/Crimson_Antique_Razor.png";
                case "Mace":
                    return "https://wiki.guildwars2.com/images/6/6d/Crimson_Antique_Flanged_Mace.png";
                case "Pistol":
                    return "https://wiki.guildwars2.com/images/4/46/Crimson_Antique_Revolver.png";
                case "Scepter":
                    return "https://wiki.guildwars2.com/images/e/e2/Crimson_Antique_Wand.png";
                case "Focus":
                    return "https://wiki.guildwars2.com/images/8/87/Crimson_Antique_Artifact.png";
                case "Shield":
                    return "https://wiki.guildwars2.com/images/b/b0/Crimson_Antique_Bastion.png";
                case "Torch":
                    return "https://wiki.guildwars2.com/images/7/76/Crimson_Antique_Brazier.png";
                case "Warhorn":
                    return "https://wiki.guildwars2.com/images/1/1c/Crimson_Antique_Herald.png";
                case "Greatsword":
                    return "https://wiki.guildwars2.com/images/5/50/Crimson_Antique_Claymore.png";
                case "Hammer":
                    return "https://wiki.guildwars2.com/images/3/38/Crimson_Antique_Warhammer.png";
                case "Longbow":
                    return "https://wiki.guildwars2.com/images/f/f0/Crimson_Antique_Greatbow.png";
                case "Shortbow":
                    return "https://wiki.guildwars2.com/images/1/17/Crimson_Antique_Short_Bow.png";
                case "Rifle":
                    return "https://wiki.guildwars2.com/images/1/19/Crimson_Antique_Musket.png";
                case "Staff":
                    return "https://wiki.guildwars2.com/images/5/5f/Crimson_Antique_Spire.png";

                case "Color-Warrior": return "rgb(255,209,102)";
                case "Color-Warrior-NonBoss": return "rgb(190,159,84)";
                case "Color-Warrior-Total": return "rgb(125,109,66)";

                case "Color-Berserker": return "rgb(255,209,102)";
                case "Color-Berserker-NonBoss": return "rgb(190,159,84)";
                case "Color-Berserker-Total": return "rgb(125,109,66)";

                case "Color-Spellbreaker": return "rgb(255,209,102)";
                case "Color-Spellbreaker-NonBoss": return "rgb(190,159,84)";
                case "Color-Spellbreaker-Total": return "rgb(125,109,66)";

                case "Color-Guardian": return "rgb(114,193,217)";
                case "Color-Guardian-NonBoss": return "rgb(88,147,165)";
                case "Color-Guardian-Total": return "rgb(62,101,113)";

                case "Color-Dragonhunter": return "rgb(114,193,217)";
                case "Color-Dragonhunter-NonBoss": return "rgb(88,147,165)";
                case "Color-Dragonhunter-Total": return "rgb(62,101,113)";

                case "Color-Firebrand": return "rgb(114,193,217)";
                case "Color-Firebrand-NonBoss": return "rgb(88,147,165)";
                case "Color-Firebrand-Total": return "rgb(62,101,113)";

                case "Color-Revenant": return "rgb(209,110,90)";
                case "Color-Revenant-NonBoss": return "rgb(159,85,70)";
                case "Color-Revenant-Total": return "rgb(110,60,50)";

                case "Color-Herald": return "rgb(209,110,90)";
                case "Color-Herald-NonBoss": return "rgb(159,85,70)";
                case "Color-Herald-Total": return "rgb(110,60,50)";

                case "Color-Renegade": return "rgb(209,110,90)";
                case "Color-Renegade-NonBoss": return "rgb(159,85,70)";
                case "Color-Renegade-Total": return "rgb(110,60,50)";

                case "Color-Engineer": return "rgb(208,156,89)";
                case "Color-Engineer-NonBoss": return "rgb(158,119,68)";
                case "Color-Engineer-Total": return "rgb(109,83,48)";

                case "Color-Scrapper": return "rgb(208,156,89)";
                case "Color-Scrapper-NonBoss": return "rgb(158,119,68)";
                case "Color-Scrapper-Total": return "rgb(109,83,48)";

                case "Color-Holosmith": return "rgb(208,156,89)";
                case "Color-Holosmith-NonBoss": return "rgb(158,119,68)";
                case "Color-Holosmith-Total": return "rgb(109,83,48)";

                case "Color-Ranger": return "rgb(140,220,130)";
                case "Color-Ranger-NonBoss": return "rgb(107,167,100)";
                case "Color-Ranger-Total": return "rgb(75,115,70)";

                case "Color-Druid": return "rgb(140,220,130)";
                case "Color-Druid-NonBoss": return "rgb(107,167,100)";
                case "Color-Druid-Total": return "rgb(75,115,70)";

                case "Color-Soulbeast": return "rgb(140,220,130)";
                case "Color-Soulbeast-NonBoss": return "rgb(107,167,100)";
                case "Color-Soulbeast-Total": return "rgb(75,115,70)";

                case "Color-Thief": return "rgb(192,143,149)";
                case "Color-Thief-NonBoss": return "rgb(146,109,114)";
                case "Color-Thief-Total": return "rgb(101,76,79)";

                case "Color-Daredevil": return "rgb(192,143,149)";
                case "Color-Daredevil-NonBoss": return "rgb(146,109,114)";
                case "Color-Daredevil-Total": return "rgb(101,76,79)";

                case "Color-Deadeye": return "rgb(192,143,149)";
                case "Color-Deadeye-NonBoss": return "rgb(146,109,114)";
                case "Color-Deadeye-Total": return "rgb(101,76,79)";

                case "Color-Elementalist": return "rgb(246,138,135)";
                case "Color-Elementalist-NonBoss": return "rgb(186,106,103)";
                case "Color-Elementalist-Total": return "rgb(127,74,72)";

                case "Color-Tempest": return "rgb(246,138,135)";
                case "Color-Tempest-NonBoss": return "rgb(186,106,103)";
                case "Color-Tempest-Total": return "rgb(127,74,72)";

                case "Color-Weaver": return "rgb(246,138,135)";
                case "Color-Weaver-NonBoss": return "rgb(186,106,103)";
                case "Color-Weaver-Total": return "rgb(127,74,72)";

                case "Color-Mesmer": return "rgb(182,121,213)";
                case "Color-Mesmer-NonBoss": return "rgb(139,90,162)";
                case "Color-Mesmer-Total": return "rgb(96,60,111)";

                case "Color-Chronomancer": return "rgb(182,121,213)";
                case "Color-Chronomancer-NonBoss": return "rgb(139,90,162)";
                case "Color-Chronomancer-Total": return "rgb(96,60,111)";

                case "Color-Mirage": return "rgb(182,121,213)";
                case "Color-Mirage-NonBoss": return "rgb(139,90,162)";
                case "Color-Mirage-Total": return "rgb(96,60,111)";

                case "Color-Necromancer": return "rgb(82,167,111)";
                case "Color-Necromancer-NonBoss": return "rgb(64,127,85)";
                case "Color-Necromancer-Total": return "rgb(46,88,60)";

                case "Color-Reaper": return "rgb(82,167,111)";
                case "Color-Reaper-NonBoss": return "rgb(64,127,85)";
                case "Color-Reaper-Total": return "rgb(46,88,60)";

                case "Color-Scourge": return "rgb(82,167,111)";
                case "Color-Scourge-NonBoss": return "rgb(64,127,85)";
                case "Color-Scourge-Total": return "rgb(46,88,60)";

                case "Color-Boss": return "rgb(82,167,250)";
                case "Color-Boss-NonBoss": return "rgb(92,177,250)";
                case "Color-Boss-Total": return "rgb(92,177,250)";

                case "Crit":
                    return "https://wiki.guildwars2.com/images/9/95/Critical_Chance.png";
                case "Scholar":
                    return "https://wiki.guildwars2.com/images/2/2b/Superior_Rune_of_the_Scholar.png";
                case "SwS":
                    return "https://wiki.guildwars2.com/images/1/1c/Bowl_of_Seaweed_Salad.png";
                case "Downs":
                    return "https://wiki.guildwars2.com/images/c/c6/Downed_enemy.png";
                case "Resurrect":
                    return "https://wiki.guildwars2.com/images/3/3d/Downed_ally.png";
                case "Dead":
                    return "https://wiki.guildwars2.com/images/4/4a/Ally_death_%28interface%29.png";
                case "Flank":
                    return "https://wiki.guildwars2.com/images/b/bb/Hunter%27s_Tactics.png";
                case "Glance":
                    return "https://wiki.guildwars2.com/images/f/f9/Weakness.png";
                case "Miss":
                    return "https://wiki.guildwars2.com/images/3/33/Blinded.png";
                case "Interupts":
                    return "https://wiki.guildwars2.com/images/7/79/Daze.png";
                case "Invuln":
                    return "https://wiki.guildwars2.com/images/e/eb/Determined.png";
                case "Blinded":
                    return "https://wiki.guildwars2.com/images/3/33/Blinded.png";
                case "Wasted":
                    return "https://wiki.guildwars2.com/images/b/b3/Out_Of_Health_Potions.png";
                case "Saved":
                    return "https://wiki.guildwars2.com/images/e/eb/Ready.png";
                case "Swap":
                    return "https://wiki.guildwars2.com/images/c/ce/Weapon_Swap_Button.png";
                case "Blank":
                    return "https://wiki.guildwars2.com/images/d/de/Sword_slot.png";
                case "Dodge":
                    return "https://wiki.guildwars2.com/images/archive/b/b2/20150601155307%21Dodge.png";
                case "Bandage":
                    return "https://wiki.guildwars2.com/images/0/0c/Bandage.png";
                case "Stack":
                    return "https://wiki.guildwars2.com/images/e/ef/Commander_arrow_marker.png";

                case "Color-Aegis": return "rgb(102,255,255)";
                case "Color-Fury": return "rgb(255,153,0)";
                case "Color-Might": return "rgb(153,0,0)";
                case "Color-Protection": return "rgb(102,255,255)";
                case "Color-Quickness": return "rgb(255,0,255)";
                case "Color-Regeneration": return "rgb(0,204,0)";
                case "Color-Resistance": return "rgb(255, 153, 102)";
                case "Color-Retaliation": return "rgb(255, 51, 0)";
                case "Color-Stability": return "rgb(153, 102, 0)";
                case "Color-Swiftness": return "rgb(255,255,0)";
                case "Color-Vigor": return "rgb(102, 153, 0)";

                case "Color-Alacrity": return "rgb(0,102,255)";
                case "Color-Glyph of Empowerment": return "rgb(204, 153, 0)";
                case "Color-Grace of the Land": return "rgb(,,)";
                case "Color-Sun Spirit": return "rgb(255, 102, 0)";
                case "Color-Banner of Strength": return "rgb(153, 0, 0)";
                case "Color-Banner of Discipline": return "rgb(0, 51, 0)";
                case "Color-Spotter": return "rgb(0,255,0)";
                case "Color-Stone Spirit": return "rgb(204, 102, 0)";
                case "Color-Storm Spirit": return "rgb(102, 0, 102)";
                case "Color-Empower Allies": return "rgb(255, 153, 0)";

                case "Condi": return "https://wiki.guildwars2.com/images/5/54/Condition_Damage.png";
                case "Healing": return "https://wiki.guildwars2.com/images/8/81/Healing_Power.png";
                case "Tough": return "https://wiki.guildwars2.com/images/1/12/Toughness.png";
                default:
                    return "";
            }

        }
    }
}
