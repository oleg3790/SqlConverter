using SqlConverter.Model;
using SqlConverter.Resources;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SqlConverter.Extensions;
using System;

namespace SqlConverter.Validation
{
    internal sealed class SqlValidationService : ConstantBase
    {
        private readonly IStatement _statement;
        private readonly AlteredSql _alteredSqlObj;
        private TimeSpan _regexTimeoutLimit = new TimeSpan(0, 0, 0, 0, 250);

        public SqlValidationService(IStatement statement)
        {
            _statement = statement;
            _alteredSqlObj = new AlteredSql(statement.Sql);
        }

        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            ValidationContext context = _statement.GetValidationContext(_alteredSqlObj);

            foreach (var check in context.ValidationList)
            {
                if (!check.Item1)
                {
                    result.ResultMessage = check.Item2;
                    result.IsValid = false;
                    return result;
                }
            }

            result.ParameterValidationResult = ValidateQueryParameters(context.QueryParameters);
            result.IsValid = result.ParameterValidationResult.IsValid;
            result.ResultMessage = (result.ParameterValidationResult.IsValid) ? string.Empty : string.Format(MessageResource.NotConverted, _statement.StatementType.ToString());

            return result;
        }        

        public ParameterValidationResult ValidateQueryParameters(string fullQueryParameters)
        {
            // Revert the altered state so that we can validate subqueries
            fullQueryParameters = _alteredSqlObj.RevertSubqueryState(fullQueryParameters);

            var paramResults = new ParameterValidationResult();            
            MatchCollection subqueryMatches = Regex.Matches(fullQueryParameters, SUBQUERY_REGEX, REGEX_OPTIONS);

            foreach (Match subqueryMatch in subqueryMatches)
            {
                ValidateSubqueries(ref paramResults, subqueryMatch.Value, SUBQUERY_PLACEHOLDER);
                fullQueryParameters = fullQueryParameters.Replace(subqueryMatch.Value, SUBQUERY_PLACEHOLDER);
            }
            DoValidateParameters(ref paramResults, fullQueryParameters);
            return paramResults;
        }        

        private void ValidateSubqueries(ref ParameterValidationResult result, string subquery, string subqueryPlaceholder)
        {
            // Using a stack and a list to store converted subqueries (ones that no longer have subqueries as params),
            // this method will store all subqueries into a list and run against DoValidateParameters            

            var workQueue = new Stack<string>();
            var convertedSubqueriesList = new List<string>();

            workQueue.Push(subquery);

            do
            {
                string modifiedQuery = workQueue.Peek().Trim(new char[] { '(', ')' });
                MatchCollection subqueryMatches = Regex.Matches(modifiedQuery, SUBQUERY_REGEX, REGEX_OPTIONS);

                if (subqueryMatches.Count > 0)
                {
                    workQueue.Pop();
                    foreach (Match match in subqueryMatches)
                    {
                        workQueue.Push(match.Value);
                        modifiedQuery = modifiedQuery.Replace(match.Value, subqueryPlaceholder);
                    }
                    convertedSubqueriesList.Add(modifiedQuery);
                }
                else
                {
                    workQueue.Pop();
                    convertedSubqueriesList.Add(modifiedQuery);
                }

            } while (workQueue.AsEnumerable().Any(x => Regex.Matches(x, SUBQUERY_REGEX, REGEX_OPTIONS).Count > 0));

            foreach (string convertedSubquery in convertedSubqueriesList)
                DoValidateParameters(ref result, Regex.Replace(convertedSubquery, @"select.+(?=where)", string.Empty, REGEX_OPTIONS));
        }

        private void DoValidateParameters(ref ParameterValidationResult result, string queryParameters)
        {
            // TODO: add some sort of parameter string index tracker in case we see parameters of subqueries that are identical. 
            //       Right now ParameterValidationResult only stores the param string where the error is encountered

            // Validate there are no more than 1 where parameter
            MatchCollection whereMatchCollection = Regex.Matches(queryParameters, "where", REGEX_OPTIONS);
            if (whereMatchCollection.Count > 1)
            {
                result.IsValid = false;

                foreach (Match match in whereMatchCollection)
                    result.ParamErrors.Add(match.Value);
            }                

            string[] paramArray = Regex.Split(queryParameters, "(^| |\n)where[ ]|(^| |\n)and[ ]|(^| |\n)or[ ]", RegexOptions.IgnoreCase);
            paramArray = paramArray.Where(x => !string.IsNullOrWhiteSpace(x.Trim(new[] { ' ', ';', '\t' }))).ToArray();

            foreach (string param in paramArray)
            {
                string cleanParameter = _alteredSqlObj.RevertQueryStringState(param.Trim(new[] { ' ', ';' }));
                if (!string.IsNullOrWhiteSpace(cleanParameter))
                {
                    try
                    {
                        Match regexLikeMatch = Regex.Match(
                            cleanParameter
                            , @"\s*(?:not )?regexp_like\s*\(.+?,\s*'.+?'(?:,\s*'[cinmx]'\s*)?\s*\)"
                            , REGEX_OPTIONS
                            , _regexTimeoutLimit);
                        Match regexSubInReplaceMatch = Regex.Match(
                            cleanParameter
                            , @"\s*regexp_(?:substr|replace)\s*\(.+?,\s*'.+?'(?:,\s*\d\s*)?(?:,\s*\d+\s*)?(?:,\s*'[cinmx]'\s*)?\s*\)\s*((?:=\s*\(?.+\)?)|(?:in\s*\((.+,.+)+\)))"
                            , REGEX_OPTIONS
                            , _regexTimeoutLimit);
                        Match regexInstrReplaceMatch = Regex.Match(
                            cleanParameter
                            , @"\s*regexp_instr\s*\(.+?,\s*'.+?'(?:,\s*\d\s*)?(?:,\s*\d+\s*)?(?:,\s*\d+\s*)?(?:,\s*'[cinmx]'\s*)?\s*\)\s*((?:=\s*\(?.+\)?)|(?:in\s*\((.+,.+)+\)))"
                            , REGEX_OPTIONS
                            , _regexTimeoutLimit);
                        Match comparisonMatch = Regex.Match(
                            cleanParameter
                            , @"^.+?(?:(?:<|>)?=|!=|>|<)[\s]*[\S]+"
                            , REGEX_OPTIONS
                            , _regexTimeoutLimit);
                        Match nullMatch = Regex.Match(
                            cleanParameter
                            , @"^.+?(?:\s+is(?:\s+not)?\s+null)"
                            , REGEX_OPTIONS
                            , _regexTimeoutLimit);
                        Match likeMatch = Regex.Match(
                            cleanParameter
                            , @"^.+?(?:\s+not)?\s+like\s+(?:\(\s*)?'[%]?.+[%]?'(?:\s*\)\s*)?"
                            , REGEX_OPTIONS
                            , _regexTimeoutLimit);
                        Match multiCompareMatch = Regex.Match(
                            cleanParameter
                            , @"^.+?(?:\s+not)?\s+in\s+(?:\(\s*)(?:'.+',?|.+)+(?:\s*\)\s*)"
                            , REGEX_OPTIONS
                            , _regexTimeoutLimit);

                        if (new[] { regexLikeMatch, regexSubInReplaceMatch, regexInstrReplaceMatch, comparisonMatch, nullMatch, likeMatch, multiCompareMatch }.All(x => !x.Success))
                        {
                            result.IsValid = false;
                            result.ParamErrors.Add(cleanParameter);
                        }
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        result.IsValid = false;
                        result.ParamErrors.Add(cleanParameter);
                    }
                }
            }
        }
    }
}
