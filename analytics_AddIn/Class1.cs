using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swpublished;
using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Security.AccessControl;
using System.Diagnostics;
using System.Security.Principal;
using Newtonsoft.Json;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using System.Security.Cryptography;
using SolidWorks.Interop.swconst;
using System.Windows.Forms;
using System.Net;
using System.Drawing;
using System.Configuration;

namespace analytics_AddIn
{
    [ComVisible(true)]
    [Guid("31b803e0-7a01-4841-a0de-895b726625c9")]
    [DisplayName("Analytics")]
    [Description("Analytics SOLIDWORKS Add-In")]
    public class Class1 : ISwAddin
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        private const string AppName = "analytics";
        private const string ADDIN_KEY_TEMPLATE = @"SOFTWARE\SolidWorks\Addins\{{{0}}}";
        private const string ADDIN_STARTUP_KEY_TEMPLATE = @"Software\SolidWorks\AddInsStartup\{{{0}}}";
        private const string ADD_IN_TITLE_REG_KEY_NAME = "Title";
        private const string ADD_IN_DESCRIPTION_REG_KEY_NAME = "Description";
        private const string API_Xpertme_URL = "https://api-academy.xpertcad.com/v2/analytics/users/getAnalyticsInstallers";
        private const string API_Analitics_URL = "https://api-ncsw.xpertme.com/api/createSession";
        private const string TOKEN_Xpertme_URL = "https://api-academy.xpertcad.com/v2/system/oauth/token";
        private const string TOKEN_Analitics_URL = "https://api-ncsw.xpertme.com/api/auth";
        private string baseApplicationDataFolder;
        private string LogFilePath;
        private string ConfigFilePath;
        private string tempFolder;
        private ICommandManager commandManager;
        private SldWorks solidWorksEventHandler;
        private Hashtable openDocuments;
        private static readonly HttpClient client = new HttpClient();
        private ISldWorks solidWorksApp;
        private int addInCookie;
        private string VERSION;
        private string versionActual;
        private string cambios;
        private string link;

        // Variables de configuración
        private string accessKeyId;
        private string secretAccessKey;
        private string bucketName;
        private string region;
        private string encryptionKey;
        private bool isConfigured;

        // Eventos
        public Class1()
        {
            string chosenBasePathForAppFolder = null;
            string pathSourceOrigin = "No determinado"; // Para loguear de dónde vino la ruta

            try
            {
                string sourcePathFromRegistry = "C:\\ProgramData\\SOLIDWORKS";
                chosenBasePathForAppFolder = Path.Combine(sourcePathFromRegistry, AppName);

                if (!Directory.Exists(chosenBasePathForAppFolder))
                {
                    Directory.CreateDirectory(chosenBasePathForAppFolder);
                }

                this.baseApplicationDataFolder = chosenBasePathForAppFolder; // Asignar a la variable de instancia
                LogFilePath = Path.Combine(this.baseApplicationDataFolder, AppName + "_log.txt");
                ConfigFilePath = Path.Combine(this.baseApplicationDataFolder, AppName + "_sessions.txt");
                tempFolder = Path.Combine(this.baseApplicationDataFolder, "temp");

                if (!Directory.Exists(tempFolder))
                {
                    Directory.CreateDirectory(tempFolder);
                }

                // --- Paso 6: Ahora sí podemos loguear de forma segura ---
                LogToFile($"Constructor Class1: Rutas inicializadas. Origen de ruta base: {pathSourceOrigin}.", "INFO");
                LogToFile($"LogFilePath establecido en: {LogFilePath}", "INFO");
                LogToFile($"ConfigFilePath establecido en: {ConfigFilePath}", "INFO");
                LogToFile($"tempFolder establecido en: {tempFolder}", "INFO");
                if (string.IsNullOrEmpty(sourcePathFromRegistry) || !Directory.Exists(sourcePathFromRegistry))
                {
                    LogToFile($"Fallback a LocalApplicationData porque 'Source' del registro ('{sourcePathFromRegistry ?? "null"}') no es válido/existente.", "WARNING");
                }

                LoadConfiguration();
            }
            catch (Exception ex)
            {
                Debug.Print($"ERROR CRÍTICO en constructor Class1 al inicializar rutas: {ex.ToString()}");
            }
        }

        [ComRegisterFunction]
        public static void RegisterFunction(Type t)
        {
            Class1 logger = null;
            try
            {
                logger = new Class1();
                var addInTitle = t.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? t.ToString();
                var addInDesc = t.GetCustomAttribute<DescriptionAttribute>()?.Description ?? t.ToString();
                var addInKeyPath = string.Format(ADDIN_KEY_TEMPLATE, t.GUID);

                using (var addInKey = Registry.LocalMachine.CreateSubKey(addInKeyPath))
                {
                    addInKey.SetValue(ADD_IN_TITLE_REG_KEY_NAME, addInTitle);
                    addInKey.SetValue(ADD_IN_DESCRIPTION_REG_KEY_NAME, addInDesc);

                    string pathInstall = GetSource();
                    string iconPath = Path.Combine(pathInstall, AppName, "AddinIcon.bmp");
                    addInKey.SetValue("Icon Path", iconPath, RegistryValueKind.String);
                }

                var addInStartupKeyPath = string.Format(ADDIN_STARTUP_KEY_TEMPLATE, t.GUID);
                using (var addInStartupKey = Registry.CurrentUser.CreateSubKey(addInStartupKeyPath))
                {
                    addInStartupKey.SetValue(null, 1, RegistryValueKind.DWord);
                }

                logger.LogToFile("Add-in registrado correctamente con ícono.");
            }
            catch (Exception ex)
            {
                logger = new Class1();
                logger.LogToFile("Error al registrar el Add-in: " + ex.Message);
            }
        }

        [ComUnregisterFunction]
        public static void UnregisterFunction(Type t)
        {
            try
            {
                var logger = new Class1();
                Registry.LocalMachine.DeleteSubKey(string.Format(ADDIN_KEY_TEMPLATE, t.GUID));
                Registry.CurrentUser.DeleteSubKey(string.Format(ADDIN_STARTUP_KEY_TEMPLATE, t.GUID));
                logger.LogToFile("Add-in desregistrado correctamente.");
            }
            catch (Exception e)
            {
                var logger = new Class1();
                logger.LogToFile("Error al desregistrar el Add-in: " + e.Message);
            }
        }

        public bool ConnectToSW(object solidWorksInstance, int addInId)
        {
            
            try
            {   
                LogToFile("ConnectToSW: Iniciando conexión...", "INFO");
                solidWorksApp = solidWorksInstance as ISldWorks;
                if (solidWorksApp == null)
                {
                    LogToFile("Error: La instancia de SOLIDWORKS no es válida", "ERROR");
                    return false;
                }
 
                addInCookie = addInId;
                LogToFile($"Cookie del add-in establecido: {addInCookie}", "INFO");

                solidWorksApp.SetAddinCallbackInfo(0, this, addInCookie);
                commandManager = solidWorksApp.GetCommandManager(addInCookie);
                if (commandManager == null)
                {
                    LogToFile("Error: No se pudo obtener el CommandManager", "ERROR");
                    return false;
                }

                LogToFile("Inicializando manejadores de eventos...", "INFO");
                InitializeEventHandlers();

                if (IsInternetAvailable())
                {
                    LogToFile("Conexión a internet detectada. Verificando actualizaciones...", "INFO");
                    Task.Run(async () =>
                    {
                        try
                        {
                            LogToFile("Iniciando verificación de actualizaciones...", "INFO");
                            await CheckForUpdates();
                            LogToFile("Iniciando envío de sesión...", "INFO");
                            await SendSesionSW();
                            LogToFile("Iniciando envío de archivos JWL...", "INFO");
                            await SendJwl();
                            LogToFile("Tareas asincrónicas completadas con éxito", "INFO");
                        }
                        catch (Exception asyncEx)
                        {
                            LogToFile($"Error en tareas asincrónicas de conexión: {asyncEx.Message}\nStack Trace: {asyncEx.StackTrace}", "ERROR");
                        }
                    });
                }
                else
                {
                    LogToFile("No hay conexión a internet. No se verificarán actualizaciones.", "WARNING");
                }

                LogToFile("Verificando archivo de configuración...", "INFO");
                HandleSolidWorksSessionStart();

                LogToFile("Conexión con SOLIDWORKS completada con éxito", "INFO");
                return true;
            }
            catch (COMException comEx)
            {
                LogToFile($"Error COM en ConnectToSW: {comEx.Message}\nCódigo: {comEx.ErrorCode}\nStack Trace: {comEx.StackTrace}", "ERROR");
                return false;
            }
            catch (Exception ex)
            {
                LogToFile($"Error en ConnectToSW: {ex.Message}\nStack Trace: {ex.StackTrace}", "ERROR");
                return false;
            }
        }

        /// <summary>
        /// Lógica principal para determinar el estado de la sesión al iniciar SOLIDWORKS.
        /// </summary>
        private void HandleSolidWorksSessionStart()
        {
            try
            {
                if (!DoesConfigFileExist() || string.IsNullOrWhiteSpace(getTextFile())) // Usamos getTextFile para verificar si está vacío
                {
                    LogToFile("Archivo de configuración de sesiones no existe o está vacío. Registrando 'Open'.", "INFO");
                    RegisterSolidWorksSession("Open"); // Registrar 'Open'
                    return;
                }

                LogToFile("Verificando el estado de la última sesión en el archivo...", "INFO");
                string lastSessionEntry = ReadLastSessionEntryFromConfigFile(); // <-- ¡NUEVA FUNCIÓN!

                if (string.IsNullOrWhiteSpace(lastSessionEntry))
                {
                    LogToFile("No se pudo leer una última entrada de sesión válida. Registrando 'Open'.", "WARNING");
                    RegisterSolidWorksSession("Open");
                    return;
                }

                // El formato esperado es: "YYYY/MM/DD HH:MM:SS,Action,UserID,Email;"
                string[] parts = lastSessionEntry.TrimEnd(';').Split(','); // Eliminar el ';' final antes de dividir
                if (parts.Length < 2) // Necesitamos al menos fecha y acción
                {
                    LogToFile($"Formato inválido en la última entrada de sesión: '{lastSessionEntry}'. No se pudo determinar el estado. Registrando 'Open'.", "WARNING");
                    RegisterSolidWorksSession("Open"); // Siempre iniciar una nueva sesión en caso de duda
                    return;
                }

                string sessionStatus = parts[1].Trim(); // La acción es la segunda parte
                LogToFile($"Estado de la última sesión registrada: '{sessionStatus}'", "INFO");

                if (sessionStatus.Equals("Open", StringComparison.OrdinalIgnoreCase))
                {
                    // Si la última sesión fue "Open" y estamos iniciando de nuevo,
                    // significa que la sesión anterior no se cerró correctamente (Crash).
                    LogToFile("Se detectó una sesión 'Open' previa sin cerrar. Registrando 'Crash'.", "WARNING");
                    RegisterSolidWorksSession("Crash"); // Registrar el Crash para la sesión anterior
                }
                // Si la última sesión fue "Close" o "Crash", no necesitamos hacer nada especial antes de registrar la nueva "Open".

                // Después de manejar el "Crash" (si aplica) o si la última fue "Close"/"Crash",
                // SIEMPRE registramos la nueva sesión "Open" para este inicio.
                LogToFile("Registrando la nueva sesión de SOLIDWORKS como 'Open'.", "INFO");
                RegisterSolidWorksSession("Open");
            }
            catch (Exception ex)
            {
                LogToFile($"Error en HandleSolidWorksSessionStart: {ex.Message}\nStack Trace: {ex.StackTrace}", "ERROR");
                // Como último recurso, si algo falla al leer o procesar, intenta registrar un Open.
                try { RegisterSolidWorksSession("Open"); } catch { /* Silenciar si falla */ }
            }
        }

        /// 
        /// Configura los manejadores de eventos para capturar eventos de SOLIDWORKS.
        /// 
        private void InitializeEventHandlers()
        {
            try
            {
                LogToFile("Configurando manejadores de eventos de SOLIDWORKS...", "INFO");
                solidWorksEventHandler = (SldWorks)solidWorksApp;
                if (solidWorksEventHandler == null)
                {
                    throw new InvalidCastException("No se pudo convertir SolidWorksApp a SldWorks");
                }

                openDocuments = new Hashtable();

                LogToFile("Manejadores de eventos inicializados correctamente", "INFO");

                // Validación de licencia (comentada actualmente)
                // LogToFile("Validando licencia...", "INFO");
                // if (!ValidateLicense())
                // {
                //     LogToFile("La validación de licencia ha fallado", "WARNING");
                // }
            }
            catch (InvalidCastException icEx)
            {
                LogToFile($"Error al convertir tipos en InitializeEventHandlers: {icEx.Message}", "ERROR");
                throw;  // Relanzamos para que se maneje en el nivel superior
            }
            catch (Exception ex)
            {
                LogToFile($"Error al inicializar manejadores de eventos: {ex.Message}\nDetalles: {ex.StackTrace}", "ERROR");
                throw;  // Relanzamos para que se maneje en el nivel superior
            }
        }
        private void LoadConfiguration()
        {
            try
            {
                LogToFile("LoadConfiguration cargado correctamente", "INFO");

                // Limpiar configuración anterior
                
                isConfigured = true;

                // Log final de configuración (sin mostrar valores sensibles)
                LogToFile("-----------------Estado de configuración------------------", "INFO");
                LogToFile($"AccessKeyId: {(string.IsNullOrEmpty(accessKeyId) ? "NO CONFIGURADO" : "CONFIGURADO")}", "INFO");
                LogToFile($"SecretAccessKey: {(string.IsNullOrEmpty(secretAccessKey) ? "NO CONFIGURADO" : "CONFIGURADO")}", "INFO");
                LogToFile($"BucketName: {(string.IsNullOrEmpty(bucketName) ? "NO CONFIGURADO" : bucketName)}", "INFO");
                LogToFile($"Region: {(string.IsNullOrEmpty(region) ? "NO CONFIGURADO" : region)}", "INFO");
                LogToFile($"EncryptionKey: {(string.IsNullOrEmpty(encryptionKey) ? "NO CONFIGURADO" : "CONFIGURADO")}", "INFO");
            }
            catch (ConfigurationErrorsException confEx)
            {
                LogToFile($"Error de configuración: {confEx.Message}", "ERROR");
                Debug.Print($"Error de configuración: {confEx.ToString()}");
                this.isConfigured = false;
            }
            catch (Exception ex)
            {
                LogToFile($"Error inesperado al cargar configuración: {ex.Message}", "ERROR");
                Debug.Print($"Error inesperado: {ex.ToString()}");
                this.isConfigured = false;
            }
        }
        private void LogToFile(string message, string level = "INFO")
        {
            // Si LogFilePath no se pudo inicializar en el constructor (error crítico), no podemos loguear a archivo.
            if (string.IsNullOrEmpty(LogFilePath))
            {
                Debug.Print($"FALLBACK LOG (LogFilePath no disponible): [{level}] - {message}");
                return;
            }

            try
            {
                string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logEntry;
                if (level.ToUpper() == "ERROR")
                {
                    logEntry = $"\r\n[---------------------------- ERROR ----------------------------]\r\n" +
                               $"Fecha: {timeStamp}\r\n" +
                               $"Mensaje: {message}\r\n" +
                               $"[--------------------------------------------------------------]\r\n";
                }
                else
                {
                    logEntry = $"{timeStamp} [{level}] - {message}\r\n";
                }
                File.AppendAllText(LogFilePath, logEntry); // File.AppendAllText maneja el using y close.
            }
            catch (Exception ex)
            {
                // Si falla el logging, escribir a Debug para no entrar en bucle infinito.
                Debug.Print($"ERROR AL ESCRIBIR EN LOG '{LogFilePath}': {ex.Message}. Mensaje original: {message}");
            }
        }

        /// 
        /// ////////////////////////////////////////////////////////////////////////// Trabajar con el archivo txt de sesiones /////////////////////////////////////////////////////////////////////////
        /// 
        /// 
        #region
        /// 
        /// Verifica si el archivo de configuración existe.
        /// 
        private bool DoesConfigFileExist()
        {
            try
            {
                return File.Exists(ConfigFilePath);
            }
            catch (Exception ex)
            {
                LogToFile($"Error al verificar la existencia del archivo de configuración: {ex.Message}", "ERROR");
                return false;
            }
        }

        /// 
        /// Lee el contenido del archivo de configuración.
        /// 
        private void ReadConfigFile()
        {
            try
            {
                LogToFile("Iniciando lectura del archivo de configuración...", "INFO");
                if (!File.Exists(ConfigFilePath))
                {
                    LogToFile($"El archivo de configuración no existe en la ruta: {ConfigFilePath}", "ERROR");
                    throw new FileNotFoundException("El archivo de configuración no existe.", ConfigFilePath);
                }

                string fileContent = File.ReadAllText(ConfigFilePath);
                LogToFile("Archivo de configuración leído correctamente", "INFO");

                if (string.IsNullOrWhiteSpace(fileContent))
                {
                    LogToFile("El archivo de configuración está vacío. Registrando nueva sesión.", "WARNING");
                    RegisterSolidWorksSession("Open");
                }
                else
                {
                    LogToFile("Verificando el estado de la última sesión...", "INFO");
                    string lastLine = ReadLastLine(ConfigFilePath);
                    if (string.IsNullOrEmpty(lastLine))
                    {
                        LogToFile("No se pudo leer la última línea. Registrando nueva sesión.", "WARNING");
                        RegisterSolidWorksSession("Open");
                        return;
                    }

                    string[] parts = lastLine.TrimEnd(';', ',').Split(',');
                    if (parts.Length < 2)
                    {
                        LogToFile($"Formato inválido en la última línea: {lastLine}", "WARNING");
                        RegisterSolidWorksSession("Open");
                        return;
                    }

                    string sessionStatus = parts[1].Trim();
                    LogToFile($"Estado de la última sesión: {sessionStatus}", "INFO");

                    if (sessionStatus == "Open")
                    {
                        LogToFile("Se detectó una sesión abierta sin cerrar. Registrando como crash.", "WARNING");
                        RegisterSolidWorksSession("Crash");
                    }

                    LogToFile("Registrando nueva sesión de SOLIDWORKS", "INFO");
                    RegisterSolidWorksSession("Open");
                }
            }
            catch (FileNotFoundException fnfEx)
            {
                LogToFile($"Error: Archivo no encontrado: {fnfEx.Message}\nRuta: {fnfEx.FileName}", "ERROR");
                CreateConfigFile();  // Intentamos crear el archivo
            }
            catch (IOException ioEx)
            {
                LogToFile($"Error de E/S al leer el archivo de configuración: {ioEx.Message}", "ERROR");
                // Intentamos esperar un momento y reintentar una vez
                try
                {
                    System.Threading.Thread.Sleep(500);
                    if (File.Exists(ConfigFilePath))
                    {
                        string fileContent = File.ReadAllText(ConfigFilePath);
                        if (string.IsNullOrWhiteSpace(fileContent))
                        {
                            RegisterSolidWorksSession("Open");
                        }
                    }
                    else
                    {
                        CreateConfigFile();
                    }
                }
                catch (Exception retryEx)
                {
                    LogToFile($"Error en el reintento de lectura: {retryEx.Message}", "ERROR");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error al leer el archivo de configuración: {ex.Message}\nStack Trace: {ex.StackTrace}", "ERROR");
                try
                {
                    CreateConfigFile();  // Intentamos crear un nuevo archivo como último recurso
                }
                catch { }  // Si falla, ya hemos registrado el error original
            }
        }

        /// 
        /// Crea un nuevo archivo de configuración si no existe.
        /// 
        private void CreateConfigFile()
        {
            try
            {
                LogToFile($"Intentando crear archivo de configuración en: {ConfigFilePath}", "INFO");
                string directoryPath = Path.GetDirectoryName(ConfigFilePath);

                if (!Directory.Exists(directoryPath))
                {
                    LogToFile($"El directorio no existe. Creando directorio: {directoryPath}", "INFO");
                    Directory.CreateDirectory(directoryPath);
                }

                using (FileStream fs = File.Create(ConfigFilePath))
                {
                    LogToFile("Archivo de configuración creado exitosamente", "INFO");
                }

                LogToFile("Configurando permisos del archivo...", "INFO");
                SetFilePermissions(ConfigFilePath);

                // LogToFile("Registrando sesión inicial de SOLIDWORKS", "INFO");
                // RegisterSolidWorksSession("Open");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                LogToFile($"Error de permisos al crear archivo de configuración: {uaEx.Message}", "ERROR");
                // Intentar crear en una ubicación alternativa como último recurso
                try
                {
                    string altPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                                                 AppName, $"{AppName}_sessions.txt");
                    LogToFile($"Intentando crear archivo en ubicación alternativa: {altPath}", "WARNING");

                    Directory.CreateDirectory(Path.GetDirectoryName(altPath));
                    using (FileStream fs = File.Create(altPath))
                    {
                        ConfigFilePath = altPath;  // Actualizar la ruta del archivo
                        LogToFile($"Archivo creado en ubicación alternativa: {altPath}", "INFO");
                    }
                    SetFilePermissions(ConfigFilePath);
                    //RegisterSolidWorksSession("Open");
                }
                catch (Exception altEx)
                {
                    LogToFile($"Error al crear archivo en ubicación alternativa: {altEx.Message}", "ERROR");
                }
            }
            catch (IOException ioEx)
            {
                LogToFile($"Error de E/S al crear archivo de configuración: {ioEx.Message}", "ERROR");
            }
            catch (Exception ex)
            {
                LogToFile($"Error al crear el archivo de configuración: {ex.Message}\nStack Trace: {ex.StackTrace}", "ERROR");
            }
        }

        /// 
        /// Crea los permisos para leer el archivo.
        /// 
        private void SetFilePermissions(string filePath)
        {
            try
            {
                LogToFile($"Configurando permisos para el archivo: {filePath}", "INFO");
                FileSecurity fileSecurity = File.GetAccessControl(filePath);
                string currentUser = WindowsIdentity.GetCurrent().Name;

                LogToFile($"Otorgando permisos al usuario actual: {currentUser}", "INFO");
                fileSecurity.AddAccessRule(new FileSystemAccessRule(
                    currentUser,
                    FileSystemRights.Read | FileSystemRights.Write,
                    InheritanceFlags.None,
                    PropagationFlags.None,
                    AccessControlType.Allow
                ));

                File.SetAccessControl(filePath, fileSecurity);
                LogToFile("Permisos configurados correctamente", "INFO");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                LogToFile($"Error de permisos al configurar permisos del archivo: {uaEx.Message}", "ERROR");
            }
            catch (Exception ex)
            {
                LogToFile($"Error al configurar permisos del archivo: {ex.Message}", "ERROR");
            }
        }

        /// 
        /// Registra las sesiones en el archivo.
        /// 
        private void RegisterSolidWorksSession(string action)
        {
            try
            {
                LogToFile($"Registrando sesión de SOLIDWORKS: {action}", "INFO");
                string email = GetEmail();
                string userID = GetUserID();

                if (string.IsNullOrEmpty(email))
                {
                    LogToFile("No se pudo obtener el email del usuario", "WARNING");
                    email = "unknown@email.com";  // Valor por defecto
                }

                if (string.IsNullOrEmpty(userID))
                {
                    LogToFile("No se pudo obtener el ID del usuario", "WARNING");
                    userID = "unknown";  // Valor por defecto
                }

                string sessionEntry = $"{DateTime.Now:yyyy/MM/dd HH:mm:ss},{action},{userID},{email};";
                
                File.AppendAllText(ConfigFilePath, sessionEntry);
                LogToFile($"Sesión {action} registrada correctamente", "INFO");
            }
            catch (IOException ioEx)
            {
                LogToFile($"Error de E/S al registrar sesión: {ioEx.Message}", "ERROR");
            }
            catch (Exception ex)
            {
                LogToFile($"Error al registrar la sesión en SOLIDWORKS: {ex.Message}", "ERROR");
            }
        }

        /// 
        /// Obtiene la ultima linea del archivo.
        /// 
        private string ReadLastLine(string filePath)
        {
            string lastLine = string.Empty;
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader sr = new StreamReader(fs))
                {
                    while (!sr.EndOfStream)
                    {
                        lastLine = sr.ReadLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print("Error al leer la última línea del archivo: " + ex.Message);
                LogToFile($"Error al leer la última línea del archivo: {ex.Message}", "ERROR");
            }
            return lastLine;
        }


        /// <summary>
        /// Obtiene la última entrada de sesión del archivo, asumiendo el formato ";"-separador.
        /// </summary>
        private string ReadLastSessionEntryFromConfigFile()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    return string.Empty;
                }

                string fileContent = File.ReadAllText(ConfigFilePath);
                if (string.IsNullOrWhiteSpace(fileContent))
                {
                    return string.Empty;
                }

                // Dividir la cadena por el delimitador de sesión ';'
                // y obtener la última parte no vacía.
                string[] sessions = fileContent.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (sessions.Length > 0)
                {
                    // La última sesión será el último elemento del array.
                    // Añadimos el ';' de nuevo porque la API.
                    string lastEntry = sessions[sessions.Length - 1].Trim();
                    LogToFile($"Última entrada de sesión leída del archivo: '{lastEntry}'", "INFO");
                    return lastEntry + ";"; // Re-añadimos el ';' si la API lo espera al final
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error al leer la última entrada de sesión del archivo de sesiones: {ex.Message}", "ERROR");
            }
            return string.Empty;
        }
        /// 
        /// Enviar las sesiones a la API.
        ///
        public async Task SendSesionSW()
        {
            try
            {
                LogToFile("Iniciando envío de información de sesiones...", "INFO");
                string token = await GetToken(TOKEN_Analitics_URL);

                if (string.IsNullOrEmpty(token))
                {
                    LogToFile("Error: No se pudo obtener el token para enviar sesiones", "ERROR");
                    return;
                }

                string fileContent = getTextFile();
                if (string.IsNullOrEmpty(fileContent))
                {
                    LogToFile("No hay datos de sesión para enviar", "WARNING");
                    return;
                }

                string serialNumber = GetSerialnumber();
                string swVersion = GetSwVersion();
                string deviceId = GetDeviceID();

                if (string.IsNullOrEmpty(deviceId))
                {
                    LogToFile("No se pudo obtener el ID del dispositivo", "WARNING");
                    deviceId = "unknown-device";
                }

                LogToFile("Preparando payload para envío de sesiones...", "INFO");
                var jsonPayload = new
                {
                    code = "r5ncccmGhzLG",
                    DeviceID = deviceId,
                    License = serialNumber,
                    SWVersion = swVersion,
                    file = fileContent
                };

                string requestBody = JsonConvert.SerializeObject(jsonPayload, Formatting.Indented);
                LogToFile($"Tamaño del payload: {requestBody.Length} bytes", "DEBUG");

                var request = new HttpRequestMessage(HttpMethod.Post, API_Analitics_URL);
                request.Headers.Add("authorization", token);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                LogToFile("Enviando datos de sesión al servidor...", "INFO");
                var response = await client.SendAsync(request);
                string responseText = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    LogToFile($"Error en la solicitud de envío de sesiones: {response.StatusCode} - {responseText}", "ERROR");
                    return;
                }

                try
                {
                    JObject jsonResponse = JObject.Parse(responseText);

                    if (jsonResponse.ContainsKey("message"))
                    {
                        JArray messageArray = (JArray)jsonResponse["message"];
                        JToken firstItem = messageArray?.FirstOrDefault();

                        if (firstItem != null && firstItem["statusInsert"]?.ToString() == "Insertado")
                        {
                            LogToFile("Sesión enviada e insertada correctamente", "INFO");
                            ClearAlltext();
                        }
                        else
                        {
                            LogToFile($"Advertencia: La sesión no fue insertada correctamente. Respuesta: {responseText}", "WARNING");
                        }
                    }
                    else
                    {
                        LogToFile($"Advertencia: No se encontró 'message' en la respuesta: {responseText}", "WARNING");
                    }
                }
                catch (JsonException jsonEx)
                {
                    LogToFile($"Error al procesar respuesta JSON: {jsonEx.Message}\nRespuesta: {responseText}", "ERROR");
                }
            }
            catch (HttpRequestException httpEx)
            {
                LogToFile($"Error de red en SendSesionSW: {httpEx.Message}", "ERROR");
            }
            catch (Exception ex)
            {
                LogToFile($"Error en SendSesionSW: {ex.Message}\nStack Trace: {ex.StackTrace}", "ERROR");
            }
        }


        /// 
        /// Obtiene todo el contenido del archivo.
        ///
        public string getTextFile()
        {
            string filecontest = "";
            try
            {
                string filecontent = File.ReadAllText(ConfigFilePath);
                filecontest = filecontent;
                return filecontest;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                LogToFile($"Error al leer el contenido del archivo: {ex.Message}", "ERROR");
                return "";
            }
        }

        /// 
        /// Vacia el archivo.
        ///
        public void ClearAlltext()
        {
            try
            {
                File.WriteAllText(ConfigFilePath, string.Empty);
                LogToFile("Archivo de configuración vaciado correctamente", "INFO");
            }
            catch (Exception ex)
            {
                LogToFile($"Error al vaciar el archivo de configuración: {ex.Message}", "ERROR");
            }
        }
        #endregion
        /// 
        /// ////////////////////////////////////////////////////////////////////////// Miselaneos /////////////////////////////////////////////////////////////////////////
        /// 
        #region
        /// 
        /// Desconectar de SW.
        /// 
        public bool DisconnectFromSW()
        {
            try
            {
                RegisterSolidWorksSession("Close");
                System.Runtime.InteropServices.Marshal.ReleaseComObject(commandManager);
                commandManager = null;
                System.Runtime.InteropServices.Marshal.ReleaseComObject(solidWorksApp);
                solidWorksApp = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                LogToFile("Desconexión de SOLIDWORKS completada", "INFO");
                return true;
            }
            catch (COMException comEx)
            {
                LogToFile($"Error COM al desconectar de SOLIDWORKS: {comEx.Message}\nCódigo: {comEx.ErrorCode}", "ERROR");
                return false;
            }
            catch (Exception ex)
            {
                LogToFile($"Error al desconectar de SOLIDWORKS: {ex.Message}", "ERROR");
                return false;
            }
        }

        /// 
        /// Agregar icono ---- Aun esta en test.
        ///
        private string GetIconPathFromRegistry()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName"))
                {
                    if (key != null)
                    {
                        string iconPath = key.GetValue("Source")?.ToString();
                        LogToFile($"Ruta del icono obtenida del registro: {iconPath}", "INFO");
                        return iconPath;
                    }
                    else
                    {
                        LogToFile("No se encontró la clave del registro para la ruta del icono.", "WARNING");
                        return string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener la ruta del icono del registro: " + ex.Message);
                LogToFile($"Error al obtener la ruta del icono del registro: {ex.Message}", "ERROR");
                return string.Empty;
            }
        }

        /// 
        /// Validar que el usuario tenga licencia de analitics.
        ///
        private bool ValidateLicense()
        {
            try
            {
                LogToFile("Iniciando validación de licencia...", "INFO");
                // Lógica pendiente de implementación
                LogToFile("Validación de licencia completada (lógica no implementada)", "INFO");
                return true;
            }
            catch (Exception ex)
            {
                LogToFile($"Error durante la validación de licencia: {ex.Message}", "ERROR");
                return false;
            }
        }

        /// 
        /// Verifica si hay conexión a internet.
        /// 
        private bool IsInternetAvailable()
        {
            try
            {
                LogToFile("Verificando conexión a internet...", "INFO");
                using (var client = new HttpClient())
                {
                    var response = client.GetAsync("https://www.google.com").Result;
                    bool isAvailable = response.IsSuccessStatusCode;
                    LogToFile($"Estado de conexión a internet: {(isAvailable ? "Disponible" : "No disponible")}", isAvailable ? "INFO" : "WARNING");
                    return isAvailable;
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error al verificar la conexión a internet: {ex.Message}", "ERROR");
                return false;
            }
        }

        /// 
        /// Obtiene el email.
        ///
        private string GetEmail()
        {
            string email = null;
            try
            {
                LogToFile("Obteniendo email del usuario...", "INFO");
                RegistryKey key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ComputerName");
                if (key != null)
                {
                    object o = key.GetValue("Email");
                    if (o != null)
                    {
                        email = o.ToString();
                        LogToFile($"Email del usuario obtenido: {email}", "INFO");
                    }
                    else
                    {
                        LogToFile("No se encontró el valor 'Email' en el registro.", "WARNING");
                    }
                    key.Close();
                }
                else
                {
                    LogToFile("No se encontró la clave del registro para el email.", "WARNING");
                }
            }
            catch (Exception e)
            {
                Debug.Print("Error al obtener el email: " + e.ToString());
                LogToFile($"Error al obtener el email del usuario: {e.Message}", "ERROR");
            }
            return email;
        }

        /// 
        /// Obtiene el ID del usuario.
        ///
        private string GetUserID()
        {
            string userID = null;
            try
            {
                LogToFile("Obteniendo ID del usuario...", "INFO");
                RegistryKey key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ComputerName");
                if (key != null)
                {
                    object o = key.GetValue("UserID");
                    if (o != null)
                    {
                        userID = o.ToString();
                        LogToFile($"ID del usuario obtenido: {userID}", "INFO");
                    }
                    else
                    {
                        LogToFile("No se encontró el valor 'UserID' en el registro.", "WARNING");
                    }
                    key.Close();
                }
                else
                {
                    LogToFile("No se encontró la clave del registro para el ID de usuario.", "WARNING");
                }
            }
            catch (Exception e)
            {
                Debug.Print("Error al obtener el UserID: " + e.ToString());
                LogToFile($"Error al obtener el ID del usuario: {e.Message}", "ERROR");
            }
            return userID;
        }

        /// 
        /// Obtiene el ID del cliente.
        ///
        private string GetCustomerID()
        {
            string customerID = null;
            try
            {
                LogToFile("Obteniendo ID del cliente...", "INFO");
                RegistryKey key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ComputerName");
                if (key != null)
                {
                    object o = key.GetValue("CustomerID");
                    if (o != null)
                    {
                        customerID = o.ToString();
                        LogToFile($"ID del cliente obtenido: {customerID}", "INFO");
                    }
                    else
                    {
                        LogToFile("No se encontró el valor 'CustomerID' en el registro.", "WARNING");
                    }
                    key.Close();
                }
                else
                {
                    LogToFile("No se encontró la clave del registro para el ID del cliente.", "WARNING");
                }
            }
            catch (Exception e)
            {
                Debug.Print("Error al obtener el customerID: " + e.ToString());
                LogToFile($"Error al obtener el ID del cliente: {e.Message}", "ERROR");
            }
            return customerID;
        }

        /// 
        /// Obtiene el numero serial.
        ///
        public string GetSerialnumber()
        {
            string serialNumber = null;
            string keyPath = @"SOFTWARE\SolidWorks\Licenses\Serial Numbers";
            List<string> valueList = new List<string>();
            try
            {
                LogToFile("Obteniendo número de serie de SOLIDWORKS...", "INFO");
                using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (baseKey != null)
                    {
                        string[] valueNames = baseKey.GetValueNames();

                        foreach (string valueName in valueNames)
                        {
                            if (baseKey.GetValueKind(valueName) == RegistryValueKind.String)
                            {
                                string valueData = baseKey.GetValue(valueName)?.ToString();
                                if (!string.IsNullOrEmpty(valueData))
                                {
                                    valueList.Add($"{valueName}: {valueData}");
                                }
                            }
                        }
                        serialNumber = string.Join(System.Environment.NewLine, valueList);
                        LogToFile($"Número de serie de SOLIDWORKS obtenido:\n{serialNumber}", "INFO");
                    }
                    else
                    {
                        LogToFile($"No se encontró la clave del registro: {keyPath}", "WARNING");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener el número de serie: " + ex.Message);
                LogToFile($"Error al obtener el número de serie de SOLIDWORKS: {ex.Message}", "ERROR");
            }
            return serialNumber;
        }

        /// 
        /// Obtiene la version de SW.
        ///
        public string GetSwVersion()
        {
            string fullVersion = "";
            try
            {
                LogToFile("Obteniendo versión de SOLIDWORKS...", "INFO");
                string baseVersion;
                string currentVersion;
                string hotfixed;

                solidWorksApp.GetBuildNumbers2(out baseVersion, out currentVersion, out hotfixed);
                fullVersion = $"SOLIDWORKS:{baseVersion} Current:{currentVersion} Hotfix:{hotfixed}";
                LogToFile($"Versión de SOLIDWORKS obtenida: {fullVersion}", "INFO");
            }
            catch (Exception ex)
            {
                LogToFile($"Error al obtener la versión de SOLIDWORKS: {ex.Message}", "ERROR");
                fullVersion = "Error al obtener la versión";
            }
            return fullVersion;
        }

        /// 
        /// Obtiene el ID del Device.
        ///
        private string GetDeviceID()
        {
            string deviceID = null;
            try
            {
                LogToFile("Obteniendo ID del dispositivo...", "INFO");
                RegistryKey key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ComputerName");
                if (key != null)
                {
                    object o = key.GetValue("deviceID");
                    if (o != null)
                    {
                        deviceID = o.ToString();
                        LogToFile($"ID del dispositivo obtenido: {deviceID}", "INFO");
                    }
                    else
                    {
                        LogToFile("No se encontró el valor 'deviceID' en el registro.", "WARNING");
                    }
                    key.Close();
                }
                else
                {
                    LogToFile("No se encontró la clave del registro para el ID del dispositivo.", "WARNING");
                }
            }
            catch (Exception e)
            {
                Debug.Print("Error al obtener deviceID: " + e.ToString());
                LogToFile($"Error al obtener el ID del dispositivo: {e.Message}", "ERROR");
            }
            return deviceID;
        }

        /// 
        /// Obtiene el ID del usuario.
        ///
        private static string GetSource()
        {
            string source = null;
            try
            {
                // No intentes loguear aquí, ya que LogFilePath podría no estar listo.
                RegistryKey key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ComputerName");
                if (key != null)
                {
                    object o = key.GetValue("Source");
                    if (o != null)
                    {
                        source = o.ToString();
                    }
                    key.Close();
                }
            }
            catch (Exception ex)
            {
                // No loguear a archivo aquí. Solo a Debug si es necesario.
                Debug.Print($"Error silencioso al leer 'Source' del registro: {ex.Message}");
                source = null; // Asegurarse de que sea null en caso de error
            }
            return source;
        }
        #endregion

        /// 
        /// ////////////////////////////////////////////////////////////////////////// Actualizacion automatica /////////////////////////////////////////////////////////////////////////
        /// 
        #region
        /// 
        /// Obtiene la ultima vesrion de analitycs desde una peticion.
        ///
        private async Task CheckForUpdates()
        {
            try
            {
                LogToFile("Iniciando verificación de actualizaciones...", "INFO");
                string token = await GetToken(TOKEN_Xpertme_URL);

                if (string.IsNullOrEmpty(token))
                {
                    LogToFile("No se pudo obtener el token para verificar actualizaciones", "ERROR");
                    return;
                }

                LogToFile("Token obtenido. Consultando información de la última versión...", "INFO");
                var request = new HttpRequestMessage(HttpMethod.Post, API_Xpertme_URL);
                request.Headers.Add("Authorization", $"Bearer {token}");
                request.Content = new StringContent("{ \"mode\": \"dev\" }", Encoding.UTF8, "application/json");

                LogToFile("Enviando solicitud de verificación de actualizaciones...", "INFO");
                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    LogToFile($"Error al verificar actualizaciones. Código: {response.StatusCode}", "ERROR");
                    return;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                JObject jsonObject = JObject.Parse(responseBody);
                var lastInstaller = jsonObject["data"]?["lastInstaller"];

                if (lastInstaller == null)
                {
                    LogToFile("No se encontró información del último instalador en la respuesta", "WARNING");
                    return;
                }

                VERSION = lastInstaller["Version"]?.ToString();
                cambios = lastInstaller["ReleaseNotes"]?.ToString();
                link = lastInstaller["publicDllLink"]?.ToString();

                if (string.IsNullOrEmpty(VERSION))
                {
                    LogToFile("No se pudo obtener el número de versión", "WARNING");
                    return;
                }

                if (CompareVersions())
                {
                    LogToFile($"Nueva versión disponible detectada por Add-In: {this.VERSION}. Actual: {this.versionActual}. Lanzando actualizador externo.", "INFO");

                    string rutaInstalacion;
                    try
                    {
                        rutaInstalacion = Path.Combine(GetSource(), AppName); // Asume que esto da el directorio del Add-In o del Updater
                        if (string.IsNullOrEmpty(rutaInstalacion) || !Directory.Exists(rutaInstalacion))
                        {
                            LogToFile($"Ruta de GetSource() inválida ('{rutaInstalacion}'). Usando ruta del Add-In.", "WARNING");
                            rutaInstalacion = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        }
                    }
                    catch (Exception exSource)
                    {
                        LogToFile($"Error en GetSource(): {exSource.Message}. Usando ruta del Add-In.", "ERROR");
                        rutaInstalacion = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    }

                    string updaterExeName = "updates.exe"; // Nombre del ejecutable de tu actualizador
                    string fullUpdaterPath = Path.Combine(rutaInstalacion, updaterExeName);

                    if (File.Exists(fullUpdaterPath))
                    {
                        try
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo(fullUpdaterPath);
                            startInfo.UseShellExecute = true; // Importante para que funcione la elevación de privilegios del manifest del updater.exe

                            Process.Start(startInfo); // Iniciar sin argumentos
                            LogToFile($"Actualizador '{updaterExeName}' ejecutado SIN argumentos.", "INFO");
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"Error al ejecutar '{fullUpdaterPath}': {ex.Message}", "ERROR");
                        }
                    }
                    else
                    {
                        LogToFile($"El actualizador '{fullUpdaterPath}' no fue encontrado.", "ERROR");
                    }
                }
                else
                {
                    LogToFile("El software está actualizado a la última versión", "INFO");
                }
            }
            catch (HttpRequestException httpEx)
            {
                LogToFile($"Error de red al verificar actualizaciones: {httpEx.Message}", "ERROR");
            }
            catch (JsonException jsonEx)
            {
                LogToFile($"Error al procesar respuesta JSON: {jsonEx.Message}", "ERROR");
            }
            catch (Exception ex)
            {
                LogToFile($"Error al verificar actualizaciones: {ex.Message}\nStack Trace: {ex.StackTrace}", "ERROR");
            }
        }

        /// 
        /// Obtiene la version actual de analitycs y la compara con la ultima.
        ///
        private bool CompareVersions()
        {
            try
            {
                var assemblyVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;
                if (string.IsNullOrEmpty(assemblyVersion) || string.IsNullOrEmpty(VERSION))
                {
                    LogToFile($"No se pudo obtener la versión actual ({assemblyVersion}) o la última versión ({VERSION}) para comparar.", "WARNING");
                    return false;
                }

                var v1 = assemblyVersion.Split('.').Select(int.Parse).ToList();
                versionActual = assemblyVersion;
                var v2 = VERSION.Split('.').Select(int.Parse).ToList();

                LogToFile($"Versión más reciente: {VERSION}", "INFO");
                LogToFile($"Versión actual: {versionActual}", "INFO");

                // Asegurarse de que ambas listas tengan la misma cantidad de elementos para la comparación
                int maxLength = Math.Max(v1.Count, v2.Count);
                for (int i = 0; i < maxLength; i++)
                {
                    int version1Part = (i < v1.Count) ? v1[i] : 0;
                    int version2Part = (i < v2.Count) ? v2[i] : 0;

                    if (version2Part > version1Part) return true;
                    if (version2Part < version1Part) return false;
                }

                return false; // Las versiones son iguales
            }
            catch (FormatException feEx)
            {
                string errorMsg = $"Error al comparar versiones (formato incorrecto): {feEx.Message}";
                LogToFile(errorMsg, "ERROR");
                Debug.Print(errorMsg);
                return false;
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error al comparar versiones: {ex.Message}";
                LogToFile(errorMsg, "ERROR");
                Debug.Print(errorMsg);
                return false; // Por defecto, no hay actualización en caso de error
            }
        }

        /// 
        /// Obtiene el token de forma dinamica.
        ///
        private async Task<string> GetToken(string url, string requestBody = "")
        {
            string token = null;
            try
            {
                LogToFile($"Obteniendo token desde la URL: {url}", "INFO");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var request = new HttpRequestMessage(HttpMethod.Post, url);

                if (url == TOKEN_Xpertme_URL)
                {
                    request.Headers.Add("Authorization", "Basic YW5hbHl0aWNzX3N3OlRtMFF6QTlR");
                    request.Content = new FormUrlEncodedContent(new[]
                    {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", "analytics:installers:get")
            });
                    LogToFile("Preparando solicitud de token para Xpertme.", "DEBUG");
                }
                else if (url == TOKEN_Analitics_URL)
                {
                    request.Content = new StringContent("", Encoding.UTF8, "application/x-www-form-urlencoded");
                    LogToFile("Preparando solicitud de token para Analitics.", "DEBUG");
                }
                else if (!string.IsNullOrEmpty(requestBody))
                {
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    LogToFile("Preparando solicitud de token con cuerpo JSON.", "DEBUG");
                }

                var response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string errorMsg = $"Error al obtener token (HTTP {response.StatusCode}): {responseBody}";
                    LogToFile(errorMsg, "ERROR");
                    Debug.Print(errorMsg);
                    return null;
                }

                JObject jsonObject = JObject.Parse(responseBody);

                token = jsonObject["token"]?.ToString() ?? jsonObject["accessToken"]?.ToString();

                if (string.IsNullOrEmpty(token))
                {
                    string errorMsg = "Error: El token no se encontró en la respuesta.";
                    LogToFile(errorMsg, "ERROR");
                    Debug.Print(errorMsg);
                    return null;
                }

                LogToFile("Token obtenido correctamente.", "INFO");
                Debug.Print("Token obtenido correctamente.");
                return token;
            }
            catch (HttpRequestException httpEx)
            {
                string errorMsg = "Error HTTP en GetToken: " + httpEx.Message;
                LogToFile(errorMsg, "ERROR");
                Debug.Print(errorMsg);
                return null;
            }
            catch (JsonException jsonEx)
            {
                string errorMsg = "Error al procesar JSON en GetToken: " + jsonEx.Message;
                LogToFile(errorMsg, "ERROR");
                Debug.Print(errorMsg);
                return null;
            }
            catch (Exception ex)
            {
                string errorMsg = "Error general en GetToken: " + ex.Message;
                LogToFile(errorMsg, "ERROR");
                Debug.Print(errorMsg);
                return null;
            }
        }

        #endregion

        /// 
        /// ////////////////////////////////////////////////////////////////////////// Enviar archivos a AWS /////////////////////////////////////////////////////////////////////////
        /// 
         #region
        private async Task SendJwl()
        {
            try
            {
                LogToFile("Iniciando proceso de envío de archivos de registro...", "INFO");

                // Obtener versión de SOLIDWORKS
                string baseVersion, currentVersion, hotfixes;
                try
                {
                    solidWorksApp.GetBuildNumbers2(out baseVersion, out currentVersion, out hotfixes);
                    LogToFile($"Versión de SOLIDWORKS: Base={baseVersion}, Current={currentVersion}, Hotfixes={hotfixes}", "INFO");
                }
                catch (Exception versionEx)
                {
                    LogToFile($"Error al obtener información de versión de SOLIDWORKS: {versionEx.Message}", "ERROR");
                    return;
                }

                string versionSW = ExtractSWVersion(baseVersion);
                if (string.IsNullOrEmpty(versionSW) || versionSW == "UnknownVersion")
                {
                    LogToFile("No se pudo determinar la versión de SOLIDWORKS correctamente", "WARNING");
                }

                string userFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                string solidworksPath = Path.Combine(userFolder, "SOLIDWORKS", $"SOLIDWORKS {versionSW}");

                LogToFile($"Buscando archivos de registro en: {solidworksPath}", "INFO");

                // Verificar si el directorio existe
                if (!Directory.Exists(solidworksPath))
                {
                    LogToFile($"El directorio de SOLIDWORKS no existe: {solidworksPath}", "WARNING");

                    // Intentar buscar en una ubicación alternativa
                    string altPath = Path.Combine(userFolder, "SOLIDWORKS");
                    if (Directory.Exists(altPath))
                    {
                        // Buscar subdirectorios que contengan "SOLIDWORKS"
                        var swDirs = Directory.GetDirectories(altPath).Where(d => Path.GetFileName(d).Contains("SOLIDWORKS"));
                        if (swDirs.Any())
                        {
                            solidworksPath = swDirs.First();
                            LogToFile($"Se encontró un directorio alternativo: {solidworksPath}", "INFO");
                        }
                        else
                        {
                            LogToFile("No se encontraron directorios alternativos de SOLIDWORKS", "WARNING");
                            return;
                        }
                    }
                    else
                    {
                        LogToFile("No se pudo encontrar ningún directorio de SOLIDWORKS", "WARNING");
                        return;
                    }
                }

                // Intentar subir los archivos
                LogToFile("Intentando subir archivos de registro swxJRNL...", "INFO");
                await UploadIfExists(solidworksPath, "swxJRNL.swj");
                await UploadIfExists(solidworksPath, "swxJRNL.bak");

            }
            catch (Exception ex)
            {
                LogToFile($"Error al enviar archivos JRNL: {ex.Message}\nStack Trace: {ex.StackTrace}", "ERROR");
            }
        }

        private string ExtractSWVersion(string baseVersion)
        {
            try
            {
                if (string.IsNullOrEmpty(baseVersion))
                {
                    LogToFile("La versión base de SOLIDWORKS está vacía.", "WARNING");
                    return "UnknownVersion";
                }

                int startIndex = baseVersion.IndexOf("20");
                int endIndex = baseVersion.IndexOf("_");

                if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
                {
                    string version = baseVersion.Substring(startIndex, endIndex - startIndex);
                    LogToFile($"Versión de SOLIDWORKS extraída: {version}", "INFO");
                    return version;
                }
                else
                {
                    LogToFile($"No se pudo extraer la versión de SOLIDWORKS de la cadena: {baseVersion}", "WARNING");
                    return "UnknownVersion";
                }
            }
            catch (Exception ex)
            {
                string errorMsg = "Error al extraer la versión de SOLIDWORKS: " + ex.Message;
                LogToFile(errorMsg, "ERROR");
                Debug.Print(errorMsg);
                return "UnknownVersion";
            }
        }

        private async Task<bool> UploadIfExists(string directory, string fileName)
        {
            try
            {
                string filePath = Path.Combine(directory, fileName);
                LogToFile($"Verificando existencia del archivo: {filePath}", "INFO");

                if (File.Exists(filePath))
                {
                    LogToFile($"Archivo encontrado. Iniciando subida: {fileName}", "INFO");
                    await UploadToS3(filePath, fileName);
                    return true;
                }
                else
                {
                    LogToFile($"Archivo no existe: {filePath}", "WARNING");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error al verificar o subir archivo {fileName}: {ex.Message}", "ERROR");
                return false;
            }
        }

        private async Task UploadToS3(string filePath, string fileKeyName)
        {
            try
            {
                if (!isConfigured)
                {
                    LogToFile("ERROR: La configuración de AWS no se cargó correctamente desde App.config. No se puede subir el archivo.", "ERROR");
                    return;
                }

                string userId = GetUserID();
                string customerId = GetCustomerID();
                DateTime date = DateTime.Now;

                // Crear carpeta temporal si no existe
                if (!Directory.Exists(tempFolder))
                {
                    try
                    {
                        Directory.CreateDirectory(tempFolder);
                        LogToFile($"Carpeta temporal creada en: {tempFolder}", "INFO");
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"Error al crear la carpeta temporal: {ex.Message}", "ERROR");
                        return;
                    }
                }

                // Crear ruta del archivo temporal
                string tempFilePath = Path.Combine(tempFolder, Path.GetFileName(filePath));

                // Copiar el archivo a la carpeta temporal
                try
                {
                    File.Copy(filePath, tempFilePath, true);
                    LogToFile($"Archivo copiado a la carpeta temporal: {tempFilePath}", "INFO");
                }
                catch (IOException ioEx)
                {
                    LogToFile($"Error al copiar el archivo a la carpeta temporal: {ioEx.Message}", "ERROR");
                    return;
                }

                // Encriptar los valores
                string userIdEncryption = EncryptString(userId)?.Replace("/", "0");
                string customerIdEncryption = EncryptString(customerId)?.Replace("/", "0");
                string fileKeyNameEncryption = EncryptString(fileKeyName)?.Replace("/", "0");
                string fecha = date.ToString("yyyy-MM-dd HH-mm-ss");
                string keyName = $"{customerId}/{userId}/{fileKeyName}/{fecha}/{fileKeyName}";
                string encryptedKeyName = $"{customerIdEncryption}/{userIdEncryption}/{fileKeyNameEncryption}/{fecha}/{fileKeyName}";

                Debug.Print($"Subiendo archivo a S3: {tempFilePath}");
                LogToFile($"Subiendo archivo a S3 con clave encriptada: {encryptedKeyName}", "INFO");

                AWSCredentials credentials = new BasicAWSCredentials(this.accessKeyId, this.secretAccessKey);
                RegionEndpoint awsRegion = RegionEndpoint.GetBySystemName(this.region);


                using (var s3Client = new AmazonS3Client(credentials, awsRegion))
                using (var fileTransferUtility = new TransferUtility(s3Client))
                {
                    try
                    {
                        await fileTransferUtility.UploadAsync(tempFilePath, bucketName, encryptedKeyName);
                        Debug.Print("Archivo subido correctamente a AWS S3.");
                        LogToFile("Archivo subido correctamente a AWS S3.", "INFO");
                        await InsertLogs(keyName);
                    }
                    catch (AmazonS3Exception e)
                    {
                        string errorMsg = $"Error en AWS S3: {e.Message}";
                        LogToFile(errorMsg, "ERROR");
                        Debug.Print(errorMsg);
                    }
                    catch (Exception e)
                    {
                        string errorMsg = $"Error desconocido al subir a S3: {e.Message}";
                        LogToFile(errorMsg, "ERROR");
                        Debug.Print(errorMsg);
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error general en UploadToS3: {ex.Message}", "ERROR");
            }
            finally
            {
                // Borrar carpeta temporal completa
                try
                {
                    if (Directory.Exists(tempFolder))
                    {
                        Directory.Delete(tempFolder, true);
                        LogToFile($"Carpeta temporal eliminada: {tempFolder}", "INFO");
                    }
                }
                catch (Exception ex)
                {
                    LogToFile($"Error al eliminar la carpeta temporal: {ex.Message}", "WARNING");
                }
            }
        }

        public async Task InsertLogs(string FilesLogs)
        {
            try
            {
                LogToFile("Iniciando la inserción de logs...", "INFO");

                // Obtener el token de autenticación
                string token = await GetToken(TOKEN_Analitics_URL);

                if (string.IsNullOrEmpty(token))
                {
                    string errorMsg = "Error: No se pudo obtener el token para insertar logs.";
                    LogToFile(errorMsg, "ERROR");
                    Debug.Print(errorMsg);
                    return;
                }

                // Preparar la URL de la API
                string url = "https://api-ncsw.xpertme.com/api/createLogs"; // URL de la API de logs

                // Obtener los datos del dispositivo y usuario
                string device = GetDeviceID();
                string UserID = GetUserID(); // Asegúrate de que si UserID es null, sea manejado adecuadamente
                string UserMail = GetEmail(); // Asegúrate de obtener el correo del usuario

                // Preparar los datos de la solicitud
                var Json = new
                {
                    code = "r5ncccmGhzLG",
                    deviceID = device,
                    UserID = (UserID == null ? (object)null : UserID), // Si es null, pasamos un null explícito
                    UserMail = UserMail,
                    logList = new[]
                    {
                new { nameFile = FilesLogs } // Aquí convertimos FilesLogs en un objeto con la propiedad nameFile
            }
                };

                // Serializar los datos en formato JSON
                string requestBody = JsonConvert.SerializeObject(Json, Formatting.Indented);
                LogToFile($"Cuerpo de la solicitud para insertar logs: {requestBody}", "DEBUG");

                // Crear la solicitud HTTP
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"{token}"); // Añadir el token en el encabezado
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                // Enviar la solicitud
                LogToFile("Enviando solicitud para insertar logs...", "INFO");
                var response = await client.SendAsync(request);
                string responseText = await response.Content.ReadAsStringAsync();

                // Comprobar si la respuesta es exitosa
                if (!response.IsSuccessStatusCode)
                {
                    string errorMsg = $"Error en la solicitud para insertar logs: {response.StatusCode} - {responseText}";
                    LogToFile(errorMsg, "ERROR");
                    Debug.Print(errorMsg);
                    return;
                }

                // Procesar la respuesta de la API
                try
                {
                    JObject jsonResponse = JObject.Parse(responseText);
                    Debug.Print(jsonResponse.ToString());
                    LogToFile($"Respuesta de la API para insertar logs: {jsonResponse}", "INFO");
                }
                catch (JsonException jsonEx)
                {
                    string errorMsg = $"Error al procesar la respuesta JSON para insertar logs: {jsonEx.Message}\nRespuesta: {responseText}";
                    LogToFile(errorMsg, "ERROR");
                    Debug.Print(errorMsg);
                }
            }
            catch (HttpRequestException httpEx)
            {
                string errorMsg = $"Error de red al insertar logs: {httpEx.Message}";
                LogToFile(errorMsg, "ERROR");
                Debug.Print(errorMsg);
            }
            catch (Exception ex)
            {
                string errorMsg = "Error general al insertar logs: " + ex.Message;
                LogToFile(errorMsg, "ERROR");
                Debug.Print(errorMsg);
            }
        }

        private string EncryptString(string plainText)
        {
            try
            {
                if (string.IsNullOrEmpty(plainText))
                {
                    LogToFile("Texto plano para encriptar está vacío.", "WARNING");
                    return string.Empty;
                }
                if (string.IsNullOrEmpty(this.encryptionKey))
                {
                    LogToFile("La clave de encriptación está vacía.", "ERROR");
                    return string.Empty;
                }

                // Reemplazar las diagonales ("/") por "0"
                plainText = plainText.Replace("/", "0");

                byte[] iv = new byte[16];
                byte[] array;

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(this.encryptionKey.PadRight(32).Substring(0, 32));
                    aes.IV = iv;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                            {
                                streamWriter.Write(plainText);
                            }
                            array = memoryStream.ToArray();
                        }
                    }
                }

                // Log de la cadena encriptada (en Base64)
                string encryptedString = Convert.ToBase64String(array);
                LogToFile($"Cadena encriptada (Base64): {encryptedString}", "DEBUG");

                return encryptedString;
            }
            catch (EncoderFallbackException encoderEx)
            {
                string errorMsg = "Error de codificación al encriptar la cadena: " + encoderEx.Message;
                LogToFile(errorMsg, "ERROR");
                Debug.Print(errorMsg);
                return string.Empty;
            }
            catch (CryptographicException cryptoEx)
            {
                string errorMsg = "Error criptográfico al encriptar la cadena: " + cryptoEx.Message;
                LogToFile(errorMsg, "ERROR");
                Debug.Print(errorMsg);
                return string.Empty;
            }
            catch (Exception ex)
            {
                string errorMsg = "Error general al encriptar la cadena: " + ex.Message;
                LogToFile(errorMsg, "ERROR");
                Debug.Print(errorMsg);
                return string.Empty;
            }
        }
        #endregion


        /// 
        /// ////////////////////////////////////////////////////////////////////////// Eventos SW /////////////////////////////////////////////////////////////////////////
        /// 
    
    }
}