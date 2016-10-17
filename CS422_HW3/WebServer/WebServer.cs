using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

// maybe useless
using System.Net;

namespace CS422
{
	public class WebServer
	{
		// globals for time and size requirements
		private const int MAX_BYTES_FIRST_LINE = 2048;
		private const int MAX_BYTES_BODY = 100 * 1024;
		private const int MAX_TIMEOUT_SINGLE = 2;
		private const int MAX_TIMEOUT_TOTAL = 10;

		private static Thread _listener_thread;
		private static TcpListener _listener;
		private static BlockingCollection<TcpClient> _clients;
		private static Thread[] _thread_workers;
		static ConcurrentDictionary<string, WebService> _web_services;
		static bool _isServerRunning;

		// REGEXs
		private const string REQ_REGEX = @"^GET \S* HTTP/1.1$";
		private const string COLON_REGEX = @"^\S+:\s*.*$";
		private const string HOST_REGEX = @"^Host::\s*$";
		private static char[] _separator = new char[] { '\r', '\n' };

		public WebServer ()
		{
			
		}

		public static bool Start (int port, int numThreads) {

			// if number is less than or equal to zero set to 64
			if (numThreads <= 0) {
				numThreads = 64;
			}
			try {
				// initialize blocking collection
				_clients = new BlockingCollection<TcpClient> ();

				//initialize listener
				_listener = new TcpListener (IPAddress.Any, port);

				// initialize threads
				_thread_workers = new Thread[numThreads];
				for (int i = 0; i < numThreads; i++) {
					_thread_workers [i] = new Thread (ThreadWork);
					_thread_workers [i].Start ();
				}
				
				// start listener thread
				_listener_thread = new Thread (ListenerStart);
				_listener_thread.Start ();
				return true;
			} catch {
				return false;
			}

		}

		public static void Stop ()
		{
		}

		// function that starts the web server
		public static void ListenerStart ()
		{
			Console.WriteLine ("Listener thread started");
			while (_isServerRunning) {
				try {
					// start listening
					_listener.Start ();
					Console.Write ("Waiting for a connection... ");

					// Perform a blocking call to accept requests.
					// You could also user server.AcceptSocket() here.
					TcpClient client = _listener.AcceptTcpClient ();
					Console.WriteLine ("Connected!");

					// once client is accepted add it to a thread
					_clients.TryAdd (client);
				} catch (Exception ex) {
					Console.WriteLine ("Listener Start Failed.");
				}
			}


		}

		private static WebRequest BuildRequest (TcpClient client)
		{
			Console.WriteLine ("Building Request from client stream.");
			//initialize
			WebRequest request = new WebRequest (client.GetStream ());
			NetworkStream stream = client.GetStream ();

			int numBytesRead = 0;
			int totalBytesRead = 0;
			//byte[] bytes = new byte[1024];

			string requestString = "";

			// bools for logic of checks
			bool isDone = false;
			bool requestChecked = false;
			bool hostFound = false;
			bool bodyFound = false;
			int lineToCheck = 1; // this starts at 1 because parsedStringRequest[0] is always the request line


			// set read timeout for single read
			stream.ReadTimeout = MAX_TIMEOUT_SINGLE;

			// intialize stopwatch for max timeout
			Stopwatch stopwatch = Stopwatch.StartNew ();

			while (!isDone) {
				byte[] bytes = new byte[1024];
				numBytesRead = stream.Read (bytes, 0, bytes.Length);
				totalBytesRead += numBytesRead;
				requestString += Encoding.ASCII.GetString (bytes, 0, numBytesRead);
				string[] parsedRequestString = requestString.Split (_separator, StringSplitOptions.None);

				// check for valid Request line
				if (!requestChecked) {
					if (parsedRequestString.Length > 1) {
						bool valid = CheckRequestLine (parsedRequestString [0]);
						if (valid) {
							request.Method = parsedRequestString [0].Split (' ') [0];
							request.RequestTarget = parsedRequestString [0].Split (' ') [1];
							request.HTTPVersion = parsedRequestString [0].Split (' ') [2]; //TODO: fix
							requestChecked = true;
						} else { 
							Console.WriteLine ("Request line not recieved before timeout! Closing connection.");
							client.Close ();
							return null;
						}
					} else {
						if (totalBytesRead > MAX_BYTES_FIRST_LINE) {
							Console.WriteLine ("Max size of request line exceeded! Closing connection."); 
							client.Close ();
							return null;
						}
					}
				} else {
					// check headers to see if they are correct format
					// request line is valid
					if (lineToCheck < (parsedRequestString.Length - 1)) { // there is at least one new line to check
						int i = lineToCheck;
						while ((i < parsedRequestString.Length - 1) && !bodyFound) { // check each new line read
							// check for body
							if (parsedRequestString [i] == String.Empty) {
								if (!hostFound) {
									Console.WriteLine ("No host header found before body. Closing connection."); 
									client.Close ();
									return null;
								}
								bodyFound = true;
							} else {
								bool validHeader = ProcessHeader (parsedRequestString [i], ref hostFound, request);
								if (!validHeader) {
									Console.WriteLine ("Invalid header encountered. Closing connection."); 
									client.Close ();
									return null;
								}
							}
							i++;
						}
					}

				}
				// check for double line break 
				if (!requestString.Contains (@"\r\n\r\n")) {
					if ((totalBytesRead > MAX_BYTES_BODY) || ((stopwatch.ElapsedMilliseconds / 1000) > MAX_TIMEOUT_TOTAL)) {
						Console.WriteLine ("Max size or time limit reached before body was found! Closing connection.");
						client.Close();
						return null;
					}
				} else { 
					// body found. quit reading. 
					isDone = true;
				}

			}
			//TODO: find how much of the body was read by core.
			//TODO: build the actual requst
			request._network_stream = stream;
			request._body = stream; //TODO: fix

			return request;
		}

		private static void ThreadWork ()
		{
			while (true) {
				try {
					TcpClient client;
					_clients.TryTake (out client, Timeout.Infinite);
					WebRequest request = BuildRequest (client);

					if (!Object.ReferenceEquals (null, request)) {
						// Find Service 
						WebService service;
						_web_services.TryGetValue(request.RequestTarget, out service); 
						// tell service to handle the request
						if (service != null){
							service.Handler(request);
						} else {
							Console.WriteLine ("WebService not found. Closing connection.");
						}

					} else {
						Console.WriteLine ("Build Request returned null. Closing connection.");
					}

					//TODO: close the client connection and dispose the networkStream and TcpClient
				} catch {
				}
			}
		}

		public static void AddService (WebService service)
		{
			string uri = service.ServiceURI;
			_web_services.TryAdd (uri, service);
		}

		// <<<  Helper Functions >>>
		private static bool CheckRequestLine (string line)
		{
			Regex expression = new Regex (REQ_REGEX);
			if (expression.IsMatch (line)) {
				return true;
			} else {
				return false;
			}
		}

		private static bool ProcessHeader (string s, ref bool hostFound, WebRequest request)
		{
			Regex cr = new Regex (COLON_REGEX);
			if (!cr.IsMatch (s)) {
				
				return false;
			}
			Regex hr = new Regex (HOST_REGEX);
			if (hr.IsMatch (s)) {
				hostFound = true;
			}
			// checks passed add header to dictionary
			string key = s.Split (':') [0].ToLower ();
			string value = s.Split (':') [1];
			request.Headers.TryAdd (key, value);
			return true;
		}
	}
}

