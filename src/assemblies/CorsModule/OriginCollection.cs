using System.Collections;
using Microsoft.Web.Administration;

namespace Cors
{
    public class OriginCollection : ConfigurationElementCollectionBase<OriginConfigurationElement>
    {
        public void CopyTo(OriginConfigurationElement[] array, int index)
        {
            ((ICollection) this).CopyTo(array, index);
        }
    }
}
