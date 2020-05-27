using BitmexCore.Models;

namespace BitmexCore
{
	public interface IBitmexAuthorization
	{
		BitmexEnvironment BitmexEnvironment { get; set; }
		string Key { get; set; }
		string Secret { get; set; }
	}
}
