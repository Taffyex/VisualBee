using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal static class Prompt
{
	public static string Ask(IWin32Window owner, string title, string question, string initial)
	{
		Form f = new Form();
		try
		{
			f.Text = title;
			f.FormBorderStyle = FormBorderStyle.FixedDialog;
			f.MaximizeBox = false;
			f.MinimizeBox = false;
			f.ShowInTaskbar = false;
			f.StartPosition = FormStartPosition.CenterParent;
			f.BackColor = Ui.Card;
			f.ForeColor = Ui.Text;
			f.Font = Ui.Body;
			f.AutoScaleMode = AutoScaleMode.None;
			f.ClientSize = new Size(Ui.S(360), Ui.S(128));
			Label label = new Label
			{
				Text = question,
				AutoSize = true,
				Location = new Point(Ui.S(16), Ui.S(14)),
				ForeColor = Ui.Dim
			};
			InputBox box = new InputBox
			{
				Location = new Point(Ui.S(16), Ui.S(38)),
				Size = new Size(Ui.S(328), Ui.S(28))
			};
			box.Box.Text = initial ?? "";
			FlatButton flatButton = new FlatButton("OK")
			{
				Primary = true
			};
			FlatButton flatButton2 = new FlatButton("Cancel");
			int width = (flatButton2.Width = Ui.S(84));
			flatButton.Width = width;
			flatButton2.Location = new Point(f.ClientSize.Width - Ui.S(16) - flatButton2.Width, Ui.S(84));
			flatButton.Location = new Point(flatButton2.Left - Ui.S(8) - flatButton.Width, Ui.S(84));
			flatButton.Click += (object s, EventArgs a) =>
			{
				f.DialogResult = DialogResult.OK;
			};
			flatButton2.Click += (object s, EventArgs a) =>
			{
				f.DialogResult = DialogResult.Cancel;
			};
			box.Box.KeyDown += (object s, KeyEventArgs a) =>
			{
				if (a.KeyCode == Keys.Return)
				{
					f.DialogResult = DialogResult.OK;
					a.SuppressKeyPress = true;
				}
				else if (a.KeyCode == Keys.Escape)
				{
					f.DialogResult = DialogResult.Cancel;
				}
			};
			f.Controls.AddRange(new Control[4] { label, box, flatButton, flatButton2 });
			f.Shown += (object s, EventArgs a) =>
			{
				box.Box.Focus();
				box.Box.SelectAll();
			};
			f.HandleCreated += (object s, EventArgs a) =>
			{
				Palette.DarkTitle(f, Palette.From(Ui.Card, Ui.Text, Ui.Card, Ui.Text));
			};
			return (f.ShowDialog(owner) == DialogResult.OK) ? box.Box.Text : null;
		}
		finally
		{
			if (f != null)
			{
				((IDisposable)f).Dispose();
			}
		}
	}
}
