using System;
using Mozilla.IoT.WebThing.Exceptions;
using Mozilla.IoT.WebThing.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Mozilla.IoT.WebThing
{
    /// <summary>
    /// A Property represents an individual state value of a thing.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Property<T> : Property
    {
        public new event EventHandler<ValueChangedEventArgs<T>> ValuedChanged;

        public new T Value
        {
            get => (T)base.Value;
            set => base.Value = value;
        }

        public Property(Thing thing, string name, T value)
            : base(thing, name, value)
        {
        }


        public Property(Thing thing, string name, T value, JObject metadata)
            : base(thing, name, value, metadata)
        {
        }

        protected override void OnValueChanged()
        {
            ValuedChanged?.Invoke(this, new ValueChangedEventArgs<T>(Value));
        }
    }

    public class Property
    {
        private const string REL = "rel";
        private const string PROPERTY = "property";
        private const string HREF = "href";
        private const string LINKS = "links";
        private const string DEFAULT_PREFIX = "/";

        /// <summary>
        /// The href of this property
        /// </summary>
        public Thing Thing { get; }

        /// <summary>
        /// The name of this property
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The href of this property
        /// </summary>
        public string Href { get; }

        private string _hrefPreix;

        /// <summary>
        /// The prefix of any hrefs associated with this property.
        /// </summary>
        public string HrefPrefix
        {
            get => string.IsNullOrEmpty(_hrefPreix) ? DEFAULT_PREFIX : _hrefPreix;
            set
            {
                _hrefPreix = value;
                if (!_hrefPreix.EndsWith("/"))
                {
                    _hrefPreix += DEFAULT_PREFIX;
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public JObject Metadata { get; }

        protected JSchema Schema { get; }

        private object _value;

        public object Value
        {
            get => _value;
            set
            {
                ValidateValue(value);
                _value = value switch
                {
                    JValue jValue => jValue.Value,
                    _ => value
                };
                
                OnValueChanged();
            }
        }


        public event EventHandler<ValueChangedEventArgs> ValuedChanged;

        public Property(Thing thing, string name, object value)
            : this(thing, name, value, null)
        {
        }

        public Property(Thing thing, string name, object value, JObject metadata)
        {
            Thing = thing;
            Name = name;
            HrefPrefix = string.Empty;
            Href = $"properties/{name}";
            Metadata = metadata ?? new JObject();
            _value = value;
            Schema = JSchema.Load(Metadata.CreateReader());
        }

        /// <summary>
        /// Get the property description.
        /// </summary>
        /// <returns>Description of the property as an object</returns>
        public JObject AsPropertyDescription()
        {
            var description = new JObject(Metadata);
            var link = new JObject(
                new JProperty(REL, PROPERTY),
                new JProperty(HREF, HrefPrefix + Href));

            if (description.TryGetValue(LINKS, out JToken token))
            {
                if (token is JArray array)
                {
                    array.Add(link);
                }
                else
                {
                    throw new JsonException();
                }
            }
            else
            {
                description.Add(LINKS, new JArray(link));
            }

            return description;
        }

        protected virtual void ValidateValue(object value)
        {
            if (Schema.ReadOnly.HasValue && Schema.ReadOnly.Value)
            {
                throw new PropertyException($"readonly property {Name}");
            }

            if (!Schema.IsValid(value))
            {
                throw new PropertyException("Invalid property value");
            }
        }

        protected virtual void OnValueChanged()
        {
            ValuedChanged?.Invoke(this, new ValueChangedEventArgs(Value));
        }
    }

    public class ValueChangedEventArgs : EventArgs
    {
        public object Value { get; }

        public ValueChangedEventArgs(object value)
            => Value = value;
    }

    public class ValueChangedEventArgs<T> : EventArgs
    {
        public T Value { get; }

        public ValueChangedEventArgs(T value)
            => Value = value;
    }
}
