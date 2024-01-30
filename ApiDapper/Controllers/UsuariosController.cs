using ApiDapper.Models;
using ApiDapper.Repositories;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace ApiDapper.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioRepository _usuariosRepository;

        public UsuariosController(IUsuarioRepository usuariosRepository)
        {
            _usuariosRepository = usuariosRepository;
        }

        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                // Log de informação sobre a solicitação de obtenção de todos os usuários.
                Log.Information("Solicitação HTTP GET para obter todos os usuários recebida.");

                // Chama o método GetAll do repositório para obter todos os usuários.
                var usuarios = _usuariosRepository.GetAll();


                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                // Log de erro se houver exceção ao tentar obter os usuários.
                Log.Error(ex, "Erro ao tentar obter todos os usuários através da API.");

                // Retorna uma resposta de erro interno do servidor (500) se ocorrer uma exceção.
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ocorreu um erro ao processar sua solicitação." });
            }
        }


        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            try
            {
                // Tenta obter o usuário do repositório usando o ID fornecido.
                var usuario = _usuariosRepository.Get(id);

                // Se um usuário é retornado, envia-o na resposta com o status 200 (OK).
                return Ok(usuario);
            }
            catch (KeyNotFoundException)
            {
                // Uma resposta com o status 404 (Not Found) é enviada de volta ao cliente.
                return NotFound(new { message = "Usuário não encontrado." });
            }
        }


        [HttpPost]
        public IActionResult Create([FromBody] Usuario usuario)
        {
            try
            {
                _usuariosRepository.Create(usuario);
                return Ok(usuario);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro durante a criação do usuário na controladora. Mensagem: {ErrorMessage}", ex.Message);
                return StatusCode(500, "Erro interno durante a criação do usuário. Consulte os logs para obter mais detalhes.");
            }
        }



        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] Usuario usuario)
        {
            // Verifica se o Id fornecido na URL é o mesmo que no corpo da requisição.
            if (id != usuario.Id)
            {
                Log.Warning("Tentativa de atualização de usuário com ID inconsistente: {Id} != {UserId}", id, usuario.Id);
                return BadRequest(new { message = "ID inconsistente entre a URL e o corpo da requisição." });
            }

            try
            {
                Log.Information("Tentativa de atualizar o usuário com ID: {Id}", id);
                _usuariosRepository.Update(usuario);
                Log.Information("Usuário com ID: {Id} atualizado com sucesso.", id);
                return Ok(usuario);
            }
            catch (KeyNotFoundException ex)
            {
                Log.Warning(ex, "Não foi possível encontrar um usuário com ID: {Id} para atualização.", id);
                return NotFound(new { message = $"Usuário com Id {id} não encontrado." });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao tentar atualizar o usuário com ID: {Id}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ocorreu um erro ao atualizar o usuário. Por favor, tente novamente mais tarde." });
            }
        }



        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _usuariosRepository.Delete(id);
            return Ok();
        }
    }
}
