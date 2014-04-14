using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using System.Collections.Specialized;
using System.Globalization;
using System.Web;
using System.Linq;

namespace Cors
{
    [TestFixture]
    public class CorsRequestProcessorFixture
    {
        private CorsRequestProcessor targetObject;
        private Mock<HttpContextBase> mockContext;
        private Mock<HttpResponseBase> mockResponse;
        private Mock<HttpRequestBase> mockRequest;
        private List<OriginConfigurationElement> origins;
        private NameValueCollection requestHeaders;
        private Uri requestUrl;
        private NameValueCollection responseHeaders;
        private Mock<CorsConfigurationSection> mockConfig;

        private void AssertResponseUntouched()
        {
            mockResponse.VerifySet(res => res.StatusCode = It.IsAny<int>(), Times.Never(),
                                   "status code should not have been set");
            mockResponse.Verify(res => res.AddHeader(It.IsAny<string>(), It.IsAny<string>()), Times.Never(),
                                "no headers should have been added");
            Assert.AreEqual(0, responseHeaders.Count, "should be no response headers");
            mockResponse.Verify(res => res.Flush(), Times.Never(), "response should not have been flushed");
            mockResponse.Verify(res => res.End(), Times.Never(), "response should not have been terminated");
        }

        private void AssertResponseDenied(string expectedResponse)
        {
            mockResponse.VerifySet(res => res.StatusCode = 403, Times.Once(), "status code should have been set");
            mockResponse.Verify(res => res.AddHeader(It.IsAny<string>(), It.IsAny<string>()), Times.Never(),
                                "no headers should have been added");
            Assert.AreEqual(0, responseHeaders.Count, "should be no response headers");
            mockResponse.Verify(res => res.Flush(), Times.Never(), "response should not have been flushed");
            mockResponse.Verify(res => res.End(), Times.Once(), "response should have been terminated");
            mockResponse.Verify(res => res.Write(expectedResponse), Times.Once(), "expected response content to be set");
        }

        private void CreateTargetObject()
        {
            targetObject = new CorsRequestProcessor(mockContext.Object, mockConfig.Object);
        }

        private void SetupMockConfig()
        {
            Mock<OriginConfigurationElement> mockOriginConfig = new Mock<OriginConfigurationElement>(MockBehavior.Strict);
            mockOriginConfig.SetupGet(o => o.Origin).Returns("http://chitter.com");

            Mock<ResourceConfigurationElement> mockResourceConfig1 =
                new Mock<ResourceConfigurationElement>(MockBehavior.Strict);
            mockResourceConfig1.SetupGet(o => o.Path).Returns("/api/friends/");
            mockResourceConfig1.SetupGet(o => o.AllowMethods).Returns("GET,POST,PUT,DELETE");
            mockResourceConfig1.SetupGet(o => o.ExposeHeaders).Returns((string)null);
            mockResourceConfig1.SetupGet(o => o.AllowHeaders).Returns((string)null);

            Mock<ResourceConfigurationElement> mockResourceConfig2 =
                new Mock<ResourceConfigurationElement>(MockBehavior.Strict);
            mockResourceConfig2.SetupGet(o => o.Path).Returns("/api/profile/");
            mockResourceConfig2.SetupGet(o => o.AllowMethods).Returns("GET,HEAD");
            mockResourceConfig2.SetupGet(o => o.ExposeHeaders).Returns("Wibble,Wobble");
            mockResourceConfig2.SetupGet(o => o.AllowHeaders).Returns("Device-type");

            origins = new List<OriginConfigurationElement> { mockOriginConfig.Object };

            mockConfig = new Mock<CorsConfigurationSection>(MockBehavior.Strict);
            mockConfig.SetupGet(cfg => cfg.Origins)
                      .Returns(origins);
            mockConfig.SetupGet(cfg => cfg.Resources)
                      .Returns(new List<ResourceConfigurationElement>
                          {
                              mockResourceConfig1.Object,
                              mockResourceConfig2.Object
                          });
            mockConfig.SetupGet(cfg => cfg.AllowCredentials).Returns(false);
            mockConfig.SetupGet(cfg => cfg.AllowHeaders).Returns((string)null);
            mockConfig.SetupGet(cfg => cfg.ExposeHeaders).Returns("Hoge,Page");
            mockConfig.SetupGet(cfg => cfg.PreflightCacheMaxAge).Returns(300);
        }

        private void SetupRequest(string origin = "http://chitter.com",
                                  string url = "http://friendface.com/api/friends/")
        {
            requestUrl = new Uri(url);
            mockRequest.SetupGet(req => req.Url).Returns(requestUrl);
            requestHeaders.Add("Origin", origin);
        }

        private void SetupPreflightRequest(
            string origin = "http://chitter.com",
            string url = "http://friendface.com/api/friends/",
            string requestedMethod = "PUT")
        {
            SetupRequest(origin, url);
            mockRequest.SetupGet(req => req.HttpMethod).Returns("OPTIONS");
            requestHeaders.Add("Access-Control-Request-Method", requestedMethod);
        }

        [SetUp]
        public void SetUp()
        {
            requestHeaders = new NameValueCollection();
            responseHeaders = new NameValueCollection();

            mockResponse = new Mock<HttpResponseBase>();
            mockResponse.SetupGet(res => res.Headers).Returns(responseHeaders);

            mockRequest = new Mock<HttpRequestBase>(MockBehavior.Strict);
            mockRequest.SetupGet(req => req.Headers).Returns(requestHeaders);

            mockContext = new Mock<HttpContextBase>(MockBehavior.Strict);
            mockContext.SetupGet(ctx => ctx.Request).Returns(mockRequest.Object);
            mockContext.SetupGet(ctx => ctx.Response).Returns(mockResponse.Object);

            SetupMockConfig();
        }

        [TearDown]
        public void TearDown()
        {
            targetObject = null;
            mockContext = null;
            mockConfig = null;
        }

        [TestCase("http://chitter.com", true)]
        [TestCase("http://hoge.page.com", false)]
        public void IsOriginPermitted_Correct(string origin, bool expected)
        {
            SetupRequest(origin, "http://friendface.com/api/friends");
            CreateTargetObject();

            Assert.AreEqual(expected, targetObject.IsOriginPermitted, "IsOriginPermitted is incorrect");
        }

        [TestCase("http://friendface.com/api/friends/MauriceMoss", true)]
        [TestCase("http://friendface.com/api/ceos/DouglasReynholm", false)]
        public void IsResourcePermitted_Correct(string urlToTest, bool expected)
        {
            SetupRequest(url: urlToTest);
            CreateTargetObject();

            Assert.AreEqual(expected, targetObject.IsResourcePermitted);
        }

        [TestCase("GET", "http://friendface.com/api/friends/", true)]
        [TestCase("PUT", "http://friendface.com/api/friends/RichmondAvenal", true)]
        [TestCase("DELETE", "http://friendface.com/api/friends/JenBarber", true)]
        [TestCase("CUKE", "http://friendface.com/api/friends/JenBarber", false)]
        [TestCase("GET", "http://friendface.com/api/profile/", true)]
        [TestCase("DELETE", "http://friendface.com/api/profile/", false)]
        public void IsMethodPermitted_Correct(string requestedMethod, string targetUrl, bool expected)
        {
            SetupPreflightRequest(url: targetUrl, requestedMethod: requestedMethod);
            CreateTargetObject();
            Assert.IsTrue(targetObject.IsResourcePermitted, "pre-req check failed");
            Assert.AreEqual(expected, targetObject.IsMethodPermitted, "IsMethodPermitted incorrect");
        }

        [TestCase(30)]
        [TestCase(600)]
        public void HandlePreflightRequest_SetsCacheMaxAge(int cacheTime)
        {
            SetupPreflightRequest();
            mockConfig.SetupGet(cfg => cfg.PreflightCacheMaxAge).Returns(cacheTime);

            string expected = cacheTime.ToString(CultureInfo.InvariantCulture);

            CreateTargetObject();
            targetObject.HandlePreflightRequest();

            Assert.AreEqual(expected, responseHeaders["Access-Control-Max-Age"],
                            "Pre-flight request cache header was not set.");
        }

        [Test]
        public void HandlePreflightRequest_OmitsMaxAge_IfMaxAgeZero()
        {
            SetupPreflightRequest();
            mockConfig.SetupGet(cfg => cfg.PreflightCacheMaxAge).Returns(0);

            CreateTargetObject();
            targetObject.HandlePreflightRequest();

            Assert.IsFalse(responseHeaders.AllKeys.Contains("Access-Control-Max-Age"),
                           "Access-Control-Max-Age should not be present");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void HandlePreflightRequest_SetsAllowCredsHeader(bool allowCreds)
        {
            mockConfig.SetupGet(cfg => cfg.AllowCredentials).Returns(allowCreds);
            SetupPreflightRequest();

            CreateTargetObject();
            targetObject.HandlePreflightRequest();

            if (allowCreds)
            {
                Assert.AreEqual("true", responseHeaders["Access-Control-Allow-Credentials"],
                                "Access-Control-Allow-Credentials missing or incorrect");
            }
            else
            {
                Assert.IsFalse(responseHeaders.AllKeys.Contains("Access-Control-Allow-Credentials"),
                               "should be no Access-Control-Allow-Credentials header");
            }
        }

        [TestCase("http://friendface.com/api/friends/", null)]
        [TestCase("http://friendface.com/api/friends/RichmondAvenal", null)]
        [TestCase("http://friendface.com/api/friends/JenBarber", null)]
        [TestCase("http://friendface.com/api/profile/", "Device-type")]
        [TestCase("http://friendface.com/api/profile/detailed", "Device-type")]
        public void HandlePreflightRequest_SetsAllowedHeadersHeader_ResourceOnly(string targetUrl, string expected)
        {
            SetupPreflightRequest(url: targetUrl, requestedMethod: "GET");

            CreateTargetObject();
            targetObject.HandlePreflightRequest();

            mockResponse.VerifySet(res => res.StatusCode = 403, Times.Never(),
                                   "Seems like response was denied?");

            if (expected == null)
            {
                Assert.IsFalse(responseHeaders.AllKeys.Contains("Access-Control-Allow-Headers"),
                               "should be no Access-Control-Allow-Headers header");
            }
            else
            {
                Assert.AreEqual(expected, responseHeaders["Access-Control-Allow-Headers"],
                                "Value of header Access-Control-Allow-Headers incorrect");
            }
        }

        [TestCase("http://friendface.com/api/friends/", "Token,UID")]
        [TestCase("http://friendface.com/api/friends/RichmondAvenal", "Token,UID")]
        [TestCase("http://friendface.com/api/friends/JenBarber", "Token,UID")]
        [TestCase("http://friendface.com/api/profile/", "Token,UID,Device-type")]
        [TestCase("http://friendface.com/api/profile/detailed", "Token,UID,Device-type")]
        public void HandlePreflightRequest_SetsAllowedHeadersHeader_RootAndResource(string targetUrl, string expected)
        {
            mockConfig.SetupGet(cfg => cfg.AllowHeaders).Returns("Token,UID");
            SetupPreflightRequest(url: targetUrl, requestedMethod: "GET");

            CreateTargetObject();
            targetObject.HandlePreflightRequest();

            mockResponse.VerifySet(res => res.StatusCode = 403, Times.Never(),
                                   "Seems like response was denied?");

            Assert.AreEqual(expected, responseHeaders["Access-Control-Allow-Headers"],
                            "Value of header Access-Control-Allow-Headers incorrect");
        }

        [Test]
        public void HandlePreflightRequest_Exits_IfNotPreflightRequest_MissingOrigin()
        {
            SetupPreflightRequest(origin: null);
            CreateTargetObject();

            targetObject.HandlePreflightRequest();

            AssertResponseUntouched();
        }

        [Test]
        public void HandlePreflightRequest_Exits_IfNotPreflightRequest_NotOptions()
        {
            SetupPreflightRequest();
            mockRequest.SetupGet(req => req.HttpMethod).Returns("GET");
            CreateTargetObject();

            targetObject.HandlePreflightRequest();

            AssertResponseUntouched();
        }

        [Test]
        public void HandlePreflightRequest_Exits_IfNotPreflightRequest_NoRequestedMethod()
        {
            SetupPreflightRequest();
            requestHeaders.Remove("Access-Control-Request-Method");
            CreateTargetObject();

            targetObject.HandlePreflightRequest();

            AssertResponseUntouched();
        }

        [TestCase("http://www.reynholm.co.uk", "The origin is not permitted: http://www.reynholm.co.uk")]
        [TestCase("http://bluffball.co.uk", "The origin is not permitted: http://bluffball.co.uk")]
        public void HandlePreflightRequest_Denies_IfOriginNotPermitted(string origin, string expectedMessage)
        {
            SetupPreflightRequest(origin: origin);
            CreateTargetObject();

            targetObject.HandlePreflightRequest();

            AssertResponseDenied(expectedMessage);
        }

        [TestCase("http://friendface.com/api/password","The resource is not permitted: /api/password")]
        [TestCase("http://friendface.com/cgi-bin/turnifoffanon.exe", "The resource is not permitted: /cgi-bin/turnifoffanon.exe")]
        public void HandlePreflightRequest_Denies_IfResourceNotPermitted(string targetUrl, string expectedMessage)
        {
            SetupPreflightRequest(url: targetUrl);
            CreateTargetObject();

            targetObject.HandlePreflightRequest();

            AssertResponseDenied(expectedMessage);
        }

        [TestCase("http://friendface.com/api/profile/", "DELETE", "The method is not permitted: DELETE")]
        [TestCase("http://friendface.com/api/friends/richard", "TRACE", "The method is not permitted: TRACE")]
        public void HandlePreflightRequest_Denies_IfMethodNotPermitted(string targetUrl, string requestedMethod, string expectedMessage)
        {
            SetupPreflightRequest(url: targetUrl, requestedMethod: requestedMethod);
            CreateTargetObject();

            targetObject.HandlePreflightRequest();

            AssertResponseDenied(expectedMessage);
        }

        [TestCase("http://www.reynholm.co.uk","The origin is not permitted: http://www.reynholm.co.uk")]
        [TestCase("http://bluffball.co.uk","The origin is not permitted: http://bluffball.co.uk")]
        public void HandleRequest_Denies_IfOriginNotPermitted(string origin, string expectedMessage)
        {
            SetupRequest(origin: origin);
            CreateTargetObject();

            targetObject.HandleRequest();

            AssertResponseDenied(expectedMessage);
        }

        [Test]
        public void HandleRequest_Exits_IfResourceNotPermitted()
        {
            SetupRequest(url: "http://friendface.com/api/password");
            CreateTargetObject();

            targetObject.HandleRequest();

            AssertResponseUntouched();
        }

        [Test]
        public void HandleRequest_Exits_IfNotValidCors_MissingOrigin()
        {
            SetupRequest(origin: null);
            CreateTargetObject();

            targetObject.HandleRequest();

            AssertResponseUntouched();
        }

        [Test]
        public void HandleRequest_SetsAllowCredsHeader_IfSet()
        {
            mockConfig.SetupGet(cfg => cfg.AllowCredentials).Returns(true);
            SetupRequest();
            CreateTargetObject();

            targetObject.HandleRequest();

            mockResponse.VerifySet(res => res.StatusCode = It.IsAny<int>(),Times.Never, "status code should not have been set");
            Assert.AreEqual("true", responseHeaders["Access-Control-Allow-Credentials"], "value of Access-Control-Allow-Credentials header incorrect");
        }

        [Test]
        public void HandleRequest_SkipsAllowCredsHeader_IfUnset()
        {
            SetupRequest();
            CreateTargetObject();

            targetObject.HandleRequest();

            mockResponse.VerifySet(res => res.StatusCode = It.IsAny<int>(),Times.Never, "status code should not have been set");
            Assert.IsFalse(responseHeaders.AllKeys.Contains("Access-Control-Allow-Credentials"), "Access-Control-Allow-Credentials header should not be present");
        }

        [TestCase("http://wibble.com:8080")]
        [TestCase("https://wobble.com")]
        public void HandleRequest_SetsOriginHeader(string origin)
        {
            Mock<OriginConfigurationElement> mockOrigin1 = new Mock<OriginConfigurationElement>(MockBehavior.Strict);
            Mock<OriginConfigurationElement> mockOrigin2 = new Mock<OriginConfigurationElement>(MockBehavior.Strict);
            mockOrigin1.SetupGet(org => org.Origin).Returns("http://wibble.com:8080");
            mockOrigin2.SetupGet(org => org.Origin).Returns("https://wobble.com");
            origins.Add(mockOrigin1.Object);
            origins.Add(mockOrigin2.Object);

            SetupRequest(origin: origin);
            CreateTargetObject();

            targetObject.HandleRequest();

            mockResponse.VerifySet(res => res.StatusCode = It.IsAny<int>(),Times.Never, "status code should not have been set");
            Assert.AreEqual(origin, responseHeaders["Access-Control-Allow-Origin"], "Access-Control-Allow-Origin header incorrect");
        }

        [Test]
        public void HandleRequest_SetsExposedHeaders_RootOnly()
        {
            SetupRequest();
            CreateTargetObject();

            targetObject.HandleRequest();

            mockResponse.VerifySet(res => res.StatusCode = It.IsAny<int>(),Times.Never, "status code should not have been set");
            Assert.AreEqual("Hoge,Page", responseHeaders["Access-Control-Expose-Headers"], "Access-Control-Expose-Headers header incorrect");
        }

        [TestCase( "http://friendface.com/api/profile/")]
        [TestCase( "http://friendface.com/api/profile/detailed")]
        public void HandleRequest_SetsExposedHeaders_RootAndResource(string targetUrl)
        {
            SetupRequest(url: targetUrl);
            CreateTargetObject();

            targetObject.HandleRequest();

            mockResponse.VerifySet(res => res.StatusCode = It.IsAny<int>(),Times.Never, "status code should not have been set");
            Assert.AreEqual("Hoge,Page,Wibble,Wobble", responseHeaders["Access-Control-Expose-Headers"], "Access-Control-Expose-Headers header incorrect");
        }

        [TestCase("http://friendface.com/api/profile/")]
        [TestCase("http://friendface.com/api/profile/richard.avenal")]
        public void HandleRequest_SetsExposedHeaders_ResourceOnly(string targetUrl)
        {
            mockConfig.SetupGet(cfg => cfg.ExposeHeaders).Returns((string)null);
            SetupRequest(url: targetUrl);
            CreateTargetObject();

            targetObject.HandleRequest();

            mockResponse.VerifySet(res => res.StatusCode = It.IsAny<int>(),Times.Never, "status code should not have been set");
            Assert.AreEqual("Wibble,Wobble", responseHeaders["Access-Control-Expose-Headers"], "Access-Control-Expose-Headers header incorrect");
        }

        [Test]
        public void HandleRequest_SetsExposedHeaders_NeitherRootNorResource()
        {
            mockConfig.SetupGet(cfg => cfg.ExposeHeaders).Returns((string)null);
            SetupRequest();
            CreateTargetObject();

            targetObject.HandleRequest();

            mockResponse.VerifySet(res => res.StatusCode = It.IsAny<int>(),Times.Never, "status code should not have been set");
            Assert.IsFalse(responseHeaders.AllKeys.Contains("Access-Control-Expose-Headers"), "Access-Control-Expose-Headers should not be set");
        }

        [TestCase("http://chitter.com", "PUT", "OPTIONS", true)]
        [TestCase("http://chitter.com", "", "OPTIONS", false)]
        [TestCase("http://chitter.com", null, "OPTIONS", false)]
        [TestCase("", "PUT", "OPTIONS", false)]
        [TestCase(null, "PUT", "OPTIONS", false)]
        [TestCase("http://chitter.com", "PUT", "GET", false)]
        public void IsPreFlightRequestValid(string origin, string requestedMethod, string method, bool expectedResult)
        {
            mockRequest.SetupGet(req => req.HttpMethod).Returns(method);
            if (origin != null)
            {
                requestHeaders.Add("Origin", origin);
            }
            if (requestedMethod != null)
            {
                requestHeaders.Add("Access-Control-Request-Method", requestedMethod);
            }

            CreateTargetObject();

            Assert.AreEqual(expectedResult, targetObject.IsCorsPreFlightRequest, "Result incorrect.");
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void IsRequestValid_Correct(bool deleteOriginHeader, bool expected)
        {
            SetupRequest();
            if (deleteOriginHeader)
            {
                requestHeaders.Remove("Origin");
            }
            CreateTargetObject();
            Assert.AreEqual(expected, targetObject.IsCorsRequest, "IsCorsRequest incorrect");
        }
    }
}
