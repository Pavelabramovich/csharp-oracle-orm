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

        //var res0 = context.Students
        //    .Select(g => new { Id = g.Id, Arr = context.Students.ToList() })
        //    .ToList();

        //var res1 = context.Students
        //    .Select(s => s)
        //    .Where(s => s.Name != "Petya")
        //    .Select(s => s.Name)
        //    .ToList();

        //var res2 = context.Students
        //    .Select(s => new { s.Name, Groups = "13" })
        //    .ToList();


        var res101 = from c in context.Students
                     where c.Name == "Petya"
                     select new
                     {
                         Name = "lolal",

                         Orders = from o in context.Students

                                  where o.Id == c.Id

                                  select o
                        };

                 
        var res3 = context.Groups
            .Where(g => g.Name != "1")
            .Select(g => new { Id = g.Id, Arr = context.Students.ToList() })
            .ToList();

        Console.ReadKey();
    }
}
