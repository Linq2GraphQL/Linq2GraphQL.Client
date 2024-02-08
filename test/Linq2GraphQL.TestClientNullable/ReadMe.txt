
*Upgrade Tool
dotnet tool update Linq2GraphQL.Generator -g --prerelease


*Update Schema
Linq2GraphQL https://localhost:50741/graphql/ -c="SampleNullableClient" -n="Linq2GraphQL.TestClientNullable" -o="Generated" 

*Generate local
https://localhost:50741/graphql/ -c="SampleNullable" -n="Linq2GraphQL.TestClientNullable" -o="C:\temp\Linq2GraphQL\Linq2GraphQL.TestClientNullable\Generated" -s=true

