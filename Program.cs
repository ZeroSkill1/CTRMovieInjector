using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO.Compression;
using image_nintendo.CGFX;
// using image_nintendo.BIMG;
using System.Reflection;
using System.Drawing;
using CommandLine;
using System.Text;
using System.IO;
using System;

namespace CTRMovieInjector
{
	internal static class Program
	{
		private static readonly byte[] wavHeader = { 0x52, 0x49, 0x46, 0x46 }; // RIFF
		private static readonly byte[] cwavHeader = { 0x43, 0x57, 0x41, 0x56 }; // CWAV
		private static readonly byte[] moflexHeader = { 0x4C, 0x32, 0xAA, 0xAB };

		private static readonly Regex titleIdRegex = new Regex(@"^0004[A-Fa-f0-9]{12}$", RegexOptions.Compiled);

		private static bool IsCwav;
		
		private static void VerifyArguments(InjectOptions options)
		{
			// string values/paths
			
			Dictionary<string, string> filesToCheck = new Dictionary<string, string>
			{
				{ "Banner Audio File Path", options.BannerAudioFilePath },
				{ "Banner Image File Path", options.BannerImagePath },
				{ "Icon Image File Path", options.IconPath },
				{ "Moflex File Path", options.MoflexPath }
			};

			foreach ((string fileName, string filePath) in filesToCheck)
			{
				InjectOptions.VerifyStringValue(filePath, fileName, 4096, path =>
				{
					if (!File.Exists(path))
					{
						throw new FileNotFoundException($"File at {path} was not found");
					}
				});
			}
			
			InjectOptions.VerifyStringValue(options.ExHeaderAppName, "ExHeader App Name", 8);
			InjectOptions.VerifyStringValue(options.LongName, "Long Name", 128);
			InjectOptions.VerifyStringValue(options.ShortName, "Short Name", 64);
			InjectOptions.VerifyStringValue(options.ProductCode, "Product Code", 16);
			InjectOptions.VerifyStringValue(options.Publisher, "Publisher", 64);
			InjectOptions.VerifyStringValue(options.TitleId, "Title ID", 16, tid =>
			{
				if (!titleIdRegex.IsMatch(tid))
				{
					throw new ArgumentException("Invalid Title ID format");
				}
			});
			
			// file headers

			InjectOptions.VerifyFileHeader(options.BannerAudioFilePath, "Banner Audio", 0, cwavHeader, out IsCwav, wavHeader);
			InjectOptions.VerifyFileHeader(options.MoflexPath, "Moflex", 0, moflexHeader, out _);
			
			// images
			
			InjectOptions.VerifyBitmapProperties(() => new Bitmap(options.BannerImagePath), "Banner", ImageFormat.Png, 120, 200);
			InjectOptions.VerifyBitmapProperties(() => new Bitmap(options.IconPath), "Icon", ImageFormat.Png, 24, 24, 48, 48);
		}

		private static void RecursivePrintException(Exception e)
		{
			Console.Error.WriteLine($"An exception occurred.\nException Type: {e.GetType()}\nException Message: {e.Message}\nStack Trace: {e.StackTrace}\n");

			while (e.InnerException != null)
			{
				Console.Error.WriteLine($"Inner Exception:\nException Type: {e.InnerException.GetType()}\nException Message: {e.InnerException.Message}\nStack Trace: {e.StackTrace}\n");
				e = e.InnerException;
			}
		}

		private static int Main(string[] args)
		{
			try
			{
				Parser.Default.ParseArguments<InjectOptions>(args)
					.WithParsed(Inject);
			}
			catch (Exception e)
			{
				RecursivePrintException(e);
				return -1;
			}

			return 0;
		}

		private static void Inject(InjectOptions options)
		{
			VerifyArguments(options);

			string tempPath = Path.GetTempFileName();

			using (FileStream fs = File.Create(tempPath))
			{
				using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("CTRMovieInjector.Resources.tmp.zip"))
				{
					s!.CopyTo(fs);
				}
			}

			Console.WriteLine($"Copied template zip to {tempPath}");

			if (Directory.Exists("./working_dir")) Directory.Delete("./working_dir", true);

			DirectoryInfo workingDir = Directory.CreateDirectory("./working_dir");

			Console.WriteLine($"Created working_dir folder in {workingDir.FullName}");

			ZipFile.ExtractToDirectory(tempPath, workingDir.FullName);

			Console.WriteLine($"Extracted zip to working_dir, deleting {tempPath}...");

			ConfigureNcch(File.OpenWrite("./working_dir/ncch.bin"), options.TitleId, options.ProductCode);

			Console.WriteLine("Wrote Product Code and Title ID into NCCH Header");

			ConfigureExHeader(File.OpenWrite("./working_dir/extheader.bin"), options.ExHeaderAppName, options.TitleId);

			Console.WriteLine("Wrote Title ID and App Name into ExHeader");

			CreateBanner(options.BannerAudioFilePath, options.BannerImagePath);

			Console.WriteLine("Created Banner");

			CreateIcon(options.IconPath, options.ShortName, options.LongName, options.Publisher, options.Use3d);

			Console.WriteLine("Created Icon");

			PlaceMovie(options.MoflexPath, options.LongName);

			Console.WriteLine("Placed Movie and added Movie Name");

			ConfigureSettings(options.TitleId, options.LongName, options.UseFfRw);

			Console.WriteLine("Configured movie player settings");

			RebuildExeFs();

			Console.WriteLine("Rebuilt ExeFS");

			Console.WriteLine("If you see an error below \\/, ignore it");

			RebuildRomFs();

			Console.WriteLine("Rebuilt RomFS");

			RebuildNcch();

			Console.WriteLine("Built NCCH");

			BuildCia(options.OutputPath);

			Console.WriteLine("Built CIA and deleted working directory");
		}

		private static void BuildCia(string outputFileName)
		{
			Tools.RunCommand("makerom", $"-f cia -o \"{outputFileName}\" -i ./working_dir/output.cxi:0:1 -ignoresign", Environment.CurrentDirectory);

			if (File.Exists(outputFileName)) Directory.Delete("./working_dir", true);
		}

		private static void PlaceMovie(string moflexPath, string movieName)
		{
			File.Copy(new FileInfo(moflexPath).FullName, "./working_dir/romfs/movie/movie.moflex");

			using (StreamWriter sw = new StreamWriter(File.Create("./working_dir/romfs/movie/movie_title.csv"), Encoding.Unicode))
			{
				sw.NewLine = "\r\n";

				sw.WriteLine("#JP,#EN,#FR,#GE,#IT,#SP,#CH,#KO,#DU,#PO,#RU,#TW");

				for (int i = 0; i < 11; i++)
				{
					sw.Write($"{movieName},");
				}

				sw.WriteLine($"{movieName}");
			}
		}

		private static void RebuildNcch()
		{
			Tools.RunCommand("3dstool", "-ctf cxi output.cxi --header ncch.bin --extendedheader extheader.bin --logo logo.bin --plain plain.bin --exefs exefs.bin --romfs romfs.bin --not-encrypt", "./working_dir");

			File.Delete("./working_dir/exefs.bin");
			File.Delete("./working_dir/extheader.bin");
			File.Delete("./working_dir/logo.bin");
			File.Delete("./working_dir/ncch.bin");
			File.Delete("./working_dir/plain.bin");
			File.Delete("./working_dir/romfs.bin");
		}

		private static void RebuildExeFs()
		{
			Tools.RunCommand("3dstool", "-ctf exefs exefs.bin --header exefsheader.bin --exefs-dir exefs", "./working_dir");

			File.Delete("./working_dir/exefsheader.bin");
			Directory.Delete("./working_dir/exefs", true);
		}

		private static void RebuildRomFs()
		{
			Tools.RunCommand("3dstool", "-ctf romfs romfs.bin --romfs-dir romfs", "./working_dir");

			Directory.Delete("./working_dir/romfs", true);
		}

		private static void ConfigureSettings(string titleid, string movieName, bool useFfRw)
		{
			using (StreamReader sr = new StreamReader(File.OpenRead("./working_dir/romfs/settings/settingsTemp.csv"), Encoding.Unicode))
			{
				using (StreamWriter sw = new StreamWriter(File.Create("./working_dir/romfs/settings/settingsTL.csv"), Encoding.Unicode))
				{
					sw.NewLine = "\r\n";

					for (int i = 0; i < 39; i++)
						sw.WriteLine(sr.ReadLine());

					for (int i = 0; i < 12; i++)
					{
						sw.WriteLine(movieName);
						sr.ReadLine();
						sw.WriteLine(sr.ReadLine());
						sw.WriteLine(sr.ReadLine());
					}

					sr.ReadLine();
					sw.WriteLine(titleid.Substring(9, 5));
					sw.WriteLine(sr.ReadLine());

					for (int i = 0; i < 12; i++)
						sw.WriteLine(sr.ReadLine());

					sw.WriteLine(sr.ReadLine());
					sw.WriteLine(useFfRw.ToString().ToLower());
					sr.ReadLine();

					for (int i = 0; i < 3; i++)
						sw.WriteLine(sr.ReadLine());

					sw.WriteLine();
				}
			}

			File.Delete("./working_dir/romfs/settings/settingsTemp.csv");
		}

		private static void CreateIcon(string iconPath, string shortName, string longName, string publisher, bool allow3d = false)
		{
			string args = $"makesmdh -i \"{new FileInfo(iconPath).FullName}\" -s \"{shortName}\" -l \"{longName}\" -p \"{publisher}\" -o icon.icn -f visible,recordusage,nosavebackups";

			if (allow3d) args += ",allow3d";

			Tools.RunCommand("bannertool", args, "./working_dir/exefs");
		}

		private static void CreateBanner(string soundPath, string imagePath)
		{
			using (Image i = new Bitmap(256, 128))
			{
				using (Graphics g = Graphics.FromImage(i))
				{
					g.DrawImage(Image.FromFile(imagePath), 0, 0, 200, 120);
				}

				using (Bitmap b = new Bitmap(i))
				{
					CgfxAdapter adapter = new CgfxAdapter();

					adapter.Load("./working_dir/exefs/banner/banner0.bcmdl");

					adapter.Image.Bitmaps[0] = b;

					adapter.Save("./working_dir/exefs/banner/banner0.bcmdl");

					Console.WriteLine("Imported Banner Image into CGFX");

					if (IsCwav) //CWAV
					{
						Console.WriteLine("CWAV sound detected");
						File.Copy(soundPath, "./working_dir/exefs/banner/banner.bcwav", true);
						Console.WriteLine("Copied WAV to banner folder");
					}
					else
					{
						Console.WriteLine("WAV sound detected");
						Tools.RunCommand("bannertool", $"makecwav -i \"{new FileInfo(soundPath).FullName}\" -o banner.bcwav", "./working_dir/exefs/banner");
						Console.WriteLine("Converted WAV to CWAV and copied to banner folder");
					}

					Tools.RunCommand("3dstool", "-ctf banner banner.bnr --banner-dir banner", "./working_dir/exefs");

					if (File.Exists("./working_dir/exefs/banner.bnr"))
					{
						Console.WriteLine("Successfully created Banner");
					}

					Directory.Delete("./working_dir/exefs/banner", true);
				}
			}
		}

		private static void ConfigureNcch(Stream ncchHeader, string tid, string productCode)
		{
			using (ncchHeader)
			{
				ncchHeader.Seek(0x108, SeekOrigin.Begin);
				ncchHeader.Write(tid.ToBytes(true), 0, tid.Length / 2);
				
				ncchHeader.Seek(0x118, SeekOrigin.Begin);
				ncchHeader.Write(tid.ToBytes(true), 0, tid.Length / 2);

				ncchHeader.Seek(0x150, SeekOrigin.Begin);

				byte[] productCodeBytes = new byte[16];
				byte[] rawProductCodeBytes = Encoding.ASCII.GetBytes(productCode);
				
				Array.Clear(productCodeBytes, 0, 16);
				Buffer.BlockCopy(rawProductCodeBytes, 0, productCodeBytes, 0, rawProductCodeBytes.Length);

				ncchHeader.Write(productCodeBytes, 0, 16);
			}
		}

		private static void ConfigureExHeader(Stream exHeader, string exHeaderAppName, string tid)
		{
			using (exHeader)
			{
				byte[] tidBytes = tid.ToBytes(true);
				byte[] nameBytes = new byte[8];
				byte[] rawNameBytes = Encoding.ASCII.GetBytes(exHeaderAppName);

				Buffer.BlockCopy(rawNameBytes, 0, nameBytes, 0, rawNameBytes.Length);

				exHeader.Seek(0, SeekOrigin.Begin);
				exHeader.Write(nameBytes, 0, 8);

				exHeader.Seek(0x1C8, SeekOrigin.Begin);
				exHeader.Write(tidBytes, 0, 8);

				exHeader.Seek(0x200, SeekOrigin.Begin);
				exHeader.Write(tidBytes, 0, 8);
			}
		}
	}
}