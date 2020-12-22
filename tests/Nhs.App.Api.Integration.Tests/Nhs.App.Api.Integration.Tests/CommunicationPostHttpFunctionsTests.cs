using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Nhs.App.Api.Integration.Tests
{
    [TestClass]
    public class CommunicationPostHttpFunctionsTests: CommunicationHttpFunctionBase
    {
        private static string _sendToNhsNumbers;
        private static string _sendToOdsCode;

        private const string ValidAppMessageJson =
            " \"appMessage\": { \"sender\":\"Hello Team E\", \"body\":\"This is a message body\" }";

        private const string ValidPushNotificationJson =
            " \"pushNotification\": { \"title\":\"Title\", \"body\":\"This is a notification body\", \"url\":\"https://www.nhs.uk\" }";

        [ClassInitialize]
        public static void ClassInitialise(TestContext context)
        {
            TestClassSetup(context);

            var sendToNhsNumbers= context!.Properties["SendToNhsNumbers"]?.ToString()
                .Split(',')
                .Select( x => $"\"{x.Trim()}\"")
                .ToArray();

            _sendToNhsNumbers = $"[{string.Join(',', sendToNhsNumbers ?? Array.Empty<string>())}]";

            var sendToOdsCode = context!.Properties["SendToOdsCode"]?.ToString();

            _sendToOdsCode = sendToOdsCode == null ? "null" : $"\"{sendToOdsCode}\"";
        }

        [TestMethod]
        public async Task CommunicationPost_ValidAppMessageByNhsNumbers_ReturnsCreatedStatusCode()
        {
            // Arrange
            var validPayload = BuildValidCommunicationPostBody(_sendToNhsNumbers, "null", ValidAppMessageJson);

            await CommunicationPost_ValidTest(validPayload);
        }

        [TestMethod]
        public async Task CommunicationPost_ValidAppMessageByOdsCode_ReturnsCreatedStatusCode()
        {
            // Arrange
            var validPayload = BuildValidCommunicationPostBody("null", _sendToOdsCode, ValidAppMessageJson);

            await CommunicationPost_ValidTest(validPayload);
        }

        [TestMethod]
        public async Task CommunicationPost_ValidPushNotificationByNhsNumbers_ReturnsCreatedStatusCode()
        {
            // Arrange
            var validPayload = BuildValidCommunicationPostBody(_sendToNhsNumbers, "null", ValidPushNotificationJson);

            await CommunicationPost_ValidTest(validPayload);
        }

        [TestMethod]
        public async Task CommunicationPost_ValidPushNotificationByOdsCode_ReturnsCreatedStatusCode()
        {
            // Arrange
            var validPayload = BuildValidCommunicationPostBody("null", _sendToOdsCode, ValidPushNotificationJson);

            await CommunicationPost_ValidTest(validPayload);
        }

        [DataTestMethod]
        [DataRow("{ \"channels\": { " + ValidAppMessageJson + " } }", DisplayName = "No recipients")]
        [DataRow("{ \"recipients\": { \"nhsNumbers\": [\"9487416153\"], \"odsCode\": null } }", DisplayName = "No channels")]
        [DataRow("{ \"recipients\": { \"nhsNumbers\": [\"9487416153\"], \"odsCode\": \"A12355\" }, \"channels\": { " + ValidAppMessageJson +" } }", DisplayName = "Both recipients")]
        [DataRow("{ \"recipients\": { \"nhsNumbers\": [\"9487416153\"], \"odsCode\": \"A12355\" }, \"channels\": {  \"appMessage\": { \"sender\":\"Hello Team E\", \"body\":\"Body with <b>html</b> tags\" } } }", DisplayName = "Unsafe content")]
        public async Task CommunicationPost_WithInvalidPayload_Returns400BadRequest(string invalidJson)
        {
            // Arrange
            using var httpClient = CreateHttpClient();

            var httpContent = new StringContent(invalidJson, Encoding.UTF8, "application/json");

            // Act
            var response = await httpClient.PostAsync("communication", httpContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task CommunicationPost_InvalidApiKey_Returns401Unauthorized()
        {
            // Arrange
            using var httpClient = CreateHttpClient();
            httpClient.DefaultRequestHeaders.Remove("x-api-key");
            httpClient.DefaultRequestHeaders.Add("x-api-key", "invalid-key");

            var stringPayload = BuildValidCommunicationPostBody(_sendToNhsNumbers, "null", ValidAppMessageJson);
            var httpContent = new StringContent(stringPayload, Encoding.UTF8, "application/json");

            // Act
            var response = await httpClient.PostAsync("communication", httpContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private static async Task CommunicationPost_ValidTest(string validPayload)
        {
            using var httpClient = CreateHttpClient();

            var httpContent = new StringContent(validPayload, Encoding.UTF8, "application/json");

            // Act
            var response = await httpClient.PostAsync("communication", httpContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var responseObject = await DeserializeResponseAsync<CommunicationPostResponse>(response);
            Guid.TryParse(responseObject.Id, out _).Should().BeTrue();
        }
    }
}
