using Newtonsoft.Json;

namespace BitmexCore.Models
{
	public abstract class QueryJsonParams : IJsonQueryParams
	{
		public string ToJson()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}
