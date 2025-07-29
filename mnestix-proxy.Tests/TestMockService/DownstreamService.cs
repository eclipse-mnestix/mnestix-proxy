using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;

namespace mnestix_proxy.Tests.TestMockService
{
    public class DownstreamService
    {
        private IHost? _host;
        public HttpClient? Client;
        public string? Url;
        public DownstreamService()
        {
            StartServer();
        }

        private void StartServer()
        {
            if (_host != null) return;
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

                                      if (path != null && path.StartsWith("/api/test-endpoint"))
                                      {
                                          context.Response.StatusCode = 200;
                                          await context.Response.WriteAsync("Mnestix Api called!");
                                      }
                                      else if (path != null && path.StartsWith("/shells"))
                                      {
                                          context.Response.StatusCode = 200;
                                          await context.Response.WriteAsync("AAS Repository Service called!");
                                      }
                                      else if (path != null && path.StartsWith("/submodels"))
                                      {
                                          context.Response.StatusCode = 200;
                                          await context.Response.WriteAsync("Submodel Repository Service called!");
                                      }
                                      else if (path != null && path.StartsWith("/discovery-test-endpoint"))
                                      {
                                          context.Response.StatusCode = 200;
                                          await context.Response.WriteAsync("Discovery Service called!");
                                      }
                                      else
                                      {
                                          context.Response.StatusCode = 404;
                                          await context.Response.WriteAsync($"Not found");
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

            if (address == null) return;
            Url = address.TrimEnd('/');
            Client = new HttpClient { BaseAddress = new Uri(Url) };
        }

        public void Dispose()
        {
            Client?.Dispose();
            _host?.Dispose();
        }
    }
}
