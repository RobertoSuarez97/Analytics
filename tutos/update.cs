using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.IO.Compression;


namespace msgapp
{
    public partial class update : Form
    {
        private string temp = ""; // Carpeta de descarga
        private string versionActual;
        private string versionNueva;

        public update()
        {
            InitializeComponent();
            progressBar1.Visible = false;

            // Obtener versiones
            versionActual = Actualizaciones.getVersionActual();
            versionNueva = Actualizaciones.getVersionNueva();

            // Mostrar en la interfaz
            lblVersionActual.Text = $"Versión actual: {versionActual}";
            lbversion.Text = $"Nueva versión: {versionNueva}";
            rtbcambios.Text = Actualizaciones.getCambios();
        }

        private void btnactualizar_Click(object sender, EventArgs e)
        {
            progressBar1.Visible = true;
            btnactualizar.Text = "Actualizando...";
            btnactualizar.Enabled = false;
            progressBar1.Value = 0;

            temp = Path.Combine(Path.GetTempPath(), "msgapp_update");

            if (Directory.Exists(temp))
                eliminarCarpeta(temp);

            Directory.CreateDirectory(temp);

            string zipPath = Path.Combine(temp, "update.zip");
            string extractPath = Path.Combine(temp, "extract");
            Directory.CreateDirectory(extractPath);

            string link = $"http://localhost/msgapp/msgapp-v{versionNueva}.zip";
            WebClient wc = new WebClient();

            wc.DownloadProgressChanged += (s, progress) =>
            {
                progressBar1.Value = progress.ProgressPercentage;
            };

            wc.DownloadFileCompleted += (s, _e) =>
            {
                if (_e.Error != null)
                {
                    MostrarError("Error en la descarga", _e.Error.Message);
                    return;
                }

                if (!VerificarArchivo(zipPath))
                {
                    MostrarError("Error", "El archivo descargado está vacío o corrupto.");
                    return;
                }

                try
                {
                    try
                    {
                        ZipFile.ExtractToDirectory(zipPath, extractPath);
                    }
                    catch (Exception ex)
                    {
                        MostrarError("Error al extraer el archivo ZIP", ex.Message);
                    }
                    string programa = AppDomain.CurrentDomain.BaseDirectory;
                    string backupPath = Path.Combine(programa, "backup");

                    RealizarCopiaSeguridad(programa, backupPath);

                    string batch = Path.Combine(temp, "_update.bat");
                    try
                    {
                        File.WriteAllText(batch, GenerarScriptBatch(programa, extractPath, temp));
                    }
                    catch (Exception ex)
                    {
                        MostrarError("Error al generar el script", ex.Message);
                    }

                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = batch,
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });

                    progressBar1.Visible = false;
                    Application.Exit();
                }
                catch (Exception ex)
                {
                    MostrarError("Error en la actualización", ex.Message);
                    RestaurarCopiaSeguridad();
                }
            };

            try
            {
                wc.DownloadFileAsync(new Uri(link), zipPath);
            }
            catch (Exception ex)
            {
                MostrarError("Error al iniciar la descarga", ex.Message);
            }
        }

        private bool VerificarArchivo(string filePath)
        {
            return File.Exists(filePath) && new FileInfo(filePath).Length > 0;
        }

        private void RealizarCopiaSeguridad(string sourcePath, string backupPath)
        {
            if (Directory.Exists(backupPath))
                eliminarCarpeta(backupPath);

            Directory.CreateDirectory(backupPath);
            foreach (string file in Directory.GetFiles(sourcePath))
            {
                File.Copy(file, Path.Combine(backupPath, Path.GetFileName(file)), true);
            }
        }

        private void RestaurarCopiaSeguridad()
        {
            string programa = AppDomain.CurrentDomain.BaseDirectory;
            string backupPath = Path.Combine(programa, "backup");

            if (Directory.Exists(backupPath))
            {
                foreach (string file in Directory.GetFiles(backupPath))
                {
                    File.Copy(file, Path.Combine(programa, Path.GetFileName(file)), true);
                }
            }
        }

        private string GenerarScriptBatch(string programa, string extractPath, string temp)
        {
            return $@"
@echo off
echo Cerrando aplicación...
taskkill /IM msgapp.exe /F
timeout /t 3

echo Restaurando archivos...
xcopy /E /Y /C ""{extractPath}\*"" ""{programa}""

echo Eliminando archivos temporales...
rd /S /Q ""{temp}""

del /F /Q ""%~f0""
exit
";
        }

        private void MostrarError(string titulo, string mensaje)
        {
            MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnactualizar.Text = "Actualizar";
            btnactualizar.Enabled = true;
        }

        private void eliminarCarpeta(string ruta)
        {
            try
            {
                foreach (string f in Directory.GetFiles(ruta)) File.Delete(f);
                foreach (string d in Directory.GetDirectories(ruta)) eliminarCarpeta(d);
                Directory.Delete(ruta, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error eliminando carpeta: " + ex.Message);
            }
        }

    }
}
