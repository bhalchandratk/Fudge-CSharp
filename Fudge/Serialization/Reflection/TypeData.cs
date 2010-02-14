﻿/* <!--
 * Copyright (C) 2009 - 2010 by OpenGamma Inc. and other contributors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 *     
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * -->
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Fudge.Serialization.Reflection
{
    public class TypeData
    {
        private readonly TypeData subType;
        private readonly FudgeFieldType fieldType;

        public TypeData(FudgeContext context, TypeDataCache cache, Type type, FudgeFieldNameConvention fieldNameConvention)
        {
            Type = type;
            DefaultConstructor = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            Constructors = type.GetConstructors();
            CustomAttributes = type.GetCustomAttributes(true);

            cache.RegisterTypeData(this);

            fieldNameConvention = OverrideFieldNameConvention(type, fieldNameConvention);
            Kind = CalcKind(context, cache, type, fieldNameConvention, out subType, out fieldType);
            ScanProperties(context, cache, fieldNameConvention);

            PublicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            StaticPublicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
        }

        public Type Type { get; private set; }
        public ConstructorInfo DefaultConstructor { get; private set; }
        public ConstructorInfo[] Constructors { get; private set; }
        public PropertyData[] Properties { get; private set; }
        public object[] CustomAttributes { get; private set; }
        public TypeKind Kind { get; private set; }
        public TypeData SubTypeData { get { return subType; } }                      // For lists and arrays, this is the type of the elements in the list
        public Type SubType { get { return subType == null ? null : subType.Type; } }
        public FudgeFieldType FieldType { get { return fieldType; } }                   // If the Kind is primitive
        public MethodInfo[] PublicMethods { get; private set; }
        public MethodInfo[] StaticPublicMethods { get; private set; }

        public enum TypeKind
        {
            FudgePrimitive,
            List,
            Object
        }

        private static FudgeFieldNameConvention OverrideFieldNameConvention(Type type, FudgeFieldNameConvention fieldNameConvention)
        {
            var attribs = type.GetCustomAttributes(typeof(FudgeFieldNameConventionAttribute), true);
            if (attribs.Length > 0)
                fieldNameConvention = ((FudgeFieldNameConventionAttribute)attribs[0]).Convention;
            return fieldNameConvention;
        }

        private static TypeKind CalcKind(FudgeContext context, TypeDataCache typeCache, Type type, FudgeFieldNameConvention fieldNameConvention, out TypeData subType, out FudgeFieldType fieldType)
        {
            subType = null;
            fieldType = context.TypeDictionary.GetByCSharpType(type);
            if (fieldType != null)
            {
                // Just a simple field
                return TypeKind.FudgePrimitive;
            }

            // Check for lists
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
            {
                // It's a list
                Type elementType = type.GetGenericArguments()[0];
                subType = typeCache.GetTypeData(elementType, fieldNameConvention);
                return TypeKind.List;
            }

            foreach (var interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    // It's a list
                    Type elementType = type.GetGenericArguments()[0];
                    subType = typeCache.GetTypeData(elementType, fieldNameConvention);
                    return TypeKind.List;
                }
            }

            return TypeKind.Object;
        }

        private void ScanProperties(FudgeContext context, TypeDataCache cache, FudgeFieldNameConvention fieldNameConvention)
        {
            var props = Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var list = from prop in props
                       where prop.GetCustomAttributes(typeof(FudgeTransientAttribute), true).Length == 0
                       select new PropertyData(cache, fieldNameConvention, prop);
            Properties = list.ToArray();
        }

        public sealed class PropertyData
        {
            private readonly PropertyInfo info;
            private readonly string name;
            private readonly bool hasPublicSetter;
            private string serializedName;
            private readonly TypeData typeData;

            public PropertyData(TypeDataCache typeCache, FudgeFieldNameConvention fieldNameConvention, PropertyInfo info)
            {
                this.info = info;
                this.name = info.Name;
                this.hasPublicSetter = (info.GetSetMethod() != null);   // Can't just use CanWrite as it may be non-public
                this.typeData = typeCache.GetTypeData(info.PropertyType, fieldNameConvention);

                var attribs = info.GetCustomAttributes(typeof(FudgeFieldNameAttribute), true);
                if (attribs.Length > 0)
                {
                    // Attribute takes priority over convention
                    this.serializedName = ((FudgeFieldNameAttribute)attribs[0]).Name;
                }
                else
                {
                    switch (fieldNameConvention)
                    {
                        case FudgeFieldNameConvention.Identity:
                            this.serializedName = info.Name;
                            break;
                        case FudgeFieldNameConvention.AllLowerCase:
                            this.serializedName = info.Name.ToLower();
                            break;
                        case FudgeFieldNameConvention.AllUpperCase:
                            this.serializedName = info.Name.ToUpper();
                            break;
                        case FudgeFieldNameConvention.CamelCase:
                            this.serializedName = info.Name.Substring(0, 1).ToLower() + info.Name.Substring(1);
                            break;
                        case FudgeFieldNameConvention.PascalCase:
                            this.serializedName = info.Name.Substring(0, 1).ToUpper() + info.Name.Substring(1);
                            break;
                        default:
                            throw new FudgeRuntimeException("Unknown FudgeFieldNameConvention: " + fieldNameConvention.ToString());
                    }
                }
            }

            public PropertyInfo Info { get { return info; } }
            public string Name { get { return name; } }
            public TypeData TypeData { get { return typeData; } }
            public Type Type { get { return typeData.Type; } }
            public TypeKind Kind { get { return typeData.Kind; } }
            public bool HasPublicSetter { get { return hasPublicSetter; } }
            public string SerializedName
            {
                get { return serializedName; }
                set { serializedName = value; }
            }
        }
    }
}
