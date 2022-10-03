using System.Diagnostics.CodeAnalysis;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using CommandLine;
using System.IO;
using System;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace CTRMovieInjector
{
	[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
	public class InjectOptions
	{
		[Option('m', "moflex", HelpText = "The input MoFlex to inject into a CIA.", Required = true)]
		public string MoflexPath { get; set; }

		[Option('o', "output", HelpText = "The path to output the movie CIA to.", Required = true)]
		public string OutputPath { get; set; }

		[Option('t', "titleid", HelpText = "The Title ID to use for the NCCH container.", Required = true)]
		public string TitleId { get; set; }

		[Option('l', "long-name", HelpText = "The Long Name to use for the SMDH Icon Info. (This will also be displayed in the player app)", Required = true)]
		public string LongName { get; set; }

		[Option('s', "short-name", HelpText = "The Short Name to use for the SMDH Icon Info.", Required = true)]
		public string ShortName { get; set; }

		[Option('p', "publisher", HelpText = "The Publisher Name to use for the SMDH Icon Info.", Required = true)]
		public string Publisher { get; set; }

		[Option('b', "banner-audio", HelpText = "The banner audio file to use. (Has to be WAV, CWAV, or BCWAV.)", Required = true)]
		public string BannerAudioFilePath { get; set; }

		[Option('c', "product-code", HelpText = "The Product code to use for the NCCH Container.", Required = true)]
		public string ProductCode { get; set; }

		[Option('e', "exheader-appname", HelpText = "The ExHeader app name to use.", Required = true)]
		public string ExHeaderAppName { get; set; }

		[Option('g', "banner-image", HelpText = "The path to the banner image to use. Must be 200x120 (WxH) and in PNG format.", Required = true)]
		public string BannerImagePath { get; set; }

		[Option('i', "icon", HelpText = "The icon to use on the HOME Menu. Must be either 24x24 or 48x48 and in PNG format.", Required = true)]
		public string IconPath { get; set; }

		[Option('f', "use-ff-rw", HelpText = "Enable fast-forward and rewind buttons in the movie player application.", Required = false, Default = false)]
		public bool UseFfRw { get; set; } = false;

		[Option('3', "use-3d", HelpText = "Set the 3D flag in the icon so 3D can be used in case of 3D movies.", Required = false, Default = false)]
		public bool Use3d { get; set; } = false;

#nullable enable

		public static void VerifyBitmapProperties(Func<Bitmap> bitmapLoader, string imageType, ImageFormat format, int height, int width, int? height2 = null, int? width2 = null)
		{
			bool allowedSecondResolution = height2 != null && width2 != null;

			try
			{
				using (Bitmap bitmap = bitmapLoader())
				{
					if ((bitmap.Height != height || bitmap.Width != width) &&
						(allowedSecondResolution && (bitmap.Height != height2 || bitmap.Width != width2)))
					{
						throw allowedSecondResolution ?
							new ArgumentException($"Invalid {imageType} image resolution: expected {width}x{height} or {width2}x{height2} but got {bitmap.Width}x{bitmap.Height} instead (WxH)") :
							new ArgumentException($"Invalid {imageType} image resolution: expected {width}x{height} but got {bitmap.Width}x{bitmap.Height} instead (WxH)");
					}

					if (!Equals(bitmap.RawFormat, format))
					{
						throw new ArgumentException($"Invalid format for {imageType} image: expected {format} but got {bitmap.RawFormat} instead");
					}
				}
			}
			catch (ArgumentException)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new ArgumentException($"Could not load specified {imageType} image.", e);
			}
		}

		public static void VerifyStringValue(string input, string argumentName, int maximumLength, Action<string>? additionalVerifier = null)
		{
			if (string.IsNullOrWhiteSpace(input))
			{
				throw new ArgumentException($"Specified {argumentName} is empty");
			}

			if (input.Length > maximumLength)
			{
				throw new ArgumentException($"Expected maximum length for {argumentName} is {maximumLength}, but got {input.Length} instead");
			}

			additionalVerifier?.Invoke(input);
		}

		public static void VerifyFileHeader(string filePath, string fileType, long headerOffset, byte[] validheader, out bool isFirstHeader, byte[]? validHeader2 = null)
		{
			bool canUseSecondHeader = validHeader2 != null;

			byte[] firstHeader = new byte[validheader.Length];
			byte[]? secondHeader = validHeader2 != null ? new byte[validHeader2.Length] : null;

			using (FileStream fs = File.OpenRead(filePath))
			{
				if (fs.Seek(headerOffset, SeekOrigin.Begin) != headerOffset || fs.Read(firstHeader) != validheader.Length)
					throw new IOException($"Could not read {fileType} header at offset {headerOffset}");

				Console.WriteLine($"first header: {firstHeader.Hex()}");
				
				if (canUseSecondHeader)
					Console.WriteLine($"second header: {secondHeader.Hex()}");

				isFirstHeader = validheader.SequenceEqual(firstHeader);

				if (!isFirstHeader && canUseSecondHeader)
				{
					fs.Seek(headerOffset, SeekOrigin.Begin);
					fs.Read(secondHeader);
				}

				if (!isFirstHeader && (canUseSecondHeader && (!validHeader2!.SequenceEqual(secondHeader!))))
				{
					Console.WriteLine($"{validHeader2!.Hex()} == {secondHeader.Hex()} ? false??");
					throw canUseSecondHeader ?
						new ArgumentException($"Invalid {fileType} header: expected 0x{validheader.Hex()} or 0x{validHeader2.Hex()} but got 0x{firstHeader.Hex()}/0x{secondHeader.Hex()} instead") :
						new ArgumentException($"Invalid {fileType} header: expected 0x{validheader.Hex()} but got 0x{firstHeader.Hex()} instead");
				}
			}
		}

#nullable disable
	}
}