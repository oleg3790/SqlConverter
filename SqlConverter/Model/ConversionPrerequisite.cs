namespace SqlConverter.Model
{
    internal sealed class ConversionPrerequisite
    {
        private readonly bool _isValid;
        private readonly string _validationErrorMessage;

        public ConversionPrerequisite(bool isValid, string validationErrorMessage)
        {
            _isValid = isValid;
            _validationErrorMessage = validationErrorMessage;
        }

        public bool GetValidationResult()
        {
            return _isValid;
        }

        public string GetValidationErrorMessage()
        {
            return _validationErrorMessage;
        }
    }
}
