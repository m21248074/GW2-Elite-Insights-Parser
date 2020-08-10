﻿using System;
using System.Collections.Generic;
using System.IO;

namespace GW2EIEvtcParser
{

    public abstract class ParserController
    {

        protected List<string> StatusList { get; }

        public string ParserName { get; }

        public Version ParserVersion { get; }

        public ParserController(string parserName, Version parserVersion)
        {
            StatusList = new List<string>();
            ParserName = parserName;
            ParserVersion = parserVersion;
        }

        protected virtual void ThrowIfCanceled()
        {

        }

        public void WriteLogMessages(StreamWriter sw)
        {
            foreach (string str in StatusList)
            {
                sw.WriteLine(str);
            }
        }

        public virtual void UpdateProgressWithCancellationCheck(string status)
        {
            UpdateProgress(status);
            ThrowIfCanceled();
        }
        public void UpdateProgress(string status)
        {
            StatusList.Add(status);
        }
    }
}