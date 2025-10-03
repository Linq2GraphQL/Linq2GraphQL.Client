
*Upgrade Tool
dotnet tool update Linq2GraphQL.Generator -g --prerelease


*Update Schema
Linq2GraphQL https://localhost:7184/graphql/ -c="SampleClient" -n="Linq2GraphQL.TestClient" -o="Generated" -s=true

*Generate local
https://localhost:7184/graphql/ -c="SampleClient" -n="Linq2GraphQL.TestClient" -o="C:\Code\Linq2GraphQL.Client\test\Linq2GraphQL.TestClient\Generated" -s=true -d=true
