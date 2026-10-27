using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linq2GraphQL.TestClientNullable
{
    public partial class MacAddress
    {

        public MacAddress() { }

        public MacAddress(string value)
        {
            Value = value;
        }




        public override object Value
        {
            get
            {
                //Custom Code
              
                return base.Value;
            }
            set
            {
                //Custom code

                base.Value = value;
            }
        }



    }
}
