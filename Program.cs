using CASCLib;
using System.Text;
using System.Text.Json;

namespace ADTHeightDump
{
    public class TextureInfo
    {
        public int Scale { get; set; }
        public float HeightScale { get; set; }
        public float HeightOffset { get; set; }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: ADTHeightDump.exe <wowProd> (wowDir)\nExample online mode: ADTHeightDump.exe wowt\nExample local mode: ADTHeightDump.exe wowt \"C:\\World of Warcraft\"");
                Environment.Exit(-1);
            }

            var wowProd = args[0];

            string? wowDir = null;
            if (args.Length == 2)
            {
                wowDir = args[1];
            }

            var texDetails = new Dictionary<string, TextureInfo>();

            // Load TACT keys in case there are encrypted maps
            CASC.LoadKeys();

            CASC.InitCasc(wowDir, wowProd);

            foreach (var file in CASC.Listfile.Where(x => x.Value.EndsWith("tex0.adt")))
            {

                if (!CASC.FileExists((uint)file.Key))
                {
                    Console.WriteLine("File " + file.Key + " " + file.Value + " does not exist, skipping..");
                    continue;
                }

                var adtfile = new ADT();

                using (var ms = new MemoryStream())
                using (var bin = new BinaryReader(ms))
                {
                    try
                    {
                        CASC.GetFileByID((uint)file.Key).CopyTo(ms);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Error opening file " + file.Key + ": " + file.Value + " because of " + e.Message + ", skipping..");
                        continue;
                    }
                    ms.Position = 0;

                    long position = 0;

                    while (position < ms.Length)
                    {
                        ms.Position = position;
                        var chunkName = (ADTChunks)bin.ReadUInt32();
                        var chunkSize = bin.ReadUInt32();

                        position = ms.Position + chunkSize;
                        switch (chunkName)
                        {
                            case ADTChunks.MVER:
                                if (bin.ReadUInt32() != 18) { throw new Exception("Unsupported ADT version!"); }
                                break;
                            case ADTChunks.MTEX:
                                adtfile.textures = ReadMTEXChunk(chunkSize, bin);
                                break;
                            case ADTChunks.MTXP:
                                adtfile.texParams = ReadMTXPChunk(chunkSize, bin);
                                break;
                            case ADTChunks.MHID: // Height texture fileDataIDs
                                adtfile.heightTextureFileDataIDs = ReadFileDataIDChunk(chunkSize, bin);
                                break;
                            case ADTChunks.MDID: // Diffuse texture fileDataIDs
                                adtfile.diffuseTextureFileDataIDs = ReadFileDataIDChunk(chunkSize, bin);
                                break;
                            case ADTChunks.MCNK:
                            case ADTChunks.MAMP:
                                break;
                            default:
                                Console.WriteLine(string.Format("Found unknown header at offset {1} \"{0}\" while we should've already read them all!", chunkName, position));
                                break;
                        }
                    }
                }

                if (adtfile.texParams != null)
                {
                    Console.WriteLine(file.Key + " " + file.Value);
                    for (var i = 0; i < adtfile.texParams.Length; i++)
                    {
                        var filename = "";

                        var mtxp = adtfile.texParams[i];
                        if (adtfile.diffuseTextureFileDataIDs != null && adtfile.diffuseTextureFileDataIDs[i] != 0)
                            filename = CASC.Listfile[(int)adtfile.diffuseTextureFileDataIDs[i]].Replace("_s.blp", ".blp");

                        if (adtfile.textures.filenames != null)
                            filename = adtfile.textures.filenames[i];
                 
                        if (mtxp.height == 0 && mtxp.offset == 1)
                            continue;

                        if (adtfile.heightTextureFileDataIDs == null || adtfile.heightTextureFileDataIDs[i] == 0)
                            continue;

                        if (!texDetails.ContainsKey(filename))
                        {
                            texDetails.Add(filename, new TextureInfo
                            {
                                Scale = (int)(mtxp.flags >> 4),
                                HeightScale = mtxp.height,
                                HeightOffset = mtxp.offset
                            });
                        }
                        else
                        {
                            if (texDetails[filename].Scale != (int)(mtxp.flags >> 4))
                            {
                                Console.WriteLine("Warning! Scale mismatch for " + filename + " " + texDetails[filename].Scale + " " + (int)(mtxp.flags >> 4));
                            }
                            if (texDetails[filename].HeightScale != mtxp.height)
                            {
                                Console.WriteLine("Warning! HeightScale mismatch for " + filename + " " + texDetails[filename].HeightScale + " " + mtxp.height);
                            }
                            if (texDetails[filename].HeightOffset != mtxp.offset)
                            {
                                Console.WriteLine("Warning! HeightOffset mismatch for " + filename + " " + texDetails[filename].HeightOffset + " " + mtxp.offset);
                            }

                            texDetails[filename] = new TextureInfo
                            {
                                Scale = (int)(mtxp.flags >> 4),
                                HeightScale = mtxp.height,
                                HeightOffset = mtxp.offset
                            };
                        }
                    }
                }
            }

            Console.WriteLine("Adding tilesets from listfile starting with tileset and ending in _h.blp with default values (can be wrong)");
            foreach (var file in CASC.Listfile.Where(x => x.Value.EndsWith("_h.blp") && x.Value.StartsWith("tileset")))
            {
                var basename = file.Value.Replace("_h.blp", ".blp");
                if (!texDetails.ContainsKey(basename))
                {
                    Console.WriteLine("Adding " + file.Value + " from listfile as " + basename);
                    texDetails[basename] = new TextureInfo
                    {
                        Scale = 1,
                        HeightScale = 6,
                        HeightOffset = 1
                    };
                }
            }

            var json = JsonSerializer.Serialize(texDetails, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("texinfo.json", json);
        }

        private static MTEX ReadMTEXChunk(uint size, BinaryReader bin)
        {
            var txchunk = new MTEX();

            //List of BLP filenames
            var blpFilesChunk = bin.ReadBytes((int)size);
            var blpFiles = new List<string>();
            var str = new StringBuilder();

            for (var i = 0; i < blpFilesChunk.Length; i++)
            {
                if (blpFilesChunk[i] == '\0')
                {
                    blpFiles.Add(str.ToString());
                    str = new StringBuilder();
                }
                else
                {
                    str.Append((char)blpFilesChunk[i]);
                }
            }

            txchunk.filenames = blpFiles.ToArray();
            return txchunk;
        }

        private static uint[] ReadFileDataIDChunk(uint size, BinaryReader bin)
        {
            var count = size / 4;
            var filedataids = new uint[count];
            for (var i = 0; i < count; i++)
            {
                filedataids[i] = bin.ReadUInt32();
            }
            return filedataids;
        }

        private static MTXP[] ReadMTXPChunk(uint size, BinaryReader bin)
        {
            var count = size / 16;

            var txparams = new MTXP[count];

            for (var i = 0; i < count; i++)
            {
                txparams[i] = bin.Read<MTXP>();
            }

            return txparams;
        }
    }
}