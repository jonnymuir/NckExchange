using Microsoft.AspNetCore.Mvc;
using System.Text;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.PublishedModels;


namespace NckExchange.Controllers;

public class SitemapController(IUmbracoContextFactory umbracoContextFactory) : Controller
{
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    [Route("sitemap.xml")]
    public IActionResult Sitemap()
    {
        var xmlStringBuilder = new StringBuilder();

        xmlStringBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xmlStringBuilder.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        // Ensure Umbraco context exists for the duration of this method.
        using (var cref = umbracoContextFactory.EnsureUmbracoContext())
        {
            // This is the correct way to access the query service.
            // The 'Content' property on UmbracoContext is an IPublishedContentQuery.
            var publishedContentQuery = cref.UmbracoContext.Content;

            // Get the root of the website
            var root = publishedContentQuery?.GetAtRoot().FirstOrDefault();
            if (root != null)
            {
                var allPages = root.DescendantsOrSelf()
                                   .Where(x => x is ISEoproperties seo && seo.IsIndexable);

                foreach (var page in allPages)
                {
                    var url = page.Url(mode: UrlMode.Absolute);
                    var lastModified = page.UpdateDate.ToString("yyyy-MM-dd");

                    xmlStringBuilder.AppendLine("\t<url>");
                    xmlStringBuilder.AppendLine($"\t\t<loc>{url}</loc>");
                    xmlStringBuilder.AppendLine($"\t\t<lastmod>{lastModified}</lastmod>");
                    xmlStringBuilder.AppendLine("\t</url>");
                }
            }
        }

        xmlStringBuilder.AppendLine("</urlset>");

        return Content(xmlStringBuilder.ToString(), "application/xml", Encoding.UTF8);
    }
}