using System.Collections.Generic;
using Mozilla.IoT.WebThing.Converts;

namespace Mozilla.IoT.WebThing
{
    public class ThingContext
    {
        public ThingContext(IThingConverter converter, IProperties properties, Dictionary<string, EventCollection> events)
        {
            Converter = converter;
            Properties = properties;
            Events = events;
        }

        public IThingConverter Converter { get; }
        
        public IProperties Properties { get; }
        
        public Dictionary<string, EventCollection> Events { get; }
    }
}
