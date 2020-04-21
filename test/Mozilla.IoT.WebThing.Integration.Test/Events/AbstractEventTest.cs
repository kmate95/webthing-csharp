using AutoFixture;
using Microsoft.Extensions.DependencyInjection;
using Mozilla.IoT.WebThing.Factories;
using Xunit;

namespace Mozilla.IoT.WebThing.Integration.Test.Events
{
    public abstract class AbstractEventTest
    {
        protected IThingContextFactory Factory { get; }
        protected Fixture Fixture { get; }

        public AbstractEventTest()
        {
            var collection = new ServiceCollection();
            collection.AddThings();
            var provider = collection.BuildServiceProvider();
            Factory = provider.GetRequiredService<IThingContextFactory>();
            Fixture = new Fixture();
        }
    }
}
