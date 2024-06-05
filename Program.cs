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
using static Microsoft.EntityFrameworkCore.DbSet<object>;

namespace OracleEFCore;

class Program
{
    static void Main(string[] args)
    {
        List<int> l = [1, 2, 3, 4, 5];
        var res = l.Select(x => x + 1).ToList();


        DevContext context = new();

        var res1 = context.Students
            .Select(s => s)
            .Where(s => s.Name != "Katya")
            .Select(s => s.Name[0])
            .ToList();

        var res2 = context.Students
            .Select(s => new { s.Name, Groups = "13" })
            .ToList();

        var res3 = context.Groups
            .Where(g => g.Name != "1")
            .Select(g => new { Id = g.Id, Arr = context.Students.ToList() })
            .ToList();

        var res4 = (from s in context.Students
                    join g in context.Groups
                      on s.GroupId equals g.Id
                  select new { s.Name }).ToList();

        var res4_2 = context.Students
            .Join(context.Groups, s => s.GroupId, g => g.Id, (sName, gName) => sName)
            .ToList();

       // context.Students.Delete(s => s.Name == "Katya");

        context.Students.Update([("Name", "lol"), ("Id", 100)], s => s.Name == "Lera");

        var res09 = context.Students
            .Where(s => context.Students
                .Exists(s1 => s1.Id == 215))
            .ToList();

        List<int> A = [1, 2, 3, 4, 5];
        List<int> B = [.. from a in A select a + 12];

        Console.ReadKey();
    }

    //private static (string, TProperty) SetProperty<TProperty>(string property, TProperty newValue)
    //{
    //    throw new NotImplementedException();
    //}
}


