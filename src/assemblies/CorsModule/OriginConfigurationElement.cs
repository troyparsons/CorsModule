using Microsoft.Web.Administration;

namespace Cors
{
    public class OriginConfigurationElement : ConfigurationElement
    {
        public virtual string Origin
        {
            get
            {
                return (string) this["origin"];
            }
        }
    }
}
