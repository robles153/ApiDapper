using ApiDapper.Models;
using Dapper;
using Serilog;
using System.Data;
using System.Data.SqlClient;

namespace ApiDapper.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private IDbConnection _connection;
        public UsuarioRepository()
        {
            _connection = new SqlConnection("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Dapper;Integrated Security=True;Connect Timeout=30;Encrypt=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");

        }

        public void Create(Usuario usuario)
        {
            Log.Information("Iniciando a criação do usuário.");

            _connection.Open();
            var transaction = _connection.BeginTransaction();

            try
            {
                string sql = "INSERT INTO Usuarios(Nome, Email, RG, CPF, NomeMae, SituacaoCadastro, DataCadastro) VALUES (@Nome, @Email, @RG, @CPF, @NomeMae, @SituacaoCadastro, @DataCadastro); SELECT CAST(SCOPE_IDENTITY() AS INT)";
                usuario.Id = _connection.Query<int>(sql, usuario, transaction).Single();

                Log.Information("Usuário criado com sucesso. ID: {UserId}", usuario.Id);

                if (usuario.Contato != null)
                {
                    usuario.Contato.UsuarioId = usuario.Id;
                    string sqlContato = "INSERT INTO Contatos(UsuarioId, Telefone, Celular) VALUES (@UsuarioId, @Telefone, @Celular);  SELECT CAST(SCOPE_IDENTITY() AS INT)";
                    usuario.Contato.Id = _connection.Query<int>(sqlContato, usuario.Contato, transaction).Single();

                    Log.Information("Contato adicionado ao usuário. Contato ID: {ContatoId}", usuario.Contato.Id);
                }

                if (usuario.Enderecos != null && usuario.Enderecos.Count > 0)
                {
                    foreach (var endereco in usuario.Enderecos)
                    {
                        endereco.Usuarioid = usuario.Id;
                        string sqlEndereco = "INSERT INTO EnderecosEntrega (" +
                                                              " UsuarioId, " +
                                                            " NomeEndereco," +
                                                                     " CEP," +
                                                                  " Estado," +
                                                                  " Cidade," +
                                                                 " Bairro, " +
                                                               " Endereco, " +
                                                                 " Numero, " +
                                                            " Complemento) " +
                                                  "VALUES (" +
                                                              " @UsuarioId," +
                                                           " @NomeEndereco," +
                                                                   " @CEP, " +
                                                                 " @Estado," +
                                                                 " @Cidade," +
                                                                 " @Bairro," +
                                                              " @Endereco, " +
                                                                " @Numero, " +
                                                             " @Complemento); " +
                                                   "SELECT " +
                             "CAST(SCOPE_IDENTITY() AS INT)";
                        endereco.Id = _connection.Query<int>(sqlEndereco, endereco, transaction).Single();

                        Log.Information("Endereço de entrega adicionado ao usuário. Endereço ID: {EnderecoId}", endereco.Id);
                    }
                }

                if (usuario.Departamentos != null && usuario.Departamentos.Count > 0)
                {
                    foreach (var departamento in usuario.Departamentos)
                    {
                        string sqlDepartamento = "INSERT INTO UsuariosDepartamentos (UsuarioId, DepartamentoId) VALUES (@UsuarioId, @DepartamentoId)";
                        _connection.Execute(sqlDepartamento, new { UsuarioId = usuario.Id, DepartamentoId = departamento.Id }, transaction);

                        Log.Information("Departamento adicionado ao usuário. Departamento ID: {DepartamentoId}", departamento.Id);
                    }
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro durante a criação do usuário. Mensagem: {ErrorMessage}", ex.Message);

                try
                {
                    transaction.Rollback();
                    Log.Warning("Rollback da transação realizado devido a erro durante a criação do usuário.");

                }
                catch (Exception rollbackEx)
                {
                    Log.Error(rollbackEx, "Erro durante o rollback da transação. Mensagem: {ErrorMessage}", rollbackEx.Message);
                }
                finally
                {
                    _connection.Close();
                }

                throw;


            }


        }


        public void Delete(int id)
        {
            _connection.Execute("DELETE FROM Usuarios WHERE Id = @Id", new { Id = id });
        }

        public Usuario Get(int id)
        {
            Log.Information("Iniciando a obtenção do usuário com ID: {Id}.", id);
            Usuario? usuarioEncontrado = null;

            string sql = @"
                             SELECT U.*, C.*, EE.*, D.*
                               FROM Usuarios U
                          LEFT JOIN Contatos C ON C.UsuarioId = U.Id
                          LEFT JOIN EnderecosEntrega EE ON EE.UsuarioId = U.Id
                          LEFT JOIN UsuariosDepartamentos UD ON UD.UsuarioId = U.Id
                          LEFT JOIN Departamentos D ON D.Id = UD.DepartamentoId
                              WHERE U.Id = @Id";

            _connection.Query<Usuario, Contato, EnderecoEntrega, Departamento, Usuario>(
                sql,
                (usuario, contato, enderecoEntrega, departamento) =>
                {
                    if (usuarioEncontrado == null)
                    {
                        usuarioEncontrado = usuario;
                        usuarioEncontrado.Contato = contato;
                        usuarioEncontrado.Enderecos = new List<EnderecoEntrega>();
                        usuarioEncontrado.Departamentos = new List<Departamento>();
                    }

                    // Se EnderecosEntrega for nulo, inicialize-o antes de chamar Any.
                    usuarioEncontrado.Enderecos ??= new List<EnderecoEntrega>();

                    if (enderecoEntrega != null && !usuarioEncontrado.Enderecos.Any(ee => ee.Id == enderecoEntrega.Id))
                    {
                        usuarioEncontrado.Enderecos.Add(enderecoEntrega);
                    }

                    // Se EnderecosEntrega for nulo, inicialize-o antes de chamar Any.
                    usuarioEncontrado.Departamentos ??= new List<Departamento>();

                    if (departamento != null && !usuarioEncontrado.Departamentos.Any(d => d.Id == departamento.Id))
                    {
                        usuarioEncontrado.Departamentos.Add(departamento);
                    }

                    return usuarioEncontrado;
                },
                new { Id = id }
            ).FirstOrDefault();

            if (usuarioEncontrado != null)
            {
                Log.Information("Usuário com ID: {Id} obtido com sucesso.", id);
                return usuarioEncontrado;
            }
            else
            {
                Log.Warning("Nenhum usuário com ID: {Id} foi encontrado.", id);
                throw new KeyNotFoundException($"Nenhum usuário com ID {id} foi encontrado.");
            }

        }


        public List<Usuario> GetAll()
        {
            Log.Information("Iniciando a obtenção de todos os usuários.");

            // Lista para armazenar os objetos Usuario após serem construídos a partir dos dados do banco de dados.
            List<Usuario> usuarios = new List<Usuario>();

            try
            {
                // Consulta SQL que junta as tabelas de usuário, contato, endereço de entrega e departamento.
                string sql = @"
                              SELECT U.*, C.*, EE.*, D.*
                                FROM Usuarios U
                           LEFT JOIN Contatos C ON C.UsuarioId = U.Id
                           LEFT JOIN EnderecosEntrega EE ON EE.UsuarioId = U.Id
                           LEFT JOIN UsuariosDepartamentos UD ON UD.UsuarioId = U.Id
                           LEFT JOIN Departamentos D ON UD.DepartamentoId = D.Id";

                // Executa a consulta e mapeia os resultados para as entidades do domínio.
                _connection.Query<Usuario, Contato, EnderecoEntrega, Departamento, Usuario>(sql,
                    (usuario, contato, enderecoEntrega, departamento) =>
                    {
                        // Se o resultado da consulta for 'null', inicializa um novo objeto Usuario.
                        usuario ??= new Usuario();

                        // Procura por um usuário existente na lista ou adiciona o novo usuário.
                        Usuario usuarioExistente = usuarios.FirstOrDefault(u => u.Id == usuario.Id) ?? usuario;

                        if (!usuarios.Contains(usuarioExistente))
                        {
                            // Inicializa as listas de endereços e departamentos para o novo usuário.
                            usuarioExistente.Enderecos = new List<EnderecoEntrega>();
                            usuarioExistente.Departamentos = new List<Departamento>();

                            // Associa o contato recuperado da consulta ao usuário.
                            usuarioExistente.Contato = contato;

                            // Adiciona o usuário à lista de usuários compilada.
                            usuarios.Add(usuarioExistente);
                            Log.Information("Usuário com ID {Id} adicionado à lista de usuários.", usuarioExistente.Id);
                        }

                        usuarioExistente.Enderecos ??= new List<EnderecoEntrega>();

                        // Adiciona o endereço de entrega ao usuário se ainda não estiver presente.
                        if (enderecoEntrega != null && !usuarioExistente.Enderecos.Any(e => e.Id == enderecoEntrega.Id))
                        {
                            usuarioExistente.Enderecos.Add(enderecoEntrega);
                            Log.Information("Endereço de entrega adicionado ao usuário com ID {Id}.", usuarioExistente.Id);
                        }

                        usuarioExistente.Departamentos ??= new List<Departamento>();
                        // Adiciona o departamento ao usuário se ainda não estiver presente.
                        if (departamento != null && !usuarioExistente.Departamentos.Any(d => d.Id == departamento.Id))
                        {
                            usuarioExistente.Departamentos.Add(departamento);
                            Log.Information("Departamento adicionado ao usuário com ID {Id}.", usuarioExistente.Id);
                        }

                        return usuarioExistente;
                    },
                    splitOn: "Id");

                Log.Information("Obtenção de todos os usuários completada com sucesso.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ocorreu um erro ao obter todos os usuários.");
                throw;
            }

            return usuarios;
        }




        public void Update(Usuario usuario)
        {
            IDbTransaction? transaction = null;

            Log.Information("Iniciando a atualização do usuário com Id: {UserId}", usuario.Id);

            try
            {
                _connection.Open();
                Log.Debug("Conexão com o banco de dados aberta.");

                transaction = _connection.BeginTransaction();
                Log.Debug("Transação iniciada.");

                string sql = "UPDATE Usuarios SET Nome = @Nome, Email = @Email, Sexo = @Sexo, RG = @RG, CPF = @CPF, NomeMae = @NomeMae, SituacaoCadastro = @SituacaoCadastro WHERE Id = @Id";
                _connection.Execute(sql, usuario, transaction);
                Log.Information("Usuário com Id: {UserId} atualizado.", usuario.Id);

                if (usuario.Contato != null)
                {
                    string sqlContato = "UPDATE Contatos SET Telefone = @Telefone, Celular = @Celular WHERE UsuarioId = @UsuarioId";
                    _connection.Execute(sqlContato, usuario.Contato, transaction);
                    Log.Information("Contato do usuário com Id: {UserId} atualizado.", usuario.Id);
                }

                string sqlDelete = "DELETE FROM EnderecosEntrega WHERE UsuarioId = @Id;";
                _connection.Execute(sqlDelete, new { Id = usuario.Id }, transaction);
                Log.Information("Endereços de entrega do usuário com Id: {UserId} removidos.", usuario.Id);

                if (usuario.Enderecos != null && usuario.Enderecos.Count > 0)
                {
                    foreach (var endereco in usuario.Enderecos)
                    {
                        endereco.Usuarioid = usuario.Id;
                        string sqlEndereco = "INSERT INTO EnderecosEntrega (UsuarioId, NomeEndereco, CEP, Estado, Cidade, Bairro, Endereco, Numero, Complemento) " +
                                             "VALUES (@UsuarioId, @NomeEndereco, @CEP, @Estado, @Cidade, @Bairro, @Endereco, @Numero, @Complemento); " +
                                             "SELECT CAST(SCOPE_IDENTITY() AS INT);";
                        endereco.Id = _connection.Query<int>(sqlEndereco, endereco, transaction).Single();
                        Log.Information("Endereço com Id: {EnderecoId} adicionado para o usuário com Id: {UserId}.", endereco.Id, usuario.Id);
                    }
                }

                string sqlDeletedepartamento = "DELETE FROM UsuariosDepartamentos WHERE UsuarioId = @Id";
                _connection.Execute(sqlDeletedepartamento, new { Id = usuario.Id }, transaction);

                if (usuario.Departamentos != null && usuario.Departamentos.Count > 0)
                {
                    foreach (var departamento in usuario.Departamentos)
                    {
                        string sqlDepartamento = "INSERT INTO UsuariosDepartamentos (UsuarioId, DepartamentoId) VALUES (@UsuarioId, @DepartamentoId)";
                        _connection.Execute(sqlDepartamento, new { UsuarioId = usuario.Id, DepartamentoId = departamento.Id }, transaction);

                        Log.Information("Departamento adicionado ao usuário. Departamento ID: {DepartamentoId}", departamento.Id);
                    }
                }

                transaction.Commit();
                Log.Information("Transação commitada para o usuário com Id: {UserId}.", usuario.Id);
            }
            catch (Exception e)
            {
                Log.Error(e, "Erro ao atualizar o usuário com Id: {UserId}.", usuario.Id);
                try
                {
                    transaction?.Rollback();
                    Log.Information("Transação revertida para o usuário com Id: {UserId} devido a erro.", usuario.Id);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Falha ao reverter a transação para o usuário com Id: {UserId}.", usuario.Id);
                    throw;
                }
                throw new Exception("Erro ao atualizar usuário.", e);
            }
            finally
            {
                _connection.Close();
                Log.Debug("Conexão com o banco de dados fechada.");
            }


        }


    }
}