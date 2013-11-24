using System;

namespace Bmbsqd.ElasticIdentity
{
	public class ElasticUserStoreTraceEventArgs : EventArgs
	{
		private readonly string _operation;
		private readonly string _url;
		private readonly string _request;
		private readonly string _response;

		public ElasticUserStoreTraceEventArgs( string operation, string url, string request, string response )
		{
			_operation = operation;
			_url = url;
			_request = request;
			_response = response;
		}

		public string Operation
		{
			get { return _operation; }
		}

		public string Url
		{
			get { return _url; }
		}

		public string Request
		{
			get { return _request; }
		}

		public string Response
		{
			get { return _response; }
		}
	}
}