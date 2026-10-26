using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Service
{
    public interface IPlanoService
    {
        void Edit(Plano plano);
        Plano? Get(uint id);
        IEnumerable<Plano> GetAll();
        void Activate(uint id, bool ativo);
    }
}
