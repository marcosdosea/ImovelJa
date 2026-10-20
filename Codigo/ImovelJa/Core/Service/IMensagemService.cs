using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Service
{
    public interface IMensagemService
    {
        uint Create(Mensagem mensagem);
        Mensagem? Get(uint id);
        IEnumerable<Mensagem> GetAll();
    }
}
