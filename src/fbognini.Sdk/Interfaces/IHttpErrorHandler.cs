using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.Sdk.Interfaces
{
    public class FakeHttpErrorHandler : IHttpErrorHandler
    {
        public Task HandleResponse(HttpResponseMessage response)
        {
            return Task.CompletedTask;
        }
    }

    public interface IHttpErrorHandler
    {
        Task HandleResponse(HttpResponseMessage response);
    }
}
