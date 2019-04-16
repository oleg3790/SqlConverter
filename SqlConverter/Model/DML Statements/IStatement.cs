namespace SqlConverter.Model
{
    internal interface IStatement
    {
        StatementType StatementType { get; }
        string Sql { get; set; }

        /// <summary>
        /// Validates the stement specific parameters
        /// </summary>
        /// <param name="alteredSqlObj"></param>
        /// <returns></returns>
        ValidationContext GetValidationContext(AlteredSql alteredSqlObj);
        void ExecuteAlterations(AlteredSql alteredSql);
    }
}
