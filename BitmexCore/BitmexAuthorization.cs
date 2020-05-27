using BitmexCore.Models;

namespace BitmexCore
{
	public class BitmexAuthorization : IBitmexAuthorization
	{
		public BitmexEnvironment BitmexEnvironment { get; set; }
		public string Key { get; set; }
		public string Secret { get; set; }
	}
}
