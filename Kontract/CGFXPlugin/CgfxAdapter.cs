using System.IO;

namespace image_nintendo.CGFX
{
    public class CgfxAdapter
    {
        public CGFX Image;
        public FileInfo FileInfo { get; set; }

        public void Load(string filename)
        {
            FileInfo = new FileInfo(filename);

            if (FileInfo.Exists)
            {
                Image = new CGFX(FileInfo.OpenRead());
            }
        }

        public void Save(string filename = "")
        {
            if (filename.Trim() != string.Empty)
                FileInfo = new FileInfo(filename);
            Image.Save(FileInfo.FullName);
        }
    }
}
