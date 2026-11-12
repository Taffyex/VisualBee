using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MusicBeePlugin
{
    public class MiniPlayerArtwork
    {
        public Image LoadArtworkFromFile(string path)
        {
            if (!File.Exists(path))
                return null;

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (var ms = new MemoryStream())
                {
                    fs.CopyTo(ms);
                    ms.Position = 0;
                    return Image.FromStream(ms);
                }
            }
        }
    }
}