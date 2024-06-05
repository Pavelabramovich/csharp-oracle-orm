using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Humanizer;


namespace OracleOrm;


public static class CaseConverter
{
    public static string ToPascalCase(string word)
    {
        return string
            .Join("", word.Split('_')
            .Select(w => w.Trim())
            .Where(w => w.Length > 0)
            .Select(w => w.Substring(0, 1).ToUpper() + w.Substring(1).ToLower()));
    }

    public static string ToSnakeCase(string word)
    {
        return string.Concat((word ?? string.Empty)
            .Select((x, i) => i > 0 && i < word.Length - 1 && char.IsUpper(x) && !char.IsUpper(word[i - 1]) ? $"_{x}" : x.ToString())).ToUpper();
    }

    public static string Plurarize(string word)
    {
        return word.Pluralize();
    }
}
