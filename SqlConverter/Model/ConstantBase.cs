using System.Text.RegularExpressions;

namespace SqlConverter.Model
{
    public class ConstantBase
    {
        protected const RegexOptions REGEX_OPTIONS = RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace;
        protected const string SUBQUERY_REGEX = @"(\(\s*select[^()]+(?>(?>(?'open'\()[^()]*)+(?>(?'-open'\))[^()]*)+)*(?(open)(?!))\))";
        protected const string QUERYSTRING_REGEX = @"'([^']+)?'";
        protected const string SUBQUERY_PLACEHOLDER = "(SUB_{0})";
        protected const string QUERYSTRING_PLACEHOLDER = "STR_{0}";        
    }
}
