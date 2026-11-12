using System;
using System.Drawing;
using System.IO;

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
                    return Image.FromStream(ms);
                }
            }

        }
    }
}