namespace Bmbsqd.ElasticIdentity
{
	internal static class UserNameUtils
	{
		public static string FormatUserName( string userName )
		{
			// You may wonder why this is? Yeah, only because "term" filters in ES are case sensitive. It's faster!
			return userName == null ? null : userName.ToLowerInvariant();
		}
	}
}