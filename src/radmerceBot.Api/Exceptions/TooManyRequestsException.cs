namespace radmerceBot.Api.Exceptions
{
    [Serializable]
    internal class TooManyRequestsException : Exception
    {
        public TooManyRequestsException()
        {
        }

        public TooManyRequestsException(string? message) : base(message)
        {
        }

        public TooManyRequestsException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}