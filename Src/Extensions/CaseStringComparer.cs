using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
//using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Humanizer;


namespace OracleOrm;


class CaseStringComparer : IEqualityComparer<string>
{
    public CaseStringComparer()
    { }

    public bool Equals(string? first, string? second)
    {
        if (first is null && second is null)
            return true;

        if (first is null || second is null)
            return false;

        first = first.Humanize(LetterCasing.LowerCase);
        second = second.Humanize(LetterCasing.LowerCase);

        return first == second;
    }

    public int GetHashCode([DisallowNull] string str)
    {
        return str.Humanize(LetterCasing.LowerCase).GetHashCode();
    }
}
