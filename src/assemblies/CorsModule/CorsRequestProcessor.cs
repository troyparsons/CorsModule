using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace Cors
{
    internal class CorsRequestProcessor
    {
        #region Fields

        private const string allowCredsHeaderName = "Access-Control-Allow-Credentials";
        private const string allowHeadersHeaderName = "Access-Control-Allow-Headers";
        private const string allowMethodsHeaderName = "Access-Control-Allow-Methods";
        private const string allowOriginHeaderName = "Access-Control-Allow-Origin";
        private const string cacheTimeHeaderName = "Access-Control-Max-Age";
        private const string errorFormatString = "The {0} is not permitted: {1}";
        private const string exposeHeadersHeaderName = "Access-Control-Expose-Headers";
        private const string requestedMethodHeaderName = "Access-Control-Request-Method";

        private readonly CorsConfigurationSection configuration;
        private readonly HttpContextBase httpContext;
        private readonly string origin;
        private string requestedResource;
        private ResourceConfigurationElement resourceConfiguration;

        #endregion

        #region Constructors

        internal CorsRequestProcessor(HttpContextBase context, CorsConfigurationSection corsConfig)
        {
            httpContext = context;
            configuration = corsConfig;
            origin = httpContext.Request.Headers["Origin"];
        }

        #endregion

        #region Properties

        private string AllowedMethods
        {
            get
            {
                return resourceConfiguration.AllowMethods;
            }
        }

        internal bool IsCorsPreFlightRequest
        {
            get
            {
                return string.Equals(httpContext.Request.HttpMethod, "OPTIONS") &&
                       !string.IsNullOrEmpty(origin) &&
                       !string.IsNullOrEmpty(RequestedMethod);
            }
        }

        internal bool IsCorsRequest
        {
            get
            {
                return !string.IsNullOrEmpty(origin);
            }
        }

        internal bool IsMethodPermitted
        {
            get
            {
                string[] allowedMethods = resourceConfiguration.AllowMethods.Split(new[] {','});
                return allowedMethods.Contains(RequestedMethod);
            }
        }

        internal bool IsOriginPermitted
        {
            get
            {
                return configuration.Origins.Any(o => o.Origin == origin);
            }
        }

        internal bool IsResourcePermitted
        {
            get
            {
                resourceConfiguration = configuration.Resources.FirstOrDefault(res => RequestedResource.Contains(res.Path));
                return resourceConfiguration != null;
            }
        }

        private string RequestedMethod
        {
            get
            {
                return httpContext.Request.Headers[requestedMethodHeaderName];
            }
        }

        private string RequestedResource
        {
            get
            {
                // ReSharper disable PossibleNullReferenceException
                return requestedResource ?? (requestedResource = httpContext.Request.Url.PathAndQuery);
                // ReSharper restore PossibleNullReferenceException
            }
        }

        private bool UseCredentials
        {
            get
            {
                return configuration.AllowCredentials;
            }
        }

        #endregion

        #region Methods

        private void AddCorsResponseHeaders(HttpResponseBase response)
        {
            if (UseCredentials)
            {
                response.Headers.Add(allowCredsHeaderName, "true");
            }
            response.Headers.Add(allowOriginHeaderName, origin);
            string exposedHeaders = GetExposedHeaders();
            if (!string.IsNullOrEmpty(exposedHeaders))
            {
                response.Headers.Add(exposeHeadersHeaderName, exposedHeaders);
            }
        }

        private static void DenyRequest(HttpResponseBase response, string failedItemName, string failedItemValue)
        {
            string message = string.Format(CultureInfo.InvariantCulture, errorFormatString, failedItemName, failedItemValue);
            response.StatusCode = (int) HttpStatusCode.Forbidden;
            response.Write(message);
            response.End();
        }

        private string GetAllowedHeaders()
        {
            string rootHeaders = configuration.AllowHeaders;
            string resourceHeaders = resourceConfiguration.AllowHeaders;

            if (string.IsNullOrEmpty(rootHeaders))
            {
                return resourceHeaders;
            }

            var allowedHeaders = new StringBuilder(rootHeaders);
            if (!string.IsNullOrEmpty(resourceHeaders))
            {
                allowedHeaders.Append(",");
                allowedHeaders.Append(resourceHeaders);
            }
            return allowedHeaders.ToString();
        }

        private string GetExposedHeaders()
        {
            string rootHeaders = configuration.ExposeHeaders;
            string resourceHeaders = resourceConfiguration.ExposeHeaders;

            if (string.IsNullOrEmpty(rootHeaders))
            {
                return resourceHeaders;
            }

            var exposedHeaders = new StringBuilder(rootHeaders);
            if (!string.IsNullOrEmpty(resourceHeaders))
            {
                exposedHeaders.Append(",");
                exposedHeaders.Append(resourceHeaders);
            }
            return exposedHeaders.ToString();
        }

        internal void HandlePreflightRequest()
        {
            if (!IsCorsPreFlightRequest)
            {
                return;
            }
            if (!IsOriginPermitted)
            {
                DenyRequest(httpContext.Response, "origin", origin);
                return;
            }
            if (!IsResourcePermitted)
            {
                DenyRequest(httpContext.Response, "resource", RequestedResource);
                return;
            }
            if (!IsMethodPermitted)
            {
                DenyRequest(httpContext.Response, "method", RequestedMethod);
                return;
            }
            SendCorsPreFlightResponse(httpContext.Response);
        }

        internal virtual void HandleRequest()
        {
            if (!IsCorsRequest)
            {
                return;
            }
            if (!IsOriginPermitted)
            {
                DenyRequest(httpContext.Response, "origin", origin);
                return;
            }
            if (IsResourcePermitted)
            {
                AddCorsResponseHeaders(httpContext.Response);
            }
        }

        private void SendCorsPreFlightResponse(HttpResponseBase response)
        {
            response.Headers.Add(allowOriginHeaderName, origin);

            if (UseCredentials)
            {
                response.Headers.Add(allowCredsHeaderName, "true");
            }

            response.Headers.Add(allowMethodsHeaderName, AllowedMethods);

            string allowedHeaders = GetAllowedHeaders();
            if (!string.IsNullOrEmpty(allowedHeaders))
            {
                response.Headers.Add(allowHeadersHeaderName, allowedHeaders);
            }

            uint cacheMaxAge = configuration.PreflightCacheMaxAge;
            if (cacheMaxAge > 0)
            {
                response.Headers.Add(cacheTimeHeaderName, cacheMaxAge.ToString(CultureInfo.InvariantCulture));
            }

            response.StatusCode = 200;
            response.Flush();
            response.End();
        }

        #endregion
    }
}
