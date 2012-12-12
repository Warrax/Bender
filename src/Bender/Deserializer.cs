﻿using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Bender
{
    public class UnmatchedElementException : Exception { public UnmatchedElementException(XElement element) : base(element.GetXPath()) {} }

    public class Deserializer
    {
        private readonly Options _options;

        public Deserializer(Options options)
        {
            _options = options;
        }

        public static Deserializer Create(Action<DeserializerOptions> configure = null)
        {
            var options = new Options();
            if (configure != null) configure(new DeserializerOptions(options));
            return new Deserializer(options);
        }

        public T DeserializeFile<T>(string path)
        {
            return (T)DeserializeFile(typeof(T), path);
        }

        public object DeserializeFile(Type type, string path)
        {
            return Deserialize(type, XDocument.Load(path));
        }

        public T Deserialize<T>(Stream stream)
        {
            return (T)Deserialize(typeof(T), stream);
        }

        public object Deserialize(Type type, Stream stream)
        {
            return Deserialize(type, XDocument.Load(stream));
        }

        public T Deserialize<T>(string source)
        {
            return (T)Deserialize(typeof(T), source);
        }

        public object Deserialize(Type type, string source)
        {
            return Deserialize(type, XDocument.Parse(source));
        }

        public object Deserialize<T>(XDocument document)
        {
            return Deserialize(typeof (T), document);
        }

        public object Deserialize(Type type, XDocument document)
        {
            var instance = Activator.CreateInstance(type);
            ValidateTypeElementName(type, document.Root);
            Traverse(instance, document.Root);
            return instance;
        }

        private void Traverse(object @object, XElement element)
        {
            if (@object.GetType().IsList())
            {
                var type = @object.GetType().GetListType();
                foreach (var itemElement in element.Elements())
                {
                    ValidateTypeElementName(type, itemElement);
                    var item = Activator.CreateInstance(type);
                    Traverse(item, itemElement);
                    ((IList)@object).Add(item);
                }
                return;
            }
            var properties = @object.GetType().GetSerializableProperties()
                .ToDictionary(x => x.GetXmlName(), x => x);
            foreach (var propertyElement in element.Elements())
            {
                if (!properties.ContainsKey(propertyElement.Name.LocalName))
                {
                    if (_options.IgnoreUnmatchedElements) continue;
                    throw new UnmatchedElementException(propertyElement);
                }
                var property = properties[propertyElement.Name.LocalName];
                var propertyType = property.PropertyType;
                if (_options.ExcludedTypes.Any(x => x(propertyType)) || 
                    property.HasCustomAttribute<XmlIgnoreAttribute>()) continue;

                if (_options.Readers.ContainsKey(propertyType)) 
                    property.SetValue(@object, _options.Readers[propertyType](_options, property, propertyElement), null);
                else if (propertyType.IsPrimitive || propertyType.IsValueType || propertyType == typeof(string))
                    property.SetValue(@object, propertyElement.Value.Parse(propertyType, _options.DefaultNonNullableTypesWhenEmpty), null);
                else if (propertyType == typeof(object)) property.SetValue(@object, propertyElement, null);
                else
                {
                    var propertyValue = Activator.CreateInstance(propertyType);
                    property.SetValue(@object, propertyValue, null);
                    Traverse(propertyValue, propertyElement);
                }
            }
        }

        private void ValidateTypeElementName(Type type, XElement element)
        {
            if (!_options.IgnoreTypeElementNames &&
                type.GetXmlName(_options.DefaultGenericListNameFormat,
                        _options.DefaultGenericTypeNameFormat) != element.Name.LocalName)
                throw new UnmatchedElementException(element);
        }
    }
}