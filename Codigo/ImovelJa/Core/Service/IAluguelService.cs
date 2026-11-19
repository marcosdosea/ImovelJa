using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Service
{
    public interface IAluguelService
    {
        uint Create(Aluguel aluguel);
        void Edit(Aluguel aluguel);
        void Delete(uint id);
        Aluguel? Get(uint id);
        Aluguel? GetDoInquilino(uint id, uint idPessoa);
        IEnumerable<Aluguel> GetAll();
        IEnumerable<Aluguel> GetByImovel(uint idImovel);
        IEnumerable<Aluguel> GetByInquilino(uint idPessoa);
    }
}
