
namespace OracleOrm;


// To "extend" an existing enum, needed to create another enum with
// numeric identifiers that do not intersect with the "base" enum
public enum SqlExpressionType
{
    Table, 
    Column,
    Select,
    Projection,
    FunctionCalling,
    Join,
    SubQuery
}
