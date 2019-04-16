using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using SqlConverter.Resources;
using SqlConverter.Model;
using SqlConverter.Extensions;
using Newtonsoft.Json;
using SqlConverter.Validation;

namespace SqlConverter.Services
{
    public sealed class SqlConverterService : ConstantBase
    {
        private readonly string _sql;
        private readonly string _logicalId;

        private ConversionPrerequisite _conversionPrerequisite { get; set; }

        public SqlConverterService(string sql, string logicalId)
        {
            _sql = sql;
            _logicalId = CleanLogicalId(logicalId); 

            ValidateConversionPrerequisites();
        }

        public bool IsSqlValid()
        {
            return _conversionPrerequisite.GetValidationResult();
        }

        public string GetValidationErrorMessage()
        {
            return _conversionPrerequisite.GetValidationErrorMessage();
        }

        /// <summary>
        /// Converts SQL DML to a select query
        /// </summary>
        /// <returns>A JSON serialized result object</returns>
        public string Convert()
        {
            List<IStatement> context = ToContextList();
            return ParseSQLCollection(context);
        }   
        
        private string CleanLogicalId(string logicalId)
        {
            return logicalId.Trim(' ').ToUpper(); // logicalId should always be uppercase
        }

        private void ValidateConversionPrerequisites()
        {            
            List<Tuple<bool, string>> checkList = new List<Tuple<bool, string>>()
            {
                new Tuple<bool, string>(string.IsNullOrEmpty(_sql) && string.IsNullOrEmpty(_logicalId), MessageResource.DefaultRequirements),
                new Tuple<bool, string>(string.IsNullOrEmpty(_sql), MessageResource.SQLRequired),
                new Tuple<bool, string>(string.IsNullOrEmpty(_logicalId), MessageResource.MpRequired),
                new Tuple<bool, string>(_sql.Contains('’'), MessageResource.ContainsInvalidSingleQuote),
                new Tuple<bool, string>(!_sql.Contains(";"), MessageResource.NoSemicolon)
            };

            string result = string.Empty;
            foreach (var item in checkList)
            {
                if (item.Item1)
                {
                    result = item.Item2;
                    break;
                }                    
            }
            _conversionPrerequisite = new ConversionPrerequisite(string.IsNullOrEmpty(result), result);
        }

        private List<IStatement> ToContextList()
        {
            MatchCollection sqlMatchCollection = Regex.Matches(_sql, @"[\s\S]+?;", REGEX_OPTIONS);
            var statementCollection = new List<IStatement>();

            foreach (Match queryMatch in sqlMatchCollection)
            {
                char[] trimChars = new[] { ' ', '.', '\'', '"', '\n', '\r', '\t', '\v', '\f' };
                string cleanedSQL = queryMatch.Value.ToString().Trim(trimChars);
                Match firstWordMatch = Regex.Match(cleanedSQL, @"^\w+", REGEX_OPTIONS);

                StatementType matchedType;
                Enum.TryParse(firstWordMatch.Value.ToTitleCase(), out matchedType);

                IStatement statement = null;
                switch (matchedType)
                {
                    case StatementType.Update:
                        statement = new UpdateStatement(matchedType, cleanedSQL);
                        break;
                    case StatementType.Delete:
                        statement = new DeleteStatement(matchedType, cleanedSQL);
                        break;
                    case StatementType.Merge:
                        statement = new MergeStatement(matchedType, cleanedSQL);
                        break;
                    default:
                        statement = null;
                        break;
                }
                statementCollection.Add(statement);
            }
            return statementCollection;
        }

        private string ParseSQLCollection(List<IStatement> statementCollection)
        {
            var resultList = new List<ConvertResult>();
            foreach (IStatement statement in statementCollection)
            {
                ConvertResult result = DoConvert(statement);
                resultList.Add(result);
            }
            return JsonConvert.SerializeObject(resultList);
        }        

        private ConvertResult DoConvert(IStatement statement)
        {
            var result = new ConvertResult();
            result.Sql = statement.Sql; // set result sql to unmodified sql in case error is encountered

            try
            {
                var validationService = new SqlValidationService(statement);
                ValidationResult validationResult = validationService.Validate();

                if (validationResult.IsValid)
                {
                    // Get Altered SQL object (placeholding subqueries, query strings, etc.)
                    // We do this to make the conversion process possible on the actual sql; disregarding 
                    // elements that would throw the conversion process off
                    AlteredSql alteredSqlObj = new AlteredSql(result.Sql);

                    statement.ExecuteAlterations(alteredSqlObj);                    

                    // Execute all final replacements 
                    alteredSqlObj.Sql = ExecuteReplacements(alteredSqlObj.Sql);

                    // Revert altered state and reinstall placeheld items sql statement
                    result.Sql = alteredSqlObj.RevertAlteredState();
                    VerifyFinalResult(ref result, statement.StatementType.ToString());
                }
                else
                {
                    result.IsError = true;
                    result.ResultMessage = validationResult.ResultMessage;

                    if (validationResult.ParameterValidationResult != null && !validationResult.ParameterValidationResult.IsValid)
                    {
                        foreach (string error in validationResult.ParameterValidationResult.ParamErrors)
                            result.Sql = result.Sql.Replace(error, string.Format("[PARAMERROR]{0}", error));
                    }
                }
            }
            catch (RegexMatchTimeoutException)
            {
                result.IsError = true;
                result.ResultMessage = MessageResource.Timeout;
            }            

            return result;
        }        

        private string ExecuteReplacements(string sql)
        {
            // replace Item1 with Item2
            Tuple<string, string>[] replacements = new Tuple<string, string>[6]
            {
                new Tuple<string, string>("using", string.Empty),
                new Tuple<string, string>("tmp_", string.Empty),
                new Tuple<string, string>(@"toentityid\('[\w\d]+'\)", string.Format(@"toentityid('{0}')", _logicalId)),
                new Tuple<string, string>(@":entityid", string.Format(@"toentityid('{0}')", _logicalId)),
                new Tuple<string, string>(@"[ \n]+(from|join|and|where|or)[ \n]+", string.Format("{0}$1 ", Environment.NewLine)),
                new Tuple<string, string>(@"[ ]{2,}", " ")
            };

            foreach (var x in replacements)
            {
                sql = Regex.Replace(sql, x.Item1, x.Item2, REGEX_OPTIONS);
            }

            return sql;
        }

        private void VerifyFinalResult(ref ConvertResult convertResult, string dmlType)
        {
            if (Regex.Match(convertResult.Sql, "^(select|with)", REGEX_OPTIONS).Success)
            {
                convertResult.IsError = false;
                convertResult.ResultMessage = string.Format(MessageResource.SuccessfullyConverted, dmlType);
            }
            else
            {
                convertResult.IsError = true;
                convertResult.ResultMessage = string.Format(MessageResource.NotConverted, dmlType);                
            }
        }
    }
}
