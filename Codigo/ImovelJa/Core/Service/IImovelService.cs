using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Service
{
    public interface IImovelService
    {
        uint Create(Imovel imovel);
        void Edit(Imovel imovel);
        void Delete(uint id);
        Imovel? Get(uint id);
        IEnumerable<Imovel> GetAll();
        IEnumerable<Imovel> GetByProprietario(uint idPessoa);
        IEnumerable<Imovel> GetByValidacao(string statusValidacao);
        IEnumerable<Imovel> GetByNome(string nome);
        void Approve(uint id, bool aprovado);
    }
}
