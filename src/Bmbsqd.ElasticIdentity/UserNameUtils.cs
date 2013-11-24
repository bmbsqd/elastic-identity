namespace Bmbsqd.ElasticIdentity
{
	internal static class UserNameUtils
	{
		public static string FormatUserName( string userName )
		{
			return userName == null ? null : userName.ToLowerInvariant();
		}
	}
}