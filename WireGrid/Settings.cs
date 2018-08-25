using System;
using System.IO;
using System.Net;
using System.Reflection;
using IniParser;
using IniParser.Model;
using Microsoft.Win32;

namespace WireGrid
{
    public class Settings
    {
        public static readonly string path = Assembly.GetExecutingAssembly().Location.Replace(Assembly.GetExecutingAssembly().GetName().Name + ".exe", string.Empty);
        public static readonly string ftpDir = path + "root\\ftp\\";

        public static readonly string filename = "Properties.settings";

        private static FileIniDataParser parser;
        private static IniData data;

        public static void Initialize()
        {
            parser = new FileIniDataParser();
            data = new IniData();
            Console.WriteLine("Initializing settings");
            if(File.Exists(path + filename))
            {
                Console.WriteLine("Loading settings file.");
                data = parser.ReadFile(path + filename);
                Console.WriteLine("Settings have been loaded sucessfully.");
            }
            else
            {
                Console.WriteLine("Settings file does not exist, creating default settings file.");
                DefaultSettings();
            }
            Console.WriteLine("Settings initialized compleate");
        }

        private static void DefaultSettings()
        {
                 
        }

        public static void Set(string section, string key, object value)
        {
            try
            {
                data[section][key] = value.ToString();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
            Save();
        }
        public static T Get<T>(string section, string key)
        {
            Type type = typeof(T);
            try
            {
                switch(type.ToString())
                {
                    case "System.Net.IPAddress":
                        return (T)(object)IPAddress.Parse(data[section][key]);
                    case "System.Int32":
                        return (T)(object)int.Parse(data[section][key]);
                    case "System.Boolean":
                        return (T)(object)bool.Parse(data[section][key]);
                    case "System.String":
                        return (T)(object)data[section][key];
                    default:
                        Console.WriteLine("TYPE: " + type.ToString());
                        return (T)(object)data[section][key];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return default(T);
            }
        }
        public static bool Got(string session, string key)
        {
            if (data[session].ContainsKey(key)) return true;
            else return false;
        }
        private static void Save() => parser.WriteFile(path + filename, data);
       
    }
}