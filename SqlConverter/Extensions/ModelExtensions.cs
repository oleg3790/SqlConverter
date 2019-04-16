using SqlConverter.Model;

namespace SqlConverter.Extensions
{
    internal static class ModelExtensions
    {
        /// <summary>
        /// Reverts modifications executed on a AlteredSql object back into a sql string
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static string RevertAlteredState(this AlteredSql alteredSqlObj)
        {
            foreach (var val in alteredSqlObj.SubqueryDictionary)
                alteredSqlObj.Sql = alteredSqlObj.Sql.Replace(val.Key, val.Value);

            foreach (var val in alteredSqlObj.QueryStringDictionary)
                alteredSqlObj.Sql = alteredSqlObj.Sql.Replace(val.Key, val.Value);

            return alteredSqlObj.Sql;
        }

        /// <summary>
        /// Reverts subquery modifications executed on a AlteredSql's query parameters back into a sql string
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static string RevertSubqueryState(this AlteredSql alteredSqlObj, string queryParameters)
        {
            foreach (var val in alteredSqlObj.SubqueryDictionary)
                queryParameters = queryParameters.Replace(val.Key, val.Value);

            return queryParameters;
        }

        /// <summary>
        /// Reverts queryParameter modifications executed on a AlteredSql's query parameters back into a sql string
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static string RevertQueryStringState(this AlteredSql alteredSqlObj, string queryParameter)
        {
            foreach (var val in alteredSqlObj.QueryStringDictionary)
                queryParameter = queryParameter.Replace(val.Key, val.Value);

            return queryParameter;
        }
    }
}
