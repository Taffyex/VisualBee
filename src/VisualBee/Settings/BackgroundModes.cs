namespace MusicBeePlugin;

internal static class BackgroundModes
{
	public const int Solid = 0;

	public const int Linear = 1;

	public const int Radial = 2;

	public const int Aurora = 3;

	public const int AlbumArt = 4;

	public static readonly string[] Names = new string[5] { "Solid colour", "Linear gradient", "Radial glow", "Aurora (animated)", "Album art" };

	public static readonly string[] Tokens = new string[5] { "solid", "linear", "radial", "aurora", "art" };
}
