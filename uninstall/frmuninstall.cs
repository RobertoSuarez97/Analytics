using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace uninstall
{
    public partial class frmuninstall : Form
    {
        private const string AppName = "analytics";
        string DllAddIn = "analytics_AddIn";
        string Keypath1 = @"SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName";
        string archivoEliminar = @"C:\ProgramData\SOLIDWORKS\analytics_sessions.txt";

        public frmuninstall()
        {
            InitializeComponent();
        }

       private void frmuninstall_Load(object sender, EventArgs e)
        {
            // Preguntamos si desea desinstalar
            var d = MessageBox.Show("¿Seguro que quieres desinstalar \"" + AppName + "\"?",
                "Desinstalar", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk);
            if (d == DialogResult.No) Environment.Exit(0);

            // 📂 Ruta del programa instalado
            string ruta = AppDomain.CurrentDomain.BaseDirectory;
            string _nombre = "";

            // 1️⃣ ✅ Eliminar accesos directos
            try
            {
                string commonStartMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
                string appStartMenuPath = Path.Combine(commonStartMenuPath, "Programs", AppName);

                if (Directory.Exists(appStartMenuPath))
                {
                    foreach (string file in Directory.GetFiles(appStartMenuPath, "*.lnk"))
                    {
                        File.Delete(file);
                    }
                    Directory.Delete(appStartMenuPath, true);
                }
            }
            catch (Exception) { }

            // 2️⃣ ✅ Eliminar la DLL registrada
            string dllPath = Path.Combine(ruta, "Dll", DllAddIn + ".dll");
            if (File.Exists(dllPath))
            {
                DesregistrarDLL(dllPath);
            }

            // 3️⃣ ✅ Eliminar las claves del Registro creadas por el instalador
            eliminarRegistrosInstalacion();

            // 3️⃣ ✅ Eliminar archivo de sesiónes
            eliminarArchivo(archivoEliminar);

            // 3️⃣ ✅ Eliminar archivo carpeta
            eliminarCarpeta();

            // 4️⃣ ✅ Eliminar los registros de App Paths
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion", true);
                RegistryKey app = key.OpenSubKey("App Paths", true);
                RegistryKey nombre = app.OpenSubKey(AppName + ".exe", true);

                _nombre = new DirectoryInfo(nombre.GetValue("Path").ToString()).Name;
                app.DeleteSubKeyTree(AppName + ".exe", false);

                RegistryKey unins = key.OpenSubKey("Uninstall", true);
                unins.DeleteSubKeyTree(AppName, false);
            }
            catch (Exception) { }

            // 5️⃣ ✅ Crear un script para borrar la carpeta y el desinstalador
            string temp = Path.Combine(Path.GetTempPath(), AppName + "_uninstall");
            if (!Directory.Exists(temp)) Directory.CreateDirectory(temp);

            string batchPath = Path.Combine(temp, "uninstall.bat");
            File.WriteAllText(batchPath, string.Format(
                @"@echo off
                timeout /t 3
                rmdir /s /q ""{0}""
                del ""%~f0""", ruta));

            // 6️⃣ ✅ Ejecutamos el script para borrar la carpeta
            MessageBox.Show("Aplicación desinstalada correctamente.");
            Process.Start(new ProcessStartInfo()
            {
                FileName = batchPath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            // Cerramos la aplicación
            Environment.Exit(0);
        }

        // ✅ FUNCIÓN PARA DESREGISTRAR LA DLL
        private void DesregistrarDLL(string dllPath)
        {
            try
            {
                string regasmPath = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe";
                string argument = $"\"{dllPath}\" /u /tlb /codebase";

                ProcessStartInfo startInfo = new ProcessStartInfo(regasmPath, argument)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    MessageBox.Show("DLL desregistrada correctamente.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al desregistrar la DLL: {ex.Message}");
            }
        }

        // ✅ ELIMINAR LAS CLAVES DEL REGISTRO (UserID, CustomerID, etc.)
        private void eliminarRegistrosInstalacion()
        {
            try
            {
                // Eliminar las claves del registro que creaste al instalar
                RegistryKey Key1 = Registry.LocalMachine.OpenSubKey(Keypath1, true);
                Key1.DeleteValue("CustomerID", false);
                Key1.DeleteValue("UserID", false);
                Key1.DeleteValue("deviceID", false);
                Key1.DeleteValue("Email", false);
                Key1.DeleteValue("Source", false);
                Key1.Close();

                string addinKeyPath = @"SOFTWARE\SolidWorks\AddIns\{31b803e0-7a01-4841-a0de-895b726625c9}";
                RegistryKey solidworksKey = Registry.LocalMachine.OpenSubKey(addinKeyPath, true);

                if (solidworksKey != null)
                {
                    Registry.LocalMachine.DeleteSubKeyTree(addinKeyPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar claves de registro: {ex.Message}");
            }
        }

        void eliminarCarpeta()
        {
            try
            {
                // Obtener la ruta de instalación
                string ruta = Path.Combine(GetSource(), AppName);

                if (Directory.Exists(ruta))
                {
                    // Eliminar todos los archivos en la carpeta
                    foreach (string archivo in Directory.GetFiles(ruta))
                    {
                        try
                        {
                            File.Delete(archivo);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error al eliminar archivo:\n{archivo}\nDetalles: {ex.Message}");
                        }
                    }

                    // Eliminar todas las subcarpetas de manera recursiva
                    foreach (string subCarpeta in Directory.GetDirectories(ruta))
                    {
                        eliminarCarpetaRecursiva(subCarpeta);
                    }

                    // Intentar eliminar la carpeta vacía
                    try
                    {
                        Directory.Delete(ruta, true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al eliminar carpeta:\n{ruta}\nDetalles: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show($"La carpeta no existe:\n{ruta}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado al eliminar la carpeta:\nDetalles: {ex.Message}");
            }
        }

        void eliminarCarpetaRecursiva(string ruta)
        {
            try
            {
                foreach (string archivo in Directory.GetFiles(ruta))
                {
                    try
                    {
                        File.Delete(archivo);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al eliminar archivo:\n{archivo}\nDetalles: {ex.Message}");
                    }
                }

                foreach (string subCarpeta in Directory.GetDirectories(ruta))
                {
                    eliminarCarpetaRecursiva(subCarpeta);
                }

                try
                {
                    Directory.Delete(ruta, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar subcarpeta:\n{ruta}\nDetalles: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado en eliminarCarpetaRecursiva:\nDetalles: {ex.Message}");
            }
        }

        void eliminarArchivo(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar el archivo:\n{path}\n\nDetalles: {ex.Message}");
            }
        }

        /// 
        /// Obtiene el ID del usuario.
        ///
        private string GetSource()
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
