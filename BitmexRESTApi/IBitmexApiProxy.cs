using BitmexCore.Models;
using System.Threading.Tasks;

namespace BitmexRESTApi
{
    public interface IBitmexApiProxy
    {
        Task<BitmexApiResult<string>> Get(string action, IQueryStringParams parameters);
        Task<BitmexApiResult<string>> Post(string action, IJsonQueryParams parameters);
        Task<BitmexApiResult<string>> Put(string action, IJsonQueryParams parameters);
        Task<BitmexApiResult<string>> Delete(string action, IQueryStringParams parameters);
    }
}
