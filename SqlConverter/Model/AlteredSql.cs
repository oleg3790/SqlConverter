using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace SqlConverter.Model
{
    /// <summary>
    /// Returns an AlteredSql object which contains sql that has been modified to contain content that has 
    /// been substituted for placeholders to make the conversion process simplier
    /// </summary>
    internal sealed class AlteredSql : ConstantBase
    {
        public string Sql { get; set; }

        public Dictionary<string, string> SubqueryDictionary { get; set; }

        public Dictionary<string, string> QueryStringDictionary { get; set; }

        public AlteredSql(string sql)
        {
            // Clean sql first to remove newline chars
            string cleanedSql = CleanSql(sql);

            // Execute query string removals first as there might be query strings inside of subqueries
            // which will cause an issue if not replaced first and RevertSubqueryState() extension is used
            QueryStringDictionary = GetQueryStringDictionary(ref cleanedSql);
            SubqueryDictionary = GetSubqueryDictionary(ref cleanedSql);
            Sql = cleanedSql;
        }        

        private Dictionary<string, string> GetSubqueryDictionary(ref string sql)
        {
            return RemoveMatchItems(ref sql, SUBQUERY_REGEX, SUBQUERY_PLACEHOLDER);
        }

        private Dictionary<string, string> GetQueryStringDictionary(ref string sql)
        {
            return RemoveMatchItems(ref sql, QUERYSTRING_REGEX, QUERYSTRING_PLACEHOLDER);
        }

        /// <summary>
        /// Replaces the matched items in matchRegex with a placeholder; returning the removed items as a dictionary
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="matchRegex"></param>
        /// <param name="placeholder"></param>
        /// <returns></returns>
        private Dictionary<string, string> RemoveMatchItems(ref string sql, string matchRegex, string placeholder)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            int count = 1;

            MatchCollection matchCollection = Regex.Matches(sql, matchRegex, REGEX_OPTIONS);

            foreach (var match in matchCollection.Cast<Match>().Select(x => x.Value).Distinct())
            {
                string tmp = string.Format(placeholder, count.ToString());
                dictionary.Add(tmp, match);
                sql = sql.Replace(match, tmp);
                count += 1;
            }

            // Order the collection by desc order to prevent revert issues when 
            // collection has more than 9 elements
            return dictionary.OrderByDescending(x => x.Key)
                .ToDictionary(k => k.Key, v => v.Value);
        }

        private string CleanSql(string sql)
        {
            return Regex.Replace(sql, "\\r|\\n", " ", REGEX_OPTIONS).Trim(' ');
        }
    }
}
