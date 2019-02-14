using System;

namespace PotatoTcp.Client
{
    /// <summary>
    /// The base class for exceptions that are thrown by a PotatoTcp client.
    /// </summary>
    [Serializable]
    public abstract class ClientException : Exception
    {
        protected ClientException() { }

        protected ClientException(string message) : base(message) { }

        protected ClientException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// The exception that is thrown when a client is no longer connected to the remote endpoint.
    /// </summary>
    [Serializable]
    public class NotConnectedException : ClientException
    {
        /// <summary>
        /// Initializes a new instance of the NotConnectedException class.
        /// </summary>
        public NotConnectedException() { }

        /// <summary>
        /// Initializes a new instance of the NotConnectedException class with a specified error message.
        /// </summary>
        /// <param name="name"></param>
        public NotConnectedException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the NotConnectedException class with a specified error message and the exception that is the cause of this exception.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public NotConnectedException(string message, Exception inner) : base(message, inner) { }
    }
}