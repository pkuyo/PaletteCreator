using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            loaded = true;

            represents.Sort((x,y) => (x.index.CompareTo(y.index)));
            foreach(var represent in represents)
            {
                Debug.Log(String.Format("Index : {0} , name : {1} , path{2}", represent.index, represent.name, represent.path));
            }
            currentNewTexIndex = represents.Last().index + 1;
        } 

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
                    represents.Add(new PaletteRepresent(name, file));
                }
            }
            //mergedmod中的
            string path = AssetManager.ResolveDirectory("Palettes");
            var files = Directory.GetFiles(path, "*.png");

            foreach (var file in files)
            {
                string name = Path.GetFileName(file);
                Debug.Log(name);
                represents.Add(new PaletteRepresent(name, file));
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
                    represents.Add(new PaletteRepresent(name, file, true));
                }
            }
        }

        public static PaletteRepresent GetPaletteRepresentOfName(string name)
        {
            foreach(var represent in represents)
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
            catch (FileLoadException)
            {
                path = AssetManager.ResolveFilePath("Palettes" + Path.DirectorySeparatorChar.ToString() + "palette-1.png");
                AssetManager.SafeWWWLoadTexture(ref tex, "file:///" + path, false, true);
            }
        }

        public static void WriteTextureInfoFile(Texture2D texture,PaletteRepresent represent)
        {
            var bytes = texture.EncodeToPNG();
            File.WriteAllBytes(represent.path, bytes);
        }

        public static PaletteRepresent CreateCustomPalette(int pal)
        {
            string name = "palette" + pal.ToString() + ".png";
            string path = AssetManager.ResolveDirectory("OutputPalettes") + Path.DirectorySeparatorChar + name;

            var newPalette = new PaletteRepresent(name, path, true);
            represents.Add(newPalette);

            return newPalette;
        }

        public static void SaveVanilaidPaletteAsNew(Texture2D texture)
        {
            var tempRepresent = CreateCustomPalette(currentNewTexIndex++);
            WriteTextureInfoFile(texture, tempRepresent);
        }
    }

    public class PaletteRepresent
    {
        public int index;
        public string name;
        public string path;
        public bool isCustomPalette;

        public PaletteRepresent(string name, string path,bool isCustomPalette = false)
        {
            this.name = name;
            this.path = path;
            this.isCustomPalette = isCustomPalette;
            index = int.Parse(name.Replace("palette","").Replace(".png",""));
        }
    }
}
