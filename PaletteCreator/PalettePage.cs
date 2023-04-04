using DevInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace pkuyo.PaletteCreator
{
    public class PalettePage : Page
    {
		public Panel palettePanel;
		public PaletteRepresent[] allPaletteRepresents;

		public int maxPalettesPerPage = 20;
		public int currPalettesPage;
		public int totalPalettesPages;

		public PalettePage(DevUI owner, string IDstring, DevUINode parentNode, string name) : base(owner, IDstring, parentNode, name)
		{
			this.parentNode = parentNode;
			palettePanel = new Panel(owner, "Palettes_Panel", this, new Vector2(1050f, 250f), new Vector2(200f, 440f), "Palettes");
			allPaletteRepresents = new PaletteRepresent[PaletteManager.represents.Count];
			for (int i = 0; i < this.allPaletteRepresents.Length; i++)
			{
				allPaletteRepresents[i] = PaletteManager.represents[i];
			}
			totalPalettesPages = 1 + (int)((float)allPaletteRepresents.Length / (float)this.maxPalettesPerPage + 0.5f);
			subNodes.Add(palettePanel);
			for (int j = 0; j < 2; j++)
			{
				palettePanel.subNodes.Add(new Button(owner, (j != 0) ? "Next_Button" : "Prev_Button", palettePanel, new Vector2(5f + 100f * (float)j, palettePanel.size.y - 16f - 5f), 95f, (j != 0) ? "Next Page" : "Previous Page"));
			}
			this.RefreshPalettesPage();
		}

		public void RefreshPalettesPage()
		{
			if (totalPalettesPages == 0)
			{
				currPalettesPage = 0;
			}
			for (int i = palettePanel.subNodes.Count - 1; i >= 2; i--)
			{
				palettePanel.subNodes[i].ClearSprites();
				palettePanel.subNodes.RemoveAt(i);
			}
			int num = currPalettesPage * maxPalettesPerPage;
			int num2 = 0;
			while (num2 < maxPalettesPerPage && num2 + num < allPaletteRepresents.Length)
			{
				palettePanel.subNodes.Add(new SelectPaletteButton(owner, palettePanel, new Vector2(5f, palettePanel.size.y - 16f - 35f - 20f * (float)num2), 190f, allPaletteRepresents[num + num2]));
				num2++;
			}
		}

		public override void Signal(DevUISignalType type, DevUINode sender, string message)
		{
			Debug.Log(sender.ToString());
			if(sender is SelectPaletteButton)
            {
				Debug.Log(parentNode.ToString());
				(parentNode as PaletteDrawPannel).Signal(DevUISignalType.ButtonClick, this, message);
			}
            else
            {
				string idstring = sender.IDstring;
				switch (idstring)
				{
					case "Prev_Button":
						currPalettesPage--;
						if (this.currPalettesPage < 0)
						{
							this.currPalettesPage = this.totalPalettesPages - 1;
						}
						RefreshPalettesPage();
						break;
					case "Next_Button":
						this.currPalettesPage++;
						if (this.currPalettesPage >= this.totalPalettesPages)
						{
							this.currPalettesPage = 0;
						}
						RefreshPalettesPage();
						break;
				}
			}
		}
	}

	public class SelectPaletteButton : Button
	{
		// Token: 0x06000C22 RID: 3106 RVA: 0x0007FD3C File Offset: 0x0007DF3C
		public SelectPaletteButton(DevUI owner, DevUINode parentNode, Vector2 pos, float width, PaletteRepresent palette) : base(owner, "SelectPalette_" + palette.name, parentNode, pos, width, palette.name)
		{
			this.palette = palette;
		}

		// Token: 0x06000C23 RID: 3107 RVA: 0x0007FD80 File Offset: 0x0007DF80
		public override void Clicked()
		{
			Debug.Log(palette);
			Debug.Log(parentNode.parentNode.ToString());
			(parentNode.parentNode as IDevUISignals).Signal(DevUISignalType.ButtonClick, this, palette.name);
		}

		public PaletteRepresent palette;
	}
}
