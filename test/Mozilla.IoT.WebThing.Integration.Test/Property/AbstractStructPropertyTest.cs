using System.Reflection;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Mozilla.IoT.WebThing.Extensions;
using Xunit;

namespace Mozilla.IoT.WebThing.Integration.Test.Property
{
    public abstract class AbstractStructPropertyTest<T> : AbstractPropertyTest<T>
        where T : struct
    {
        protected virtual T CreateValue()
        {
            return Fixture.Create<T>();
        }
        
        [Fact]
        public void ValidProperty()
        {
            var thing = new PropertyThing();
            var context = Factory.Create(thing, new ThingOption());
        
            thing.ThingContext = context;
        
            context.Actions.Should().BeEmpty();
            context.Events.Should().BeEmpty();

            context.Properties.Should().NotBeEmpty();
            context.Properties.Should().HaveCount(2);
            context.Properties.Should().ContainKey(nameof(PropertyThing.Value));
            context.Properties.Should().ContainKey(nameof(PropertyThing.NullableValue));

            var value = CreateValue();
            var jsonElement = CreateJson(value);
        
            context.Properties[nameof(PropertyThing.Value)].TrySetValue(jsonElement).Should().Be(SetPropertyResult.Ok);
            thing.Value.Should().Be(value);
            context.Properties[nameof(PropertyThing.Value)].TryGetValue(out var getValue).Should().BeTrue();
            getValue.Should().Be(value);
        
            context.Properties[nameof(PropertyThing.NullableValue)].TrySetValue(jsonElement).Should().Be(SetPropertyResult.Ok);
            thing.NullableValue.Should().Be(value);
            context.Properties[nameof(PropertyThing.NullableValue)].TryGetValue(out getValue).Should().BeTrue();
            getValue.Should().Be(value);
        
            jsonElement = JsonSerializer.Deserialize<JsonElement>(@"{ ""input"": null }").GetProperty("input");
            context.Properties[nameof(PropertyThing.NullableValue)].TrySetValue(jsonElement).Should().Be(SetPropertyResult.Ok);
            thing.NullableValue.Should().BeNull();
            context.Properties[nameof(PropertyThing.NullableValue)].TryGetValue(out getValue).Should().BeTrue();
            getValue.Should().BeNull();
        }
        
        [Fact]
        public void Serialize()
        {
            var type = typeof(T).ToJsonType().ToString().ToLower();
            var value = typeof(T).IsEnum
                ? $@" ""enum"": [""{string.Join(@""" , """, typeof(T).GetEnumNames())}""], "
                : string.Empty;
            TestResponseProperty<PropertyThing>(string.Format(RESPONSE, type, value));
        }
        
        [Fact]
        public void InvalidValidProperty()
        {
            var thing = new PropertyThing();
            var context = Factory.Create(thing, new ThingOption());
            
            thing.ThingContext = context;
            
            context.Actions.Should().BeEmpty();
            context.Events.Should().BeEmpty();

            context.Properties.Should().NotBeEmpty();
            context.Properties.Should().HaveCount(2);
            context.Properties.Should().ContainKey(nameof(PropertyThing.Value));
            context.Properties.Should().ContainKey(nameof(PropertyThing.NullableValue));

            var value = Fixture.Create<T>();
            var jsonElement = CreateInvalidJson();
            
            var defaultValue = Fixture.Create<T>();
            thing.Value = defaultValue;
            
            foreach (var element in jsonElement)
            {
                context.Properties[nameof(PropertyThing.Value)].TrySetValue(element).Should().Be(SetPropertyResult.InvalidValue);
                thing.Value.Should().NotBe(value);
                thing.Value.Should().Be(defaultValue);
                context.Properties[nameof(PropertyThing.Value)].TryGetValue(out var getValue).Should().BeTrue();
                getValue.Should().Be(defaultValue);
            }
            
            thing.NullableValue = defaultValue;
            foreach (var element in jsonElement)
            {
                context.Properties[nameof(PropertyThing.NullableValue)].TrySetValue(element).Should().Be(SetPropertyResult.InvalidValue);
                thing.NullableValue.Should().NotBe(value);
                thing.NullableValue.Should().Be(defaultValue);
                context.Properties[nameof(PropertyThing.NullableValue)].TryGetValue(out var nullableValue).Should().BeTrue();
                nullableValue.Should().Be(defaultValue);
            }
        }
        
        public class PropertyThing : Thing
        {
            public override string Name => "property-thing";
            
            public T Value { get; set; }
            public T? NullableValue { get; set; }
        }
        
        private const string RESPONSE = @"{{
  ""@context"": ""https://iot.mozilla.org/schemas"",
  ""properties"": {{
    ""value"": {{
      ""type"": ""{0}"",
      {1}
      ""links"": [
        {{
          ""href"": ""/things/property-thing/properties/value"",
          ""rel"": ""property""
        }}
      ]
    }},
    ""nullableValue"": {{
      ""type"": ""{0}"",
      {1}
      ""links"": [
        {{
          ""href"": ""/things/property-thing/properties/nullableValue"",
          ""rel"": ""property""
        }}
      ]
    }}
  }},
  ""links"": [
    {{
      ""href"": ""properties"",
      ""rel"": ""/things/property-thing/properties""
    }},
    {{
      ""href"": ""events"",
      ""rel"": ""/things/property-thing/events""
    }},
    {{
      ""href"": ""actions"",
      ""rel"": ""/things/property-thing/actions""
    }}
  ]
}}";
    }
}
