using System;
using System.Reflection;
using System.Text.Json;
using Mozilla.IoT.WebThing.Factories.Generator.Intercepts;

namespace Mozilla.IoT.WebThing.Factories.Generator.Properties
{
    internal class PropertiesInterceptFactory : IInterceptorFactory
    {
        private readonly PropertiesPropertyIntercept _intercept;

        public PropertiesInterceptFactory(Type thingType)
        {
            var builder = Factory.CreateTypeBuilder($"{thingType.Name}Properties", thingType.Name, 
                typeof(IProperty), TypeAttributes.AutoClass | TypeAttributes.Class | TypeAttributes.Public);
            _intercept = new PropertiesPropertyIntercept(builder);
        }

        public IThingIntercept CreateThingIntercept() => new EmptyIntercept();

        public IPropertyIntercept CreatePropertyIntercept()
            => _intercept;

        public IActionIntercept CreatActionIntercept()
            => new EmptyIntercept();

        public IEventIntercept CreatEventIntercept()
            => new EmptyIntercept();
    }
}
