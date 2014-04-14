using System;
using System.Web;
using Microsoft.Web.Administration;

namespace Cors
{
    public class CorsRequestModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            context.BeginRequest += HandlePreFlightRequest;
            context.PreRequestHandlerExecute += HandleCorsRequest;
        }

        private static CorsConfigurationSection Configuration
        {
            get
            {
                return (CorsConfigurationSection) WebConfigurationManager.GetSection(
                    HttpContext.Current,
                    CorsConfigurationSection.Path,
                    typeof (CorsConfigurationSection)
                                                      );
            }
        }

        private void HandleCorsRequest(object sender, EventArgs e)
        {
            HttpContextBase context = new HttpContextWrapper(((HttpApplication) sender).Context);
            CorsRequestProcessor corsRequest = new CorsRequestProcessor(context, Configuration);
            corsRequest.HandleRequest();
        }

        private void HandlePreFlightRequest(object sender, EventArgs e)
        {
            HttpContextBase context = new HttpContextWrapper(((HttpApplication)sender).Context);
            CorsRequestProcessor corsRequest = new CorsRequestProcessor(context, Configuration);
            corsRequest.HandlePreflightRequest();
        }

        public void Dispose()
        {
        }
    }
}
