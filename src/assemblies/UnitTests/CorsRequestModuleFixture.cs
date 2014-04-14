using System;
using NUnit.Framework;

namespace Cors
{
    [TestFixture]
    public class CorsRequestModuleFixture
    {
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Init_Throws_IfNullApplication()
        {
            CorsRequestModule targetObject = new CorsRequestModule();
            targetObject.Init(null);
        }
    }
}
