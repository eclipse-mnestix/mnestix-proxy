using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace mnestix_proxy.Tests.TestMockService
{
    /// <summary>
    /// A lightweight HTTP mock server for the AAS Registry that records every request it receives.
    /// Tests can inspect ReceivedRequests to verify the middleware made the expected side-calls.
    /// </summary>
    public class RegistryMockService : IDisposable
    {
        private IHost? _host;
        public string? Url;

        private readonly List<ReceivedRequest> _receivedRequests = [];
        private readonly object _lock = new();

        public IReadOnlyList<ReceivedRequest> ReceivedRequests
        {
            get { lock (_lock) { return [.. _receivedRequests]; } }
        }

        public void Clear()
        {
            lock (_lock) { _receivedRequests.Clear(); }
        }

        public RegistryMockService()
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
                                      var method = context.Request.Method;
                                      var path = context.Request.Path.Value ?? string.Empty;

                                      var body = string.Empty;
                                      if (context.Request.ContentLength > 0)
                                      {
                                          using var reader = new StreamReader(context.Request.Body);
                                          body = await reader.ReadToEndAsync();
                                      }

                                      lock (_lock)
                                      {
                                          _receivedRequests.Add(new ReceivedRequest(method, path, body));
                                      }

                                      if (path.StartsWith("/shell-descriptors", StringComparison.OrdinalIgnoreCase))
                                      {
                                          context.Response.StatusCode = method switch
                                          {
                                              "POST" => StatusCodes.Status201Created,
                                              "DELETE" => StatusCodes.Status204NoContent,
                                              _ => StatusCodes.Status200OK
                                          };
                                      }
                                      else
                                      {
                                          context.Response.StatusCode = StatusCodes.Status404NotFound;
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
        }

        public void Dispose()
        {
            _host?.Dispose();
        }
    }

    public record ReceivedRequest(string Method, string Path, string? Body);
}
