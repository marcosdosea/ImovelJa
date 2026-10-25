using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Service
{
    public interface IPessoaService
    {
        uint Create(Pessoa pessoa);
        void Edit(Pessoa pessoa);
        Pessoa? Get(uint id);
        IEnumerable<Pessoa> GetAll();
        Pessoa? GetByDocumento(string documento);
        void Activate(uint id, bool ativo);
    }
}
