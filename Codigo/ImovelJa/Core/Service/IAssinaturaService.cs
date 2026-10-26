using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Service
{
    public interface IAssinaturaService
    {
        uint Create(Assinatura assinatura);
        void Edit(Assinatura assinatura);
        void Cancel(uint id);
        Assinatura? Get(uint id);
        IEnumerable<Assinatura> GetAll();
        Assinatura? GetByPessoa(uint idPessoa);
    }
}
