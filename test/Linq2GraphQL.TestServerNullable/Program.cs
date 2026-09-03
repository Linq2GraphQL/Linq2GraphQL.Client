using Linq2GraphQL.TestServerNullable;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

public class ProgramNullable
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddGraphQLServer()
            .AddInMemorySubscriptions()
            .AddTestServerNullableTypes()
            .AddFiltering()
            .AddSorting();

        builder.Services.AddMemoryCache();

        var app = builder.Build();

        app.UseWebSockets();

        app.MapGraphQL();

        app.Run();
    }
}
