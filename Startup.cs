using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(FluentMY.Startup))]
namespace FluentMY
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
