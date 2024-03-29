﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Web.Http;
using System.Web.Http.SelfHost;
using Moq;
using NUnit.Framework;

namespace Heimdall.Client.Tests
{
    public class SigningHttpClientIntegrationTests
    {
        private HttpSelfHostServer server;
        private HttpClient client;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var config = new HttpSelfHostConfiguration("http://localhost:8080");

            config.Routes.MapHttpRoute(
                "API Default", "api/{controller}/{id}",
                new { id = RouteParameter.Optional });

            server = new HttpSelfHostServer(config);
            server.OpenAsync().Wait();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            server.CloseAsync();
        }

        [SetUp]
        public void SetUp()
        {
            client = HeimdallClientFactory.Create("anyusername", "secret");
            client.BaseAddress = new Uri("http://localhost:8080");
        }

        [Test]
        public void sets_username_header()
        {
            var content = new StringContent("value=xyz");

            var result = client.PostAsync("api/Test", content, new JsonMediaTypeFormatter())
                .Result;

            var request = result.RequestMessage;

            var header = request.Headers.GetValues(HeaderNames.UsernameHeader).FirstOrDefault();
            Assert.That(header, Is.EqualTo("anyusername"));
        }

        [Test]
        public void sets_content_md5_header()
        {
            var field1 = new KeyValuePair<string, string>("firstName", "Alex");
            var field2 = new KeyValuePair<string, string>("lastName", "Brown");

            var content = new FormUrlEncodedContent(new[] { field1, field2 });

            var response = client.PostAsync("api/Test", content)
                .Result;

            var request = response.RequestMessage;

            var expectedMD5 = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(string.Empty))
                .ComputeHash(Encoding.UTF8.GetBytes("firstName=Alex&lastName=Brown"));

            Assert.That(request.Content.Headers.ContentMD5, Is.Not.Null.Or.Empty);
            Assert.IsTrue(request.Content.Headers.ContentMD5.SequenceEqual(expectedMD5));
        }

        [Test]
        public void sets_signature_using_client_with_username_and_secret_passed_in()
        {
            testSignature();
        }

        [Test]
        public void can_use_client_created_with_getsecretfromkey_implementation_to_set_signature()
        {
            var mockGetSecretFromKey = new Mock<IGetSecretFromUsername>();
            mockGetSecretFromKey.Setup(x => x.Secret("anyusername"))
                .Returns("secret");

            client = HeimdallClientFactory.Create("anyusername", mockGetSecretFromKey.Object);
            client.BaseAddress = new Uri("http://localhost:8080");

            testSignature();
        }

        private void testSignature()
        {
            var field1 = new KeyValuePair<string, string>("firstName", "Alex");
            var field2 = new KeyValuePair<string, string>("lastName", "Brown");

            var content = new FormUrlEncodedContent(new[] { field1, field2 });

            var response = client.PostAsync("api/Test", content)
                .Result;

            var request = response.RequestMessage;

            Assert.That(request.Headers.Authorization.Scheme, Is.EqualTo(HeaderNames.AuthenticationScheme));
            Assert.That(request.Headers.Authorization.Parameter, Is.Not.Null.Or.Empty);
        }
    }
}
