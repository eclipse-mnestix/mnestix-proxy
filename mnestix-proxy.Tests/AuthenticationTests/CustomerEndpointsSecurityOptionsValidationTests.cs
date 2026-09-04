using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using mnestix_proxy.Configuration;
using Moq;

namespace mnestix_proxy.Tests.AuthenticationTests
{
    [TestFixture]
    public class CustomerEndpointsSecurityOptionsValidationTests
    {
        private Mock<ILogger<CustomerEndpointsSecurityOptionsValidation>> _loggerMock = null!;
        private CustomerEndpointsSecurityOptionsValidation _cut = null!;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<CustomerEndpointsSecurityOptionsValidation>>();
            _cut = new CustomerEndpointsSecurityOptionsValidation(_loggerMock.Object);
        }

        [Test]
        public void Validate_EmptyApiKey_LogsCriticalAndReturnsSuccessWithoutThrowing()
        {
            // An empty ApiKey is unsafe, but the warning hook must keep the app running.
            var options = new CustomerEndpointsSecurityOptions { ApiKey = "" };

            var result = _cut.Validate(null, options);

            Assert.That(result, Is.EqualTo(ValidateOptionsResult.Success));
            VerifyCriticalLogged(Times.Once());
        }

        [Test]
        public void Validate_DefaultApiKey_LogsCriticalAndReturnsSuccessWithoutThrowing()
        {
            // The shipped/known default is unsafe, but the warning hook must keep the app running.
            var options = new CustomerEndpointsSecurityOptions { ApiKey = "verySecureApiKey" };

            var result = _cut.Validate(null, options);

            Assert.That(result, Is.EqualTo(ValidateOptionsResult.Success));
            VerifyCriticalLogged(Times.Once());
        }

        [Test]
        public void Validate_PreviouslyShippedDefaultApiKey_LogsCriticalAndReturnsSuccessWithoutThrowing()
        {
            // A previously shipped default is also publicly known and unsafe.
            var options = new CustomerEndpointsSecurityOptions { ApiKey = "9FB8BCDFAEE81367A1668E16BDC37" };

            var result = _cut.Validate(null, options);

            Assert.That(result, Is.EqualTo(ValidateOptionsResult.Success));
            VerifyCriticalLogged(Times.Once());
        }

        [Test]
        public void Validate_SecureApiKey_ReturnsSuccessWithoutLogging()
        {
            var options = new CustomerEndpointsSecurityOptions { ApiKey = "a-long-random-secret" };

            var result = _cut.Validate(null, options);

            Assert.That(result, Is.EqualTo(ValidateOptionsResult.Success));
            VerifyCriticalLogged(Times.Never());
        }

        private void VerifyCriticalLogged(Times times)
        {
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Critical,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                times);
        }
    }
}
