using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Compression;
using Microsoft.Win32;

namespace analytics_AddIn
{
    public partial class update : Form
    {
        private string temp = "";
        public string versionActual { get; set; }
        public string versionNueva { get; set; }
        public string cambios { get; set; }
        public string link { get; set; }
        public string AppName = "analytics";

        public update()
        {
            InitializeComponent();
            this.Load += update_Load;
            progressBar1.Visible = false;
        }

        private void update_Load(object sender, EventArgs e)
        {
            lblVersionActual.Text = $"Versión actual: {versionActual}";
            lbversion.Text = $"Nueva versión: {versionNueva}";
            rtbcambios.Text = cambios;
        }

        private async void btnactualizar_Click(object sender, EventArgs e)
        {
            try
            {
                // 🔴 Mensaje de confirmación antes de actualizar
                DialogResult confirmacion = MessageBox.Show(
                    "La actualización requiere cerrar SolidWorks.\n¿Está seguro de continuar?",
                    "Confirmar actualización",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (confirmacion != DialogResult.Yes)
                {
                    return;
                }
                
                progressBar1.Visible = true;
                btnactualizar.Text = "Actualizando...";
                btnactualizar.Enabled = false;
                progressBar1.Value = 0;

                temp = Path.Combine(Path.GetTempPath(), AppName + "_update");

                if (Directory.Exists(temp))
                {
                    eliminarCarpeta(temp);
                }

                Directory.CreateDirectory(temp);
                progressBar1.Value = 10; // 🔹 10% -> Carpeta temporal creada
                await Task.Delay(500);

                string zipPath = Path.Combine(temp, "update.zip");
                string extractPath = Path.Combine(temp, "extract");
                Directory.CreateDirectory(extractPath);

                //rutalocal = $"http://localhost/{AppName}/{AppName}-v{versionNueva}.zip";
                string rutalocal = $"{link}";
                await DescargarArchivo(rutalocal, zipPath);

                if (!VerificarArchivo(zipPath))
                {
                    throw new Exception("El archivo descargado está vacío o corrupto.");
                }
                progressBar1.Value = 40; // 🔹 40% -> Descarga completada
                await Task.Delay(500);
                
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                progressBar1.Value = 50; // 🔹 50% -> Extracción completada
                await Task.Delay(500);

                string programa = ObtenerRutaDesdeRegistro() + "\\" + AppName;
                if (string.IsNullOrEmpty(programa))
                {
                    throw new Exception("No se encontró la ruta de instalación en el Registro.");
                }
                
                progressBar1.Value = 60; // 🔹 60% -> SolidWorks cerrado
                await Task.Delay(500);

                RealizarCopiaSeguridad(programa, Path.Combine(programa, "backup"));
                progressBar1.Value = 70; // 🔹 70% -> Copia de seguridad creada
                await Task.Delay(500);

                string batch = Path.Combine(temp, "_update.bat");
                File.WriteAllText(batch, GenerarScriptBatch(programa, extractPath, temp));
                progressBar1.Value = 80; // 🔹 80% -> Script de actualización generado
                await Task.Delay(500);
                
                Process.Start(new ProcessStartInfo()
                {
                    FileName = batch,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                eliminarCarpeta(Path.Combine(programa, "backup"));
                progressBar1.Value = 100; // 🔹 100% -> Actualización completada
                await Task.Delay(500);

                // 🔵 Mensaje de confirmación después de actualizar
                MessageBox.Show(
                    "La actualización se ha completado con éxito.",
                    "Actualización completada",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                CerrarSolidWorks();
                Application.Exit();
            }
            catch (Exception ex)
            {
                MostrarError("Error en la actualización", ex.Message);
            }
        }

        public async Task DescargarArchivo(string url, string destino)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                byte[] data = await response.Content.ReadAsByteArrayAsync();

                using (FileStream fs = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    await fs.WriteAsync(data, 0, data.Length);
                }
            }
        }

        private bool VerificarArchivo(string filePath)
        {
            return File.Exists(filePath) && new FileInfo(filePath).Length > 0;
        }

        private void CerrarSolidWorks()
        {
            try
            {
                Process[] procesos = Process.GetProcessesByName("SLDWORKS");
                foreach (Process proceso in procesos)
                {
                    proceso.Kill();
                    proceso.WaitForExit(); // Espera a que se cierre completamente
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error cerrando SolidWorks: " + ex.Message);
            }
        }

        private void RealizarCopiaSeguridad(string sourcePath, string backupPath)
        {
            try
            {
                if (Directory.Exists(backupPath))
                {
                    eliminarCarpeta(backupPath);
                }

                Directory.CreateDirectory(backupPath);

                // Copiar archivos individuales
                foreach (string file in Directory.GetFiles(sourcePath))
                {
                    string destino = Path.Combine(backupPath, Path.GetFileName(file));
                    File.Copy(file, destino, true);
                }

                // Copiar carpeta "DLL" si existe
                string dllSourcePath = Path.Combine(sourcePath, "DLL");
                string dllBackupPath = Path.Combine(backupPath, "DLL");

                if (Directory.Exists(dllSourcePath))
                {
                    Directory.CreateDirectory(dllBackupPath);

                    foreach (string dllFile in Directory.GetFiles(dllSourcePath))
                    {
                        string destino = Path.Combine(dllBackupPath, Path.GetFileName(dllFile));
                        File.Copy(dllFile, destino, true);
                    }
                }
                else
                {
                    Debug.WriteLine("No se encontró la carpeta DLL, omitiendo...");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al copiar archivos a la copia de seguridad: " + ex.Message);
            }
        }
        
        private void RestaurarCopiaSeguridad()
        {
            string programa = AppDomain.CurrentDomain.BaseDirectory;
            string backupPath = Path.Combine(programa, "backup");

            if (!Directory.Exists(backupPath))
            {
                MostrarError("Error crítico", "No se encontró la copia de seguridad.");
                return;
            }

            foreach (string file in Directory.GetFiles(backupPath))
            {
                try
                {
                    File.Copy(file, Path.Combine(programa, Path.GetFileName(file)), true);
                }
                catch (Exception ex)
                {
                    MostrarError("Error restaurando archivo", ex.Message);
                }
            }
        }

        private string GenerarScriptBatch(string programa, string extractPath, string temp)
        {
            return $@"
@echo off
tasklist | find /I ""{AppName}.exe"" && taskkill /IM {AppName}.exe /F
timeout /t 3

echo Restaurando archivos...
xcopy /E /Y /C ""{extractPath}\*"" ""{programa}""

echo Eliminando archivos temporales...
rd /S /Q ""{temp}""

del /F /Q ""%~f0""
PAUSE
exit
";
        }

        private void MostrarError(string titulo, string mensaje)
        {
            MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnactualizar.Text = "Actualizar";
            btnactualizar.Enabled = true;
            progressBar1.Visible = false;
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

        private string ObtenerRutaDesdeRegistro()
        {
            string source = null;
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ComputerName");
                if (key != null)
                {
                    object o = key.GetValue("Source");
                    source = o.ToString();
                }
            }
            catch (Exception e)
            {
                Debug.Print("Error de obtener usserID!!! " + e.ToString());
            }
            return source;
        }

    }
}
