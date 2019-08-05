using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mozilla.IoT.WebThing.Activator;

namespace Mozilla.IoT.WebThing.Endpoints
{
    internal static class GetProperties
    {
        internal static async Task Invoke(HttpContext httpContext)
        {
            var services = httpContext.RequestServices;
            var logger = services.GetService<ILogger>();
            
            logger.LogInformation("Get Properties is calling");
            var thingId = httpContext.GetValueFromRoute<string>("thing");

            logger.LogInformation($"Get Properties: [[thing: {thingId}]]");
            var thing = services.GetService<IThingActivator>()
                .CreateInstance(services, thingId);
            
            if (thing == null)
            {
                logger.LogInformation($"Get Properties: Thing not found [[thing: {thingId}]]");
                httpContext.Response.StatusCode = (int) HttpStatusCode.NotFound;
                return;
            }
            
            var result = thing.Properties.ToDictionary(property => property.Name, 
                property => property.Value);

            await httpContext.WriteBodyAsync(HttpStatusCode.OK, result);
        }
    }
}