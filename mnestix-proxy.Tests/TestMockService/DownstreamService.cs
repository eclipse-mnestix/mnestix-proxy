using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;

namespace mnestix_proxy.Tests.TestMockService
{
    public class DownstreamService
    {
        private readonly IHost _host;
        public readonly HttpClient Client;
        public readonly string Url;

        public DownstreamService()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel()
                              .UseUrls("http://127.0.0.1:0")
                              .Configure(app =>
                              {
                                  app.Run(async context =>
                                  {
                                      var path = context.Request.Path.Value?.ToLowerInvariant();

                                      if (path.StartsWith("/repo"))
                                      {
                                          context.Response.StatusCode = 200;
                                          await context.Response.WriteAsync("Repository Service called!");
                                      }
                                      else if (path == "/discovery")
                                      {
                                          context.Response.StatusCode = 200;
                                          await context.Response.WriteAsync("Discovery Service called!");
                                      }
                                      else
                                      {
                                          context.Response.StatusCode = 404;
                                          await context.Response.WriteAsync("Not found");
                                      }
                                  });
                              });
                })
                .Start();

            var address = _host.Services?
                .GetRequiredService<IServer>()?
                .Features?
                .Get<IServerAddressesFeature>()?
                .Addresses
                .First();

            Url = address.TrimEnd('/');
            Client = new HttpClient { BaseAddress = new Uri(Url) };
        }

        public void Dispose()
        {
            Client.Dispose();
            _host.Dispose();
        }
    }
}
