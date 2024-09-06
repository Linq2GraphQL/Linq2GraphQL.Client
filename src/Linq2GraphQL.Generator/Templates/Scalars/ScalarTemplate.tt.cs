using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linq2GraphQL.Generator.Templates.Scalars
{
    public partial class ScalarTemplate
    {
        private readonly GraphqlType scalarType;
        private readonly string namespaceName;

        public ScalarTemplate(GraphqlType scalarType, string namespaceName)
        {
            this.scalarType = scalarType;
            this.namespaceName = namespaceName;
        }
    
    private string className => scalarType.Name;
    private string classDescription => scalarType.Description ?? className;



    }



}
