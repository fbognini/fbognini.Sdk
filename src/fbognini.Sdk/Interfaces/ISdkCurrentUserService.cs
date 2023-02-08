using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.Sdk.Interfaces
{
    public interface ISdkCurrentUserService
    {
        Task<bool> IsAuthenticated();
        Task<string> GetAccessToken();
        Task<string> ReloadAccessToken();
    }
}
