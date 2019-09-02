using System;

namespace Hanaworks.MapRecource
{
	public class DownloadResultEventArg
	{
		public bool Success { get; set; }
		public string Url { get; set; }
		public string FileName { get; set; }
		public Exception Exception { get; set; }
		public bool Contains { get; set; }
	}

}