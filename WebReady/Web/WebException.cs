using System;

namespace WebCase.Web
{
    ///
    /// Thrown to indicate an illegal structuring in a work hierarchy.
    ///
    public class WebException : Exception
    {
        public WebException()
        {
        }

        public WebException(string message) : base(message)
        {
        }

        public WebException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}