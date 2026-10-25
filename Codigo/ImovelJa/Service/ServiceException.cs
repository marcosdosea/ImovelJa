using System;
using System.Collections.Generic;
using System.Text;

namespace Service
{
    /// <summary>
    /// Exceção lançada quando uma regra de negócio não é cumprida
    /// </summary>
    public class ServiceException : Exception
    {
        public ServiceException(string message) : base(message)
        {
        }
    }
}
