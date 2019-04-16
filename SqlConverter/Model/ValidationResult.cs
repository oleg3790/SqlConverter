namespace SqlConverter.Model
{
    internal sealed class ValidationResult
    {
        public bool IsValid { get; set; }

        public string ResultMessage { get; set; }

        public ParameterValidationResult ParameterValidationResult { get; set; }
    }
}
