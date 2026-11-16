using Core.Service;

namespace Service
{
    /// <summary>
    /// Simula o gateway de pagamento das assinaturas, até a definição do gateway real com o professor
    /// </summary>
    public class CobrancaSimuladaService : ICobrancaService
    {
        private readonly bool aprovar;

        /// <summary>
        /// Cria a simulação do gateway
        /// </summary>
        /// <param name="aprovar">verdadeiro para aprovar as cobranças, falso para recusá-las</param>
        public CobrancaSimuladaService(bool aprovar = true)
        {
            this.aprovar = aprovar;
        }

        /// <summary>
        /// Simula a cobrança de um valor no gateway de pagamento
        /// </summary>
        /// <param name="idPessoa">id da pessoa cobrada</param>
        /// <param name="valor">valor a cobrar</param>
        /// <returns>verdadeiro se o pagamento foi autorizado</returns>
        public bool Cobrar(uint idPessoa, decimal valor)
        {
            return aprovar;
        }
    }
}
