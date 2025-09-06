using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Dashboards;

namespace NckExchange.Composers;

class DashboardComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Dashboards()
            .Remove<ContentDashboard>();
    }
}