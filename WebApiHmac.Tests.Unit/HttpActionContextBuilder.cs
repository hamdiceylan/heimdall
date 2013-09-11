using System;
using System.Web.Http.Controllers;

namespace WebApiHmac.Tests.Unit
{
    public class HttpActionContextBuilder
    {
        public static HttpActionContext Build()
        {
            var request = HttpRequestMessageBuilder
                .Instance()
                .Build();

            var httpControllerContext = new HttpControllerContext { Request = request };

            var context = new HttpActionContext { ControllerContext = httpControllerContext };
            context.Request.Headers.Date = DateTime.UtcNow;

            return context;
        }
    }
}