using DevInterface;
using HarmonyLib;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace pkuyo.PaletteCreator.Dev
{
    
    class PalettePanel : Panel
    {
        public PalettePanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, Vector2 size, string title) : base(owner, IDstring, parentNode, pos, size, title)
        {
            //TODO : 传PaletteTexture
            int i = 0;
            foreach (var field in typeof(RoomPalette).GetFields())
            {
                if (field.FieldType == typeof(Color))
                    subNodes.Add(new ColorButton(owner, field.Name + "_Button", this, GetButtonPos(ref i), 190, field.Name));
            }
            //TODO : 添加 section button,save button
        }

        Vector2 GetButtonPos(ref int i)
        {
            var re = new Vector2((5 + ((i < 10) ? 0 : 1) * 195f), (size.y - 16f - 35f - 20f * (i < 10 ? i : i - 10)));
            i++;
            return re;
        }
    }

    class ColorButton : Button
    {
        public ColorButton(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float width, string text) : base(owner, IDstring, parentNode, pos, width, text)
        {
        }

        public override void Clicked()
        {
            DevUINode devUINode = this;
            while (devUINode != null)
            {
                devUINode = devUINode.parentNode;
                if (devUINode is Page)
                {
                    (devUINode as IDevUISignals).Signal(DevUISignalType.Create, this, IDstring.Replace("_Button", ""));
                    break;
                }
            }
        }
    }
    class LerpColorPanel : Panel
    {
        public LerpColorPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title) : base(owner, IDstring, parentNode, pos, new Vector2(200, 80), title)
        {
            subNodes.Add(new LerpColorPicker(owner, "Color", this, new Vector2(5, 5)));
        }
        public class LerpColorPicker : PositionedDevUINode, IDevUISignals
        {
            public RenderTexture depthTexuture_render;
            private Texture2D depthTexuture;
            bool held;


            ColorPanel activePanel;

            static readonly Vector2 offest = new Vector2(0, 30f);
            public LerpColorPicker(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos)
            {

                depthTexuture = new Texture2D(30, 6);
                depthTexuture.filterMode = FilterMode.Point;
                depthTexuture.wrapMode = TextureWrapMode.Clamp;

                depthTexuture_render = new RenderTexture(new RenderTextureDescriptor(30, 6));
                depthTexuture_render.filterMode = FilterMode.Point;
                depthTexuture_render.wrapMode = TextureWrapMode.Clamp;

                for (int i = 0; i < 6; i++)
                {
                    KeyColors[i] = new List<KeyValuePair<int, Color>>();
                    KeyColors[i].Add(new KeyValuePair<int, Color>(0, Color.HSVToRGB(i / 6f, 1, 0)));
                    KeyColors[i].Add(new KeyValuePair<int, Color>(30, Color.HSVToRGB(i / 6f, 1, 1)));
                }

                UpdateTexturePixel();


                fSprites.Add(new FTexture(depthTexuture_render, "SimpleLerp"));
                fSprites[fSprites.Count - 1].anchorX = 0;
                fSprites[fSprites.Count - 1].anchorY = 0;
                fSprites[fSprites.Count - 1].width = 180;
                fSprites[fSprites.Count - 1].SetPosition(offest);
                fSprites[fSprites.Count - 1].height = 36;
                if (owner != null)
                    Futile.stage.AddChild(fSprites[fSprites.Count - 1]);

                fSprites.Add(new FSprite("pixel"));
                fSprites[fSprites.Count - 1].anchorX = 0;
                fSprites[fSprites.Count - 1].anchorY = 0;
                fSprites[fSprites.Count - 1].x = 200 - 5;
                fSprites[fSprites.Count - 1].y = -5;
                fSprites[fSprites.Count - 1].isVisible = false;
                if (owner != null)
                    Futile.stage.AddChild(fSprites[fSprites.Count - 1]);

                subNodes.Add(new ArrowButton(owner, "Left", this, new Vector2(20, 5), -90));
                subNodes.Add(new ArrowButton(owner, "Right", this, new Vector2(120, 5), 90));

            }

            public override void Refresh()
            {
                base.Refresh();
                MoveSprite(0, absPos + offest);
                MoveSprite(1, absPos + new Vector2(200 - 5, -5));
            }

            bool TryGetPixelIndex(out IntVector2 index)
            {
                index = new IntVector2(-1, -1);
                var pos = (owner.mousePos - absPos - offest);
                if (pos.x <= 180 && pos.x >= 0 && pos.y <= 36 && pos.y >= 0)
                {
                    index.x = Mathf.FloorToInt(pos.x / 6);
                    index.y = Mathf.FloorToInt(pos.y / 6);
                    return true;
                }
                else
                    return false;
            }

            void UpdateTexturePixel()
            {
                for (int i = 0; i < 6; i++)
                {
                    int index = 0;
                    for (int j = 0; j < 30; j++)
                    {
                        if (index + 1 != KeyColors[i].Count && j > KeyColors[i][index + 1].Key)
                            index++;

                        if (KeyColors[i][index].Key >= j)
                            depthTexuture.SetPixel(j, i, KeyColors[i][index].Value);
                        else if (index + 1 == KeyColors[i].Count)
                            depthTexuture.SetPixel(j, i, KeyColors[i][index].Value);
                        else
                            depthTexuture.SetPixel(j, i, Color.Lerp(KeyColors[i][index].Value, KeyColors[i][index + 1].Value, Mathf.InverseLerp(KeyColors[i][index].Key, KeyColors[i][index + 1].Key, j)));

                    }
                }
                depthTexuture.Apply();
                Graphics.Blit(depthTexuture, depthTexuture_render);

            }

            void CreateNewPicker(IntVector2 index)
            {
                if (activePanel != null)
                {
                    subNodes.Remove(activePanel);
                    activePanel.ClearSprites();
                    activePanel = null;
                }
                activePanel = new ColorPanel(owner, IDstring.Replace("_LerpColor", "") + index.ToString() + "_ColorPicker", this, new Vector2(200, 0), IDstring.Replace("_LerpColor", "") + index.ToString(), depthTexuture.GetPixel(index.x, index.y), index);
                subNodes.Add(activePanel);
                activePanel.Refresh();
            }
            public override void Update()
            {
                base.Update();
                if (owner != null && owner.mouseClick && !held)
                {
                    IntVector2 index;
                    if (TryGetPixelIndex(out index))
                    {
                        CreateNewPicker(index);
                    }
                    held = true;
                }
                if (held && (owner == null || !owner.mouseDown))
                    held = false;

                if (activePanel != null)
                {
                    var pos = activePanel.pos - new Vector2(200 - 5, -5);
                    fSprites[1].isVisible = true;
                    fSprites[1].width = pos.magnitude;
                    fSprites[1].rotation = Custom.VecToDeg(Vector2.Perpendicular(pos));
                }
                else
                    fSprites[1].isVisible = false;
            }

            void IDevUISignals.Signal(DevUISignalType type, DevUINode sender, string message)
            {

                if (sender is ColorPanel.ColorPicker)
                {
                    var index = (sender.parentNode as ColorPanel).index;
                    int insertPos = 0;
                    for (int i = 0; i < KeyColors[index.y].Count; i++)
                    {
                        if (KeyColors[index.y][i].Key < index.x)
                            insertPos = i + 1;
                        else if (KeyColors[index.y][i].Key == index.x)
                        {
                            KeyColors[index.y].RemoveAt(i);
                            break;
                        }
                        else
                            break;

                    }
                    KeyColors[index.y].Insert(insertPos, new KeyValuePair<int, Color>(index.x, (sender as ColorPanel.ColorPicker).PickedColor));
                }
                else if (sender is Button)
                {

                    var index = (sender.parentNode as ColorPanel).index;
                    if (KeyColors[index.y].Count == 0)
                        return;

                    for (int i = 0; i < KeyColors[index.y].Count; i++)
                    {
                        if (KeyColors[index.y][i].Key == index.x)
                        {
                            KeyColors[index.y].RemoveAt(i);
                            break;
                        }
                    }

                }
                else if (sender is ArrowButton)
                {

                    if (activePanel != null)
                    {
                        int pos = 0;
                        var index = activePanel.index;
                        for (int i = 0; i < KeyColors[index.y].Count; i++)
                            if (index.x >= KeyColors[index.y][i].Key)
                                pos = i;
                        pos += (sender.IDstring == "Left") ? -1 : 1;
                        pos += (pos < 0) ? 1 : (pos == KeyColors[index.y].Count) ? -1 : 0;
                        CreateNewPicker(new IntVector2(KeyColors[index.y][pos].Key, index.y));
                    }
                    return;

                }
                UpdateTexturePixel();
                (owner as IDevUISignals).Signal(DevUISignalType.ButtonClick, this, IDstring);

            }
            List<KeyValuePair<int, Color>>[] KeyColors = new List<KeyValuePair<int, Color>>[6];
        }
    }

    class ColorPanel : Panel
    {
        public IntVector2 index;
        public ColorPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title, Color? color = null, IntVector2? index = null) : base(owner, IDstring, parentNode, pos, new Vector2(200, 140), title)
        {
            subNodes.Add(new ColorPicker(owner, "Color", this, new Vector2(5, 5), color));
            if (index != null)
            {
                this.index = (IntVector2)index;
                subNodes.Add(new Button(owner, "Delete", this, new Vector2(110, 20), 20, "X"));
            }
        }


        public class ColorPicker : PositionedDevUINode
        {
            static Texture2D hueTexuture;
            public Color PickedColor { get; set; }


            RectNub rectNub;
            SliderNub sliderNub;

            public ColorPicker(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, Color? color) : base(owner, IDstring, parentNode, pos)
            {

                if (hueTexuture == null)
                {
                    hueTexuture = new Texture2D(512, 20, TextureFormat.ARGB32, mipChain: false);
                    hueTexuture.wrapMode = TextureWrapMode.Clamp;
                    for (int i = 0; i < 512; i += 2)
                    {
                        for (int j = 0; j < 20; j++)
                        {
                            hueTexuture.SetPixel(i, j, Color.HSVToRGB(i / 512f, 1, 1));
                            hueTexuture.SetPixel(i + 1, j, Color.HSVToRGB(i / 512f, 1, 1));
                        }
                    }
                    hueTexuture.Apply(updateMipmaps: false);
                }

                float h = 0, s = 1, v = 1;

                //留给测试的
                if (color == null)
                {
                    Debug.LogError("[Picker] Use null Color");
                }
                else
                    Color.RGBToHSV((Color)color, out h, out s, out v);

                fSprites.Add(new FSprite("Futile_White"));
                fSprites[fSprites.Count - 1].width = 20;
                fSprites[fSprites.Count - 1].height = 20;
                fSprites[fSprites.Count - 1].anchorX = 0;
                fSprites[fSprites.Count - 1].anchorY = 0;
                fSprites[fSprites.Count - 1].x = 107.5f;
                fSprites[fSprites.Count - 1].y = 107.5f;
                if (owner != null)
                    Futile.stage.AddChild(fSprites[fSprites.Count - 1]);


                fSprites.Add(new FSprite("Futile_White"));
                fSprites[fSprites.Count - 1].width = 15;
                fSprites[fSprites.Count - 1].height = 15;
                fSprites[fSprites.Count - 1].anchorX = 0;
                fSprites[fSprites.Count - 1].anchorY = 0;
                fSprites[fSprites.Count - 1].x = 110;
                fSprites[fSprites.Count - 1].y = 110;
                fSprites[fSprites.Count - 1].color = Color.HSVToRGB(h, s, v);
                if (owner != null)
                    Futile.stage.AddChild(fSprites[fSprites.Count - 1]);



                fSprites.Add(new CustomFSprite("Futile_White") { shader = owner.room.game.rainWorld.Shaders["HSVPanel"]});
                var rect = (fSprites[fSprites.Count - 1] as CustomFSprite);
                rect.anchorX = 0;
                rect.anchorY = 0;
                rect.MoveVertice(1, new Vector2(0, 100));
                rect.MoveVertice(3, new Vector2(100, 0));
                rect.MoveVertice(2, new Vector2(100, 100));
                rect.verticeColors[0] = new Color(1f - h, 0, 0);
                rect.verticeColors[1] = new Color(1f - h, 0, 1);
                rect.verticeColors[2] = new Color(1f - h, 1, 1);
                rect.verticeColors[3] = new Color(1f - h, 1, 0);
                if (owner != null)
                    Futile.stage.AddChild(fSprites[fSprites.Count - 1]);




                fSprites.Add(new FTexture(hueTexuture, "SimpleHUE"));
                fSprites[fSprites.Count - 1].anchorX = 0;
                fSprites[fSprites.Count - 1].anchorY = 0;
                fSprites[fSprites.Count - 1].width = 100;
                fSprites[fSprites.Count - 1].height = 20;
                fSprites[fSprites.Count - 1].y = 110;
                if (owner != null)
                    Futile.stage.AddChild(fSprites[fSprites.Count - 1]);

                subNodes.Add(rectNub = new RectNub(owner, "Rect_Nub", this, new Vector2(s * 100, v * 100)));
                subNodes.Add(sliderNub = new SliderNub(owner, "Slider_Nub", this, new Vector2(h * 100, 110)));

            }

            public override void Update()
            {
                base.Update();
                if (sliderNub != null && sliderNub.held)
                {
                    float hue = sliderNub.pos.x / 100f;
                    (fSprites[fSprites.Count - 2] as CustomFSprite).verticeColors[0] = new Color(1f - hue, 0, 0);
                    (fSprites[fSprites.Count - 2] as CustomFSprite).verticeColors[1] = new Color(1f - hue, 0, 1);
                    (fSprites[fSprites.Count - 2] as CustomFSprite).verticeColors[2] = new Color(1f - hue, 1, 1);
                    (fSprites[fSprites.Count - 2] as CustomFSprite).verticeColors[3] = new Color(1f - hue, 1, 0);
                }

                if ((sliderNub != null && rectNub != null) && (sliderNub.held || rectNub.held))
                {
                    fSprites[1].color = PickedColor = Color.HSVToRGB(sliderNub.pos.x / 100f, rectNub.pos.x / 100f, rectNub.pos.y / 100f);

                    DevUINode devUINode = this;
                    while (devUINode != null)
                    {
                        devUINode = devUINode.parentNode;
                        if (devUINode is IDevUISignals)
                            (devUINode as IDevUISignals).Signal(DevUISignalType.ButtonClick, this, IDstring.Replace("_Button", ""));
                    }
                }
            }

            public override void Refresh()
            {
                base.Refresh();
                MoveSprite(0, absPos + new Vector2(107.5f, 107.5f));
                MoveSprite(1, absPos + new Vector2(110, 110));
                MoveSprite(2, absPos);
                MoveSprite(3, absPos + new Vector2(0, 110));
            }

            public class SliderNub : RectangularDevUINode
            {
                public bool held;


                public SliderNub(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(8f, 16f))
                {
                    this.pos = pos;
                    fSprites.Add(new FSprite("pixel"));
                    fSprites[fSprites.Count - 1].scaleY = size.y;
                    fSprites[fSprites.Count - 1].scaleX = size.x;
                    fSprites[fSprites.Count - 1].anchorX = 0f;
                    fSprites[fSprites.Count - 1].anchorY = 0f;
                    if (owner != null)
                    {
                        Futile.stage.AddChild(fSprites[fSprites.Count - 1]);
                    }
                }

                public bool CanPick
                {
                    get
                    {
                        var mouse = owner.mousePos - (parentNode as PositionedDevUINode).absPos;
                        return mouse.x <= 100 && mouse.x >= 0 && mouse.y <= 130 && mouse.y >= 110;
                    }
                }


                public override void Update()
                {
                    base.Update();

                    if (held)
                        fSprites[fSprites.Count - 1].color = new Color(0f, 0f, 0);
                    else
                        fSprites[fSprites.Count - 1].color = (MouseOver ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.5f, 0.5f, 0.5f));


                    if (owner != null && owner.mouseClick && CanPick)
                        held = true;
                    if (held && (owner == null || !owner.mouseDown || !CanPick))
                        held = false;

                    if (held)
                        Move(new Vector2(pos.x + owner.mousePos.x - absPos.x, pos.y));
                }

                public override void Refresh()
                {
                    base.Refresh();
                    MoveSprite(0, absPos);
                }
            }

            public class RectNub : CircleDevUINode
            {
                public bool held;
                public RectNub(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, 15)
                {
                    fSprites.Add(new FSprite("Circle20"));
                    fSprites[fSprites.Count - 1].width = 15;
                    fSprites[fSprites.Count - 1].height = 15;
                    if (owner != null)
                        Futile.stage.AddChild(fSprites[fSprites.Count - 1]);

                }
                public bool CanPick
                {
                    get
                    {
                        var mouse = owner.mousePos - (parentNode as PositionedDevUINode).absPos;
                        return mouse.x <= 100 && mouse.x >= 0 && mouse.y <= 100 && mouse.y >= 0;
                    }
                }

                public override void Update()
                {
                    base.Update();
                    if (held)
                        fSprites[fSprites.Count - 1].color = new Color(0f, 0f, 0);

                    else
                        fSprites[fSprites.Count - 1].color = (MouseOver ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.5f, 0.5f, 0.5f));


                    if (owner != null && owner.mouseClick && CanPick)
                        held = true;

                    if (held && (owner == null || !owner.mouseDown || !CanPick))
                        held = false;

                    if (held)
                        Move(pos + owner.mousePos - absPos);
                }
                public override void Refresh()
                {
                    base.Refresh();
                    MoveSprite(0, absPos);
                }

            }

        }
    }

    abstract public class CircleDevUINode : PositionedDevUINode
    {
        public float size;

        public bool MouseOver
        {
            get
            {
                if (owner != null)
                {
                    if (Custom.DistLess(absPos, owner.mousePos, size))
                    {
                        return true;
                    }
                    return false;
                }
                return false;
            }
        }
        public CircleDevUINode(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, float size) : base(owner, IDstring, parentNode, pos)
        {
            this.size = size;
        }
    }

}
