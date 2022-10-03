using System.IO;

namespace image_nintendo.BIMG
{
    public class BimgAdapter
    {
        public BIMG Image;
        public FileInfo FileInfo { get; set; }

        public void Load(string filename)
        {
            FileInfo = new FileInfo(filename);

            if (FileInfo.Exists)
                Image = new BIMG(FileInfo.OpenRead());
        }

        public void Save(string filename = "")
        {
            if (filename.Trim() != string.Empty)
                FileInfo = new FileInfo(filename);

            this.Image.Save(filename);
        }
    }
}
