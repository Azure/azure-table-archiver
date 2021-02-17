// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
module EntitySerializationTests

open System
open Expecto
open Microsoft.Azure.Cosmos.Table
open AzureTableArchiver
open EntitySerialization

[<Tests>]
let entityPropertyTests =
    testList "Building Entity Property" [
        test "Check Defaults" {
            let defaultPropVal = PropertyValue.Default
            Expect.isNull defaultPropVal.BinaryValue "BinaryValue wasn't null"
            Expect.isFalse defaultPropVal.BooleanValue.HasValue "BooleanValue had a value"
            Expect.isFalse defaultPropVal.DoubleValue.HasValue "DoubleValue had a value"
            Expect.isFalse defaultPropVal.GuidValue.HasValue "GuidValue had a value"
            Expect.isFalse defaultPropVal.Int32Value.HasValue "Int32Value had a value"
            Expect.isFalse defaultPropVal.Int64Value.HasValue "Int64Value had a value"
            Expect.isNull defaultPropVal.StringValue "StringValue wasn't null"
            Expect.isFalse defaultPropVal.DateTimeValue.HasValue "DateTimeValue had a value"
            Expect.equal defaultPropVal.EdmType EdmType.String "Default EdmType should be a string"
        }
        test "Creates Binary EntityProperty" {
            let prop =
                { PropertyValue.Default with BinaryValue = [| 0uy; 1uy; 2uy; 3uy |]; EdmType = EdmType.Binary }
                |> PropertyValue.AsEntityProperty
            Expect.sequenceEqual prop.BinaryValue [| 0uy; 1uy; 2uy; 3uy |] "Expecting a byte array value on the EntityProperty"
            Expect.equal prop.PropertyType EdmType.Binary "Incorrect PropertyType"
        }
        test "Creates Boolean EntityProperty" {
            let prop =
                { PropertyValue.Default with BooleanValue = Nullable(true); EdmType = EdmType.Boolean }
                |> PropertyValue.AsEntityProperty
            Expect.isTrue prop.BooleanValue.HasValue "BooleanValue should have a value"
            Expect.isTrue prop.BooleanValue.Value "Expecting a true boolean value the EntityProperty"
            Expect.equal prop.PropertyType EdmType.Boolean "Incorrect PropertyType"
        }
        test "Creates Double EntityProperty" {
            let prop =
                { PropertyValue.Default with DoubleValue = Nullable(0.3); EdmType = EdmType.Double }
                |> PropertyValue.AsEntityProperty
            Expect.isTrue prop.DoubleValue.HasValue "DoubleValue should have a value"
            Expect.equal prop.DoubleValue.Value 0.3 "Expecting a double value value on the EntityProperty"
            Expect.equal prop.PropertyType EdmType.Double "Incorrect PropertyType"
        }
        test "Creates Guid EntityProperty" {
            let guid = Guid("5e020cd0-cfd7-4416-b8cf-d8f57e15a9b3")
            let prop =
                { PropertyValue.Default with GuidValue = Nullable(guid); EdmType = EdmType.Guid }
                |> PropertyValue.AsEntityProperty
            Expect.isTrue prop.GuidValue.HasValue "GuidValue should have a value"
            Expect.equal prop.GuidValue.Value guid "Expecting a guid value on the EntityProperty"
            Expect.equal prop.PropertyType EdmType.Guid "Incorrect PropertyType"
        }
        test "Creates Int32 EntityProperty" {
            let prop =
                { PropertyValue.Default with Int32Value = Nullable(1234); EdmType = EdmType.Int32 }
                |> PropertyValue.AsEntityProperty
            Expect.isTrue prop.Int32Value.HasValue "Int32Value should have a value"
            Expect.equal prop.Int32Value.Value 1234 "Expecting an int32 value on the EntityProperty"
            Expect.equal prop.PropertyType EdmType.Int32 "Incorrect PropertyType"
        }
        test "Creates Int64 EntityProperty" {
            let prop =
                { PropertyValue.Default with Int64Value = Nullable(1234L); EdmType = EdmType.Int64 }
                |> PropertyValue.AsEntityProperty
            Expect.isTrue prop.Int64Value.HasValue "Int64Value should have a value"
            Expect.equal prop.Int64Value.Value 1234L "Expecting an int64 value on the EntityProperty"
            Expect.equal prop.PropertyType EdmType.Int64 "Incorrect PropertyType"
        }
        test "Creates String EntityProperty" {
            let prop =
                { PropertyValue.Default with StringValue = "abcdefg"; EdmType = EdmType.String }
                |> PropertyValue.AsEntityProperty
            Expect.equal prop.StringValue "abcdefg" "Expecting a string value on the EntityProperty"
            Expect.equal prop.PropertyType EdmType.String "Incorrect PropertyType"
        }
        test "Creates DateTime EntityProperty" {
            let now = DateTime.Now
            let prop =
                { PropertyValue.Default with DateTimeValue = Nullable(now); EdmType = EdmType.DateTime }
                |> PropertyValue.AsEntityProperty
            Expect.isTrue prop.DateTime.HasValue "DateTime should have a value"
            Expect.equal prop.DateTime.Value now "Expecting a datetime value on the EntityProperty"
            Expect.equal prop.PropertyType EdmType.DateTime "Incorrect PropertyType"
        }
    ]

[<Tests>]
let propertyValueTests =
    testList "Building PropertyValue" [
        test "Creates Binary PropertyValue" {
            let prop = EntityProperty([|0uy; 1uy; 2uy; 3uy|]) |> PropertyValue.OfEntityProperty
            Expect.sequenceEqual [| 0uy; 1uy; 2uy; 3uy |] prop.BinaryValue "Expecting a byte array value on the PropertyValue"
            Expect.equal prop.EdmType EdmType.Binary "Incorrect EdmType"
        }
        test "Creates Boolean PropertyValue" {
            let prop = EntityProperty(true) |> PropertyValue.OfEntityProperty
            Expect.equal true prop.BooleanValue.Value "Expecting a boolean true on the PropertyValue"
            Expect.equal prop.EdmType EdmType.Boolean "Incorrect EdmType"
        }
        test "Creates Double PropertyValue" {
            let prop = EntityProperty(1.23) |> PropertyValue.OfEntityProperty
            Expect.equal 1.23 prop.DoubleValue.Value "Expecting a double value on the PropertyValue"
            Expect.equal prop.EdmType EdmType.Double "Incorrect EdmType"
        }
        test "Creates Guid PropertyValue" {
            let guid = Guid("93a7a7e7-9fef-4ad0-a4e4-b77889738d29")
            let prop = EntityProperty guid |> PropertyValue.OfEntityProperty
            Expect.equal guid prop.GuidValue.Value "Expecting a GUID value on the PropertyValue"
            Expect.equal prop.EdmType EdmType.Guid "Incorrect EdmType"
        }
        test "Creates Int32 PropertyValue" {
            let prop = EntityProperty(123) |> PropertyValue.OfEntityProperty
            Expect.equal 123 prop.Int32Value.Value "Expecting an int32 value on the PropertyValue"
            Expect.equal prop.EdmType EdmType.Int32 "Incorrect EdmType"
        }
        test "Creates Int64 PropertyValue" {
            let prop = EntityProperty(1234L) |> PropertyValue.OfEntityProperty
            Expect.equal 1234L prop.Int64Value.Value "Expecting an int64 value on the PropertyValue"
            Expect.equal prop.EdmType EdmType.Int64 "Incorrect EdmType"
        }
        test "Creates String PropertyValue" {
            let prop = EntityProperty("Hello world") |> PropertyValue.OfEntityProperty
            Expect.equal "Hello world" prop.StringValue "Expecting a string value on the PropertyValue"
            Expect.equal prop.EdmType EdmType.String "Incorrect EdmType"
        }
        test "Creates DateTime PropertyValue" {
            let now = DateTime.Now
            let prop = EntityProperty(now) |> PropertyValue.OfEntityProperty
            Expect.equal now prop.DateTimeValue.Value "Expecting a datetime value on the PropertyValue"
            Expect.equal prop.EdmType EdmType.DateTime "Incorrect EdmType"
        }
    ]

[<Tests>]
let dynamicEntityJson =
    testList "DynamicEntity to/from JSON" [
        test "DynamicEntity serializes to JSON without nulls PropertyValue fields" {
            let entity = DynamicTableEntity("somepartition","somerow")
            entity.Properties <-
                  [ "Foo", EntityProperty "bar"
                    "Count", EntityProperty 123 ] |> dict
            let json = entity.ToJson ()
            Expect.isFalse (json.Contains "BooleanValue") "null field from PropertyValue was included in JSON payload"
            Expect.equal
                json
                """{"Foo":{"EdmType":0,"StringValue":"bar"},"Count":{"EdmType":6,"Int32Value":123}}"""
                "JSON serialization has incorrect structure."
        }
        test "DynamicEntity deserializes from JSON with correct EntityProperty types" {
            let entity =
                DynamicTableEntity("somepartition","somerow")
                    .LoadJson """{"Foo":{"EdmType":0,"StringValue":"bar"},"Count":{"EdmType":6,"Int32Value":123}}"""
            Expect.hasLength entity.Properties 2 "Incorrect number of properties after deserializing."
            Expect.equal entity.["Foo"].StringValue "bar" "Incorrect value for 'Foo' after deserialization"
            Expect.equal entity.["Count"].Int32Value.Value 123 "Incorrect value for 'Count' after deserialization"
        }
    ]
