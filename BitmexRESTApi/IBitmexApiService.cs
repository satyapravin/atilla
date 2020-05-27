using System.Threading.Tasks;

namespace BitmexRESTApi
{
	public interface IBitmexApiService
	{
		Task<BitmexApiResult<TResult>> Execute<TParams, TResult>(ApiActionAttributes<TParams, TResult> apiAction, TParams @params);
	}
}
