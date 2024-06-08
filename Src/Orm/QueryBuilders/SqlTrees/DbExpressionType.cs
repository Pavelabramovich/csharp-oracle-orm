
namespace OracleOrm;


// To "extend" an existing enum, needed to create another enum with
// numeric identifiers that do not intersect with the "base" enum
internal enum DbExpressionType
{
    Table = 128, 
    Column,
    Select,
    Projection,
    FunctionCalling,
    Join,
    SubQuery
}
