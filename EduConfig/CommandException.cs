using System;
using System.Runtime.Serialization;

namespace Pollub.EduConfig
{
    /// <summary>
    /// Własna klasa wyjątku, dla rozpoznawania, że to komenda zakończyła się z błędem
    /// </summary>
    [Serializable]    
    public class CommandException : Exception
    {
        public CommandException() { }
        public CommandException(string message) : base(message) { }
        public CommandException(string message, Exception inner) : base(message, inner) { }
        protected CommandException(
          SerializationInfo info,
          StreamingContext context)
            : base(info, context) { }
    }
}
