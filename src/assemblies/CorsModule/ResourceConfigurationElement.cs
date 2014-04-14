using Microsoft.Web.Administration;

namespace Cors
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    ///		<attribute name="path" type="string" required="true"/>
	///		<attribute name="allowHeaders" type="string" required="false"/>
	///		<attribute name="allowMethods" type="string" default="GET,HEAD,POST"/>
	///		<attribute name="exposeHeaders" type="string" required="false"/>
    /// </remarks>
    public class ResourceConfigurationElement : ConfigurationElement
    {
        #region Properties

        public virtual string AllowHeaders
        {
            get
            {
                return (string) base["allowHeaders"];
            }
        }

        public virtual string AllowMethods
        {
            get
            {
                return (string) base["allowMethods"];
            }
        }

        public virtual string ExposeHeaders
        {
            get
            {
                return base["exposeHeaders"] as string;
            }
        }

        public virtual string Path
        {
            get
            {
                return (string) base["path"];
            }
        }

        #endregion
    }
}
