using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitHubDesktopApp
{
    /// <summary>
    /// Servicio para interactuar con la API de GitHub
    /// </summary>
    public class GitHubApiService
    {
        private readonly GitHubClient _client;
        private string? _token;

        public GitHubApiService(string? token = null)
        {
            _token = token;
            _client = new GitHubClient(new ProductHeaderValue("GitHubDesktopApp"));
            
            if (!string.IsNullOrEmpty(_token))
            {
                var tokenAuth = new Credentials(_token);
                _client.Credentials = tokenAuth;
            }
        }

        /// <summary>
        /// Establece el token de autenticación
        /// </summary>
        public void SetToken(string token)
        {
            _token = token;
            var tokenAuth = new Credentials(token);
            _client.Credentials = tokenAuth;
        }

        /// <summary>
        /// Obtiene los commits de un repositorio
        /// </summary>
        /// <param name="owner">Propietario del repositorio</param>
        /// <param name="repoName">Nombre del repositorio</param>
        /// <param name="path">Ruta del directorio (opcional)</param>
        /// <returns>Lista de commits</returns>
        public async Task<List<CommitInfo>> GetCommitsAsync(string owner, string repoName, string? path = null)
        {
            try
            {
                var request = new CommitRequest();
                if (!string.IsNullOrEmpty(path))
                {
                    request.Path = path;
                }

                var commits = await _client.Repository.Commit.GetAll(owner, repoName, request);
                
                return commits.Select(c => new CommitInfo
                {
                    Sha = c.Sha,
                    Message = c.Commit.Message,
                    Author = c.Commit.Author.Name,
                    Date = c.Commit.Author.Date.DateTime,
                    Url = c.HtmlUrl
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener commits: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obtiene el contenido de un directorio
        /// </summary>
        /// <param name="owner">Propietario del repositorio</param>
        /// <param name="repoName">Nombre del repositorio</param>
        /// <param name="path">Ruta del directorio</param>
        /// <returns>Lista de archivos y directorios</returns>
        public async Task<List<ContentInfo>> GetDirectoryContentsAsync(string owner, string repoName, string path = "")
        {
            try
            {
                var contents = await _client.Repository.Content.GetAllContents(owner, repoName, path);
                
                return contents.Select(c => new ContentInfo
                {
                    Name = c.Name,
                    Path = c.Path,
                    Type = c.Type.ToString(),
                    Size = c.Size,
                    Url = c.HtmlUrl,
                    DownloadUrl = c.DownloadUrl
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener contenido del directorio: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Verifica si las credenciales son válidas
        /// </summary>
        public async Task<bool> ValidateCredentialsAsync()
        {
            try
            {
                var user = await _client.User.Current();
                return user != null;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Información de un commit
    /// </summary>
    public class CommitInfo
    {
        public string Sha { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Url { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"[{Date:yyyy-MM-dd HH:mm}] {Author}: {Message.Split('\n')[0]} (SHA: {Sha.Substring(0, 7)})";
        }
    }

    /// <summary>
    /// Información de contenido de directorio
    /// </summary>
    public class ContentInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Size { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? DownloadUrl { get; set; }

        public override string ToString()
        {
            return $"{(Type == "Dir" ? "📁" : "📄")} {Name} ({Type})";
        }
    }
}
