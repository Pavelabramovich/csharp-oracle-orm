using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OracleOrm;

public static class ParsableMethods
{
    public static MethodInfo StringIndexing { get; } = typeof(string).GetMethod("get_Chars")!;


}
