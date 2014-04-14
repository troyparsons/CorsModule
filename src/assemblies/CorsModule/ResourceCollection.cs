using System.Collections;
using Microsoft.Web.Administration;

namespace Cors
{
    public class ResourceCollection : ConfigurationElementCollectionBase<ResourceConfigurationElement>
    {
        public void CopyTo(ResourceConfigurationElement[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }
    }
}
