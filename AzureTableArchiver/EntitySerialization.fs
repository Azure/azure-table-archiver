// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
namespace AzureTableArchiver

open System
open Microsoft.Azure.Cosmos.Table

module EntitySerialization =

    /// JSON serializable view of an EntityProperty.
    type PropertyValue =
        { EdmType : EdmType
          StringValue : string
          BinaryValue : byte array
          BooleanValue : Nullable<bool>
          DateTimeValue : Nullable<DateTime>
          DoubleValue : Nullable<Double>
          GuidValue : Nullable<Guid>
          Int32Value : Nullable<Int32>
          Int64Value : Nullable<Int64> }
    with
        /// Default record with every field set to default values - null / Nullable()
        static member Default =
            {
                EdmType = EdmType.String
                StringValue = null
                BinaryValue = null
                BooleanValue = Nullable ()
                DateTimeValue = Nullable ()
                DoubleValue = Nullable ()
                GuidValue = Nullable ()
                Int32Value = Nullable ()
                Int64Value = Nullable ()
            }
        /// Create an EntityProperty from a PropertyValue record.
        static member AsEntityProperty (propertyValue:PropertyValue) : EntityProperty =
            match propertyValue.EdmType with
            | EdmType.String ->
                EntityProperty(propertyValue.StringValue)
            | EdmType.Binary ->
                EntityProperty(propertyValue.BinaryValue)
            | EdmType.Boolean ->
                EntityProperty(propertyValue.BooleanValue)
            | EdmType.DateTime ->
                EntityProperty(propertyValue.DateTimeValue)
            | EdmType.Double ->
                EntityProperty(propertyValue.DoubleValue)
            | EdmType.Guid ->
                EntityProperty(propertyValue.GuidValue)
            | EdmType.Int32 ->
                EntityProperty(propertyValue.Int32Value)
            | EdmType.Int64 ->
                EntityProperty(propertyValue.Int64Value)
            | _ -> null
        /// Create a PropertyValue record from an EntityProperty.
        static member OfEntityProperty (entityProperty:EntityProperty) : PropertyValue =
            match entityProperty.PropertyType with
            | EdmType.String ->
                { PropertyValue.Default with EdmType = entityProperty.PropertyType; StringValue = entityProperty.StringValue }
            | EdmType.Binary ->
                { PropertyValue.Default with EdmType = entityProperty.PropertyType; BinaryValue = entityProperty.BinaryValue }
            | EdmType.Boolean ->
                { PropertyValue.Default with EdmType = entityProperty.PropertyType; BooleanValue = entityProperty.BooleanValue }
            | EdmType.DateTime ->
                { PropertyValue.Default with EdmType = entityProperty.PropertyType; DateTimeValue = entityProperty.DateTime }
            | EdmType.Double ->
                { PropertyValue.Default with EdmType = entityProperty.PropertyType; DoubleValue = entityProperty.DoubleValue }
            | EdmType.Guid ->
                { PropertyValue.Default with EdmType = entityProperty.PropertyType; GuidValue = entityProperty.GuidValue }
            | EdmType.Int32 ->
                { PropertyValue.Default with EdmType = entityProperty.PropertyType; Int32Value = entityProperty.Int32Value }
            | EdmType.Int64 ->
                { PropertyValue.Default with EdmType = entityProperty.PropertyType; Int64Value = entityProperty.Int64Value }
            | _ -> PropertyValue.Default

    /// Extension methods on DynamicTableEntity for converting to and from JSON
    type DynamicTableEntity with
        /// Converts this to a dictionary of PropertyValue records and serializes it to JSON.
        member this.ToJson () =
            let props =
                this.Properties
                |> Seq.map (fun kvp -> kvp.Key, (PropertyValue.OfEntityProperty kvp.Value))
                |> dict
            System.Text.Json.JsonSerializer.Serialize (props, System.Text.Json.JsonSerializerOptions (IgnoreNullValues=true))
        /// Parses JSON into a dictionary and PropertyValue records and converts each to EntityProperties on this entity.
        member this.LoadJson (json:string) =
            let (dictionary:System.Collections.Generic.Dictionary<string, PropertyValue>) = System.Text.Json.JsonSerializer.Deserialize json
            for kvp in dictionary do
                this.Properties.[kvp.Key] <- kvp.Value |> PropertyValue.AsEntityProperty
            this
