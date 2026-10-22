using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Service
{
    public interface IPagamentoService
    {
        uint Create(Pagamento pagamento);
        void Edit(Pagamento pagamento);
        Pagamento? Get(uint id);
        IEnumerable<Pagamento> GetAll();
    }
}
