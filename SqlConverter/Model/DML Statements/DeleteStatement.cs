using SqlConverter.Resources;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SqlConverter.Model
{
    internal class DeleteStatement : ConstantBase, IStatement
    {
        private const string DELETE_REGEX = @"delete[ ]+from[ ]+(\w+\.\w+){1}[ ]+(\w+[ ]+)?(?=where)";

        private StatementType _statementType;
        public StatementType StatementType
        {
            get { return _statementType; }
        }

        public string Sql { get; set; }

        public DeleteStatement(StatementType statementType, string sql)
        {
            _statementType = statementType;
            Sql = sql;
        }

        public void ExecuteAlterations(AlteredSql alteredSqlObj)
        {
            Match deleteMatch = Regex.Match(alteredSqlObj.Sql, DELETE_REGEX, REGEX_OPTIONS);

            if (deleteMatch.Success)
            {
                alteredSqlObj.Sql = alteredSqlObj.Sql.Replace(
                    deleteMatch.Value
                    , string.Format("select *\nfrom {0}\n", deleteMatch.Groups[1].Value));
            }            
        }

        public ValidationContext GetValidationContext(AlteredSql alteredSqlObj)
        {
            return new ValidationContext()
            {
                ValidationList = new List<Tuple<bool, string>>()
                {
                    new Tuple<bool, string>(
                        Regex.Match(alteredSqlObj.Sql, @"delete[ ]+from[ ]+(\w+\.\w+){1}[ ]+(\w+[ ]+)?where", REGEX_OPTIONS).Success
                        , MessageResource.Delete_Invalid),
                }
                ,
                QueryParameters = Regex.Replace(alteredSqlObj.Sql, DELETE_REGEX, string.Empty, REGEX_OPTIONS)
            };
        }
    }
}
