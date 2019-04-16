using SqlConverter.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlConverter.Model
{
    internal sealed class UpdateStatement : ConstantBase, IStatement
    {
        private readonly StatementType _statementType;
        public StatementType StatementType
        {
            get { return _statementType; }
        }

        public string Sql { get; set; }

        public UpdateStatement(StatementType statementType, string sql)
        {
            _statementType = statementType;
            Sql = sql;
        }

        public void ExecuteAlterations(AlteredSql alteredSqlObj)
        {
            // Collect set fields
            Match setMatch = Regex.Match(alteredSqlObj.Sql, @"set[\s\S]+?((?:\w\.)?\w+[\s\S]+?=[\s\S]+?)where", REGEX_OPTIONS);
            MatchCollection setFieldCollection = Regex.Matches(setMatch.Groups[1].Value, @"(\w+)(?:[ ]*)[\n]*?=", REGEX_OPTIONS);

            // Merge set fields to a list
            List<string> setFields = new List<string>();
            foreach (Match match in setFieldCollection)
                setFields.Add(match.Groups[1].Value);

            Match sqlTrimMatch = Regex.Match(alteredSqlObj.Sql, @"update[\s]+(.+)[\s]+set[\s\S]+(?=where)", REGEX_OPTIONS);

            // Create field list, compose the "select ... from"
            string selectFields = setFields.Aggregate((x, y) => string.Format("{0}, {1}", x, y)).Trim(',');
            selectFields = string.Format("select {0}\nfrom {1}\n"
                , selectFields
                , Regex.Replace(sqlTrimMatch.Groups[1].Value, "tmp_", string.Empty, REGEX_OPTIONS));

            alteredSqlObj.Sql = alteredSqlObj.Sql.Replace(sqlTrimMatch.Value, selectFields);
        }

        public ValidationContext GetValidationContext(AlteredSql alteredSqlObj)
        {
            return new ValidationContext()
            {
                ValidationList = new List<Tuple<bool, string>>()
                {
                    new Tuple<bool, string>(
                        Regex.Match(alteredSqlObj.Sql, @"update[ ]+(\w+\.\w+){1}[ ]+(\w+[ ]+)?set", REGEX_OPTIONS).Success
                        , MessageResource.Update_InvalidUpdate),
                    new Tuple<bool, string>(
                        Regex.Match(alteredSqlObj.Sql, @"set[\s\S]+?((?:\w\.)?\w+[\s\S]+?=[\s\S]+?)where", REGEX_OPTIONS).Success
                        , MessageResource.Update_InvalidSet)
                }
                , QueryParameters = Regex.Replace(alteredSqlObj.Sql, @"update[\s\S]+?(.+)[\s\S]+?set[\s\S]+?(?=where)", string.Empty, REGEX_OPTIONS)
            };
        }
    }
}
