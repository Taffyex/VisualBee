namespace MusicBeePlugin;

internal sealed class Theme
{
	public string Name;

	public int BackTop;

	public int BackBottom;

	public int BackMiddle;

	public int Low;

	public int Mid;

	public int High;

	public int Peak;

	public int Accent;

	public int Grid;

	public int Text;

	public Theme(string name, int backTop, int backBottom, int backMiddle, int low, int mid, int high, int peak, int accent, int grid, int text)
	{
		Name = name;
		BackTop = backTop | -16777216;
		BackBottom = backBottom | -16777216;
		BackMiddle = backMiddle | -16777216;
		Low = low | -16777216;
		Mid = mid | -16777216;
		High = high | -16777216;
		Peak = peak | -16777216;
		Accent = accent | -16777216;
		Grid = grid | -16777216;
		Text = text | -16777216;
	}

	public int[] Swatch()
	{
		return new int[5] { BackBottom, Low, Mid, High, Peak };
	}
}
