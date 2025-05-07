using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;

namespace mnestix_proxy.Tests.TestMockService
{
    public class DownstreamTestService
    {
        private readonly TestServer _server;
        public readonly HttpClient Client;
        public readonly string Url;

        public DownstreamTestService()
        {
            var builder = new WebHostBuilder()
            .Configure(app =>
            {
                app.Run(async context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api/test"))
                    {
                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync("Fake response");
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("Not found");
                    }
                });
            });

            _server = new TestServer(builder);
         
            Client = _server.CreateClient();
            Client.BaseAddress = new Uri("http://test:12345");
            Url = _server.BaseAddress.ToString();

            Console.WriteLine("Downstream service URL: " + Url);
            Console.WriteLine("Downstream service URL: " + _server.ToString());
        }

        public void Dispose()
        {
            Client.Dispose();
            _server.Dispose();
        }
    }
}
