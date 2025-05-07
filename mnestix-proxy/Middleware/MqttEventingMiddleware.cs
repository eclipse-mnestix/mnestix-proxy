using MQTTnet;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using static mnestix_proxy.Services.Shared.StringPatternMatchingHelper;

namespace mnestix_proxy.Middleware
{
    /// <summary>
    /// This class is responsible for sending MQTT events for relevant api requests 
    /// </summary>
    public class MqttEventingMiddleware
    {
        private readonly bool _isEventingEnabled;
        private readonly string _serverAddress;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly IEnumerable<IConfigurationSection> _eventingPaths;
        private readonly int _qualityOfService;
        private readonly ILogger<MqttEventingMiddleware> _logger;

        /// <summary>
        /// Constructor for the MQTT eventing middleware
        /// </summary>
        /// <param name="eventingConfiguration">The configuration for the mqtt eventing</param>
        /// <param name="logger">The logger for MQTT eventing</param>
        public MqttEventingMiddleware(IConfiguration eventingConfiguration, ILogger<MqttEventingMiddleware> logger)
        {
            _isEventingEnabled = eventingConfiguration.GetValue<bool>("mqtt_events_enabled");
            _serverAddress = eventingConfiguration.GetValue<string>("mqtt_server");
            _port = eventingConfiguration.GetValue<int>("mqtt_port");
            _username = eventingConfiguration.GetValue<string>("mqtt_user");
            _password = eventingConfiguration.GetValue<string>("mqtt_pass");
            _eventingPaths = eventingConfiguration.GetSection("mqtt_trigger_paths").GetChildren();
            _qualityOfService = eventingConfiguration.GetValue<int>("mqtt_qos");
            _logger = logger;

            if (_isEventingEnabled && string.IsNullOrEmpty(_serverAddress))
            {
                _logger.LogError("MQTT eventing is enabled but an incomplete eventing configuration was provided. " +
                                 "Consider disabling MQTT eventing or provide a valid config.");
            }
        }

        /// <summary>
        /// Configures the handling of MQTT events
        /// </summary>
        /// <returns>Returns a middleware delegate that handles the MQTT eventing</returns>
        internal Func<HttpContext, Func<Task>, Task> ConfigureMqttEventingHandling()
        {
            var mqttFactory = new MqttClientFactory();
            var mqttClient = mqttFactory.CreateMqttClient();
            var clientId = Guid.NewGuid().ToString();
            var clientOptionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithTcpServer(_serverAddress, _port)
                .WithTlsOptions(tlsOptionsBuilder => tlsOptionsBuilder
                    .UseTls()
                    .Build());

            if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
            {
                clientOptionsBuilder.WithCredentials(_username, _password);
            }

            var clientOptions = clientOptionsBuilder.Build();


            return async (context, next) =>
            {
                if (!_isEventingEnabled || context.Request.Method == "GET")
                {
                    await next();
                    return;
                }

                var requestPath = context.Request.Path.ToString();
                var topicMatched = _eventingPaths.Any(eventingPath => CheckForPatternMatch(requestPath, eventingPath.Value));

                if (topicMatched)
                {
                    if (!mqttClient.IsConnected)
                    {
                        _logger.LogInformation("Connecting to MQTT broker...");
                        await mqttClient.ConnectAsync(clientOptions);
                    }

                    context.Request.EnableBuffering();

                    var payload = new
                    {
                        RequestMethod = context.Request.Method,
                        RequestUrl = GetFullUrl(context)
                    };
                    var jsonPayload = JsonConvert.SerializeObject(payload);
                    var applicationMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(requestPath)
                        .WithPayload(jsonPayload)
                        .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)_qualityOfService)
                        .Build();

                    await mqttClient.PublishAsync(applicationMessage);
                }

                await next();
            };
        }

        private static string GetFullUrl(HttpContext context)
        {
            var request = context.Request;
            var fullUrl = $"{request.Scheme}://{request.Host}{request.Path}";
            return fullUrl;
        }
    }
}
