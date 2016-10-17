using System;

namespace CS422
{
	public class DemoService : WebService
	{

		private const string c_template =
			"<html>This is the response to the request:<br>" +
			"Method: {0}<br>Request-Target/URI: {1}<br>" +
			"Request body size, in bytes: {2}<br><br>" +
			"Student ID: {3}</html>";

		public override string ServiceURI {
			get {
				return "/";
			}
		}

		public override void Handler (WebRequest request)
		{
			string body;
			try {
				body = request._body.Length.ToString ();
			} catch {
				body = "BodyLengthUnknown";
			}
			string uri = request.RequestTarget;
			string id = "11259460";
			string method = request.Method;
			string response = String.Format (c_template, method, uri, body, id);
			request.WriteHTMLResponse (response);

		}

	}
}

