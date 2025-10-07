namespace Linq2GraphQL.TestServer.Models;

public enum CustomerStatus
{
    Active = 0,
    Disabled = 1,

    [Obsolete("No longer in use, please use enum value Active instead")]
    InTransit = 99
}