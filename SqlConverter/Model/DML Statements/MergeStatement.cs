using SqlConverter.Resources;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SqlConverter.Model
{
    internal class MergeStatement : ConstantBase, IStatement
    {
        private const string SELECT_REGEX = @"^([ ]+)?\(([ ]+)?select.+?where";

        private StatementType _statementType;
        public StatementType StatementType
        {
            get { return _statementType;  }
        }

        public string Sql { get; set; }

        public MergeStatement(StatementType statementType, string sql)
        {
            _statementType = statementType;
            Sql = sql;
        }

        public void ExecuteAlterations(AlteredSql alteredSqlObj)
        {
            alteredSqlObj.Sql = alteredSqlObj.SubqueryDictionary["(SUB_1)"].Trim(new[] { '(', ')' }).Trim(' ');
        }

        public ValidationContext GetValidationContext(AlteredSql alteredSqlObj)
        {
            return new ValidationContext()
            {
                ValidationList = new List<Tuple<bool, string>>()
                {
                    new Tuple<bool, string>(
                        Regex.Match(
                            alteredSqlObj.Sql
                            , @"merge[ ]+into[ ]+(\w+\.\w+(?:@\w+)?){1}[ ]+(?:\w+[ ]+)?using([ ]+)?(\(.+?\))([ ]+)?(\w+[ ]+)?on"
                            , REGEX_OPTIONS
                            , new TimeSpan(0, 0, 0, 0, 250)).Success
                        , MessageResource.Merge_Invalid),
                    new Tuple<bool, string>(
                        Regex.Match(alteredSqlObj.Sql, "when[ ]+(not[ ]+)?matched[ ]+then[ ]+(update|insert|delete)", REGEX_OPTIONS).Success
                        , MessageResource.Merge_InvalidMatchedClause)
                }
                , QueryParameters = Regex.Replace(
                    alteredSqlObj.SubqueryDictionary["(SUB_1)"]
                    , SELECT_REGEX
                    , string.Empty
                    , REGEX_OPTIONS
                    , new TimeSpan(0, 0, 0, 0, 250))
            };
        }
    }
}
