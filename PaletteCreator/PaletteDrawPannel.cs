using DevInterface;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using static pkuyo.PaletteCreator.Dev.ColorPanel;

namespace pkuyo.PaletteCreator
{
    public class PaletteDrawPannel : Panel , IDevUISignals
    {
        public static PaletteDrawPannel instance;

        public static Vector2 normalSpacing = new Vector2(4f,4f);
        public static Vector2 paletteBias = new Vector2(150f, 20f);
        public static string tileDefinationTitle = "CurrentTile : ";
        public Vector2 tileDefinationLabelPos;
        public Vector2 currentPaletteLabelPos;

        public Button getCurrentButton;
        public Button clearLabelsInRowButton;
        public Button applyCurrentButton;
        public Button selectPaletteButton;
        public Button savePaletteButton;

        public Button addLabelButton;
        public Button deleteLabelButton;
        
        public Button setColorButton;

        PaletteColorPicker colorPicker;

        public List<GradientRow> rows = new List<GradientRow>();
        public PalettePixelRepresent[,] pixelRepresents;
        public Texture2D currentPalette;

        public FLabel tileDefinationLabel;
        public FLabel currentPaletteLabel;

        public PaletteRepresent currentRepresent;
        public PaletteDrawPannel(DevUI owner,DevUINode parent,Vector2 pos,Vector2 size) : base(owner, "PaletteDrawPannel", parent, pos, size, "PaletteCreator")
        {
            PaletteManager.LoadAll();

            tileDefinationLabelPos = new Vector2(5f, size.y - 5f);
            currentPaletteLabelPos = new Vector2(500f, size.y - 5f);

            currentPalette = new Texture2D(32, 16);
            currentPalette.filterMode = FilterMode.Point;

            getCurrentButton = new Button(owner, "PaletteRefreshButton", this, new Vector2(5f, size.y - 35f), 70f, "GetCurrent");
            clearLabelsInRowButton = new Button(owner, "PaletteClearLabelsButton", this, new Vector2(85f, size.y - 35f), 60f, "ClearRow");
            setColorButton = new Button(owner, "PaletteSetColor", this, new Vector2(10f, size.y - 200f - 20f), 40f, "SetCol");
            deleteLabelButton = new Button(owner, "PaletteRemoveLabel", this, new Vector2(60f, size.y - 200f - 20f), 80f, "RemoveLabel");
            applyCurrentButton = new Button(owner, "PaletteApplyCurrentButton", this, new Vector2(155f, size.y - 35f), 80f, "ApplyCurrent");
            selectPaletteButton = new Button(owner, "PaletteSelectPaletteButton", this, new Vector2(245f, size.y - 35f), 90f, "SelectPalette");
            savePaletteButton = new Button(owner, "PaletteSavePaletteButton", this, new Vector2(370, size.y - 35f), 90f, "SavePalette");
            subNodes.Add(getCurrentButton);
            subNodes.Add(clearLabelsInRowButton);
            subNodes.Add(setColorButton);
            subNodes.Add(deleteLabelButton);
            subNodes.Add(applyCurrentButton);
            subNodes.Add(selectPaletteButton);
            subNodes.Add(savePaletteButton);

            SetUpRowAndPixelRepresents();
            foreach(var pixel in pixelRepresents)
            {
                subNodes.Add(pixel);
            }

            colorPicker = new PaletteColorPicker(owner, "PaletterColorPicker", this, new Vector2(10f, size.y - 200f), null);
            subNodes.Add(colorPicker);

            tileDefinationLabel = new FLabel(Custom.GetFont(), tileDefinationTitle + "null") { anchorX = 0,scale = 1.1f};
            currentPaletteLabel = new FLabel(Custom.GetFont(), "no palette") { anchorX = 0,scale = 1.1f};

            fLabels.Add(tileDefinationLabel);
            fLabels.Add(currentPaletteLabel);

            Futile.stage.AddChild(tileDefinationLabel);
            Futile.stage.AddChild(currentPaletteLabel);

            tileDefinationLabel.SetPosition(absPos + tileDefinationLabelPos);
            currentPaletteLabel.SetPosition(absPos + currentPaletteLabelPos);

            instance = this;
        }

        public void SetUpRowAndPixelRepresents()
        {
            pixelRepresents = new PalettePixelRepresent[32, 16];
            
            for (int y = 0; y < 16; y++)
            {
                //仅在主要色板的区域使用行渐变
                DevUINode parent = this;
                if(y < GradientRow.names.Length && GradientRow.names[y] != "")
                {
                    parent = new GradientRow(owner, y, Vector2.zero, this, GradientRow.names[y]);
                    rows.Add(parent as GradientRow);
                }

                for (int x = 0; x < 32; x++)
                {
                    IntVector2 pixelPos = new IntVector2(x, y);
                    Vector2 pos = new Vector2(normalSpacing.x * (x + 1f) + 14f * x, normalSpacing.y * (y + 1) + 14f * y) + paletteBias;
                    var newRepresent = new PalettePixelRepresent(owner, pixelPos, pos, ((x < 30) ? parent : this));
                    pixelRepresents[x, y] = newRepresent;
                }
            }
            foreach (var represent in pixelRepresents)
            {
                represent.Signal(DevUISignalType.ButtonClick, this, "GetCurrentPalette");
            }
        }

        public void Signal(DevUISignalType type, DevUINode sender, string message)
        {
            if(sender == getCurrentButton)
            {
                currentRepresent = PaletteManager.GetPaletteRepresentOfIndex(owner.room.world.game.cameras[0].paletteA);
                currentRepresent.CopyPaletteToTex(ref currentPalette);
                foreach (var represent in pixelRepresents)
                {
                    represent.Signal(type, this, "GetCurrentPalette");
                }
            }
            else if(sender == clearLabelsInRowButton)
            {
                if(PalettePixelRepresent.lastClickRepresent != null && PalettePixelRepresent.lastClickRepresent.parentNode is GradientRow)
                {
                    (PalettePixelRepresent.lastClickRepresent.parentNode as GradientRow).Signal(DevUISignalType.ButtonClick, this, "ClearAllLabelInRow");
                }
            }
            else if (sender == setColorButton)
            {
                if (PalettePixelRepresent.lastClickRepresent != null)
                {
                    PalettePixelRepresent.lastClickRepresent.representCol = colorPicker.PickedColor;
                    PalettePixelRepresent.lastClickRepresent.MakeLabel();
                    PalettePixelRepresent.lastClickRepresent.Refresh();
                }
            }
            else if (sender == deleteLabelButton)
            {
                if (PalettePixelRepresent.lastClickRepresent != null && PalettePixelRepresent.lastClickRepresent.parentNode is GradientRow)
                {
                    PalettePixelRepresent.lastClickRepresent.DeleteLabel();
                    (PalettePixelRepresent.lastClickRepresent.parentNode as GradientRow).RefreshColorOfRow();
                }
            }
            else if(sender == applyCurrentButton)
            {
                ApplyPixelRepresentToTexture();
                ApplyCurrentToCamera();
            }
            else if(sender == selectPaletteButton)
            {
                foreach(var node in subNodes)
                {
                    if (node is PalettePage) return;
                }
                subNodes.Add(new PalettePage(owner, "PalettePalettePage", this, "SelectPalettes"));
            }
            else if(sender == savePaletteButton)
            {
                if (currentRepresent == null) return;
                Debug.Log(String.Format("SavePalette\nRepresentInfo : name - {0}\nindex - {1}\npath - {2}\nisCustom - {3}\n{4}\n{5}",currentRepresent.name,currentRepresent.index,currentRepresent.path,currentRepresent.isCustomPalette,currentPalette.ToString(),sender.ToString()));

                ApplyPixelRepresentToTexture();
                currentPalette.Apply();
                if (currentRepresent.isCustomPalette) PaletteManager.SavePaletteTexture(currentPalette, currentRepresent);
                else
                {
                    currentRepresent = PaletteManager.SaveVanillaPaletteAsNew(currentPalette);
                    currentRepresent.CopyPaletteToTex(ref currentPalette);

                    foreach (var represent in pixelRepresents)
                    {
                        represent.Signal(type, this, "GetCurrentPalette");
                    }
                }
            }
            else if(sender is PalettePixelRepresent)
            {
                int x = (sender as PalettePixelRepresent).representPixel.x;
                int y = (sender as PalettePixelRepresent).representPixel.y;
                string[] array = Regex.Split(message, PalettePixelRepresent.SplitSymbol);
                string title = array[0];

                switch (title)
                {
                    case "DisSelectPixel":
                        tileDefinationLabel.text = tileDefinationTitle + "null";
                        colorPicker.ConnectToPixel(null);
                        break;
                    case "SelectPixel":
                        if(sender.parentNode == this)
                        {
                            foreach (var pixel in pixelRepresents)
                            {
                                pixel.Signal(DevUISignalType.ButtonClick, this, "RowDisSelected");
                            }
                        }
                        colorPicker.ConnectToPixel(sender as PalettePixelRepresent);
                        tileDefinationLabel.text = tileDefinationTitle + (sender as PalettePixelRepresent).name;
                        break;
                }
            }
            else if(sender is PalettePage)
            {
                currentRepresent = PaletteManager.GetPaletteRepresentOfName(message);
                sender.ClearSprites();
                subNodes.Remove(sender);

                currentRepresent.CopyPaletteToTex(ref currentPalette);

                Debug.Log(String.Format("LoadTexture\nRepresentInfo : name - {0}\nindex - {1}\npath - {2}\nisCustom - {3}\n{4}\n{5}", currentRepresent.name, currentRepresent.index, currentRepresent.path, currentRepresent.isCustomPalette, currentPalette.ToString(), sender.ToString()));

                foreach (var represent in pixelRepresents)
                {
                    represent.Signal(type, this, "GetCurrentPalette");
                }
            }
            Refresh();
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            instance = null;

        }

        public override void Refresh()
        {
            base.Refresh();
            tileDefinationLabel.SetPosition(absPos + tileDefinationLabelPos);
            currentPaletteLabel.SetPosition(absPos + currentPaletteLabelPos);

            string text = "";
            if (currentRepresent != null)
            {
                text += currentRepresent.name.Replace(".png","");
                if (currentRepresent.isCustomPalette)
                {
                    text += "  isCustom";
                    text += currentRepresent.everSaved ? "  everSaved" : "  notSaved";
                }
            }
            else text += "no palette";
            currentPaletteLabel.text = text;
        }

        public void ApplyPixelRepresentToTexture()
        {
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    currentPalette.SetPixel(x, y, pixelRepresents[x,y].representCol);
                    if(currentRepresent.labels != null)
                    {
                        currentRepresent.labels[x, y] = pixelRepresents[x, y].isFadeLabel;
                    }
                }
            }
        }

        public void ApplyCurrentToCamera()
        {
            var cam = owner.room.world.game.cameras[0];
            var camTexA = cam.fadeTexA;
            for(int x = 0;x < 32; x++)
            {
                for(int y = 0;y < 16; y++)
                {
                    camTexA.SetPixel(x, y, currentPalette.GetPixel(x, y));
                }
            }

            cam.ApplyEffectColorsToPaletteTexture(ref camTexA, cam.room.roomSettings.EffectColorA, cam.room.roomSettings.EffectColorB);
            camTexA.Apply(false);
            cam.ApplyFade();
        }

        public void LoadCurrentPalette()
        {
            var cam = owner.room.world.game.cameras[0];
            int pal = cam.paletteA;

            PaletteManager.LoadTexture(currentRepresent, ref currentPalette);
        }
    }

    public class GradientRow : PositionedDevUINode , IDevUISignals
    {
        public static string[] names = new string[] {"RainDown","RainMid","RainUp","SunRainDown","SunRainMiddle","SunRainUp","","","ShadeDown", "ShadeMid", "ShadeUp","SunDown", "SunMid", "SunUp" };
        public static string SplitSymbol = "<gR>";

        public PaletteDrawPannel paletteDrawPannel;

        public int row;
        public string name;

        public GradientRow(DevUI owner, int reprensentRow, Vector2 pos, DevUINode parent,string name) : base(owner,"GradientRow" + SplitSymbol + reprensentRow.ToString(), parent, pos)
        {
            row = reprensentRow;
            paletteDrawPannel = parent as PaletteDrawPannel;
            this.name = name;
        }

        public void Signal(DevUISignalType type, DevUINode sender, string message)
        {
            if(sender is PalettePixelRepresent)
            {
                string[] array = Regex.Split(message, PalettePixelRepresent.SplitSymbol);
                string title = array[0];
                int x = int.Parse(array[1]);
                int y = int.Parse(array[2]);

                switch (title)
                {
                    case "SelectPixel":
                        if (y == row && x < 30)
                        {
                            foreach (var pixel in paletteDrawPannel.pixelRepresents)
                            {
                                pixel.Signal(DevUISignalType.ButtonClick, this, "RowSelected");
                            }
                        }
                        break;
                }
                //向上广播
                paletteDrawPannel.Signal(DevUISignalType.ButtonClick, sender, message);
            }
            else if(sender is PaletteDrawPannel)
            {
                switch (message)
                {
                    case "ClearAllLabelInRow":
                        for(int x = 1; x < 29; x++)
                        {
                            paletteDrawPannel.pixelRepresents[x, row].DeleteLabel();
                        }
                        RefreshColorOfRow();
                        break;
                }
            }
        }

        public void RefreshColorOfRow()
        {
            List<PalettePixelRepresent> allLabels = new List<PalettePixelRepresent>();
            for (int x = 0; x < 30; x++)
            {
                if (paletteDrawPannel.pixelRepresents[x, row].isFadeLabel) allLabels.Add(paletteDrawPannel.pixelRepresents[x, row]);
            }

            int allLabelCursor = 0;
            PalettePixelRepresent leftRepresent = allLabels[allLabelCursor++];
            PalettePixelRepresent rightRepresent = allLabels[allLabelCursor++];

            for (int x = 0; x < 30; x++)
            {
                var current = paletteDrawPannel.pixelRepresents[x, row];
                if (current == leftRepresent) continue;
                if (current == rightRepresent)
                {
                    leftRepresent = rightRepresent;
                    if (allLabelCursor < allLabels.Count) rightRepresent = allLabels[allLabelCursor++];
                }
                float t = Mathf.InverseLerp(leftRepresent.representPixel.x, rightRepresent.representPixel.x, x);
                Color lerpCol = Color.Lerp(leftRepresent.representCol, rightRepresent.representCol, t);

                paletteDrawPannel.pixelRepresents[x, row].representCol = lerpCol;
                paletteDrawPannel.pixelRepresents[x, row].Refresh();
            }
        }
    }

    public class PalettePixelRepresent : PositionedDevUINode , IDevUISignals
    {
        public static PalettePixelRepresent lastClickRepresent;

        public static string SplitSymbol = "<pP>";
        public static string[] xName = { "Sky", "Fog", "Black", "Item", "DeepWaterTop", "DeepWaterBottom", "WaterSurfaceClose", "WaterSurfaceFar", "WaterSurfaceHighlight", "FogIntensity", "Shortcut1", "Shortcut2", "Shortcut3", "ShortcutSymbol", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "Darkness" };

        public readonly bool inGradientRow = false;
        public readonly string name;

        public IntVector2 representPixel;
        public FSprite pixel;
        public FSprite fadeLabel;

        public float scale = 14f;
        public Color representCol;
        public bool isFadeLabel = false;

        public bool rowSelected = false; 
        public bool selected = false;

        public float Scale
        {
            get
            {
                return 14 + (rowSelected ? 2f : 0f) + (selected ? 2f : 0f);
            }
        }
        public bool MouseOver
        {
            get
            {
                return (Mathf.Abs(absPos.x - owner.mousePos.x) < (Scale / 2f)) && (Mathf.Abs(absPos.y - owner.mousePos.y) < (Scale / 2f));
            }
        }

        public bool isPixelDefined
        {
            get
            {
                return inGradientRow || ((representPixel.y == 7 || representPixel.y == 15) && representPixel.x < xName.Length && xName[representPixel.x] != "") || (representPixel.y == 6 || representPixel.y == 14);
            }
        }

        public PalettePixelRepresent(DevUI owner,IntVector2 representPixel,Vector2 pos,DevUINode parent) : base(owner,"PixelReprensent" + SplitSymbol + representPixel.x.ToString() + SplitSymbol + representPixel.y.ToString(), parent, pos)
        {
            inGradientRow = parent is GradientRow;
            pixel = new FSprite("pixel", true) { scale = Scale };

            fadeLabel = new FSprite("Circle4", true);

            fSprites.Add(pixel);
            fSprites.Add(fadeLabel);

            Futile.stage.AddChild(pixel);
            Futile.stage.AddChild(fadeLabel);

            fadeLabel.MoveInFrontOfOtherNode(pixel);
            this.representPixel = representPixel;

            name = "null";
            if (inGradientRow)
            {
                name = (parent as GradientRow).name + "  Depth:" + representPixel.x.ToString();
            }
            else
            {
                if (isPixelDefined)
                {
                    if(representPixel.y != 6 && representPixel.y != 14)
                    {
                        name = xName[representPixel.x];
                    }
                    else
                    {
                        name = "GrimeGradient";
                    }
                }
            }
        }

        public void Signal(DevUISignalType type, DevUINode sender, string message)
        {
            if(sender is PaletteDrawPannel)
            {
                string[] array = Regex.Split(message, SplitSymbol);
                string title = array[0];

                int x = array.Length > 1 ? int.Parse(array[1]) : -1;
                int y = array.Length > 1 ? int.Parse(array[2]) : -1;

                switch (title)
                {
                    case "GetCurrentPalette":
                        PaletteDrawPannel paletterDrawPannel = sender as PaletteDrawPannel;
                        Texture2D tex = paletterDrawPannel.currentPalette;
                        representCol = tex.GetPixel(representPixel.x, representPixel.y);
                        if(paletterDrawPannel.currentRepresent != null && paletterDrawPannel.currentRepresent.labels != null)
                        {
                            isFadeLabel = paletterDrawPannel.currentRepresent.labels[representPixel.x, representPixel.y] && inGradientRow;
                        }
                        else
                        {
                            if (inGradientRow) isFadeLabel = true;
                        }

                        break;
                }
            }
            else if (sender is GradientRow)
            {
                string[] array = Regex.Split(sender.IDstring, GradientRow.SplitSymbol);
                int row = int.Parse(array[1]);
                bool messageForThisRow = (row == representPixel.y) && representPixel.x < 30;

                switch (message)
                {
                    case "RowSelected":
                        rowSelected = messageForThisRow;
                        break;
                    case "RefreshGradientColor":
                        if (!messageForThisRow) break;
                        break;
                    case "DisSelectRow":
                        rowSelected = false;
                        break;
                }
            }

            Refresh();
        }

        public override void Update()
        {
            base.Update();
            if (owner.mouseClick)
            {
                if (MouseOver && isPixelDefined)
                {
                    if(lastClickRepresent == this)
                    {
                        (parentNode as IDevUISignals).Signal(DevUISignalType.ButtonClick, this, "DisSelectPixel" + SplitSymbol + representPixel.x.ToString() + SplitSymbol + representPixel.y.ToString());
                        lastClickRepresent = null;
                        selected = false;
                        Refresh();
                    }
                    else
                    {
                        if (lastClickRepresent != null)
                        {
                            lastClickRepresent.selected = false;
                            lastClickRepresent.Refresh();
                        }
                        (parentNode as IDevUISignals).Signal(DevUISignalType.ButtonClick, this, "SelectPixel" + SplitSymbol + representPixel.x.ToString() + SplitSymbol + representPixel.y.ToString());
                        lastClickRepresent = this;
                        selected = true;

                        Refresh();
                    }
                }
            }
        }

        public void DeleteLabel()
        {
            if (representPixel.x < 29 && representPixel.x > 0) isFadeLabel = false;
        }

        public void MakeLabel()
        {
            if(parentNode is GradientRow)
            {
                if (representPixel.x < 30)
                {
                    isFadeLabel = true;
                }
                (parentNode as GradientRow).RefreshColorOfRow();
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            pixel.SetPosition(absPos);
            pixel.scale = Scale;
            pixel.color = representCol;
            fadeLabel.SetPosition(absPos);
            fadeLabel.isVisible = isFadeLabel;
            fadeLabel.color = new Color(1 - representCol.r, 1 - representCol.g, 1 - representCol.b);
        }
    }

    class PaletteColorPicker : ColorPicker
    {
        public FSprite line;

        public Vector2 posDelta;

        public PaletteColorPicker(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, Color? color) : base(owner, IDstring, parentNode, pos, color)
        {
            line = new FSprite("pixel", true);
            Futile.stage.AddChild(line);
            line.MoveToFront();
        }

        public void ConnectToPixel(PalettePixelRepresent represent)
        {
            if(represent == null)
            {
                posDelta = Vector2.zero;
                return;
            }
            posDelta = represent.absPos - absPos - Vector2.one * 107.5f;
            Refresh();
        }

        public override void Refresh()
        {
            base.Refresh();

            float length = posDelta.magnitude;
            float rotation = Custom.VecToDeg(posDelta);
            line.SetPosition(absPos + posDelta / 2f + Vector2.one * 107.5f);
            line.rotation = rotation + 90;
            line.scaleX = length;
            line.scaleY = 1f;
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            line.RemoveFromContainer();
        }
    }
}
