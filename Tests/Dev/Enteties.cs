using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleOrm.Dev;


public class Student
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int GroupId { get; set; }
}

public class Group
{
    public int Id { get; set; }
    public string Name { set; get; }
}