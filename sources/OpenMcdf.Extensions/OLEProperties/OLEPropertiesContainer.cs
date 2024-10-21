﻿using OpenMcdf.Extensions.OLEProperties.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenMcdf.Extensions.OLEProperties
{
    public class OLEPropertiesContainer
    {
        public Dictionary<uint, string> PropertyNames;

        public OLEPropertiesContainer UserDefinedProperties { get; private set; }

        public bool HasUserDefinedProperties { get; private set; }

        public ContainerType ContainerType { get; }
        private Guid? FmtID0 { get; }

        public PropertyContext Context { get; }

        private readonly List<OLEProperty> properties = new();
        internal CFStream cfStream;

        /*
         Property name	Property ID	PID	Type
        Codepage	PID_CODEPAGE	1	VT_I2
        Title	PID_TITLE	2	VT_LPSTR
        Subject	PID_SUBJECT	3	VT_LPSTR
        Author	PID_AUTHOR	4	VT_LPSTR
        Keywords	PID_KEYWORDS	5	VT_LPSTR
        Comments	PID_COMMENTS	6	VT_LPSTR
        Template	PID_TEMPLATE	7	VT_LPSTR
        Last Saved By	PID_LASTAUTHOR	8	VT_LPSTR
        Revision Number	PID_REVNUMBER	9	VT_LPSTR
        Last Printed	PID_LASTPRINTED	11	VT_FILETIME
        Create Time/Date	PID_CREATE_DTM	12	VT_FILETIME
        Last Save Time/Date	PID_LASTSAVE_DTM	13	VT_FILETIME
        Page Count	PID_PAGECOUNT	14	VT_I4
        Word Count	PID_WORDCOUNT	15	VT_I4
        Character Count	PID_CHARCOUNT	16	VT_I4
        Creating Application	PID_APPNAME	18	VT_LPSTR
        Security	PID_SECURITY	19	VT_I4
             */
        public class SummaryInfoProperties
        {
            public short CodePage { get; set; }
            public string Title { get; set; }
            public string Subject { get; set; }
            public string Author { get; set; }
            public string KeyWords { get; set; }
            public string Comments { get; set; }
            public string Template { get; set; }
            public string LastSavedBy { get; set; }
            public string RevisionNumber { get; set; }
            public DateTime LastPrinted { get; set; }
            public DateTime CreateTime { get; set; }
            public DateTime LastSavedTime { get; set; }
            public int PageCount { get; set; }
            public int WordCount { get; set; }
            public int CharacterCount { get; set; }
            public string CreatingApplication { get; set; }
            public int Security { get; set; }
        }

        public static OLEPropertiesContainer CreateNewSummaryInfo(SummaryInfoProperties sumInfoProps)
        {
            return null;
        }

        public OLEPropertiesContainer(int codePage, ContainerType containerType)
        {
            Context = new PropertyContext
            {
                CodePage = codePage,
                Behavior = Behavior.CaseInsensitive
            };

            ContainerType = containerType;
        }

        internal OLEPropertiesContainer(CFStream cfStream)
        {
            PropertySetStream pStream = new PropertySetStream();

            this.cfStream = cfStream;

            using StreamDecorator stream = new(cfStream);
            using BinaryReader reader = new(stream);
            pStream.Read(reader);

            ContainerType = pStream.FMTID0.ToString("B").ToUpperInvariant() switch
            {
                WellKnownFMTID.FMTID_SummaryInformation => ContainerType.SummaryInfo,
                WellKnownFMTID.FMTID_DocSummaryInformation => ContainerType.DocumentSummaryInfo,
                _ => ContainerType.AppSpecific,
            };
            FmtID0 = pStream.FMTID0;

            PropertyNames = (Dictionary<uint, string>)pStream.PropertySet0.Properties
                .FirstOrDefault(p => p.PropertyType == PropertyType.DictionaryProperty)?.Value;

            Context = new PropertyContext()
            {
                CodePage = pStream.PropertySet0.PropertyContext.CodePage
            };

            for (int i = 0; i < pStream.PropertySet0.Properties.Count; i++)
            {
                if (pStream.PropertySet0.PropertyIdentifierAndOffsets[i].PropertyIdentifier == 0) continue;
                //if (pStream.PropertySet0.PropertyIdentifierAndOffsets[i].PropertyIdentifier == 1) continue;
                //if (pStream.PropertySet0.PropertyIdentifierAndOffsets[i].PropertyIdentifier == 0x80000000) continue;

                var p = (ITypedPropertyValue)pStream.PropertySet0.Properties[i];
                var poi = pStream.PropertySet0.PropertyIdentifierAndOffsets[i];

                var op = new OLEProperty(this)
                {
                    VTType = p.VTType,
                    PropertyIdentifier = pStream.PropertySet0.PropertyIdentifierAndOffsets[i].PropertyIdentifier,
                    Value = p.Value
                };

                properties.Add(op);
            }

            if (pStream.NumPropertySets == 2)
            {
                UserDefinedProperties = new OLEPropertiesContainer(pStream.PropertySet1.PropertyContext.CodePage, ContainerType.UserDefinedProperties);
                HasUserDefinedProperties = true;

                for (int i = 0; i < pStream.PropertySet1.Properties.Count; i++)
                {
                    if (pStream.PropertySet1.PropertyIdentifierAndOffsets[i].PropertyIdentifier == 0) continue;
                    //if (pStream.PropertySet1.PropertyIdentifierAndOffsets[i].PropertyIdentifier == 1) continue;
                    if (pStream.PropertySet1.PropertyIdentifierAndOffsets[i].PropertyIdentifier == 0x80000000) continue;

                    var p = (ITypedPropertyValue)pStream.PropertySet1.Properties[i];
                    var poi = pStream.PropertySet1.PropertyIdentifierAndOffsets[i];

                    var op = new OLEProperty(UserDefinedProperties)
                    {
                        VTType = p.VTType,
                        PropertyIdentifier = pStream.PropertySet1.PropertyIdentifierAndOffsets[i].PropertyIdentifier,
                        Value = p.Value
                    };

                    UserDefinedProperties.properties.Add(op);
                }

                var existingPropertyNames = (Dictionary<uint, string>)pStream.PropertySet1.Properties
                    .FirstOrDefault(p => p.PropertyType == PropertyType.DictionaryProperty)?.Value;

                UserDefinedProperties.PropertyNames = existingPropertyNames ?? new Dictionary<uint, string>();
            }
        }

        public IEnumerable<OLEProperty> Properties => properties;

        public OLEProperty NewProperty(VTPropertyType vtPropertyType, uint propertyIdentifier, string propertyName = null)
        {
            //throw new NotImplementedException("API Unstable - Work in progress - Milestone 2.3.0.0");
            var op = new OLEProperty(this)
            {
                VTType = vtPropertyType,
                PropertyIdentifier = propertyIdentifier
            };

            return op;
        }

        public void AddProperty(OLEProperty property)
        {
            //throw new NotImplementedException("API Unstable - Work in progress - Milestone 2.3.0.0");
            properties.Add(property);
        }

        /// <summary>
        /// Create a new UserDefinedProperty.
        /// </summary>
        /// <param name="vtPropertyType">The type of property to create.</param>
        /// <param name="name">The name of the new property.</param>
        /// <returns>The new property.</returns>
        /// <exception cref="InvalidOperationException">If UserDefinedProperties aren't allowed for this container.</exception>
        /// <exception cref="ArgumentException">If a property with the name <paramref name="name"/> already exists."/></exception>
        public OLEProperty AddUserDefinedProperty(VTPropertyType vtPropertyType, string name)
        {
            // @@TBD@@ If this is a DocumentSummaryInfo container, we could forward the add on to that.
            if (this.ContainerType != ContainerType.UserDefinedProperties)
            {
                throw new InvalidOperationException($"UserDefinedProperties are not allowed in containers of type {this.ContainerType}");
            }

            // As per https://learn.microsoft.com/en-us/openspecs/windows_protocols/MS-OLEPS/4177a4bc-5547-49fe-a4d9-4767350fd9cf
            // the property names have to be unique, and are case insensitive.
            if (this.PropertyNames.Any(property => property.Value.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new ArgumentException($"User defined property names must be unique and {name} already exists", nameof(name));
            }

            // Work out a property identifier - must be > 1 and unique as per 
            // https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-oleps/333959a3-a999-4eca-8627-48a224e63e77
            uint identifier = 2;

            if (this.PropertyNames.Count > 0)
            {
                uint highestIdentifier = this.PropertyNames.Keys.Max();
                identifier = Math.Max(highestIdentifier, 2) + 1;
            }

            this.PropertyNames[identifier] = name;

            var op = new OLEProperty(this)
            {
                VTType = vtPropertyType,
                PropertyIdentifier = identifier
            };

            properties.Add(op);

            return op;
        }

        public void RemoveProperty(uint propertyIdentifier)
        {
            //throw new NotImplementedException("API Unstable - Work in progress - Milestone 2.3.0.0");
            var toRemove = properties.FirstOrDefault(o => o.PropertyIdentifier == propertyIdentifier);

            if (toRemove != null)
                properties.Remove(toRemove);
        }

        /// <summary>
        /// Create a new UserDefinedProperties container within this container.
        /// </summary>
        /// <remarks>
        /// Only containers of type DocumentSummaryInfo can contain user defined properties.
        /// </remarks>
        /// <param name="codePage">The code page to use for the user defined properties.</param>
        /// <returns>The UserDefinedProperties container.</returns>
        /// <exception cref="CFInvalidOperation">If this container is a type that doesn't suppose user defined properties.</exception>
        public OLEPropertiesContainer CreateUserDefinedProperties(int codePage)
        {
            // Only the DocumentSummaryInfo stream can contain a UserDefinedProperties
            if (ContainerType != ContainerType.DocumentSummaryInfo)
            {
                throw new CFInvalidOperation($"Only a DocumentSummaryInfo can contain user defined properties. Current container type is {ContainerType}");
            }

            // Create the container, and add the codepage to the initial set of properties
            UserDefinedProperties = new OLEPropertiesContainer(codePage, ContainerType.UserDefinedProperties)
            {
                PropertyNames = new Dictionary<uint, string>()
            };

            var op = new OLEProperty(UserDefinedProperties)
            {
                VTType = VTPropertyType.VT_I2,
                PropertyIdentifier = 1,
                Value = (short)codePage
            };

            UserDefinedProperties.properties.Add(op);
            HasUserDefinedProperties = true;

            return UserDefinedProperties;
        }

        public void Save(CFStream cfStream)
        {
            //throw new NotImplementedException("API Unstable - Work in progress - Milestone 2.3.0.0");
            //properties.Sort((a, b) => a.PropertyIdentifier.CompareTo(b.PropertyIdentifier));

            using StreamDecorator s = new(cfStream);
            using BinaryWriter bw = new BinaryWriter(s);

            Guid fmtId0 = FmtID0 ?? (ContainerType == ContainerType.SummaryInfo ? new Guid(WellKnownFMTID.FMTID_SummaryInformation) : new Guid(WellKnownFMTID.FMTID_DocSummaryInformation));

            PropertySetStream ps = new PropertySetStream
            {
                ByteOrder = 0xFFFE,
                Version = 0,
                SystemIdentifier = 0x00020006,
                CLSID = Guid.Empty,

                NumPropertySets = 1,

                FMTID0 = fmtId0,
                Offset0 = 0,

                FMTID1 = Guid.Empty,
                Offset1 = 0,

                PropertySet0 = new PropertySet
                {
                    NumProperties = (uint)Properties.Count(),
                    PropertyIdentifierAndOffsets = new List<PropertyIdentifierAndOffset>(),
                    Properties = new List<IProperty>(),
                    PropertyContext = Context
                }
            };

            // If we're writing an AppSpecific property set and have property names, then add a dictionary property
            if (ContainerType == ContainerType.AppSpecific && PropertyNames != null && PropertyNames.Count > 0)
            {
                AddDictionaryPropertyToPropertySet(PropertyNames, ps.PropertySet0);
                ps.PropertySet0.NumProperties += 1;
            }

            PropertyFactory factory =
                ContainerType == ContainerType.DocumentSummaryInfo ? DocumentSummaryInfoPropertyFactory.Instance : DefaultPropertyFactory.Instance;

            foreach (var op in Properties)
            {
                ITypedPropertyValue p = factory.NewProperty(op.VTType, Context.CodePage, op.PropertyIdentifier);
                p.Value = op.Value;
                ps.PropertySet0.Properties.Add(p);
                ps.PropertySet0.PropertyIdentifierAndOffsets.Add(new PropertyIdentifierAndOffset() { PropertyIdentifier = op.PropertyIdentifier, Offset = 0 });
            }

            if (HasUserDefinedProperties)
            {
                ps.NumPropertySets = 2;

                ps.PropertySet1 = new PropertySet
                {
                    // Number of user defined properties, plus 1 for the name dictionary
                    NumProperties = (uint)UserDefinedProperties.Properties.Count() + 1,
                    PropertyIdentifierAndOffsets = new List<PropertyIdentifierAndOffset>(),
                    Properties = new List<IProperty>(),
                    PropertyContext = UserDefinedProperties.Context
                };

                ps.FMTID1 = new Guid(WellKnownFMTID.FMTID_UserDefinedProperties);
                ps.Offset1 = 0;

                // Add the dictionary containing the property names
                AddDictionaryPropertyToPropertySet(UserDefinedProperties.PropertyNames, ps.PropertySet1);

                // Add the properties themselves
                foreach (var op in UserDefinedProperties.Properties)
                {
                    ITypedPropertyValue p = DefaultPropertyFactory.Instance.NewProperty(op.VTType, ps.PropertySet1.PropertyContext.CodePage, op.PropertyIdentifier);
                    p.Value = op.Value;
                    ps.PropertySet1.Properties.Add(p);
                    ps.PropertySet1.PropertyIdentifierAndOffsets.Add(new PropertyIdentifierAndOffset() { PropertyIdentifier = op.PropertyIdentifier, Offset = 0 });
                }
            }

            ps.Write(bw);
        }

        private static void AddDictionaryPropertyToPropertySet(Dictionary<uint, string> propertyNames, PropertySet propertySet)
        {
            IDictionaryProperty dictionaryProperty = new DictionaryProperty(propertySet.PropertyContext.CodePage)
            {
                Value = propertyNames
            };
            propertySet.Properties.Add(dictionaryProperty);
            propertySet.PropertyIdentifierAndOffsets.Add(new PropertyIdentifierAndOffset() { PropertyIdentifier = 0, Offset = 0 });
        }
    }
}
