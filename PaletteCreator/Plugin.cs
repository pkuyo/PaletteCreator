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
        }



        private void RoomSettingsPage_ctor(On.DevInterface.RoomSettingsPage.orig_ctor orig, RoomSettingsPage self, DevUI owner, string IDstring, DevUINode parentNode, string name)
        {
            orig(self, owner, IDstring, parentNode, name);
            //self.subNodes.Add(new PalettePanel(owner, "Palette_Panel",self,new Vector2(400,400), new Vector2(400, 260), "Palette Color :"));
            self.subNodes.Add(new PaletteDrawPannel(owner, self, new Vector2(400, 200), new Vector2(726f, 350f)));
        }


        private void RoomSettingsPage_Signal(On.DevInterface.RoomSettingsPage.orig_Signal orig, RoomSettingsPage self, DevUISignalType type, DevUINode sender, string message)
        {
            orig(self,type,sender,message);
            if (sender is ColorButton && type == DevUISignalType.Create && !panels.ContainsKey(message))
            {
                ColorPanel colorPanel = new ColorPanel(self.owner, message +"_ColorPicker" , self,(sender as PositionedDevUINode).absPos + new Vector2(300, -20), message);
                panels.Add(message, colorPanel);
                self.subNodes.Add(colorPanel);
                colorPanel.Refresh();

                //LerpColorPanel lerpColorPanel = new LerpColorPanel(self.owner, message + "_LerpColor", self, (sender as PositionedDevUINode).absPos + new Vector2(300, -20), message);
                //self.subNodes.Add(lerpColorPanel);
                //lerpColorPanel.Refresh();
            }
            else
            {
                self.subNodes.Remove(panels[message]);
                panels[message].ClearSprites();
                panels[message] = null;
                panels.Remove(message);
            }

            //TODO: Hook Signal设置房间
        }
    }
}
