using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Mozilla.IoT.WebThing.Attributes;
using Mozilla.IoT.WebThing.Factories.Generator.Intercepts;
using Mozilla.IoT.WebThing.Mapper;

namespace Mozilla.IoT.WebThing.Factories.Generator.Properties
{
    internal class PropertiesPropertyIntercept : IPropertyIntercept
    {
        private readonly JsonSerializerOptions _options;
        public Dictionary<string, Property> Properties { get; } = new Dictionary<string, Property>();

        public PropertiesPropertyIntercept(JsonSerializerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void Before(Thing thing)
        {

        }

        public void Intercept(Thing thing, PropertyInfo propertyInfo, ThingPropertyAttribute? thingPropertyAttribute)
        {
            var propertyName =  thingPropertyAttribute?.Name ?? _options.GetPropertyName(propertyInfo.Name);
            Properties.Add(propertyName, new Property(GetGetMethod(propertyInfo),
                GetSetMethod(propertyInfo),
                CreateValidator(propertyInfo, thingPropertyAttribute),
                CreateMapper(propertyInfo.PropertyType)));
        }
        
        private static Func<object, object> GetGetMethod(PropertyInfo property)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var instanceCast = property.DeclaringType.IsValueType ? 
                Expression.Convert(instance, property.DeclaringType) : Expression.TypeAs(instance, property.DeclaringType);
            
            var call = Expression.Call(instanceCast, property.GetGetMethod());
            var typeAs = Expression.TypeAs(call, typeof(object));

            return Expression.Lambda<Func<object, object>>(typeAs, instance).Compile();
        }
        
        private static Action<object, object> GetSetMethod(PropertyInfo property)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var value = Expression.Parameter(typeof(object), "value");

            // value as T is slightly faster than (T)value, so if it's not a value type, use that
            var instanceCast = property.DeclaringType.IsValueType ? 
                Expression.Convert(instance, property.DeclaringType) : Expression.TypeAs(instance, property.DeclaringType);
            
            var valueCast = property.PropertyType.IsValueType ? 
                Expression.Convert(value, property.PropertyType) : Expression.TypeAs(value, property.PropertyType);

            var call = Expression.Call(instanceCast, property.GetSetMethod(), valueCast);
            return Expression.Lambda<Action<object, object>>(call, new[] {instance, value}).Compile();
        }

        private static IPropertyValidator CreateValidator(PropertyInfo propertyInfo, ThingPropertyAttribute? thingPropertyAttribute)
        {
            return new PropertyValidator(
                thingPropertyAttribute?.IsReadOnly ?? !propertyInfo.CanWrite,
                thingPropertyAttribute?.MinimumValue,
                thingPropertyAttribute?.MaximumValue,
                thingPropertyAttribute?.MultipleOfValue,
                thingPropertyAttribute?.Enum);
        }

        private static IJsonMapper CreateMapper(Type type)
        {
            if (type == typeof(string))
            {
                return StringJsonMapper.Instance;
            }

            if(type == typeof(bool))
            {
                return BoolJsonMapper.Instance;
            }

            if (type == typeof(int))
            {
                return IntJsonMapper.Instance;
            }
            
            if (type == typeof(uint))
            {
                return UIntJsonMapper.Instance;
            }

            if (type == typeof(long))
            {
                return LongJsonMapper.Instance;
            }

            if (type == typeof(ulong))
            {
                return ULongJsonMapper.Instance;
            }

            if (type == typeof(short))
            {
                return LongJsonMapper.Instance;
            }

            if (type == typeof(ushort))
            {
                return ULongJsonMapper.Instance;
            }
            
            if (type == typeof(double))
            {
                return DoubleJsonMapper.Instance;
            }

            if (type == typeof(float))
            {
                return FloatJsonMapper.Instance;
            }
            
            if (type == typeof(byte))
            {
                return ByteJsonMapper.Instance;
            }
            
            if (type == typeof(sbyte))
            {
                return SByteJsonMapper.Instance;
            }
            
            if (type == typeof(decimal))
            {
                return DecimalJsonMapper.Instance;
            }
            
            if (type == typeof(DateTime))
            {
                return DateTimeJsonMapper.Instance;
            }
            
            return null;
        }
        
        public void After(Thing thing)
        {
        }
    }
}
