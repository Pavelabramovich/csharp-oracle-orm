using Microsoft.EntityFrameworkCore;
using OracleOrm;
using OracleOrm.Dev;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace OracleEFCore;

class Program
{
    static void Main(string[] args)
    {
        DevContext context = new();


        //Console.WriteLine(CaseConverter.ToPascalCase("GroupId"));


        var res = context.Students.Where(s => s.Id > 1).ToList();

        //context.Students.Clear();
    }
}
