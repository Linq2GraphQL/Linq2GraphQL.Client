
*Upgrade Tool
dotnet tool update Linq2GraphQL.Generator -g --prerelease


*Update Schema
Linq2GraphQL https://localhost:7184/graphql/ -c="Sample" -n="Linq2GraphQL.TestClient" -o="Generated" -s=true

*Generate local
https://localhost:7184/graphql/ -c="Sample" -n="Linq2GraphQL.TestClient" -o="C:\temp\Linq2GraphQL\Linq2GraphQL.TestClient\Generated" -s=true
