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

    type DynamicTableEntity with
        member this.ToJson () =
            let props =
                this.Properties
                |> Seq.map (fun kvp -> kvp.Key, (PropertyValue.OfEntityProperty kvp.Value))
                |> dict
            System.Text.Json.JsonSerializer.Serialize (props, System.Text.Json.JsonSerializerOptions (IgnoreNullValues=true))
        member this.LoadJson (json:string) =
            let (dictionary:System.Collections.Generic.Dictionary<string, PropertyValue>) = System.Text.Json.JsonSerializer.Deserialize json
            for kvp in dictionary do
                this.Properties.[kvp.Key] <- kvp.Value |> PropertyValue.AsEntityProperty
            this
