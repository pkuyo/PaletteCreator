using BepInEx;
using DevInterface;
using pkuyo.PaletteCreator.Dev;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
namespace pkuyo.PaletteCreator
{
    [BepInPlugin("pkuyo.palettecreator", "Palette Creator", "1.0.0")]
    public class PaletteCreator : BaseUnityPlugin
    {
        public PaletteCreator()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            panels = new Dictionary<string, ColorPanel>();

        }

        Dictionary<string, ColorPanel> panels;
        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            //On.DevInterface.RoomSettingsPage.Signal += RoomSettingsPage_Signal;
            On.DevInterface.RoomSettingsPage.ctor += RoomSettingsPage_ctor;

            string path = AssetManager.ResolveFilePath("PaletteCreatorBundle/palettecreatorbundle");
            var bundle = AssetBundle.LoadFromFile(path);

            Shader shader = bundle.LoadAsset<Shader>("assets/myshader/hsvpanelshader.shader");
            self.Shaders.Add("HSVPanel", FShader.CreateShader("HSVPanel", shader));
        }



        private void RoomSettingsPage_ctor(On.DevInterface.RoomSettingsPage.orig_ctor orig, RoomSettingsPage self, DevUI owner, string IDstring, DevUINode parentNode, string name)
        {
            orig(self, owner, IDstring, parentNode, name);
            //self.subNodes.Add(new PalettePanel(owner, "Palette_Panel",self,new Vector2(400,400), new Vector2(400, 260), "Palette Color :"));
            if(PaletteDrawPannel.instance == null) self.subNodes.Add(new PaletteDrawPannel(owner, self, new Vector2(400, 200), new Vector2(726f, 350f)));
        }

    }
}
