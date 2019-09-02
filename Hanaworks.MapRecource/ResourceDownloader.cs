using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Hanaworks.MapRecource
{
	public class ResourceDownloader
	{
		private bool isRunning = false;
		private static int _retryCount = 0;
		private ConcurrentQueue<string> _downloadQueue = new ConcurrentQueue<string>();

		public event EventHandler<DownloadResultEventArg> OnDownloadStart;
		public event EventHandler<DownloadResultEventArg> OnDownloadComplete;
		public event EventHandler OnDownloadFinished;

		public void EnqueueResource(string resourceUrl)
		{
			_downloadQueue.Enqueue(resourceUrl);

			if (!isRunning)
				Progress();
		}

		public void EnqueueResource(IEnumerable<string> resourceUrl)
		{
			foreach (var res in resourceUrl)
			{
				if (string.IsNullOrWhiteSpace(res))
					continue;

				EnqueueResource(res);
			}
		}

		private void Progress()
		{
			if (!isRunning)
				isRunning = true;

			string url = string.Empty;

			while (_downloadQueue.Count > 0 && _downloadQueue.TryDequeue(out url) && !string.IsNullOrWhiteSpace(url))
				DownloadWithUrl($"http://cstrike.surf.ga/{url}");

			isRunning = false;
			OnDownloadFinished?.Invoke(this, new EventArgs());
		}

		private async void DownloadWithUrl(string _url)
		{
			var _filePath = new Uri(_url).AbsolutePath;
			var _name = Path.GetFileName(_filePath);
			var _dir = $"./downloads/{Path.GetDirectoryName(_filePath)}";

			var _path = $"{_dir}/{_name}";

			if (!Directory.Exists(_dir))
				Directory.CreateDirectory(_dir);

			if (File.Exists(_path))
			{
				OnDownloadComplete?.Invoke(this, new DownloadResultEventArg
				{
					FileName = _name,
					Success = true,
					Url = _url,
					Contains = true
				});
			}
			else
			{
				try
				{
					OnDownloadStart?.Invoke(this, new DownloadResultEventArg
					{
						FileName = _name,
						Success = true,
						Url = _url
					});

					HttpWebRequest webRequest = WebRequest.CreateHttp(_url);
					webRequest.UserAgent = "Half-Life 2";
					webRequest.Referer = "hl2://31.186.251.23:27016";

					using (WebResponse _request = webRequest.GetResponse())
					using (Stream _stream = _request.GetResponseStream())
					using (FileStream _file = File.Create(_path))
						await _stream.CopyToAsync(_file);

					OnDownloadComplete?.Invoke(this, new DownloadResultEventArg
					{
						FileName = _name,
						Success = true,
						Url = _url
					});
				}
				catch (Exception ex)
				{
					//WebException _ex = ex as WebException;

					if (_retryCount++ < 3)
					{
						File.Delete(_path);
						DownloadWithUrl(_url);
					}
					else
					{
						_retryCount = 0;

						OnDownloadComplete?.Invoke(this, new DownloadResultEventArg
						{
							FileName = _name,
							Success = false,
							Url = _url,
							Exception = ex
						});
					}
				}
			}
		}
	}
}
