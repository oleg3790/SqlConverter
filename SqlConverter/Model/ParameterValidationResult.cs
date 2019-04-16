using System.Collections.Generic;

namespace SqlConverter.Model
{
    internal sealed class ParameterValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> ParamErrors { get; set; } = new List<string>();
    }
}
