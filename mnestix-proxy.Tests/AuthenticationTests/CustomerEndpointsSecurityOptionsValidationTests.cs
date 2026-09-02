using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using mnestix_proxy.Configuration;

namespace mnestix_proxy.Tests.AuthenticationTests
{
    [TestFixture]
    public class CustomerEndpointsSecurityOptionsValidationTests
    {
        private CustomerEndpointsSecurityOptionsValidation _cut = null!;

        [SetUp]
        public void Setup()
        {
            _cut = new CustomerEndpointsSecurityOptionsValidation(
                NullLogger<CustomerEndpointsSecurityOptionsValidation>.Instance);
        }

        [Test]
        public void Validate_EmptyApiKey_ReturnsSuccessWithoutThrowing()
        {
            // An empty ApiKey is unsafe, but the warning hook must keep the app running.
            var options = new CustomerEndpointsSecurityOptions { ApiKey = "" };

            var result = _cut.Validate(null, options);

            Assert.That(result, Is.EqualTo(ValidateOptionsResult.Success));
        }

        [Test]
        public void Validate_DefaultApiKey_ReturnsSuccessWithoutThrowing()
        {
            // The shipped/known default is unsafe, but the warning hook must keep the app running.
            var options = new CustomerEndpointsSecurityOptions { ApiKey = "verySecureApiKey" };

            var result = _cut.Validate(null, options);

            Assert.That(result, Is.EqualTo(ValidateOptionsResult.Success));
        }

        [Test]
        public void Validate_SecureApiKey_ReturnsSuccess()
        {
            var options = new CustomerEndpointsSecurityOptions { ApiKey = "a-long-random-secret" };

            var result = _cut.Validate(null, options);

            Assert.That(result, Is.EqualTo(ValidateOptionsResult.Success));
        }
    }
}