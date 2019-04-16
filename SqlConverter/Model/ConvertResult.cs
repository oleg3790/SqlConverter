namespace SqlConverter.Model
{
    internal sealed class ConvertResult
    {
        public bool IsError { get; set; }
        public string ResultMessage { get; set; }
        public string Sql { get; set; }
    }
}
