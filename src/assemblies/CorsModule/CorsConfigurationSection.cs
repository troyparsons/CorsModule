using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Web.Administration;

namespace Cors
{
    /// <summary>
    /// </summary>
    /// <remarks>
    ///     <attribute name="allowHeaders" type="string" required="false" />
    ///     <attribute name="exposeHeaders" type="string" required="false" />
    ///     <attribute name="preFlightCacheMaxAge" type="uint" defaultValue="300" />
    ///     <attribute name="allowCredentials" type="bool" defaultValue="false" />
    /// </remarks>
    public class CorsConfigurationSection : ConfigurationSection
    {
        #region Fields

        public const string Path = "system.webServer/httpCors";

        #endregion

        #region Properties

        public virtual bool AllowCredentials
        {
            get
            {
                return (bool)base["allowCredentials"];
            }
        }

        public virtual string AllowHeaders
        {
            get
            {
                return (string) base["allowHeaders"];
            }
        }

        public virtual string ExposeHeaders
        {
            get
            {
                return (string) base["exposeHeaders"];
            }
        }

        public virtual uint PreflightCacheMaxAge
        {
            get
            {
                return Convert.ToUInt32(base["preflightCacheMaxAge"], CultureInfo.InvariantCulture);
            }
        }

        public virtual IEnumerable<OriginConfigurationElement> Origins
        {
            get
            {
                return (OriginCollection) GetCollection("allowOrigins", typeof (OriginCollection));
            }
        }

        public virtual IEnumerable<ResourceConfigurationElement> Resources
        {
            get
            {
                return (ResourceCollection) GetCollection("resources", typeof (ResourceCollection));
            }
        }

        #endregion

        public IEnumerable<string> GetResourceAllowHeaders(string resourcePath)
        {
            ResourceConfigurationElement resourceConfig = Resources.First(r => resourcePath.Contains(r.Path));
            return AllowHeaders.Split(new[] {','}).Union(resourceConfig.AllowHeaders.Split(new[] {','}));
        }
        
        public IEnumerable<string> GetResourceExposeHeaders(string resourcePath)
        {
            ResourceConfigurationElement resourceConfig = Resources.First(r => resourcePath.Contains(r.Path));
            return ExposeHeaders.Split(new[] {','}).Union(resourceConfig.ExposeHeaders.Split(new[] {','}));
        }
    }
}