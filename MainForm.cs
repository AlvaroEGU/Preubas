using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace GitHubDesktopApp
{
    public partial class MainForm : Form
    {
        private GitHubApiService _gitHubService;
        private TextBox txtGitHubUrl;
        private TextBox txtToken;
        private ComboBox cmbAction;
        private Button btnExecute;
        private TreeView treeViewFiles;
        private Label lblUrl;
        private Label lblToken;
        private Label lblAction;
        private Label lblResults;
        private Button btnOpenUrl;
        private ContextMenuStrip contextMenu;
        private ProgressBar progressBar;
        private Label lblDownloadStatus;
        private HttpClient _httpClient;
        private ImageList imageList;
        private string _currentOwner = string.Empty;
        private string _currentRepo = string.Empty;
        private Dictionary<string, string> _repositoryTags = new Dictionary<string, string>();

        public MainForm()
        {
            InitializeComponent();
            _gitHubService = new GitHubApiService();
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(10); // Timeout de 10 minutos para archivos grandes
        }

        private void InitializeComponent()
        {
            // Configuración del formulario
            this.Text = "GitHub Desktop App - Conector API";
            this.Size = new System.Drawing.Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Label URL
            lblUrl = new Label
            {
                Text = "URL del Repositorio GitHub:",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(200, 20)
            };
            this.Controls.Add(lblUrl);

            // TextBox URL
            txtGitHubUrl = new TextBox
            {
                Location = new System.Drawing.Point(20, 45),
                Size = new System.Drawing.Size(740, 25),
                PlaceholderText = "Ejemplo: https://github.com/usuario/repositorio/tree/main/carpeta"
            };
            this.Controls.Add(txtGitHubUrl);

            // Label Token
            lblToken = new Label
            {
                Text = "Token de GitHub (opcional):",
                Location = new System.Drawing.Point(20, 80),
                Size = new System.Drawing.Size(200, 20)
            };
            this.Controls.Add(lblToken);

            // TextBox Token
            txtToken = new TextBox
            {
                Location = new System.Drawing.Point(20, 105),
                Size = new System.Drawing.Size(740, 25),
                PasswordChar = '*',
                PlaceholderText = "ghp_xxxxxxxxxxxxxxxxxxxx (aumenta límite de requests)"
            };
            this.Controls.Add(txtToken);

            // Label Action
            lblAction = new Label
            {
                Text = "Acción a realizar:",
                Location = new System.Drawing.Point(20, 140),
                Size = new System.Drawing.Size(150, 20)
            };
            this.Controls.Add(lblAction);

            // ComboBox Action
            cmbAction = new ComboBox
            {
                Location = new System.Drawing.Point(20, 165),
                Size = new System.Drawing.Size(250, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbAction.Items.AddRange(new object[] { "Commits", "Update (Contenido)" });
            cmbAction.SelectedIndex = 0;
            this.Controls.Add(cmbAction);

            // Botón Ejecutar
            btnExecute = new Button
            {
                Text = "Ejecutar",
                Location = new System.Drawing.Point(290, 165),
                Size = new System.Drawing.Size(120, 30),
                BackColor = System.Drawing.Color.FromArgb(0, 120, 215),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnExecute.Click += BtnExecute_Click;
            this.Controls.Add(btnExecute);

            // Botón Abrir URL seleccionada
            btnOpenUrl = new Button
            {
                Text = "Abrir URL Seleccionada",
                Location = new System.Drawing.Point(430, 165),
                Size = new System.Drawing.Size(180, 30),
                BackColor = System.Drawing.Color.FromArgb(40, 167, 69),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnOpenUrl.Click += BtnOpenUrl_Click;
            this.Controls.Add(btnOpenUrl);

            // Label Results
            lblResults = new Label
            {
                Text = "Explorador de Archivos:",
                Location = new System.Drawing.Point(20, 210),
                Size = new System.Drawing.Size(200, 20)
            };
            this.Controls.Add(lblResults);

            // ImageList para iconos del TreeView
            imageList = new ImageList
            {
                ImageSize = new System.Drawing.Size(16, 16),
                ColorDepth = ColorDepth.Depth32Bit
            };
            
            // Crear iconos simples usando Graphics
            var folderIcon = CreateFolderIcon();
            var fileIcon = CreateFileIcon();
            var downloadableIcon = CreateDownloadableIcon();
            
            imageList.Images.Add("folder", folderIcon);
            imageList.Images.Add("file", fileIcon);
            imageList.Images.Add("downloadable", downloadableIcon);

            // TreeView para archivos
            treeViewFiles = new TreeView
            {
                Location = new System.Drawing.Point(20, 235),
                Size = new System.Drawing.Size(740, 260),
                ImageList = imageList,
                ImageIndex = 0,
                SelectedImageIndex = 0
            };
            treeViewFiles.BeforeExpand += TreeViewFiles_BeforeExpand;
            treeViewFiles.NodeMouseDoubleClick += TreeViewFiles_NodeMouseDoubleClick;
            treeViewFiles.NodeMouseClick += TreeViewFiles_NodeMouseClick;
            this.Controls.Add(treeViewFiles);

            // ContextMenu para TreeView
            contextMenu = new ContextMenuStrip();
            var downloadMenuItem = new ToolStripMenuItem("📥 Descargar archivo");
            downloadMenuItem.Click += DownloadMenuItem_Click;
            var openUrlMenuItem = new ToolStripMenuItem("🌐 Abrir en navegador");
            openUrlMenuItem.Click += OpenUrlMenuItem_Click;
            var refreshMenuItem = new ToolStripMenuItem("🔄 Actualizar");
            refreshMenuItem.Click += RefreshMenuItem_Click;
            
            contextMenu.Items.Add(downloadMenuItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(openUrlMenuItem);
            contextMenu.Items.Add(refreshMenuItem);
            contextMenu.Opening += ContextMenu_Opening;
            
            treeViewFiles.ContextMenuStrip = contextMenu;

            // Label de estado de descarga (inicialmente oculto)
            lblDownloadStatus = new Label
            {
                Location = new System.Drawing.Point(20, 505),
                Size = new System.Drawing.Size(740, 20),
                Text = "",
                Visible = false
            };
            this.Controls.Add(lblDownloadStatus);

            // ProgressBar (inicialmente oculto)
            progressBar = new ProgressBar
            {
                Location = new System.Drawing.Point(20, 530),
                Size = new System.Drawing.Size(740, 25),
                Visible = false
            };
            this.Controls.Add(progressBar);
        }

        private System.Drawing.Bitmap CreateFolderIcon()
        {
            var bmp = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.Transparent);
                g.FillRectangle(System.Drawing.Brushes.Orange, 2, 6, 12, 8);
                g.FillPolygon(System.Drawing.Brushes.DarkOrange, new System.Drawing.Point[] {
                    new System.Drawing.Point(2, 6),
                    new System.Drawing.Point(6, 4),
                    new System.Drawing.Point(10, 6)
                });
            }
            return bmp;
        }

        private System.Drawing.Bitmap CreateFileIcon()
        {
            var bmp = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.Transparent);
                g.FillRectangle(System.Drawing.Brushes.LightGray, 4, 2, 8, 12);
                g.DrawRectangle(System.Drawing.Pens.Gray, 4, 2, 8, 12);
            }
            return bmp;
        }

        private System.Drawing.Bitmap CreateDownloadableIcon()
        {
            var bmp = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.Transparent);
                g.FillRectangle(System.Drawing.Brushes.LightBlue, 4, 2, 8, 12);
                g.DrawRectangle(System.Drawing.Pens.Blue, 4, 2, 8, 12);
                // Flecha de descarga
                g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Green, 2), 8, 6, 8, 10);
                g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Green, 1), 6, 9, 8, 11);
                g.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Green, 1), 10, 9, 8, 11);
            }
            return bmp;
        }

        private void TreeViewFiles_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                treeViewFiles.SelectedNode = e.Node;
            }
        }

        private async void TreeViewFiles_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is ContentInfo contentInfo)
            {
                if (contentInfo.Type == "Dir")
                {
                    // Si es un directorio y no tiene hijos reales, expandir
                    if (!e.Node.IsExpanded)
                    {
                        e.Node.Expand();
                    }
                }
                else if (!string.IsNullOrEmpty(contentInfo.DownloadUrl))
                {
                    // Si es un archivo descargable, iniciar descarga
                    using var saveDialog = new SaveFileDialog
                    {
                        FileName = contentInfo.Name,
                        Title = "Guardar archivo",
                        Filter = "Todos los archivos (*.*)|*.*"
                    };

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        await DownloadFileAsync(contentInfo.DownloadUrl, saveDialog.FileName, contentInfo.Name);
                    }
                }
            }
        }

        private async void TreeViewFiles_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
        {
            if (e.Node != null && e.Node.Tag is ContentInfo contentInfo && contentInfo.Type == "Dir")
            {
                // Verificar si ya cargamos el contenido (si solo tiene un nodo dummy)
                if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "Cargando...")
                {
                    e.Node.Nodes.Clear();
                    await LoadDirectoryContents(e.Node, contentInfo.Path);
                }
            }
        }

        private void ContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (treeViewFiles.SelectedNode == null)
            {
                e.Cancel = true;
                return;
            }

            // Habilitar/deshabilitar "Descargar archivo" según si es un archivo o directorio
            if (treeViewFiles.SelectedNode.Tag is ContentInfo contentInfo)
            {
                bool isFile = contentInfo.Type != "Dir";
                contextMenu.Items[0].Enabled = isFile && !string.IsNullOrEmpty(contentInfo.DownloadUrl);
            }
            else
            {
                contextMenu.Items[0].Enabled = false;
            }
        }

        private async void RefreshMenuItem_Click(object? sender, EventArgs e)
        {
            if (treeViewFiles.SelectedNode?.Tag is ContentInfo contentInfo)
            {
                if (contentInfo.Type == "Dir")
                {
                    treeViewFiles.SelectedNode.Nodes.Clear();
                    treeViewFiles.SelectedNode.Nodes.Add("Cargando...");
                    await LoadDirectoryContents(treeViewFiles.SelectedNode, contentInfo.Path);
                }
            }
        }

        private async void DownloadMenuItem_Click(object? sender, EventArgs e)
        {
            if (treeViewFiles.SelectedNode?.Tag is not ContentInfo contentInfo)
                return;

            if (string.IsNullOrEmpty(contentInfo.DownloadUrl))
            {
                MessageBox.Show("No se puede descargar este elemento.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Mostrar SaveFileDialog
            using var saveDialog = new SaveFileDialog
            {
                FileName = contentInfo.Name,
                Title = "Guardar archivo",
                Filter = "Todos los archivos (*.*)|*.*"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                await DownloadFileAsync(contentInfo.DownloadUrl, saveDialog.FileName, contentInfo.Name);
            }
        }

        private void OpenUrlMenuItem_Click(object? sender, EventArgs e)
        {
            if (treeViewFiles.SelectedNode?.Tag is not ContentInfo contentInfo)
                return;

            if (!string.IsNullOrEmpty(contentInfo.Url))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = contentInfo.Url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al abrir URL: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task DownloadFileAsync(string downloadUrl, string savePath, string fileName)
        {
            try
            {
                // Mostrar controles de progreso
                progressBar.Visible = true;
                progressBar.Value = 0;
                lblDownloadStatus.Visible = true;
                lblDownloadStatus.Text = $"Descargando {fileName}...";
                btnExecute.Enabled = false;

                var startTime = DateTime.Now;

                // Descargar con progreso
                using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                var canReportProgress = totalBytes != -1;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                var totalBytesRead = 0L;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;

                    if (canReportProgress)
                    {
                        var progressPercentage = (int)((totalBytesRead * 100) / totalBytes);
                        progressBar.Value = Math.Min(progressPercentage, 100);

                        var elapsed = DateTime.Now - startTime;
                        var speed = totalBytesRead / elapsed.TotalSeconds;
                        var speedText = FormatBytes((long)speed);

                        lblDownloadStatus.Text = $"Descargando {fileName}: {FormatBytes(totalBytesRead)} de {FormatBytes(totalBytes)} ({progressPercentage}%) - {speedText}/s";
                    }
                    else
                    {
                        lblDownloadStatus.Text = $"Descargando {fileName}: {FormatBytes(totalBytesRead)}...";
                    }

                    Application.DoEvents(); // Actualizar UI
                }

                // Descarga completada
                progressBar.Value = 100;
                lblDownloadStatus.Text = $"✅ Descarga completada: {fileName}";

                var result = MessageBox.Show(
                    $"Archivo descargado correctamente:\n{savePath}\n\n¿Desea abrir la carpeta de destino?",
                    "Descarga Completada",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{savePath}\"");
                }

                // Ocultar controles de progreso después de 3 segundos
                await Task.Delay(3000);
                progressBar.Visible = false;
                lblDownloadStatus.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al descargar el archivo:\n{ex.Message}", "Error de Descarga",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                lblDownloadStatus.Text = "❌ Error en la descarga";
                progressBar.Value = 0;
            }
            finally
            {
                btnExecute.Enabled = true;
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private async void BtnExecute_Click(object? sender, EventArgs e)
        {
            try
            {
                treeViewFiles.Nodes.Clear();
                
                // Validar URL
                if (string.IsNullOrWhiteSpace(txtGitHubUrl.Text))
                {
                    MessageBox.Show("Por favor, ingrese una URL de GitHub.", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Configurar token si existe
                if (!string.IsNullOrWhiteSpace(txtToken.Text))
                {
                    _gitHubService.SetToken(txtToken.Text);
                }

                // Parsear URL
                var (owner, repo, path) = ParseGitHubUrl(txtGitHubUrl.Text);
                
                if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
                {
                    MessageBox.Show("URL de GitHub inválida. Use el formato:\n" +
                        "https://github.com/usuario/repositorio\n" +
                        "o\n" +
                        "https://github.com/usuario/repositorio/tree/rama/carpeta", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                btnExecute.Enabled = false;
                _currentOwner = owner;
                _currentRepo = repo;
                
                // Cargar tags del repositorio
                _repositoryTags = await _gitHubService.GetRepositoryTagsAsync(owner, repo);
                
                lblResults.Text = $"Explorador de Archivos: {owner}/{repo}" + 
                    (!string.IsNullOrEmpty(path) ? $"/{path}" : "");

                // Ejecutar acción seleccionada
                if (cmbAction.SelectedIndex == 0) // Commits
                {
                    await GetCommitsAsync(owner, repo, path);
                }
                else // Update (Contenido)
                {
                    await LoadDirectoryContents(null, path);
                }

                btnExecute.Enabled = true;
            }
            catch (Exception ex)
            {
                btnExecute.Enabled = true;
                MessageBox.Show($"Error: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadDirectoryContents(TreeNode? parentNode, string? path)
        {
            try
            {
                var contents = await _gitHubService.GetDirectoryContentsAsync(_currentOwner, _currentRepo, path);
                
                if (contents.Count == 0)
                {
                    if (parentNode == null)
                    {
                        var emptyNode = treeViewFiles.Nodes.Add("El directorio está vacío");
                        emptyNode.ImageKey = "file";
                        emptyNode.SelectedImageKey = "file";
                    }
                    return;
                }

                // Obtener información de commits para archivos (de forma asíncrona)
                var tasks = contents.Where(c => c.Type != "Dir").Select(async item =>
                {
                    var lastCommit = await _gitHubService.GetFileLastCommitAsync(_currentOwner, _currentRepo, item.Path);
                    if (lastCommit != null)
                    {
                        item.LastCommit = lastCommit;
                        // Verificar si el commit tiene un tag asociado
                        if (_repositoryTags.ContainsKey(lastCommit.Sha))
                        {
                            item.VersionTag = _repositoryTags[lastCommit.Sha];
                        }
                    }
                });

                await Task.WhenAll(tasks);

                // Agregar nodos al árbol
                foreach (var item in contents.OrderBy(c => c.Type == "Dir" ? 0 : 1).ThenBy(c => c.Name))
                {
                    TreeNode node;
                    
                    if (parentNode == null)
                    {
                        node = treeViewFiles.Nodes.Add(item.Name);
                    }
                    else
                    {
                        node = parentNode.Nodes.Add(item.Name);
                    }

                    node.Tag = item;

                    // Configurar icono y texto según tipo
                    if (item.Type == "Dir")
                    {
                        node.ImageKey = "folder";
                        node.SelectedImageKey = "folder";
                        // Agregar nodo dummy para permitir expansión
                        node.Nodes.Add("Cargando...");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(item.DownloadUrl))
                        {
                            node.ImageKey = "downloadable";
                            node.SelectedImageKey = "downloadable";
                        }
                        else
                        {
                            node.ImageKey = "file";
                            node.SelectedImageKey = "file";
                        }

                        // Agregar información de versión como subnodo
                        if (item.LastCommit != null || !string.IsNullOrEmpty(item.VersionTag))
                        {
                            var versionInfo = item.GetVersionInfo();
                            var versionNode = node.Nodes.Add($"📌 {versionInfo}");
                            versionNode.ForeColor = System.Drawing.Color.Gray;
                            
                            if (item.LastCommit != null)
                            {
                                var commitMsg = item.LastCommit.Message.Split('\n')[0];
                                if (commitMsg.Length > 50)
                                    commitMsg = commitMsg.Substring(0, 47) + "...";
                                var msgNode = node.Nodes.Add($"💬 {commitMsg}");
                                msgNode.ForeColor = System.Drawing.Color.DarkGray;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar contenido: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task GetCommitsAsync(string owner, string repo, string? path)
        {
            try
            {
                var commits = await _gitHubService.GetCommitsAsync(owner, repo, path);
                
                if (commits.Count == 0)
                {
                    var emptyNode = treeViewFiles.Nodes.Add("No se encontraron commits.");
                    emptyNode.ImageKey = "file";
                    return;
                }

                var rootNode = treeViewFiles.Nodes.Add($"📋 Commits ({commits.Count})");
                rootNode.ImageKey = "folder";
                rootNode.SelectedImageKey = "folder";

                foreach (var commit in commits)
                {
                    var commitNode = rootNode.Nodes.Add(commit.ToString());
                    commitNode.ImageKey = "file";
                    commitNode.SelectedImageKey = "file";
                    
                    var urlNode = commitNode.Nodes.Add($"🔗 {commit.Url}");
                    urlNode.ForeColor = System.Drawing.Color.Blue;
                }

                rootNode.Expand();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener commits: {ex.Message}", ex);
            }
        }

        private void BtnOpenUrl_Click(object? sender, EventArgs e)
        {
            try
            {
                if (treeViewFiles.SelectedNode == null)
                {
                    MessageBox.Show("Por favor, seleccione un elemento del árbol.", 
                        "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (treeViewFiles.SelectedNode.Tag is ContentInfo contentInfo && !string.IsNullOrEmpty(contentInfo.Url))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = contentInfo.Url,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("No se encontró URL en el elemento seleccionado.", 
                        "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir URL: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Parsea una URL de GitHub y extrae el owner, repositorio y path
        /// </summary>
        private (string owner, string repo, string? path) ParseGitHubUrl(string url)
        {
            try
            {
                // Patrón para URLs de GitHub
                // https://github.com/usuario/repo o
                // https://github.com/usuario/repo/tree/rama/carpeta/subcarpeta
                var pattern = @"github\.com/([^/]+)/([^/]+)(?:/tree/[^/]+/(.+))?";
                var match = Regex.Match(url, pattern);

                if (match.Success)
                {
                    string owner = match.Groups[1].Value;
                    string repo = match.Groups[2].Value;
                    string? path = match.Groups[3].Success ? match.Groups[3].Value : null;

                    return (owner, repo, path);
                }

                return (string.Empty, string.Empty, null);
            }
            catch
            {
                return (string.Empty, string.Empty, null);
            }
        }

    }
}
