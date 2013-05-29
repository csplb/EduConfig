using System;
using System.Collections.Generic;
using System.Text;

namespace Streaia
{
    class ParamParser
    {
        private string[] args;
        private Dictionary<string, string[]> parsed;
        private Dictionary<string, string> expand;

        public string DefaultArg = "--default";
        public string[] Prefixes = { "--", "-", "/" }; // TODO: możliwość ustalania własnych prefiksów dla przełączników
        public char[] ParamPrefixes = { '=', ':' }; // TODO: możliwość ustalania własnych prefiksów dla listy parametrów przełącznika
        public char[] ParamSeparators = { ',' }; // TODO: możliwość ustalania własnej listy separatorów

        private string appPath;

        public string GetAppPath()
        {
            return this.appPath;
        }


        public ParamParser(string[] args)
        {
            this.args = args;
            parsed = new Dictionary<string, string[]>();
            expand = new Dictionary<string, string>();
        }

        public void AddExpanded(string shortSwitch, string longSwitch)
        {
            expand.Add(shortSwitch, longSwitch);
        }

        public void Parse()
        {
            parsed = new Dictionary<string, string[]>();
            this.appPath = args[0];

            string actKey = "--default";
            string[] actParam = new string[0];

            for (int i = 1; i < args.Length; i++)
            {
                // jeśli zaczyna się od / albo -- albo - to uznajemy, że jest dany argument jest przełącznikiem
                if (args[i].StartsWith("/") || args[i].StartsWith("--") || args[i].StartsWith("-"))
                {
                    // dodawanie poprzedniego przełącznika do listy już sparsowanych
                    parsed.Add(actKey, actParam);

                    // wydobycie "czystej" nazwy przełącznika, bez ewentualnych prefiksów parametrów
                    string newKey;

                    if (args[i].IndexOf(':') > -1)
                        newKey = args[i].Substring(0, args[i].IndexOf(':'));
                    else if (args[i].IndexOf('=') > -1)
                        newKey = args[i].Substring(0, args[i].IndexOf('='));
                    else
                        newKey = args[i];
                    
                    // nowy przełącznik
                    actKey = ExpandSwitch(newKey);
                    actParam = new string[0];

                    // sprawdzanie, czy istnieją parametry dla danego przełącznika, jeśli tak, to je uaktualniamy
                    if (args[i].IndexOf('=') > -1)
                    {
                        actParam = args[i].Substring(args[i].IndexOf('=') +1).Split(ParamSeparators);                        
                    }
                    else if (args[i].IndexOf(':') > -1)
                    {
                        actParam = args[i].Substring(args[i].IndexOf(':') + 1).Split(ParamSeparators);
                    }
                }
                else
                // w przeciwnym wypadku dany argument linii polecenia jest parametrem przełącznika
                {
                    // rozszerzanie tablicy i dodawanie
                    Array.Resize(ref actParam, actParam.Length + 1);
                    actParam[actParam.Length -1] = args[i];
                }                
            }
            // dodawanie poprzedniego (ostatniego) przęłącznika do listy już sparsowanych
            parsed.Add(actKey, actParam);
        }

        public string ExpandSwitch(string s)
        {
            string result = s;
            
            try
            {
                if (!expand.TryGetValue(s, out result))
                    return s;
                else
                    return result;
            }
            catch (ArgumentNullException)
            {
                return s;
            }            
        }

        public string[] GetSwitchArguments(string sw)
        {
            sw = ExpandSwitch(sw);
            string[] result;

            parsed.TryGetValue(sw, out result);
            return result;
        }

        public bool MatchRequiredCount(int requiredCount)
        {
            // jeśli jest tylko jeden, przełącznik znaczy "default", to zwracamy czy liczba jego
            // parametrów jest zgodna
            if (parsed.Keys.Count == 1)
                return parsed["--default"].Length >= requiredCount;
            else
                return parsed.Keys.Count >= requiredCount;
        }

        public bool SwitchExists(string sw)
        {            
            string[] r;

            sw = ExpandSwitch(sw);

            try
            {
                return parsed.TryGetValue(sw, out r);
            }
            catch (ArgumentNullException)
            {
                return false;
            }            
        }

        public bool HelpRequested()
        {
            return SwitchExists("--help");
        }

        public bool VersionRequested()
        {
            return SwitchExists("--version");
        }

        public bool DebugRequested()
        {
            return SwitchExists("--debug");
        }
    }
}
