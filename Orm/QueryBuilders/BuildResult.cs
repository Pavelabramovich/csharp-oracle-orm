using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OracleOrm;


public class BuildResult
{
    internal string CommandText;
    internal LambdaExpression Projector;
}
