using AutoFixture;
using FluentAssertions;
using Mozilla.IoT.WebThing.Convertibles.Number;
using Mozilla.IoT.WebThing.Factories;
using Xunit;
using IConvertible = Mozilla.IoT.WebThing.Convertibles.IConvertible;

namespace Mozilla.IoT.WebThing.Test.Convertibles.Numbers
{
    public class ByteConvertibleTest : BaseConvertibleTest<byte>
    {
        protected override IConvertible CreateConvertible()
        {
            return new NumberConvertible(TypeCode.Byte);
        }

        [Fact]
        public void Create_Should_ConvertType_When_ValueIsOtherType()
        {
            var convertible = CreateConvertible();
            var value = Fixture.Create<byte>();
            convertible.Convert(value.ToString())
                .Should().Be(value);
        }
    }
}
