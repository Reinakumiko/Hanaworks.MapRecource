using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hanaworks.MapRecource
{
	class Program
	{
		[DllImport("kernel32.dll")]
		private static extern bool SetConsoleCtrlHandler(Delegate HandlerRoutine, bool Add);

		static void Main(string[] args)
		{
			if (args.Length == 0 || args.Length > 0 && args[0] == "/?")
			{
				Console.WriteLine("Usage:  res <filename|dir> [-l] [-w]");
				Console.WriteLine("	-l	make list");
				Console.WriteLine("	-c	without comments");
				Console.WriteLine("	-w	download to disk");
				Console.WriteLine("	-x	make failed list");
				return;
			}

			var _ref = new List<string>();

			//解压存在内部的resgen.exe
			var executor = $"resgen_{args.GetHashCode()}.exe";
			File.WriteAllBytes(executor, Resource.RESGen);

			Action _rm = () =>
			{
				if (File.Exists(executor))
					File.Delete(executor);
			};
			
			SetConsoleCtrlHandler(_rm, true);

			try
			{
				foreach (var path in args)
				{
					//带有参数的命令行将跳过
					if (path.StartsWith("-"))
						continue;

					var hasExtension = Path.HasExtension(path);

					IEnumerable<FileInfo> files = null;

					if (!hasExtension)
					{
						files = new DirectoryInfo(path).GetFiles("*.bsp");
					}
					else
					{
						files = new[] { new FileInfo(path) };
					}

					foreach (var file in files)
					{
						Console.WriteLine($"Start Process: {file}");
						var p = Process.Start(executor, $"-k {file.FullName}");
						p.WaitForExit();

						Console.WriteLine($"Complete Process: {file}");

						_ref.Add(file.FullName);
					}
				}

				if (args.Any(a => a.Contains("-")))
				{
					var lines = _ref.Where(p => File.Exists(p.Replace(Path.GetExtension(p), ".res")))
								.SelectMany(p => File.ReadAllLines(p.Replace(Path.GetExtension(p), ".res")))
								.Distinct()
								.ToList();

					if (args.Contains("-c"))
						lines = lines.Where(l => !l.StartsWith("//")).ToList();

					if (args.Contains("-l"))
						File.WriteAllLines("manifest.txt", lines);


					if (args.Contains("-w"))
					{
						var downloader = new ResourceDownloader();

						downloader.OnDownloadStart += (s, e) =>
						{
							Console.WriteLine($"[{DateTime.Now:hh:MM:ss}] Start to download {e.FileName}");
						};

						downloader.OnDownloadComplete += (s, e) =>
						{
							Console.Write($"[{DateTime.Now:hh:MM:ss}] File {e.FileName} has download ");

							Console.ForegroundColor = e.Success ? ConsoleColor.Green : ConsoleColor.Red;
							Console.WriteLine(e.Success ? "SUCCESS" : "FAILED");
							Console.ForegroundColor = ConsoleColor.White;

							if (!e.Success && args.Contains("-x"))
								File.AppendAllText("failure.txt", $"{e.Url}\n");
						};

						//downloader.OnDownloadFinished += (s, e) =>
						//{
						//Console.Write($"[{DateTime.Now:hh:MM:ss}] All download was finished");
						//Environment.Exit(0);
						//};

						downloader.EnqueueResource(lines);
						Console.ReadLine();
					}
				}
			}
			finally
			{
				_rm.Invoke();
			}
		}
	}
}
