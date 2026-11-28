using Core.Service;
using ImovelJaAPI.Mappers;
using ImovelJaAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImovelJaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImoveisController : ControllerBase
    {
        private readonly IImovelService imovelService;

        public ImoveisController(IImovelService imovelService)
        {
            this.imovelService = imovelService;
        }

        // GET: api/imoveis?termo=casa&localizacao=Itabaiana&tipos=C&tipos=A&valorMaximo=1500
        [HttpGet]
        public ActionResult<IEnumerable<ImovelViewModel>> Get([FromQuery] ImovelFiltroViewModel filtro)
        {
            ValidarIntervalos(filtro);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var imoveis = imovelService.GetByFiltro(ImovelMapper.ToDTO(filtro));
            return Ok(ImovelMapper.ToViewModel(imoveis));
        }

        // GET: api/imoveis/5
        [HttpGet("{id}")]
        public ActionResult<ImovelViewModel> Get(uint id)
        {
            var imovel = imovelService.GetPublico(id);

            if (imovel == null)
            {
                return NotFound("Anúncio não encontrado.");
            }

            return Ok(ImovelMapper.ToViewModel(imovel));
        }

        private void ValidarIntervalos(ImovelFiltroViewModel filtro)
        {
            if (filtro.ValorMinimo > filtro.ValorMaximo)
            {
                ModelState.AddModelError(
                    nameof(filtro.ValorMaximo),
                    "O valor máximo deve ser maior ou igual ao valor mínimo");
            }

            if (filtro.AreaMinima > filtro.AreaMaxima)
            {
                ModelState.AddModelError(
                    nameof(filtro.AreaMaxima),
                    "A área máxima deve ser maior ou igual à área mínima");
            }
        }
    }
}
