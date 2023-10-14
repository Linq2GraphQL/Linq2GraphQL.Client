using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Linq2GraphQL.Client.Common
{
    public interface ICursorPaging
    {
        public PageInfo PageInfo { get; set; }
      
    }
}
