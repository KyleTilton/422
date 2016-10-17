using System;
using System.Collections.Concurrent;
using System.Text;
using System.IO;
using System.Net.Sockets;

namespace CS422
{
	public class WebRequest
	{
		public NetworkStream _network_stream;
		public Stream _body;

		public ConcurrentDictionary<string , string> Headers;

		public string Method;
		public string RequestTarget;
		public string HTTPVersion;


		public WebRequest (NetworkStream stream)
		{
			_network_stream = stream;
		}

//		public long GetContentLenghtOrDefault(long defaultValue) {
//			
//		}
//
//		public Tuple<long, long> GetRangeHeader() {
//			
//		}

		public void WriteNotFoundResponse(string pageHTML){
			
		}

		public bool WriteHTMLResponse(string htmlString){
			
			return false;
		}

	}
}

