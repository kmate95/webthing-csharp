﻿using System;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Mozilla.IoT.WebThing.Actions.Parameters.Number;
using Xunit;

namespace Mozilla.IoT.WebThing.Test.Actions.Parameters.Numbers
{
    public class ParameterSByteTest
    {
        private readonly Fixture _fixture;
        
        public ParameterSByteTest()
        {
            _fixture = new Fixture();
        }
        
        #region No Nullable 
        private static ParameterSByte CreateNoNullable(sbyte[]? enums = null, sbyte? min = null, sbyte? max = null, sbyte? multipleOf = null)
            => new ParameterSByte(false, min, max, multipleOf, enums);

        [Fact]
        public void SetNoNullableWithValue()
        {
            var value = _fixture.Create<sbyte>();
            var property = CreateNoNullable();
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": {value} }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out var jsonValue).Should().BeTrue();
            jsonValue.Should().Be(value);
        }
        
        [Fact]
        public void SetNoNullableWithValueEnums()
        {
            var values = _fixture.Create<sbyte[]>();
            var property = CreateNoNullable(values);
            foreach (var value in values)
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": {value} }}");
                property.TryGetValue(jsonElement.GetProperty("input"), out var jsonValue).Should().BeTrue();
                jsonValue.Should().Be(value);
            }
        }
        
        [Theory]
        [InlineData(11)]
        [InlineData(10)]
        public void SetNoNullableWithMinValue(sbyte value)
        {
            var property = CreateNoNullable(min: 10);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": {value} }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out var jsonValue).Should().BeTrue();
            jsonValue.Should().Be(value);
        }
        
        [Theory]
        [InlineData(9)]
        [InlineData(10)]
        public void SetNoNullableWithMaxValue(sbyte value)
        {
            var property = CreateNoNullable(max: 10);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": {value} }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out var jsonValue).Should().BeTrue();
            jsonValue.Should().Be(value);
        }
        
        [Fact]
        public void SetNoNullableWithMultipleOfValue()
        {
            var property = CreateNoNullable(multipleOf: 2);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": 10 }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out var jsonValue).Should().BeTrue();
            jsonValue.Should().Be(10);
        }
        
        [Fact]
        public void TrySetNoNullableWithNullValue()
        {
            var property = CreateNoNullable();
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(@"{ ""input"": null }");
            property.TryGetValue(jsonElement.GetProperty("input"), out _).Should().BeFalse();
        }
        
        [Theory]
        [InlineData(typeof(bool))]
        [InlineData(typeof(string))]
        public void TrySetNoNullableWitInvalidValue(Type type)
        {
            var value = type == typeof(bool) ? _fixture.Create<bool>().ToString().ToLower() : $@"""{_fixture.Create<string>()}""";
            var property = CreateNoNullable();
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": {value} }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out _).Should().BeFalse();
        }
        
        [Fact]
        public void TrySetNoNullableWithEnumValue()
        {
            var values = _fixture.Create<sbyte[]>();
            var property = CreateNoNullable(values);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": {_fixture.Create<sbyte>()} }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out _).Should().BeFalse();
        }
        
        [Theory]
        [InlineData(8)]
        [InlineData(9)]
        public void TrySetNoNullableWithMinValue(sbyte value)
        {
            var property = CreateNoNullable(min: 10);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": {value} }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out _).Should().BeFalse();
        }
        
        [Theory]
        [InlineData(12)]
        [InlineData(11)]
        public void TrySetNoNullableWithMaxValue(sbyte value)
        {
            var property = CreateNoNullable(max: 10);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": {value} }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out _).Should().BeFalse();
        }
        
        [Fact]
        public void TrySetNoNullableWithMultipleOfValue()
        {
            var property = CreateNoNullable(multipleOf: 2);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": 9 }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out _).Should().BeFalse();
        }
        #endregion
        
        #region Nullable

        private static ParameterSByte CreateNullable(sbyte[]? enums = null, sbyte? min = null, sbyte? max = null, sbyte? multipleOf = null)
            => new ParameterSByte(true, min, max, multipleOf, enums);

        [Fact]
        public void SetNullableWithValue()
        {
            var value = _fixture.Create<sbyte>();
            var property = CreateNullable();
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": {value} }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out var jsonValue).Should().BeTrue();
            jsonValue.Should().Be(value);
        }
        
        [Fact]
        public void SetNullableWithNullValue()
        {
            var property = CreateNullable();
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(@"{ ""input"": null }");
            property.TryGetValue(jsonElement.GetProperty("input"), out var jsonValue).Should().BeTrue();
            jsonValue.Should().BeNull();
        }
        
        [Fact]
        public void SetNullableWithValueEnums()
        {
            var values = _fixture.Create<sbyte[]>();
            var property = CreateNullable(values);
            foreach (var value in values)
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": {value} }}");
                property.TryGetValue(jsonElement.GetProperty("input"), out var jsonValue).Should().BeTrue();
                jsonValue.Should().Be(value);
            }
        }
        
        [Theory]
        [InlineData(11)]
        [InlineData(10)]
        public void SetNullableWithMinValue(sbyte value)
        {
            var property = CreateNullable(min: 10);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": {value} }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out var jsonValue).Should().BeTrue();
            jsonValue.Should().Be(value);
        }
        
        [Theory]
        [InlineData(9)]
        [InlineData(10)]
        public void SetNullableWithMaxValue(sbyte value)
        {
            var property = CreateNullable(max: 10);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": {value} }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out var jsonValue).Should().BeTrue();
            jsonValue.Should().Be(value);
        }
        
        [Fact]
        public void SetNullableWithMultipleOfValue()
        {
            var property = CreateNullable(multipleOf: 2);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": 10 }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out var jsonValue).Should().BeTrue();
            jsonValue.Should().Be(10);
        }
        
        [Theory]
        [InlineData(typeof(bool))]
        [InlineData(typeof(string))]
        public void TrySetNullableWitInvalidValue(Type type)
        {
            var value = type == typeof(bool) ? _fixture.Create<bool>().ToString().ToLower() : $@"""{_fixture.Create<string>()}""";
            var property = CreateNullable();
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": {value} }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out _).Should().BeFalse();
        }
        
        [Fact]
        public void TrySetNullableWitInvalidValueAndNotHaveValueInEnum()
        {
            var values = _fixture.Create<sbyte[]>();
            var property = CreateNullable(values);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": {_fixture.Create<sbyte>()} }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out _).Should().BeFalse();
        }
        
        [Theory]
        [InlineData(8)]
        [InlineData(9)]
        public void TrySetNullableWithMinValue(int param)
        {
            var value = Convert.ToSByte(param);
            var property = CreateNullable(min: 10);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": {value} }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out _).Should().BeFalse();
        }
        
        [Theory]
        [InlineData(12)]
        [InlineData(11)]
        public void TrySetNullableWithMaxValue(int param)
        {
            var value = Convert.ToSByte(param);
            var property = CreateNullable(max: 10);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": {value} }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out _).Should().BeFalse();
        }
        
        [Fact]
        public void TrySetNullableWithMultipleOfValue()
        {
            var property = CreateNullable(multipleOf: 2);
            var jsonElement = JsonSerializer.Deserialize<JsonElement>($@"{{ ""input"": 9 }}");
            property.TryGetValue(jsonElement.GetProperty("input"), out _).Should().BeFalse();
        }
        #endregion
    }
}
