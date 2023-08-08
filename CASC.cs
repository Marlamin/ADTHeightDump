using CASCLib;
using System.Globalization;

namespace ADTHeightDump
{
    public class CASC
    {
        private static CASCHandler cascHandler;
        public static string BuildName;

        public static Dictionary<int, string> Listfile = new();

        public static List<ulong> KnownKeys = new();

        private static HttpClient WebClient = new HttpClient();
        public static async void InitCasc(string? basedir = null, string program = "wowt", LocaleFlags locale = LocaleFlags.enUS)
        {
            CASCConfig.LoadFlags &= ~(LoadFlags.Download | LoadFlags.Install);
            CASCConfig.ValidateData = false;
            CASCConfig.ThrowOnFileNotFound = false;

            if (basedir == null)
            {
                Console.WriteLine("Initializing CASC from web for program " + program + " and locale " + locale);
                cascHandler = CASCHandler.OpenOnlineStorage(program, "eu");
            }
            else
            {
                basedir = basedir.Replace("_retail_", "").Replace("_ptr_", "");
                Console.WriteLine("Initializing CASC from local disk with basedir " + basedir + " and program " + program + " and locale " + locale);
                cascHandler = CASCHandler.OpenLocalStorage(basedir, program);
            }

            var splitName = cascHandler.Config.BuildName.Replace("WOW-", "").Split("patch");
            BuildName = splitName[1].Split("_")[0] + "." + splitName[0];

            cascHandler.Root.SetFlags(locale);

            Listfile = new Dictionary<int, string>();

            var listfileRes = LoadListfile();
            if (!listfileRes)
                throw new Exception("Failed to load listfile");

            KeyService.LoadKeys();

            Console.WriteLine("Finished loading " + BuildName);
        }

        public static bool LoadListfile()
        {
            if (!File.Exists("listfile.csv"))
            {
                Console.WriteLine("Listfile not found, please download the latest version and put it in the same folder as the executable.");
                return false;
            }

            Console.WriteLine("Loading listfile");

            foreach (var line in File.ReadAllLines("listfile.csv"))
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                var splitLine = line.Split(";");
                var fdid = int.Parse(splitLine[0]);

                if (!Listfile.ContainsKey(fdid))
                {
                    Listfile.Add(fdid, splitLine[1]);
                }
            }

            Console.WriteLine("Finished loading listfile: " + Listfile.Count + " named files for this build");

            return true;
        }

        public static bool LoadKeys(bool forceRedownload = false)
        {
            var download = forceRedownload;
            if (File.Exists("TactKey.csv"))
            {
                var info = new FileInfo("TactKey.csv");
                if (info.Length == 0 || DateTime.Now.Subtract(TimeSpan.FromDays(1)) > info.LastWriteTime)
                {
                    Console.WriteLine("TACT Keys outdated, redownloading..");
                    download = true;
                }
            }
            else
            {
                download = true;
            }

            if (download)
            {
                Console.WriteLine("Downloading TACT keys");

                List<string> tactKeyLines = new();
                using (var s = WebClient.GetStreamAsync("https://github.com/wowdev/TACTKeys/raw/master/WoW.txt").Result)
                using (var sr = new StreamReader(s))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        if (string.IsNullOrEmpty(line))
                            continue;

                        var splitLine = line.Split(" ");
                        tactKeyLines.Add(splitLine[0] + ";" + splitLine[1]);
                    }
                }

                File.WriteAllLines("TactKey.csv", tactKeyLines);
            }

            if (forceRedownload)
                KnownKeys.Clear();

            foreach (var line in File.ReadAllLines("TactKey.csv"))
            {
                var splitLine = line.Split(";");
                if (splitLine.Length != 2)
                    continue;
                KnownKeys.Add(ulong.Parse(splitLine[0], NumberStyles.HexNumber));
            }

            Console.WriteLine("Finished loading TACT keys: " + KnownKeys.Count + " known keys");

            return true;
        }

        public static Stream? GetFileByID(uint filedataid)
        {
            try
            {
                return cascHandler.OpenFile((int)filedataid);
            }
            catch (Exception e)
            {
                if (!e.Message.Contains("keyname"))
                {
                    Console.WriteLine("Exception retrieving FileDataID " + filedataid + ": " + e.Message);
                }
                else
                {
                    Console.WriteLine("Missing key for " + filedataid + ": " + e.Message);
                }
                return null;
            }
        }

         public static bool FileExists(uint filedataid)
        {
            return cascHandler.FileExists((int)filedataid);
        }

        public static string GetFullBuild()
        {
            return cascHandler.Config.BuildName;
        }

        public static string GetKey(ulong lookup)
        {
            if (cascHandler == null)
                return "";

            var key = KeyService.GetKey(lookup);
            if (key == null)
                return "";

            return Convert.ToHexString(key);
        }
    }
}