﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using Discord;
using GW2EIBuilders;
using GW2EIDiscord;
using GW2EIDPSReport;
using GW2EIDPSReport.DPSReportJsons;
using GW2EIWingman;
using GW2EIEvtcParser;
using GW2EIGW2API;
using GW2EIEvtcParser.ParserHelpers;
using GW2EIParserCommons.Exceptions;

[assembly: CLSCompliant(false)]
namespace GW2EIParserCommons
{
    public class ProgramHelper
    {

        public ProgramHelper(Version parserVersion, ProgramSettings settings) {
            ParserVersion = parserVersion;
            Settings = settings;
        }

        public void ApplySettings(ProgramSettings settings)
        {
            Settings = settings;
        }

        public static IReadOnlyList<string> SupportedFormats => SupportedFileFormats.SupportedFormats;

        public static bool IsSupportedFormat(string path)
        {
            return SupportedFileFormats.IsSupportedFormat(path);
        }

        public static bool IsCompressedFormat(string path)
        {
            return SupportedFileFormats.IsCompressedFormat(path);
        }

        public static bool IsTemporaryCompressedFormat(string path)
        {
            return SupportedFileFormats.IsTemporaryCompressedFormat(path);
        }

        public static bool IsTemporaryFormat(string path)
        {
            return SupportedFileFormats.IsTemporaryFormat(path);
        }

        internal static HTMLAssets htmlAssets { get; } = new HTMLAssets();

        public ProgramSettings Settings { get; private set; }
        private Version ParserVersion { get; }

        private static readonly UTF8Encoding NoBOMEncodingUTF8 = new UTF8Encoding(false);

        public static readonly string SkillAPICacheLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Content/SkillList.json";
        public static readonly string SpecAPICacheLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Content/SpecList.json";
        public static readonly string TraitAPICacheLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Content/TraitList.json";
        public static readonly string EILogPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Logs/";

        public static readonly GW2APIController APIController = new GW2APIController(SkillAPICacheLocation, SpecAPICacheLocation, TraitAPICacheLocation);

        public int GetMaxParallelRunning()
        {
            return Settings.GetMaxParallelRunning();
        }

        public bool HasFormat()
        {
            return Settings.HasFormat();
        }

        public bool ParseMultipleLogs()
        {
            return Settings.DoParseMultipleLogs();
        }

        public EmbedBuilder GetEmbedBuilder()
        {
            var builder = new EmbedBuilder();
            builder.WithAuthor("Elite Insights " + ParserVersion.ToString(), "https://github.com/baaron4/GW2-Elite-Insights-Parser/blob/master/GW2EIParser/Content/LI.png?raw=true", "https://github.com/baaron4/GW2-Elite-Insights-Parser");
            return builder;
        }

        private Embed BuildEmbed(ParsedEvtcLog log, string dpsReportPermalink)
        {
            EmbedBuilder builder = GetEmbedBuilder();
            builder.WithThumbnailUrl(log.FightData.Logic.Icon);
            //
            builder.AddField("Encounter Duration", log.FightData.DurationString);
            //
            if (log.FightData.Logic.GetInstanceBuffs(log).Any())
            {
                builder.AddField("Instance Buffs", string.Join("\n", log.FightData.Logic.GetInstanceBuffs(log).Select(x => (x.stack > 1 ? x.stack + " " : "") + x.buff.Name)));
            }
            //
            /*var playerByGroup = log.PlayerList.Where(x => !x.IsFakeActor).GroupBy(x => x.Group).ToDictionary(x => x.Key, x => x.ToList());
            var hasGroups = playerByGroup.Count > 1;
            foreach (KeyValuePair<int, List<Player>> pair in playerByGroup)
            {
                var groupField = new List<string>();
                foreach (Player p in pair.Value)
                {
                    groupField.Add(p.Character + " - " + p.Prof);
                }
                builder.AddField(hasGroups ? "Group " + pair.Key : "Party Composition", String.Join("\n", groupField));
            }*/
            //
            builder.AddField("Game Data", "ARC: " + log.LogData.ArcVersion + " | " + "GW2 Build: " + log.LogData.GW2Build);
            //
            builder.WithTitle(log.FightData.FightName);
            //builder.WithTimestamp(DateTime.Now);
            builder.WithFooter(log.LogData.LogStartStd + " / " + log.LogData.LogEndStd);
            builder.WithColor(log.FightData.Success ? Color.Green : Color.Red);
            if (dpsReportPermalink.Length > 0)
            {
                builder.WithUrl(dpsReportPermalink);
            }
            return builder.Build();
        }

        private string[] UploadOperation(FileInfo fInfo, ParsedEvtcLog originalLog, OperationController originalController)
        {
            // Only upload supported 5 men, 10 men and golem logs, without anonymous players
            var isWingmanCompatible = !originalLog.ParserSettings.AnonymousPlayers && (
                            originalLog.FightData.Logic.ParseMode == GW2EIEvtcParser.EncounterLogic.FightLogic.ParseModeEnum.Instanced10 ||
                            originalLog.FightData.Logic.ParseMode == GW2EIEvtcParser.EncounterLogic.FightLogic.ParseModeEnum.Instanced5 ||
                            originalLog.FightData.Logic.ParseMode == GW2EIEvtcParser.EncounterLogic.FightLogic.ParseModeEnum.Benchmark
                            );
            //Upload Process
            string[] uploadresult = new string[2] { "", "" };
            if (Settings.UploadToDPSReports)
            {
                originalController.UpdateProgressWithCancellationCheck("DPSReport: Uploading");
                DPSReportUploadObject response = DPSReportController.UploadUsingEI(fInfo, str => originalController.UpdateProgress("DPSReport: " + str), Settings.DPSReportUserToken,
                originalLog.ParserSettings.AnonymousPlayers,
                originalLog.ParserSettings.DetailedWvWParse);
                uploadresult[0] = response != null ? response.Permalink : "Upload process failed";
                originalController.UpdateProgressWithCancellationCheck("DPSReport: " + uploadresult[0]);
                /*
                if (Properties.Settings.Default.UploadToWingman)
                {
                    if (isWingmanCompatible)
                    {
                        traces.Add("Uploading to Wingman using DPSReport url");
                        WingmanController.UploadToWingmanUsingImportLogQueued(uploadresult[0], traces, ParserVersion);
                    }
                    else
                    {
                        traces.Add("Can not upload to Wingman using DPSReport url: unsupported log");
                    }
                }
                */
            }
            if (Settings.UploadToWingman)
            {
#if !DEBUG
                if (!isWingmanCompatible)
                {
                    originalController.UpdateProgressWithCancellationCheck("Wingman: unsupported log");
                } 
                else
                {
                    string accName = originalLog.LogData.PoV != null ? originalLog.LogData.PoVAccount : null;

                    if (WingmanController.CheckUploadPossible(fInfo, accName, str => originalController.UpdateProgress("Wingman: " + str)))
                    {
                        try
                        {
                            var expectedSettings = new EvtcParserSettings(Settings.Anonymous,
                                                            Settings.SkipFailedTries,
                                                            true,
                                                            true,
                                                            true,
                                                            Settings.CustomTooShort,
                                                            Settings.DetailledWvW);
                            ParsedEvtcLog logToUse = originalLog;
                            if (originalLog.ParserSettings.ComputeDamageModifiers != expectedSettings.ComputeDamageModifiers ||
                                originalLog.ParserSettings.ParsePhases != expectedSettings.ParsePhases ||
                                originalLog.ParserSettings.ParseCombatReplay != expectedSettings.ParseCombatReplay)
                            {
                                // We need to create a parser that matches Wingman's expected settings
                                var parser = new EvtcParser(expectedSettings, APIController);
                                originalController.UpdateProgressWithCancellationCheck("Wingman: Setting mismatch, creating a new ParsedEvtcLog, this will extend total processing duration if file generation is also requested");
                                logToUse = parser.ParseLog(originalController, fInfo, out GW2EIEvtcParser.ParserHelpers.ParsingFailureReason failureReason, !Settings.SingleThreaded);
                            }
                            byte[] jsonFile, htmlFile;
                            originalController.UpdateProgressWithCancellationCheck("Wingman: Creating JSON");
                            var uploadResult = new UploadResults();
                            {
                                var ms = new MemoryStream();
                                var sw = new StreamWriter(ms, NoBOMEncodingUTF8);
                                var builder = new RawFormatBuilder(logToUse, new RawFormatSettings(true), ParserVersion, uploadResult);

                                builder.CreateJSON(sw, false);
                                sw.Close();

                                jsonFile = ms.ToArray();
                            }
                            originalController.UpdateProgressWithCancellationCheck("Wingman: Creating HTML");
                            {
                                var ms = new MemoryStream();
                                var sw = new StreamWriter(ms, NoBOMEncodingUTF8);
                                var builder = new HTMLBuilder(logToUse, new HTMLSettings(false, false, null, null, true), htmlAssets, ParserVersion, uploadResult);

                                builder.CreateHTML(sw, null);
                                sw.Close();
                                htmlFile = ms.ToArray();
                            }
                            if (logToUse != originalLog)
                            {
                                originalController.UpdateProgressWithCancellationCheck("Wingman: new ParsedEvtcLog processing completed");
                            }
                            originalController.UpdateProgressWithCancellationCheck("Wingman: Preparing Upload");
                            string result = logToUse.FightData.Success ? "kill" : "fail";
                            WingmanController.UploadProcessed(fInfo, accName, jsonFile, htmlFile, $"_{logToUse.FightData.Logic.Extension}_{result}", str => originalController.UpdateProgress("Wingman: " + str), ParserVersion);
                        }
                        catch (Exception e)
                        {
                            originalController.UpdateProgressWithCancellationCheck("Wingman: operation failed " + e.Message);
                        }
                    } 
                    else
                    {
                        originalController.UpdateProgressWithCancellationCheck("Wingman: upload is not possible");
                    }
                }
                originalController.UpdateProgressWithCancellationCheck("Wingman: operation completed");
#endif

            }
            return uploadresult;
        }

        public void DoWork(OperationController operation)
        {
            System.Globalization.CultureInfo before = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture =
                    new System.Globalization.CultureInfo("en-US");
            operation.Reset();
            try
            {
                operation.Start();
                var fInfo = new FileInfo(operation.InputFile);

                var parser = new EvtcParser(new EvtcParserSettings(Settings.Anonymous,
                                                Settings.SkipFailedTries,
                                                Settings.ParsePhases,
                                                Settings.ParseCombatReplay,
                                                Settings.ComputeDamageModifiers,
                                                Settings.CustomTooShort,
                                                Settings.DetailledWvW),
                                            APIController);

                //Process evtc here
                ParsedEvtcLog log = parser.ParseLog(operation, fInfo, out GW2EIEvtcParser.ParserHelpers.ParsingFailureReason failureReason, !Settings.SingleThreaded && HasFormat());
                if (failureReason != null)
                {
                    failureReason.Throw();
                }
                operation.BasicMetaData = new OperationController.OperationBasicMetaData(log);
                string[] uploadStrings = UploadOperation(fInfo, log, operation);
                if (Settings.SendEmbedToWebhook && Settings.UploadToDPSReports)
                {
                    if (Settings.SendSimpleMessageToWebhook)
                    {
                        WebhookController.SendMessage(Settings.WebhookURL, uploadStrings[0], out string message);
                        operation.UpdateProgressWithCancellationCheck("Webhook: " + message);
                    } 
                    else
                    {
                        WebhookController.SendMessage(Settings.WebhookURL, BuildEmbed(log, uploadStrings[0]),out string message);
                        operation.UpdateProgressWithCancellationCheck("Webhook: " + message);
                    }
                }
                if (uploadStrings[0].Contains("https"))
                {
                    operation.DPSReportLink = uploadStrings[0];
                }
                //Creating File
                GenerateFiles(log, operation, uploadStrings, fInfo);
            }
            catch (Exception ex)
            {
                throw new ProgramException(ex);
            }
            finally
            {
                operation.Stop();
                GC.Collect();
                Thread.CurrentThread.CurrentCulture = before;
            }
        }

        private static void CompressFile(string file, MemoryStream str, OperationController operation)
        {
            // Create the compressed file.
            byte[] data = str.ToArray();
            string outputFile = file + ".gz";
            using (FileStream outFile =
                        File.Create(outputFile))
            {
                using (var Compress =
                    new GZipStream(outFile,
                    CompressionMode.Compress))
                {
                    // Copy the source file into 
                    // the compression stream.
                    Compress.Write(data, 0, data.Length);
                }
            }
            operation.AddFile(outputFile);
        }

        private DirectoryInfo GetSaveDirectory(FileInfo fInfo)
        {
            //save location
            DirectoryInfo saveDirectory;
            if (Settings.SaveAtOut || Settings.OutLocation == null)
            {
                //Default save directory
                saveDirectory = fInfo.Directory;
                if (!saveDirectory.Exists)
                {
                    throw new InvalidOperationException("Save directory does not exist");
                }
            }
            else
            {
                if (!Directory.Exists(Settings.OutLocation))
                {
                    throw new InvalidOperationException("Save directory does not exist");
                }
                saveDirectory = new DirectoryInfo(Settings.OutLocation);
            }
            return saveDirectory;
        }

        public void GenerateTraceFile(OperationController operation)
        {
            if (Settings.SaveOutTrace)
            {
                var fInfo = new FileInfo(operation.InputFile);

                string fName = fInfo.Name.Split('.')[0];
                if (!fInfo.Exists)
                {
                    fInfo = new FileInfo(AppDomain.CurrentDomain.BaseDirectory);
                }

                DirectoryInfo saveDirectory = GetSaveDirectory(fInfo);

                string outputFile = Path.Combine(
                saveDirectory.FullName,
                $"{fName}.log"
                );
                operation.AddFile(outputFile);
                using (var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                using (var sw = new StreamWriter(fs))
                {
                    operation.WriteLogMessages(sw);
                }
                operation.OutLocation = saveDirectory.FullName;
            }
        }

        private void GenerateFiles(ParsedEvtcLog log, OperationController operation, string[] uploadStrings, FileInfo fInfo)
        {
            operation.UpdateProgressWithCancellationCheck("Program: Creating File(s)");

            DirectoryInfo saveDirectory = GetSaveDirectory(fInfo);

            string result = log.FightData.Success ? "kill" : "fail";
            string encounterLengthTerm = Settings.AddDuration ? "_" + (log.FightData.FightDuration / 1000).ToString() + "s" : "";
            string PoVClassTerm = Settings.AddPoVProf ? "_" + log.LogData.PoV.Spec.ToString().ToLower() : "";
            string fName = fInfo.Name.Split('.')[0];
            fName = $"{fName}{PoVClassTerm}_{log.FightData.Logic.Extension}{encounterLengthTerm}_{result}";

            var uploadResults = new UploadResults(uploadStrings[0], uploadStrings[1]);
            operation.OutLocation = saveDirectory.FullName;
            if (Settings.SaveOutHTML)
            {
                operation.UpdateProgressWithCancellationCheck("Program: Creating HTML");
                string outputFile = Path.Combine(
                saveDirectory.FullName,
                $"{fName}.html"
                );
                operation.AddOpenableFile(outputFile);
                using (var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                using (var sw = new StreamWriter(fs))
                {
                    var builder = new HTMLBuilder(log, 
                        new HTMLSettings(
                            Settings.LightTheme, 
                            Settings.HtmlExternalScripts,
                            Settings.HtmlExternalScriptsPath,
                            Settings.HtmlExternalScriptsCdn,
                            Settings.HtmlCompressJson
                        ), htmlAssets, ParserVersion, uploadResults);
                    builder.CreateHTML(sw, saveDirectory.FullName);
                }
                operation.UpdateProgressWithCancellationCheck("Program: HTML created");
            }
            if (Settings.SaveOutCSV)
            {
                operation.UpdateProgressWithCancellationCheck("Program: Creating CSV");
                string outputFile = Path.Combine(
                    saveDirectory.FullName,
                    $"{fName}.csv"
                );
                operation.AddOpenableFile(outputFile);
                using (var fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                using (var sw = new StreamWriter(fs, Encoding.GetEncoding(1252)))
                {
                    var builder = new CSVBuilder(log, new CSVSettings(","), ParserVersion, uploadResults);
                    builder.CreateCSV(sw);
                }
                operation.UpdateProgressWithCancellationCheck("Program: CSV created");
            }
            if (Settings.SaveOutJSON || Settings.SaveOutXML)
            {
                var builder = new RawFormatBuilder(log, new RawFormatSettings(Settings.RawTimelineArrays), ParserVersion, uploadResults);
                if (Settings.SaveOutJSON)
                {
                    operation.UpdateProgressWithCancellationCheck("Program: Creating JSON");
                    string outputFile = Path.Combine(
                        saveDirectory.FullName,
                        $"{fName}.json"
                    );
                    Stream str;
                    if (Settings.CompressRaw)
                    {
                        str = new MemoryStream();
                    }
                    else
                    {
                        str = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
                    }
                    using (var sw = new StreamWriter(str, NoBOMEncodingUTF8))
                    {
                        builder.CreateJSON(sw, Settings.IndentJSON);
                    }
                    if (str is MemoryStream msr)
                    {
                        CompressFile(outputFile, msr, operation);
                        operation.UpdateProgressWithCancellationCheck("Program: JSON compressed");
                    }
                    else
                    {
                        operation.AddFile(outputFile);
                    }
                    operation.UpdateProgressWithCancellationCheck("Program: JSON created");
                }
                if (Settings.SaveOutXML)
                {
                    operation.UpdateProgressWithCancellationCheck("Program: Creating XML");
                    string outputFile = Path.Combine(
                        saveDirectory.FullName,
                        $"{fName}.xml"
                    );
                    Stream str;
                    if (Settings.CompressRaw)
                    {
                        str = new MemoryStream();
                    }
                    else
                    {
                        str = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
                    }
                    using (var sw = new StreamWriter(str, NoBOMEncodingUTF8))
                    {
                        builder.CreateXML(sw, Settings.IndentXML);
                    }
                    if (str is MemoryStream msr)
                    {
                        CompressFile(outputFile, msr, operation);
                        operation.UpdateProgressWithCancellationCheck("Program: XML compressed");
                    }
                    else
                    {
                        operation.AddFile(outputFile);
                    }
                    operation.UpdateProgressWithCancellationCheck("Program: XML created");
                }
            }
            operation.UpdateProgressWithCancellationCheck($"已完成{result} {log.FightData.Logic.Extension}");
        }

    }
}
