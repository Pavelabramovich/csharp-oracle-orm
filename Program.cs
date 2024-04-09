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

        int id = 4;

        var res = context.Students
            .Select(s => s)
            .Where(s => s.Name != "Petya")
            .Select(s => s.Name[s.Id - s.Id]).ToList();
    }
}
