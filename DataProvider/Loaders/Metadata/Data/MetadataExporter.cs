﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DataProvider.Loaders.Metadata.Data {
    public class MetadataExporter<T> : IMetadataExporter<T> where T : IMetadataItem {
        // ReSharper disable StaticFieldInGenericType - that's ok, it stores non-generic data from generic source
        private static readonly Dictionary<Type, RequestSetupBase> FieldCache;
        // ReSharper restore StaticFieldInGenericType

        static MetadataExporter() {
            FieldCache = new Dictionary<Type, RequestSetupBase>();
        }

        [Conditional("DEBUG")]
        private static void ValidateFields(IEnumerable<MetaFieldInfo> fieldInfo) {
            var metaFieldInfos = fieldInfo as MetaFieldInfo[] ?? fieldInfo.ToArray();
            var orders = metaFieldInfos.Select(f => f.Order).ToList();
            
            var min = orders.Min();
            if (min < 0)
                throw new InvalidDataException("min order < 0");
            
            var max = orders.Max();
            if (max >= metaFieldInfos.Count())
                throw new InvalidDataException("max order > total count");

            if (metaFieldInfos.Count() != metaFieldInfos.Distinct().Count())
                throw new InvalidDataException("order duplicates");
        }


        public RequestSetupBase GetMetaParams() {
            var type = typeof (T);
            if (FieldCache.ContainsKey(type)) return FieldCache[type];
            var result = Parse(type);
            ValidateFields(result.FieldInfo);
            FieldCache[type] = result;
            return result;
        }

        private static RequestSetupBase Parse(Type type) {
            var res = new RequestSetupBase();

            var attrs = type
                .GetCustomAttributes(typeof(MetaParamsAttribute), false)
                .OfType<MetaParamsAttribute>()
                .ToList();

            if (!attrs.Any())
                return null;

            var attr = attrs.First();
            res.DisplayMode = attr.Display ?? "";
            res.RequestMode = attr.Request ?? "";

            var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var fieldInfo = new List<MetaFieldInfo>();

            fieldInfo.AddRange(
                from property in allProperties
                let fieldAttrs = property
                    .GetCustomAttributes(typeof(MetaFieldAttribute), false)
                    .OfType<MetaFieldAttribute>()
                    .ToList()
                where fieldAttrs.Any()
                let fieldAttr = fieldAttrs.First()
                select new MetaFieldInfo {
                    MetaFieldName = fieldAttr.Name,
                    VariableType = property.PropertyType,
                    VariableName = property.Name,
                    Order = fieldAttr.Order,
                    Converter = fieldAttr.Converter
                });

            res.FieldInfo = fieldInfo.ToArray();

            return res;
        }
    }
}