using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace pkuyo.PaletteCreator
{
    public class PaletteManager
    {
        public static List<PaletteRepresent> represents = new List<PaletteRepresent>();
        public static bool loaded = false;

        public static int currentNewTexIndex = -1;
        public static void LoadAll()
        {
            if (loaded) return;
            LoadDefaultPalettes();
            LoadCustomPalettes();
            loaded = true;

            represents.Sort((x,y) => (x.index.CompareTo(y.index)));
            currentNewTexIndex = represents.Last().index + 1;
        }

        #region LoadPalette
        public static void LoadDefaultPalettes()
        {
            string defaultPath = string.Concat(Custom.RootFolderDirectory(),
                                               Path.DirectorySeparatorChar,
                                               "palettes");
            var defaultfiles = Directory.GetFiles(defaultPath, "*.png");

            foreach(var file in defaultfiles)
            {
                string name = Path.GetFileName(file);
                if (Regex.IsMatch(name, @"palette\d+.png"))
                {
                    var newRepresent = new PaletteRepresent(name, file);
                    represents.Add(newRepresent);
                    LoadTextureForRepresent(newRepresent);
                }
            }
            //mergedmod中的
            string path = AssetManager.ResolveDirectory("Palettes");
            var files = Directory.GetFiles(path, "*.png");

            foreach (var file in files)
            {
                string name = Path.GetFileName(file);
                Debug.Log(name);
                var newRepresent = new PaletteRepresent(name, file);
                represents.Add(newRepresent);
                LoadTextureForRepresent(newRepresent);
            }
        }

        public static void LoadCustomPalettes()
        {
            string path = AssetManager.ResolveDirectory("OutputPalettes");
            var files = Directory.GetFiles(path, "*.png");
            if (files.Length == 0) return;

            foreach (var file in files)
            {
                string name = Path.GetFileName(file);
                if (Regex.IsMatch(name, @"palette\d+.png"))
                {
                    var newRepresent = new PaletteRepresent(name, file, true, true);
                    LoadTextureForRepresent(newRepresent);
                    LoadSettingForRepresent(ref newRepresent);
                    represents.Add(newRepresent);
                }
            }
        }

        #endregion

        #region LoadTexture
        public static void LoadTextureForRepresent(PaletteRepresent represent)
        {
            LoadTexture(represent, ref represent.loadedTexture);
        }

        public static void LoadTexture(PaletteRepresent represent,ref Texture2D tex)
        {
            if (represent.isCustomPalette)
            {
                if(tex != null)
                {
                    UnityEngine.Object.Destroy(tex);
                }
                tex = new Texture2D(32, 16, TextureFormat.ARGB32, false);

                var path = represent.path;
                try
                {
                    AssetManager.SafeWWWLoadTexture(ref tex, path, false, true);
                }
                catch (Exception)
                {
                    tex = new Texture2D(32, 16, TextureFormat.ARGB32, false);
                }
            }
            else LoadTexture(represent.index, ref tex);
            
        }

        public static void LoadTexture(int pal,ref Texture2D tex)
        {
            if (tex != null)
            {
                UnityEngine.Object.Destroy(tex);
            }
            tex = new Texture2D(32, 16, TextureFormat.ARGB32, false);
            string path = AssetManager.ResolveFilePath(string.Concat(new string[]
            {
                "Palettes",
                Path.DirectorySeparatorChar.ToString(),
                "palette",
                pal.ToString(),
                ".png"
            }));
            try
            {
                AssetManager.SafeWWWLoadTexture(ref tex, "file:///" + path, false, true);
            }
            catch (Exception)
            {
                path = AssetManager.ResolveFilePath("Palettes" + Path.DirectorySeparatorChar.ToString() + "palette-1.png");
                AssetManager.SafeWWWLoadTexture(ref tex, "file:///" + path, false, true);
            }
        }
        #endregion

        #region CreateCustomPalette
        public static PaletteRepresent CreateCustomPalette()
        {
            return CreateCustomPalette(currentNewTexIndex++);
        }

        public static PaletteRepresent CreateCustomPalette(int pal)
        {
            string name = "palette" + pal.ToString() + ".png";
            string path = AssetManager.ResolveDirectory("OutputPalettes") + Path.DirectorySeparatorChar + name;

            var newPalette = new PaletteRepresent(name, path, true);
            LoadSettingForRepresent(ref newPalette);

            represents.Add(newPalette);

            return newPalette;
        }

        public static PaletteRepresent SaveVanillaPaletteAsNew(Texture2D texture)
        {
            var tempRepresent = CreateCustomPalette(currentNewTexIndex++);
            SavePaletteTexture(texture, tempRepresent);
            return tempRepresent;
        }
        #endregion

        #region Settings
        public static void LoadSettingForRepresent(ref PaletteRepresent represent)
        {
            if (!represent.isCustomPalette) return;

            string settingPath = represent.GetSettingPath();
            represent.labels = new bool[32, 16];

            string hash = ComputeRepresentHash(represent);

            bool createOrOverWrite = false;
            string labelSetting = "";
            if (!File.Exists(settingPath)) //不存在则创建
            {
                createOrOverWrite = true;
            }
            else
            {
                labelSetting = File.ReadAllText(settingPath);
                if (labelSetting.Length == 0) createOrOverWrite = true;
                else
                {
                    string[] data = labelSetting.Split('_');
                    if (data[0] != hash || data.Length < 2)
                    {
                        Debug.Log(String.Format("OverWriteSettings!\nHash in file : {0}\nHash caculated : {1}\nData length{2}", data[0], hash, data.Length.ToString()));
                        createOrOverWrite = true;
                    }
                }
            }

            if (createOrOverWrite)
            {
                File.Create(settingPath).Dispose();
                labelSetting = SaveSettingForRepresent(ref represent, true);
            }

            string[] loadedData = labelSetting.Split('_');

            Debug.Log(String.Format("LoadSetting\nRepresent : {0}\nHash : {1}", represent.name, loadedData[0]));

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    bool currentLabel = loadedData[1][y * 32 + x] == '1';
                    represent.labels[x, y] = currentLabel;
                }
            }
        }

        public static string SaveSettingForRepresent(ref PaletteRepresent represent,bool setup = false)
        {
            string settingPath = represent.GetSettingPath();
            string result = "";

            result += ComputeRepresentHash(represent) + "_";
            Debug.Log(string.Format("SaveSetting\nRepresent : {0}\nHash : {1}", represent.name, result));

            for(int y = 0;y < 16; y++)
            {
                for(int x = 0; x < 32; x++)
                {
                    result += (represent.labels[x, y] || setup) ? "1" : "0";
                }
            }
            File.WriteAllText(settingPath, result);

            return result;
        }

        #endregion

        public static PaletteRepresent GetPaletteRepresentOfName(string name)
        {
            foreach (var represent in represents)
            {
                if (represent.name == name) return represent;
            }
            return null;
        }
        public static PaletteRepresent GetPaletteRepresentOfIndex(int index)
        {
            foreach (var represent in represents)
            {
                if (represent.index == index) return represent;
            }
            return null;
        }

        public static void SavePaletteTexture(Texture2D texture, PaletteRepresent represent)
        {
            var bytes = texture.EncodeToPNG();
            if (!File.Exists(represent.path)) File.Create(represent.path).Dispose();//避免报错
            File.WriteAllBytes(represent.path, bytes);

            represent.everSaved = true;
            represent.CopyTexToPalette(texture);

            SaveSettingForRepresent(ref represent);
        }

        public static string ComputeRepresentHash(PaletteRepresent represent)
        {
            byte[] raws = represent.loadedTexture.GetRawTextureData();
            var hashed = new MD5CryptoServiceProvider().ComputeHash(raws);

            int i;
            StringBuilder sOutput = new StringBuilder(hashed.Length);
            for (i = 0; i < hashed.Length - 1; i++)
            {
                sOutput.Append(hashed[i].ToString("X2"));
            }
            return sOutput.ToString();
        }
    }

    public class PaletteRepresent
    {
        public int index;
        public string name;
        public string path;
        public bool isCustomPalette;

        public bool everSaved;

        public bool[,] labels;

        public Texture2D loadedTexture = new Texture2D(32, 16, TextureFormat.ARGB32, false);

        public PaletteRepresent(string name, string path,bool isCustomPalette = false,bool everSaved = false)
        {
            this.name = name;
            this.path = path;
            this.isCustomPalette = isCustomPalette;
            this.everSaved = everSaved;
            index = int.Parse(name.Replace("palette","").Replace(".png",""));
        }

        public string GetSettingPath()
        {
            string settingPath = AssetManager.ResolveDirectory("PaletteSettings");
            if (!Directory.Exists(settingPath)) throw new Exception("Do not have " + settingPath + " path");
            string nameWithoutEndings = name.Replace(".png", "");

            return settingPath + Path.DirectorySeparatorChar + nameWithoutEndings + ".txt";
        }

        public void CopyPaletteToTex(ref Texture2D texture)
        {
            if(texture == null || texture.width != 32 || texture.height != 16)texture = new Texture2D(32, 16, TextureFormat.ARGB32, false);

            for (int x = 0;x < 32; x++)
            {
                for(int y = 0; y < 16; y++)
                {
                    texture.SetPixel(x, y, loadedTexture.GetPixel(x, y));
                }
            }
            texture.Apply();
        }

        public void CopyTexToPalette(Texture2D src)
        {
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    loadedTexture.SetPixel(x, y, src.GetPixel(x, y));
                }
            }
            loadedTexture.Apply();
        }
    }
}
