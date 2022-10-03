using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;

namespace CTRMovieInjector
{
	public static class Tools
	{
		public static int RunCommand(string executableName, string arguments, string workingDirectory)
		{
			using (Process p = new Process())
			{
				p.StartInfo.FileName = executableName;
				p.StartInfo.Arguments = arguments;
				p.StartInfo.WorkingDirectory = workingDirectory;
				p.StartInfo.CreateNoWindow = true;
				p.StartInfo.UseShellExecute = false;

				p.Start();
				p.WaitForExit();

				return p.ExitCode;
			}
		}
	}

	public static class Extensions
	{
		public static string Hex(this IEnumerable<byte> b) =>
			b.Aggregate("", (current, t) => current + t.ToString("X2"));

		private static T[] Reverse<T>(this T[] input)
		{
			T[] output = (T[])input.Clone();

			Array.Reverse(output);

			return output;
		}

		public static byte[] ToBytes(this string hex, bool bigEndian = false)
		{
			int numberChars = hex.Length;
			byte[] bytes = new byte[numberChars / 2];

			for (int i = 0; i < numberChars; i += 2)
			{
				bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			}

			return bigEndian && BitConverter.IsLittleEndian ? bytes.Reverse() : bytes;
		}
	}
}