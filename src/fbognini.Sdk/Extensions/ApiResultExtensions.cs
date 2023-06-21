using fbognini.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.Sdk.Extensions
{
    public static class ApiResultExtensions
    {
        public static IList<T> ListOrEmpty<T>(ApiResult<IList<T>> response)
        {
            return response.ToListOrEmpty();
        }

        public static IList<T> ToListOrEmpty<T>(this ApiResult<IList<T>> response)
        {
            if (response.IsSuccess)
            {
                return response.Response;
            }

            return Enumerable.Empty<T>().ToList();
        }
    }
}
