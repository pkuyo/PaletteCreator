using DevInterface;
using HarmonyLib;
using RWCustom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Color = UnityEngine.Color;

namespace pkuyo.PaletteCreator.Dev
{
  
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


        public class ColorPicker : PositionedDevUINode , IDevUISignals
        {
            static Texture2D hueTexuture;
            Texture2D screenTexture;

            public Color PickedColor { get;  set; }
            


            bool isPicking = false;
            bool waitPicking = false;
            Color lastColor = new Color();

            RectNub rectNub;
            SliderNub sliderNub;

            Button PickButton;

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

                screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

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
                subNodes.Add(PickButton = new Button(owner, "Pick_Color", this, new Vector2(0, -40),70,"Pick Color"));
            }

            public override void Update()
            {

                base.Update();

                if (waitPicking && !owner.mouseDown)
                {
                    isPicking = true;
                    waitPicking = false;
                }

                if(isPicking)
                {
                    screenTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                    screenTexture.Apply();

                    PickedColor = screenTexture.GetPixel((int)owner.mousePos.x, (int)owner.mousePos.y);
                    SetPickedColor();

                    if (owner.mouseDown && !PickButton.MouseOver)
                        isPicking = false;
                    
                }


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

            void IDevUISignals.Signal(DevUISignalType type, DevUINode sender, string message)
            {
                if(sender is Button)
                {
                    if (!isPicking)
                    {
                        lastColor = PickedColor;
                        waitPicking = true;
                    }
                    else
                    {
                        isPicking = false;
                        PickedColor = lastColor;
                        SetPickedColor();
                    }
                }
            }

            public void SetPickedColor()
            {
                float h, s, v;
                Color.RGBToHSV(PickedColor, out h, out s, out v);

                fSprites[1].color = PickedColor;
                (fSprites[fSprites.Count - 2] as CustomFSprite).verticeColors[0] = new Color(1f - h, 0, 0);
                (fSprites[fSprites.Count - 2] as CustomFSprite).verticeColors[1] = new Color(1f - h, 0, 1);
                (fSprites[fSprites.Count - 2] as CustomFSprite).verticeColors[2] = new Color(1f - h, 1, 1);
                (fSprites[fSprites.Count - 2] as CustomFSprite).verticeColors[3] = new Color(1f - h, 1, 0);
                rectNub.Move(new Vector2(s * 100, v * 100));
                sliderNub.Move(new Vector2(h * 100, 110));
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
