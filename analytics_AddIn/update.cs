using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Compression;
using Microsoft.Win32;
using System.Security.Principal;

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
        private string LogFilePath = "";


        public update()
        {
            InitializeComponent();
            this.Load += update_Load;
            progressBar1.Visible = false;
            
            // Verificar si ya hay otra instancia en ejecución durante la inicialización
            VerificarInstanciasMultiples();
        }

        private void VerificarInstanciasMultiples()
        {
            try
            {
                // Obtener el nombre del proceso actual sin la extensión
                string processName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
                Process currentProcess = Process.GetCurrentProcess();
                Process[] processes = Process.GetProcessesByName(processName);

                // Si hay más de un proceso con el mismo nombre (excluyendo el actual)
                if (processes.Length > 1)
                {
                    LogToFile($"Múltiples instancias detectadas ({processes.Length}). Verificando si cerrar esta instancia.", "WARNING");

                    // Verificar si alguna de las otras instancias tiene permisos de administrador
                    bool otraInstanciaAdmin = false;
                    foreach (Process proc in processes)
                    {
                        if (proc.Id != currentProcess.Id)
                        {
                            // Si ya hay una instancia con permisos de admin, cerrar esta
                            if (EsAdministrador())
                            {
                                // Esta es admin, mantener esta y no hacer nada
                                LogToFile("Esta instancia tiene permisos de administrador. Continuando.", "INFO");
                                return;
                            }
                            else
                            {
                                // Esta no es admin, cerrarla si ya hay otra instancia
                                LogToFile("Esta instancia no tiene permisos de administrador y ya existe otra. Cerrando.", "WARNING");
                                MessageBox.Show("Ya hay otra instancia del actualizador en ejecución.",
                                    "Instancia duplicada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                Application.Exit();
                                return;
                            }
                        }
                    }
                }
                else
                {
                    LogToFile("No se detectaron instancias duplicadas. Continuando normalmente.");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error al verificar instancias múltiples: {ex.Message}", "ERROR");
            }
        }

        private void LogToFile(string message, string level = "INFO")
        {
            try
            {
                LogFilePath = Path.Combine(ObtenerRutaInstalacion(), "update_log.txt");
                string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logEntry = "";
                if (level.ToUpper() == "ERROR")
                {
                    logEntry = $@"
                        [---------------------------- ERROR ----------------------------]
                        Fecha: {timeStamp}
                        Mensaje: {message}
                        [--------------------------------------------------------------]";
                }
                else
                {
                    logEntry = $"{timeStamp} [{level}] - {message}";
                }
                using (StreamWriter writer = new StreamWriter(LogFilePath, true))
                {
                    writer.WriteLine(logEntry);
                }
            }
            catch
            {
                Debug.Print("Error al escribir en el log.");
            }
        }

        private void update_Load(object sender, EventArgs e)
        {
            LogToFile("Inicializando formulario de actualización");
            lblVersionActual.Text = $"Versión actual: {versionActual}";
            lbversion.Text = $"Nueva versión: {versionNueva}";
            rtbcambios.Text = cambios;
            LogToFile($"Versión actual: {versionActual}, Nueva versión: {versionNueva}");
        }

        private async void btnactualizar_Click(object sender, EventArgs e)
        {
            try
            {
                LogToFile("Iniciando proceso de actualización");

                // Verificar si la aplicación tiene permisos de administrador
                if (!EsAdministrador())
                {
                    MessageBox.Show(
                    "La actualización requiere que SolidWorks.\nSe abra como administrador",
                    "Confirmar actualización"
                    );
                    LogToFile("No se tienen permisos de administrador. Intentando reiniciar con elevación", "WARNING");

                    // Verificar si ya hay una instancia en ejecución con el mismo nombre
                    Process currentProcess = Process.GetCurrentProcess();
                    Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Application.ExecutablePath));
                    LogToFile($"Procesos con el mismo nombre encontrados: {processes.Length}");

                    // Si solo está esta instancia, reiniciar como admin
                    if (processes.Length <= 1)
                    {
                        // Reiniciar la aplicación con permisos de administrador
                        ReiniciarComoAdmin();
                    }
                    else
                    {
                        LogToFile("Ya existe otra instancia en ejecución. Cerrando esta instancia.", "WARNING");
                        MessageBox.Show("Ya hay otra instancia del actualizador en ejecución.",
                            "Instancia duplicada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Close();
                    }
                    return;
                }

                // Mensaje de confirmación antes de actualizar
                DialogResult confirmacion = MessageBox.Show(
                    "La actualización requiere cerrar SolidWorks.\n¿Está seguro de continuar?",
                    "Confirmar actualización",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (confirmacion != DialogResult.Yes)
                {
                    LogToFile("Usuario canceló la actualización");
                    return;
                }

                LogToFile("Usuario confirmó la actualización");
                progressBar1.Visible = true;
                btnactualizar.Text = "Actualizando...";
                btnactualizar.Enabled = false;
                progressBar1.Value = 0;

                // Obtener ruta de instalación correcta
                string programPath = ObtenerRutaInstalacion();
                LogToFile($"Ruta de instalación: {programPath}");

                if (string.IsNullOrEmpty(programPath))
                {
                    throw new Exception("No se pudo determinar la ruta de instalación del add-in.");
                }

                // Cerrar SolidWorks primero para evitar bloqueos de archivos
                LogToFile("Cerrando SolidWorks");
                //CerrarSolidWorks();
                progressBar1.Value = 10;
                await Task.Delay(500);

                temp = Path.Combine(Path.GetTempPath(), AppName + "_update");
                LogToFile($"Carpeta temporal: {temp}");

                if (Directory.Exists(temp))
                {
                    LogToFile("Eliminando carpeta temporal existente");
                    eliminarCarpeta(temp);
                }

                Directory.CreateDirectory(temp);
                progressBar1.Value = 20; // Carpeta temporal creada
                await Task.Delay(500);

                string zipPath = Path.Combine(temp, "update.zip");
                string extractPath = Path.Combine(temp, "extract");
                Directory.CreateDirectory(extractPath);
                LogToFile($"Ruta del ZIP: {zipPath}");
                LogToFile($"Ruta de extracción: {extractPath}");

                // Descargar el archivo de actualización
                string rutalocal = $"{link}";
                LogToFile($"Descargando actualización desde: {rutalocal}");
                await DescargarArchivo(rutalocal, zipPath);

                if (!VerificarArchivo(zipPath))
                {
                    LogToFile("El archivo descargado está vacío o corrupto", "ERROR");
                    throw new Exception("El archivo descargado está vacío o corrupto.");
                }
                LogToFile("Archivo descargado correctamente");
                progressBar1.Value = 40; // Descarga completada
                await Task.Delay(500);

                LogToFile("Extrayendo archivos de actualización");
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                progressBar1.Value = 50; // Extracción completada
                await Task.Delay(500);

                // Crear backup en carpeta temporal en lugar de directorio del programa
                string backupPath = Path.Combine(temp, "backup");
                LogToFile($"Creando copia de seguridad en: {backupPath}");
                RealizarCopiaSeguridad(programPath, backupPath);
                progressBar1.Value = 70; // Copia de seguridad creada
                await Task.Delay(500);

                try
                {
                    // Intentar actualizar directamente
                    LogToFile("Iniciando actualización de archivos");
                    ActualizarArchivos(extractPath, programPath);
                    LogToFile("Archivos actualizados correctamente");
                    progressBar1.Value = 90;
                }
                catch (UnauthorizedAccessException ex)
                {
                    // Si falla por permisos, usar un script batch con elevación
                    LogToFile($"Error de permisos al actualizar: {ex.Message}", "ERROR");
                    LogToFile("Intentando actualizar mediante script batch con permisos elevados");
                    string batch = Path.Combine(temp, "_update.bat");
                    File.WriteAllText(batch, GenerarScriptBatch(programPath, extractPath, temp, backupPath));
                    LogToFile($"Script batch creado en: {batch}");

                    // Esperar un poco para asegurarse de que el script se guardó correctamente
                    await Task.Delay(1000);

                    ProcessStartInfo startInfo = new ProcessStartInfo()
                    {
                        FileName = batch,
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Verb = "runas" // Solicitar elevación
                    };

                    Process proceso = Process.Start(startInfo);
                    if (proceso != null)
                    {
                        LogToFile("Script batch ejecutado con permisos elevados");
                        LogToFile("Esperando a que el script batch termine...");
                        // No esperar indefinidamente, establecer un tiempo máximo razonable
                        bool terminado = proceso.WaitForExit(60000); // Esperar hasta 60 segundos
                        if (terminado)
                        {
                            LogToFile($"Script batch terminado con código: {proceso.ExitCode}");
                        }
                        else
                        {
                            LogToFile("Script batch tardó demasiado tiempo en ejecutarse", "WARNING");
                        }
                    }
                    else
                    {
                        LogToFile("No se pudo iniciar el script batch", "ERROR");
                    }
                    progressBar1.Value = 90;
                    await Task.Delay(500);
                }

                progressBar1.Value = 100; // Actualización completada
                await Task.Delay(500);
                LogToFile("Actualización completada con éxito");

                // Mensaje de confirmación después de actualizar
                MessageBox.Show(
                    "La actualización se ha completado con éxito.\nSe reiniciará la aplicación para aplicar los cambios.",
                    "Actualización completada",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                LogToFile("Cerrando aplicación para aplicar cambios");
                Application.Exit();
                CerrarSolidWorks();
            }
            catch (Exception ex)
            {
                LogToFile($"Error en la actualización: {ex.Message}", "ERROR");
                MostrarError("Error en la actualización", ex.Message);
                // Intentar restaurar desde el backup si existe
                try
                {
                    string backupPath = Path.Combine(temp, "backup");
                    if (Directory.Exists(backupPath))
                    {
                        LogToFile("Intentando restaurar desde la copia de seguridad");
                        string programPath = ObtenerRutaInstalacion();
                        RestaurarCopiaSeguridad(backupPath, programPath);
                        LogToFile("Restauración completada correctamente");
                        MessageBox.Show("Se ha restaurado la versión anterior.", "Restauración completada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception exRestore)
                {
                    LogToFile($"Error al restaurar: {exRestore.Message}", "ERROR");
                    Debug.WriteLine("Error al restaurar: " + exRestore.Message);
                }
            }
        }

        // Verificar si la aplicación se está ejecutando como administrador
        private bool EsAdministrador()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                LogToFile($"Verificación de permisos de administrador: {(isAdmin ? "Sí" : "No")}");
                return isAdmin;
            }
        }

        // Reiniciar la aplicación con permisos de administrador
        private void ReiniciarComoAdmin()
        {
            try
            {
                LogToFile("Intentando reiniciar con permisos de administrador");
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                startInfo.FileName = Application.ExecutablePath;
                startInfo.Verb = "runas"; // Solicitar elevación

                // Iniciar el nuevo proceso y esperar a que comience
                Process proceso = Process.Start(startInfo);
                LogToFile("Proceso iniciado con solicitud de elevación");

                // Cerrar este formulario específicamente en lugar de toda la aplicación
                LogToFile("Cerrando instancia actual del formulario");
                this.Close();

                // Si el formulario es la ventana principal o queremos asegurarnos de que la aplicación termine
                if (Application.OpenForms.Count <= 1)
                {
                    LogToFile("Cerrando toda la aplicación (era el último formulario abierto)");
                    Application.Exit();
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error al reiniciar con permisos de administrador: {ex.Message}", "ERROR");
                MessageBox.Show("Se requieren permisos de administrador para realizar la actualización.\n\nError: " + ex.Message,
                    "Permisos insuficientes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Actualizar archivos directamente
        private void ActualizarArchivos(string sourcePath, string destinationPath)
        {
            LogToFile($"Actualizando archivos de {sourcePath} a {destinationPath}");
            // Copiar todos los archivos
            foreach (string file in Directory.GetFiles(sourcePath))
            {
                string destFile = Path.Combine(destinationPath, Path.GetFileName(file));
                LogToFile($"Copiando archivo: {Path.GetFileName(file)}");
                File.Copy(file, destFile, true);
            }

            // Copiar subdirectorios
            foreach (string directory in Directory.GetDirectories(sourcePath))
            {
                string dirName = Path.GetFileName(directory);
                string destDir = Path.Combine(destinationPath, dirName);
                LogToFile($"Procesando directorio: {dirName}");

                if (!Directory.Exists(destDir))
                {
                    LogToFile($"Creando directorio: {destDir}");
                    Directory.CreateDirectory(destDir);
                }

                ActualizarArchivos(directory, destDir);
            }
        }

        public async Task DescargarArchivo(string url, string destino)
        {
            LogToFile($"Iniciando descarga desde {url}");
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5); // Aumentar el timeout para archivos grandes
                LogToFile("Timeout establecido a 5 minutos");

                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    LogToFile($"Respuesta del servidor: {response.StatusCode}");

                    byte[] data = await response.Content.ReadAsByteArrayAsync();
                    LogToFile($"Datos descargados: {data.Length} bytes");

                    using (FileStream fs = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        await fs.WriteAsync(data, 0, data.Length);
                    }
                    LogToFile($"Archivo guardado en: {destino}");
                }
                catch (Exception ex)
                {
                    LogToFile($"Error en la descarga: {ex.Message}", "ERROR");
                    throw;
                }
            }
        }

        private bool VerificarArchivo(string filePath)
        {
            bool exists = File.Exists(filePath);
            long size = exists ? new FileInfo(filePath).Length : 0;
            LogToFile($"Verificando archivo {filePath}: Existe={exists}, Tamaño={size} bytes");
            return exists && size > 0;
        }

        private void CerrarSolidWorks()
        {
            try
            {
                LogToFile("Buscando procesos de SolidWorks para cerrar");

                // Lista de posibles procesos relacionados con SolidWorks
                string[] swProcesses = new string[] {
                    "SLDWORKS", "swShellFileLauncher", "Simulation",
                    "SWViewer", "swScheduler", "PDMworks"
                };

                foreach (string procName in swProcesses)
                {
                    try
                    {
                        Process[] procesos = Process.GetProcessesByName(procName);
                        LogToFile($"Procesos de '{procName}' encontrados: {procesos.Length}");

                        foreach (Process proceso in procesos)
                        {
                            LogToFile($"Intentando cerrar {procName} (PID: {proceso.Id})");
                            proceso.CloseMainWindow(); // Intenta cerrar normalmente primero

                            // Esperar hasta 5 segundos para que se cierre correctamente
                            if (!proceso.WaitForExit(5000))
                            {
                                LogToFile($"{procName} no respondió al cierre normal, forzando cierre", "WARNING");
                                proceso.Kill(); // Si no se cierra, forzar el cierre
                                proceso.WaitForExit();
                            }
                            LogToFile($"Proceso de {procName} cerrado correctamente");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"Error al cerrar {procName}: {ex.Message}", "ERROR");
                    }
                }

                // También limpiar archivos de journaling o temporales que puedan bloquear el inicio
                LimpiarArchivosSolidWorks();

                // También cerrar el proceso del add-in si está en ejecución
                LogToFile($"Buscando procesos de {AppName} para cerrar");
                Process[] addinProcesos = Process.GetProcessesByName(AppName);
                foreach (Process addinProceso in addinProcesos)
                {
                    if (addinProceso.Id != Process.GetCurrentProcess().Id) // No cerrar el proceso actual
                    {
                        LogToFile($"Cerrando proceso {AppName} (PID: {addinProceso.Id})");
                        addinProceso.Kill();
                        addinProceso.WaitForExit();
                        LogToFile("Proceso cerrado correctamente");
                    }
                }

                // Esperar un momento para asegurarse de que todos los procesos se cerraron
                LogToFile("Esperando 3 segundos para asegurar que todos los procesos se cerraron...");
                System.Threading.Thread.Sleep(3000);
            }
            catch (Exception ex)
            {
                LogToFile($"Error cerrando SolidWorks: {ex.Message}", "ERROR");
                Debug.WriteLine("Error cerrando SolidWorks: " + ex.Message);
            }
        }

        private void LimpiarArchivosSolidWorks()
        {
            try
            {
                LogToFile("Limpiando archivos temporales de SolidWorks");

                // Rutas donde SolidWorks almacena archivos de journaling y temporales
                string[] pathsToCheck = new string[] {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SOLIDWORKS"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "SOLIDWORKS"),
                    Path.Combine(Path.GetTempPath(), "SOLIDWORKS")
                };

                foreach (string path in pathsToCheck)
                {
                    if (Directory.Exists(path))
                    {
                        LogToFile($"Revisando archivos de journaling en: {path}");

                        // Buscar y eliminar los archivos de journal
                        try
                        {
                            string[] journalFiles = Directory.GetFiles(path, "*.swj", SearchOption.AllDirectories);
                            LogToFile($"Encontrados {journalFiles.Length} archivos de journal");

                            foreach (string file in journalFiles)
                            {
                                try
                                {
                                    File.Delete(file);
                                    LogToFile($"Eliminado archivo de journal: {file}");
                                }
                                catch (Exception ex)
                                {
                                    LogToFile($"No se pudo eliminar {file}: {ex.Message}", "WARNING");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"Error al buscar archivos de journal: {ex.Message}", "ERROR");
                        }

                        // También buscar archivos de bloqueo
                        try
                        {
                            string[] lockFiles = Directory.GetFiles(path, "*.lock", SearchOption.AllDirectories);
                            LogToFile($"Encontrados {lockFiles.Length} archivos de bloqueo");

                            foreach (string file in lockFiles)
                            {
                                try
                                {
                                    File.Delete(file);
                                    LogToFile($"Eliminado archivo de bloqueo: {file}");
                                }
                                catch (Exception ex)
                                {
                                    LogToFile($"No se pudo eliminar {file}: {ex.Message}", "WARNING");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"Error al buscar archivos de bloqueo: {ex.Message}", "ERROR");
                        }
                    }
                }

                // También eliminar archivos temporales específicos que puedan causar problemas
                string tempFolder = Path.GetTempPath();
                try
                {
                    string[] swTempFiles = Directory.GetFiles(tempFolder, "sw*.tmp", SearchOption.TopDirectoryOnly);
                    foreach (string file in swTempFiles)
                    {
                        try
                        {
                            File.Delete(file);
                            LogToFile($"Eliminado archivo temporal: {file}");
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"No se pudo eliminar {file}: {ex.Message}", "WARNING");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogToFile($"Error al limpiar archivos temporales: {ex.Message}", "ERROR");
                }

                LogToFile("Limpieza de archivos temporales completada");
            }
            catch (Exception ex)
            {
                LogToFile($"Error general en limpieza de archivos: {ex.Message}", "ERROR");
            }
        }

        private void RealizarCopiaSeguridad(string sourcePath, string backupPath)
        {
            try
            {
                LogToFile($"Iniciando copia de seguridad de {sourcePath} a {backupPath}");
                if (Directory.Exists(backupPath))
                {
                    LogToFile("Eliminando copia de seguridad anterior");
                    eliminarCarpeta(backupPath);
                }

                Directory.CreateDirectory(backupPath);
                LogToFile("Directorio de copia de seguridad creado");

                // Copiar archivos individuales
                foreach (string file in Directory.GetFiles(sourcePath))
                {
                    string fileName = Path.GetFileName(file);
                    string destino = Path.Combine(backupPath, fileName);
                    LogToFile($"Respaldando archivo: {fileName}");
                    File.Copy(file, destino, true);
                }

                // Copiar carpetas recursivamente
                foreach (string dir in Directory.GetDirectories(sourcePath))
                {
                    string dirName = Path.GetFileName(dir);
                    string destDir = Path.Combine(backupPath, dirName);
                    LogToFile($"Respaldando directorio: {dirName}");

                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    // Copiar contenido de las subcarpetas
                    foreach (string file in Directory.GetFiles(dir))
                    {
                        string destFile = Path.Combine(destDir, Path.GetFileName(file));
                        File.Copy(file, destFile, true);
                    }

                    // Procesar subdirectorios recursivamente
                    foreach (string subdir in Directory.GetDirectories(dir))
                    {
                        CopiarDirectorioRecursivo(subdir, Path.Combine(destDir, Path.GetFileName(subdir)));
                    }
                }
                LogToFile("Copia de seguridad completada correctamente");
            }
            catch (Exception ex)
            {
                LogToFile($"Error al realizar copia de seguridad: {ex.Message}", "ERROR");
                throw new Exception("Error al copiar archivos a la copia de seguridad: " + ex.Message);
            }
        }

        private void CopiarDirectorioRecursivo(string sourceDir, string destDir)
        {
            string dirName = Path.GetFileName(sourceDir);
            LogToFile($"Copiando subdirectorio: {dirName}");

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string subdir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(subdir));
                CopiarDirectorioRecursivo(subdir, destSubDir);
            }
        }

        private void RestaurarCopiaSeguridad(string backupPath, string destinationPath)
        {
            if (!Directory.Exists(backupPath))
            {
                LogToFile("No se encontró copia de seguridad para restaurar", "WARNING");
                return;
            }

            try
            {
                LogToFile($"Restaurando copia de seguridad desde {backupPath} a {destinationPath}");
                // Copiar los archivos de vuelta
                foreach (string file in Directory.GetFiles(backupPath))
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(destinationPath, fileName);
                    LogToFile($"Restaurando archivo: {fileName}");
                    File.Copy(file, destFile, true);
                }

                // Restaurar subdirectorios
                foreach (string dir in Directory.GetDirectories(backupPath))
                {
                    string dirName = Path.GetFileName(dir);
                    string destDir = Path.Combine(destinationPath, dirName);
                    LogToFile($"Restaurando directorio: {dirName}");

                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    CopiarDirectorioRecursivo(dir, destDir);
                }
                LogToFile("Restauración completada correctamente");
            }
            catch (Exception ex)
            {
                LogToFile($"Error restaurando copia de seguridad: {ex.Message}", "ERROR");
                Debug.WriteLine("Error restaurando copia de seguridad: " + ex.Message);
            }
        }

        private string GenerarScriptBatch(string programa, string extractPath, string temp, string backupPath)
        {
            LogToFile("Generando script batch para actualización con permisos elevados");
            return $@"
@echo off
echo Iniciando actualización con permisos elevados...

REM Crear archivo de log
echo %date% %time% - Iniciando actualización con permisos elevados >> ""{Path.Combine(programa, "update_batch.log")}""

REM Esperar a que se cierren todos los procesos relacionados
tasklist | find /I ""SLDWORKS.exe"" && taskkill /IM SLDWORKS.exe /F
tasklist | find /I ""{AppName}.exe"" && taskkill /IM {AppName}.exe /F
timeout /t 3

echo Actualizando archivos...
echo %date% %time% - Copiando archivos de actualización >> ""{Path.Combine(programa, "update_batch.log")}""
xcopy /E /Y /C ""{extractPath}\*.*"" ""{programa}""

IF %ERRORLEVEL% NEQ 0 (
    echo Error al actualizar. Restaurando copia de seguridad...
    echo %date% %time% - Error en actualización. Restaurando copia de seguridad >> ""{Path.Combine(programa, "update_batch.log")}""
    xcopy /E /Y /C ""{backupPath}\*.*"" ""{programa}""
)

echo Limpiando archivos temporales...
echo %date% %time% - Limpiando archivos temporales >> ""{Path.Combine(programa, "update_batch.log")}""
rd /S /Q ""{temp}""

echo Actualización completada.
echo %date% %time% - Actualización completada >> ""{Path.Combine(programa, "update_batch.log")}""
echo Presione cualquier tecla para cerrar esta ventana.
pause > nul
del /F /Q ""%~f0""
exit
";
        }

        private void MostrarError(string titulo, string mensaje)
        {
            LogToFile($"Error mostrado al usuario: {titulo} - {mensaje}", "ERROR");
            MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnactualizar.Text = "Actualizar";
            btnactualizar.Enabled = true;
            progressBar1.Visible = false;
        }

        private void eliminarCarpeta(string ruta)
        {
            try
            {
                if (Directory.Exists(ruta))
                {
                    LogToFile($"Eliminando carpeta: {ruta}");
                    Directory.Delete(ruta, true);
                    LogToFile("Carpeta eliminada correctamente");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error eliminando carpeta {ruta}: {ex.Message}", "ERROR");
                Debug.WriteLine("Error eliminando carpeta: " + ex.Message);
            }
        }

        private string ObtenerRutaInstalacion()
        {
            // Intentar obtener ruta desde el registro
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + AppName + ".exe"))
                {
                    if (key != null)
                    {
                        string path = key.GetValue(null) as string;
                        if (!string.IsNullOrEmpty(path))
                        {
                            return Path.GetDirectoryName(path);
                        }
                    }
                }

                // También probar en el registro para aplicaciones de 32 bits en sistemas de 64 bits
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\App Paths\" + AppName + ".exe"))
                {
                    if (key != null)
                    {
                        string path = key.GetValue(null) as string;
                        if (!string.IsNullOrEmpty(path))
                        {
                            return Path.GetDirectoryName(path);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error al obtener ruta desde registro: " + ex.Message);
            }

            // Si no se encuentra en el registro, usar el directorio de la aplicación actual
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists(Path.Combine(currentDir, AppName + ".exe")))
            {
                LogToFile($"Usando directorio actual como ruta de instalación: {currentDir}");
                return currentDir;
            }

            // Si todo falla, usar una ruta predeterminada
            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), AppName);
            LogToFile($"Usando ruta predeterminada: {defaultPath}", "WARNING");

            return defaultPath;
        }
    }
}