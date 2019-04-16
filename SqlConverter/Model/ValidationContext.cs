using System;
using System.Collections.Generic;

namespace SqlConverter.Model
{
    internal sealed class ValidationContext
    {
        /// <summary>
        /// A list containing match result and error message if the result is false
        /// </summary>
        public List<Tuple<bool, string>> ValidationList { get; set; }

        public string QueryParameters { get; set; }
    }
}
