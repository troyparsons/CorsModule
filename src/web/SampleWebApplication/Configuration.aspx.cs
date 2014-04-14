using Cors;
using Microsoft.Web.Administration;
using System;
using System.Web;

namespace SampleWebApplication
{
    public partial class Configuration : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            
//            ServerManager serverManager = new ServerManager();
  //          Microsoft.Web.Administration.Configuration config = serverManager.();

            Config = (CorsConfigurationSection)WebConfigurationManager.GetSection(HttpContext.Current, "system.webServer/httpCors", typeof(CorsConfigurationSection));
        }

        protected CorsConfigurationSection Config { get; private set; }
    }
}