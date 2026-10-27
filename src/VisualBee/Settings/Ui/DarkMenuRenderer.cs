using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
	private sealed class DarkColours : ProfessionalColorTable
	{
		public override Color ToolStripDropDownBackground => Ui.Card;

		public override Color MenuBorder => Ui.Border;

		public override Color MenuItemBorder => Ui.Hover;

		public override Color MenuItemSelected => Ui.Hover;

		public override Color MenuItemSelectedGradientBegin => Ui.Hover;

		public override Color MenuItemSelectedGradientEnd => Ui.Hover;

		public override Color MenuItemPressedGradientBegin => Ui.Raised;

		public override Color MenuItemPressedGradientEnd => Ui.Raised;

		public override Color ImageMarginGradientBegin => Ui.Card;

		public override Color ImageMarginGradientMiddle => Ui.Card;

		public override Color ImageMarginGradientEnd => Ui.Card;

		public override Color SeparatorDark => Ui.Border;

		public override Color SeparatorLight => Ui.Card;

		public override Color CheckBackground => Ui.Card;

		public override Color CheckSelectedBackground => Ui.Hover;

		public override Color CheckPressedBackground => Ui.Hover;
	}

	public DarkMenuRenderer()
		: base(new DarkColours())
	{
		RoundedEdges = false;
	}

	protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
	{
		e.TextColor = (e.Item.Enabled ? Ui.Text : Ui.Faint);
		base.OnRenderItemText(e);
	}

	protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
	{
		e.ArrowColor = Ui.Dim;
		base.OnRenderArrow(e);
	}

	protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
	{
		Rectangle imageRectangle = e.ImageRectangle;
		Ui.Glyph(e.Graphics, "\ue73e", Ui.Icons, Ui.Accent, new Rectangle(imageRectangle.X - 2, imageRectangle.Y - 2, imageRectangle.Width + 4, imageRectangle.Height + 4));
	}
}
