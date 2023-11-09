using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linq2GraphQL.Client.Common
{
    public class LazyProperty<T>
    {
        bool _initialized = false;
        T _result;

        public T Value(Func<T> fn)
        {
            if (!_initialized)
            {
                _result = fn();
                _initialized = true;
            }
            return _result;
        }
    }
}
