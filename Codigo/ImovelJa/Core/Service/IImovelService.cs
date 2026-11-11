using System;
using System.Collections.Generic;
using System.Text;
using Core.DTO;

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
        IEnumerable<Imovel> GetByFiltro(ImovelFiltroDTO filtro);
        void Approve(uint id, bool aprovado);
    }
}
