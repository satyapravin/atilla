using System.Collections.Generic;

namespace BitmexCore.Models
{
	public static class Environments
	{
		public static readonly IDictionary<BitmexEnvironment, string> Values = new Dictionary<BitmexEnvironment, string>
		{
			{BitmexEnvironment.Test, "testnet.bitmex.com"},
			{BitmexEnvironment.Prod, "www.bitmex.com"}
		};

	}
}
