namespace Linq2GraphQL.TestServer.Models;

public class Item
{
    public string ItemId { get; set; } = "";
    public string ItemName { get; set; } = "";

    public byte[]? Data { get; set; }
}