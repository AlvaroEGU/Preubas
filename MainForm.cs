using System;
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
        private ListBox lstResults;
        private Label lblUrl;
        private Label lblToken;
        private Label lblAction;
        private Label lblResults;
        private Button btnOpenUrl;

        public MainForm()
        {
            InitializeComponent();
            _gitHubService = new GitHubApiService();
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
                Text = "Resultados:",
                Location = new System.Drawing.Point(20, 210),
                Size = new System.Drawing.Size(150, 20)
            };
            this.Controls.Add(lblResults);

            // ListBox Results
            lstResults = new ListBox
            {
                Location = new System.Drawing.Point(20, 235),
                Size = new System.Drawing.Size(740, 300),
                ScrollAlwaysVisible = true
            };
            this.Controls.Add(lstResults);
        }

        private async void BtnExecute_Click(object? sender, EventArgs e)
        {
            try
            {
                lstResults.Items.Clear();
                
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
                lstResults.Items.Add($"Procesando: {owner}/{repo}" + 
                    (!string.IsNullOrEmpty(path) ? $"/{path}" : ""));
                lstResults.Items.Add("---");

                // Ejecutar acción seleccionada
                if (cmbAction.SelectedIndex == 0) // Commits
                {
                    await GetCommitsAsync(owner, repo, path);
                }
                else // Update (Contenido)
                {
                    await GetDirectoryContentsAsync(owner, repo, path);
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

        private async Task GetCommitsAsync(string owner, string repo, string? path)
        {
            try
            {
                var commits = await _gitHubService.GetCommitsAsync(owner, repo, path);
                
                if (commits.Count == 0)
                {
                    lstResults.Items.Add("No se encontraron commits.");
                    return;
                }

                lstResults.Items.Add($"Se encontraron {commits.Count} commits:");
                lstResults.Items.Add("");

                foreach (var commit in commits)
                {
                    lstResults.Items.Add(commit.ToString());
                    lstResults.Items.Add($"   URL: {commit.Url}");
                    lstResults.Items.Add("");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener commits: {ex.Message}", ex);
            }
        }

        private async Task GetDirectoryContentsAsync(string owner, string repo, string? path)
        {
            try
            {
                var contents = await _gitHubService.GetDirectoryContentsAsync(owner, repo, path ?? "");
                
                if (contents.Count == 0)
                {
                    lstResults.Items.Add("El directorio está vacío.");
                    return;
                }

                lstResults.Items.Add($"Contenido del directorio ({contents.Count} elementos):");
                lstResults.Items.Add("");

                foreach (var item in contents)
                {
                    lstResults.Items.Add(item.ToString());
                    lstResults.Items.Add($"   Ruta: {item.Path}");
                    lstResults.Items.Add($"   Tamaño: {item.Size} bytes");
                    lstResults.Items.Add($"   URL: {item.Url}");
                    lstResults.Items.Add("");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener contenido: {ex.Message}", ex);
            }
        }

        private void BtnOpenUrl_Click(object? sender, EventArgs e)
        {
            try
            {
                if (lstResults.SelectedItem == null)
                {
                    MessageBox.Show("Por favor, seleccione un elemento de la lista.", 
                        "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string selectedText = lstResults.SelectedItem.ToString() ?? "";
                
                // Buscar URL en el texto seleccionado o en las líneas siguientes
                string? url = null;
                int selectedIndex = lstResults.SelectedIndex;

                // Verificar si la línea seleccionada contiene URL
                if (selectedText.Contains("URL:"))
                {
                    url = selectedText.Substring(selectedText.IndexOf("URL:") + 4).Trim();
                }
                else if (selectedIndex + 1 < lstResults.Items.Count)
                {
                    // Verificar la siguiente línea
                    string nextLine = lstResults.Items[selectedIndex + 1].ToString() ?? "";
                    if (nextLine.Contains("URL:"))
                    {
                        url = nextLine.Substring(nextLine.IndexOf("URL:") + 4).Trim();
                    }
                }

                if (!string.IsNullOrEmpty(url))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
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
