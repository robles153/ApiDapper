using ApiDapper.Models;

namespace ApiDapper.Repositories
{
    public interface IUsuarioRepository
    {
        public List<Usuario> GetAll();
        public Usuario Get(int id);
        public void Update(Usuario usuario);
        public void Delete(int id);
        public void Create(Usuario usuario);
    }
}
