//---------------------------------------------------------------------
// This code was automatically generated by Linq2GraphQL
// Please don't edit this file
// Github:https://github.com/linq2graphql/linq2graphql.client
// Url: https://linq2graphql.com
//---------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Linq2GraphQL.Client;
using Linq2GraphQL.Client.Converters;

namespace StartGG.Client;

public static class ActionSetExtentions
{


    [GraphInterface]
    public static TeamActionSet TeamActionSet(this ActionSet value)
    {
        if (value.__TypeName == "TeamActionSet")
        {
            return (TeamActionSet)value;
        }
        return null;
    }
}


internal class ActionSetConverter : InterfaceJsonConverter<ActionSet>
{
    public override ActionSet Deserialize(string typeName, JsonObject json) => typeName switch
    {
          "TeamActionSet" => json.Deserialize<TeamActionSet>(),
        _ => json.Deserialize< ActionSet__Concrete>()
    };
}




[JsonConverter(typeof(ActionSetConverter))]
public interface ActionSet 
{
	[GraphQLMember("id")]
	public ID Id { get; set; }  
    [GraphQLMember("__typename")]
    public string __TypeName { get; set; }

}

internal class ActionSet__Concrete : ActionSet
{
	[GraphQLMember("id")]
	public ID Id { get; set; }  

    [GraphQLMember("__typename")]
    public string __TypeName { get; set; }

}