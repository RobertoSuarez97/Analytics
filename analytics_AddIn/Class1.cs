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
using Environment = System.Environment;

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

        #region Constantes y Miembros Estáticos

        // --- Constantes de la aplicación y API ---
        private const string AppName = "analytics";
        private const string ADDIN_KEY_TEMPLATE = @"SOFTWARE\SolidWorks\Addins\{{{0}}}";
        private const string ADDIN_STARTUP_KEY_TEMPLATE = @"Software\SolidWorks\AddInsStartup\{{{0}}}";
        private const string ADD_IN_TITLE_REG_KEY_NAME = "Title";
        private const string ADD_IN_DESCRIPTION_REG_KEY_NAME = "Description";
        private const string API_Xpertme_URL = "https://api-academy.xpertcad.com/v2/analytics/users/getAnalyticsInstallers";
        private const string API_Analitics_URL = "https://api-ncsw.xpertme.com/api/createSession";
        private const string TOKEN_Xpertme_URL = "https://api-academy.xpertcad.com/v2/system/oauth/token";
        private const string TOKEN_Analitics_URL = "https://api-ncsw.xpertme.com/api/auth";
        private readonly string executionMode = "prod";

        // Nuestro diccionario "traductor". Mapea texto técnico del Journal a acciones legibles.
        private Dictionary<string, string> actionDictionary;

        private bool testUpdate = false;

        // --- Cliente HTTP Estático ---
        private static readonly HttpClient client = new HttpClient();

        #endregion

        #region Rutas y Configuración del Add-In

        private string baseApplicationDataFolder;
        private string LogFilePath;
        private string ConfigFilePath;
        private string tempFolder;
        private string historyActionLogPath;
        private string summaryActionLogPath;
        private long lastJournalFileSize = 0;

        #endregion

        #region Objetos y Estado de SolidWorks

        private ISldWorks solidWorksApp;
        private ICommandManager commandManager;
        private int addInCookie;
        private bool backgroundTasksStarted = false;

        #endregion

        #region Variables para Seguimiento de Actividad

        private bool isUserActive;
        private System.Timers.Timer inactivityTimer;
        private System.Timers.Timer heartbeatTimer;
        private FileSystemWatcher journalFileWatcher;
        private static readonly object _sessionFileLock = new object();
        private string lastLoggedAction = "";
        private DateTime lastLoggedTime = DateTime.MinValue;

        #endregion

        #region Variables para Funcionalidades Adicionales

        private string VERSION, versionActual, cambios, link;
        private string accessKeyId, secretAccessKey, bucketName, region, encryptionKey;
        private bool isConfigured;

        #endregion
        public Class1()
        {
            // El constructor ahora solo se encarga de llamar a los métodos de inicialización en orden.
            try
            {
                // Primero las rutas, para que la función de log esté disponible inmediatamente.
                InitializePaths();
                // Luego, preparamos el sistema de seguimiento de actividad.
                InitializeActivityTracking();
                // Inicializamos nuestro diccionario de acciones para que esté listo.
                InitializeActionDictionary();
                // Finalmente, cargamos otras configuraciones si es necesario.
                LoadConfiguration();
            }
            catch (Exception ex)
            {
                // Si algo falla aquí, es un error crítico. Lo registramos en la ventana de depuración.
                Debug.Print($"ERROR CRÍTICO EN EL CONSTRUCTOR DE Class1: {ex.ToString()}");
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
                solidWorksApp = (ISldWorks)solidWorksInstance;
                if (solidWorksApp == null) { return false; }

                addInCookie = addInId;
                commandManager = solidWorksApp.GetCommandManager(addInCookie);
                solidWorksApp.SetAddinCallbackInfo(0, this, addInCookie);

                ((SldWorks)solidWorksApp).OnIdleNotify += new DSldWorksEvents_OnIdleNotifyEventHandler(OnIdleNotifyHandler);

                LogToFile("Suscripción al evento OnIdleNotify completada.", "INFO");

                LogToFile("Conexión con SOLIDWORKS completada.", "INFO");
                return true;
            }
            catch (Exception ex)
            {
                LogToFile($"Error fatal en ConnectToSW: {ex.ToString()}", "ERROR");
                return false;
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                LogToFile("LoadConfiguration cargado correctamente", "INFO");

                // Limpiar configuración anterior


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
        /// ////////////////////////////////////////////////////////////////////////// Monitoreo de Archivo Journal /////////////////////////////////////////////////////////////////////////
        /// 
        /// 
        #region Monitoreo de Archivo Journal

        /// <summary>
        /// Configura y activa el FileSystemWatcher para monitorear el archivo swxJRNL.swj.
        /// </summary>
        private void InitializeJournalMonitoring()
        {
            try
            {
                // 1. Obtenemos la información de la versión de SolidWorks para construir la ruta.
                string baseVersion, currentVersion, hotfixes;
                solidWorksApp.GetBuildNumbers2(out baseVersion, out currentVersion, out hotfixes);

                // 2. Usamos tu método existente 'ExtractSWVersion' para obtener solo el año (ej. "2024").
                string versionSW = ExtractSWVersion(baseVersion);
                if (versionSW == "UnknownVersion")
                {
                    LogToFile("No se pudo determinar la versión de SOLIDWORKS para monitorear el Journal.", "ERROR");
                    return;
                }

                // 3. Construimos la ruta al directorio del Journal.
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string swVersionFolderName = $"SOLIDWORKS {versionSW}"; // ej: "SOLIDWORKS 2024"
                string journalDirectory = Path.Combine(appDataPath, "SOLIDWORKS", swVersionFolderName);

                if (!Directory.Exists(journalDirectory))
                {
                    LogToFile($"Error: No se encontró el directorio del Journal: {journalDirectory}", "ERROR");
                    return;
                }

                // 4. Creamos y configuramos nuestro vigilante de archivos.
                journalFileWatcher = new FileSystemWatcher
                {
                    Path = journalDirectory,
                    Filter = "swxJRNL.swj", // Solo nos interesa este archivo.
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size, // Notificar si cambia tamaño o fecha.
                    EnableRaisingEvents = true // ¡Activamos el monitoreo!
                };

                // 5. Suscribimos nuestros métodos a los eventos del vigilante.
                journalFileWatcher.Changed += OnJournalFileChanged;
                journalFileWatcher.Renamed += OnJournalFileRenamed;

                LogToFile($"Monitoreo iniciado para el archivo Journal en: {journalDirectory}", "INFO");
            }
            catch (Exception ex)
            {
                LogToFile($"Error fatal al inicializar el monitoreo del Journal: {ex.Message}", "ERROR");
            }
        }

        /// <summary>
        /// Se dispara CADA VEZ que el archivo swxJRNL.swj es modificado.
        /// Lee solo las nuevas líneas y las manda a procesar.
        /// </summary>
        private void OnJournalFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                RecordUserActivity();

                // Ahora, leemos solo las líneas nuevas que se añadieron al archivo Journal.
                List<string> newLines = ReadNewJournalLines(e.FullPath);

                if (newLines.Any())
                {
                    // Por ahora, solo mostraremos en el log que hemos capturado las nuevas líneas.
                    // En el siguiente paso, aquí llamaremos a la función de "parseo".
                    if (newLines.Any())
                    {
                        ProcessNewJournalLines(newLines);
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error en OnJournalFileChanged: {ex.ToString()}", "ERROR");
            }
        }

        /// <summary>
        /// Lee un archivo de texto desde la última posición conocida y actualiza la posición.
        /// </summary>
        /// <param name="filePath">Ruta al archivo a leer.</param>
        /// <returns>Una lista de las nuevas líneas de texto.</returns>
        private List<string> ReadNewJournalLines(string filePath)
        {
            var newLines = new List<string>();
            try
            {
                // Abrimos el archivo permitiendo que otros procesos (como SolidWorks) puedan escribir en él.
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long currentSize = fs.Length;

                    // Si el archivo es más pequeño que la última vez, significa que SolidWorks lo reseteó.
                    if (currentSize < lastJournalFileSize)
                    {
                        lastJournalFileSize = 0;
                    }

                    // Si hay contenido nuevo, lo leemos.
                    if (currentSize > lastJournalFileSize)
                    {
                        fs.Seek(lastJournalFileSize, SeekOrigin.Begin); // Movemos el cursor a donde nos quedamos

                        using (var sr = new StreamReader(fs))
                        {
                            string line;
                            while ((line = sr.ReadLine()) != null)
                            {
                                newLines.Add(line);
                            }
                        }
                    }

                    // Actualizamos la última posición leída para la próxima vez.
                    lastJournalFileSize = currentSize;
                }
            }
            catch (Exception ex)
            {
                LogToFile($"No se pudo leer el archivo Journal: {ex.Message}", "WARNING");
            }
            return newLines;
        }

        /// <summary>
        /// Se dispara cuando swxJRNL.swj es renombrado (usualmente a .bak en un cierre limpio).
        /// </summary>
        private void OnJournalFileRenamed(object sender, RenamedEventArgs e)
        {
            if (e.OldName == "swxJRNL.swj" && e.Name.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
            {
                LogToFile("Detectado renombrado de Journal a .bak (cierre limpio de SW).", "INFO");
                // Si el usuario estaba activo justo antes del cierre, nos aseguramos de registrarlo.
                if (isUserActive)
                {
                    isUserActive = false;
                    inactivityTimer.Stop();
                    heartbeatTimer.Stop();
                    RegisterSolidWorksSession("Close");
                }
            }
        }


        /// <summary>
        /// Revisa el archivo HistoryAction.log al inicio y se asegura de que cualquier bloque anterior esté cerrado.
        /// </summary>
        private void EnsurePreviousHistoryLogIsClosed()
        {
            try
            {
                // Si el archivo no existe o está vacío, no hay nada que hacer.
                if (!File.Exists(historyActionLogPath) || new FileInfo(historyActionLogPath).Length == 0)
                {
                    return;
                }

                var lastLine = File.ReadLines(historyActionLogPath).LastOrDefault(line => !string.IsNullOrWhiteSpace(line));

                // Si la última línea no es "EndFile", la añadimos para cerrar el bloque anterior.
                if (lastLine != null && !lastLine.Equals("EndFile", StringComparison.OrdinalIgnoreCase))
                {
                    LogToFile("Detectado HistoryAction.log sin cerrar. Añadiendo 'EndFile' para cerrar el bloque anterior.", "WARNING");
                    File.AppendAllText(historyActionLogPath, "EndFile\r\n");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error al asegurar que el log de historial estuviera cerrado: {ex.Message}", "ERROR");
            }
        }

        /// <summary>
        /// Inicializa y puebla nuestro diccionario de acciones.
        /// Aquí es donde "enseñamos" al Add-In qué buscar en el archivo Journal.
        /// </summary>
        private void InitializeActionDictionary()
        {
            actionDictionary = new Dictionary<string, string>
        {
            // --- Operaciones 3D Específicas (Más prioritarias) ---
            { "swFmSweepCut", "Corte de Barrido" },
            { "swFmSweep", "Saliente de Barrido" },
            { "swFmExtrude", "Extrusión" }, // Cubre tanto saliente como corte si no se especifica más
            { "swFmLoft", "Saliente de Recubrir" },
            { "swFmLoftCut", "Corte de Recubrir" },
            { "swFmRevolve", "Revolución" },
            { "swFmRevCut", "Corte de Revolución" },
            { "swFmFillet", "Redondeo de Operación" },
            { "swFmChamfer", "Chaflán de Operación" },
            { "swApp.ActivateDoc2", "Documento activo"  },

            // --- Acciones de Croquis ---
            { ".SketchManager.CreateCircle", "Crear Círculo" },
            { ".SketchManager.CreateLine", "Crear Línea" },
            { ".SketchManager.CreateFillet", "Crear Redondeo de Croquis" },
            { ".SketchManager.CreatePoint", "Crear Punto" },
            { ".SketchManager.InsertSketch", "Insertar/Salir de Croquis" },
            { ".Insert3DSketch2", "Iniciar Croquis 3D" },
            { ".SketchAddConstraints", "Añadir Restricción de Croquis" },
            { ".AddDimension2", "Añadir Cota" },

            // --- Acciones de Vista ---
            { ".ViewZoomtofit2", "Zoom para Ajustar" },
            { ".RotateAboutCenter", "Rotar Vista" },
            { ".RollBy", "Rodar Vista" },
            { ".GraphicsRedraw2", "Redibujar Gráficos" },

            // --- Acciones de Documento ---
            { ".NewDocument", "Nuevo Documento" },
            { ".CloseDoc", "Cerrar Documento" },
            { ".Save3", "Guardar Documento" },
            
            // --- Acciones Generales (Menos prioritarias) ---
            { ".FeatureManager.CreateFeature", "Crear Operación Genérica" },
            { ".Extension.SelectByID2", "Seleccionar Entidad" },
            { ".ClearSelection2", "Limpiar Selección" }
        };
            LogToFile("Diccionario de acciones expandido e inicializado.", "INFO");
        }

        /// <summary>
        /// Se llama cada vez que el FileSystemWatcher detecta nuevas líneas en el archivo Journal.
        /// </summary>
        private void ProcessNewJournalLines(List<string> newLines)
        {
            try
            {
                var actionsToLog = new List<string>();

                foreach (var line in newLines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("'") || line.Trim().StartsWith("Dim ")) continue;

                    // Gracias al orden del diccionario, la primera coincidencia que encontremos será la más específica.
                    foreach (var entry in actionDictionary)
                    {
                        if (line.Contains(entry.Key))
                        {
                            string action = entry.Value;

                            // Lógica anti-spam solo para los eventos de vista que se repiten mucho
                            if ((action == "Rotar Vista" || action == "Rodar Vista") &&
                                action == lastLoggedAction && (DateTime.Now - lastLoggedTime).TotalSeconds < 2)
                            {
                                continue; // Ignora este evento repetido, pero sigue buscando otras acciones en la misma línea
                            }

                            string value = ExtractValueFromLine(line, action);

                            if (value.Contains("C:\\ProgramData\\SolidWorks\\SOLIDWORKS 2025"))
                            {
                                value = value.Split('\\')[4];
                            }
                            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            actionsToLog.Add($"{action}, {value}, {timestamp}");

                            lastLoggedAction = action;
                            lastLoggedTime = DateTime.Now;

                            break; // Encontramos la acción más específica para esta línea, pasamos a la siguiente.
                        }
                    }
                }

                if (actionsToLog.Any())
                {
                    lock (_sessionFileLock)
                    {
                        File.AppendAllLines(historyActionLogPath, actionsToLog);
                    }
                    LogToFile($"Registradas {actionsToLog.Count} nuevas acciones en HistoryAction.log.", "DEBUG");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error procesando líneas del Journal: {ex.ToString()}", "ERROR");
            }
        }

        /// <summary>
        /// Función simple para extraer el "valor" principal de una línea de comando del Journal.
        /// Por ejemplo, el nombre de un archivo o las coordenadas de un círculo.
        /// </summary>
        private string ExtractValueFromLine(string line, string action)
        {
            try
            {
                // Para acciones que involucran nombres (archivos, entidades), buscamos texto entre comillas.
                if (action.Contains("Documento") || action.Contains("Seleccionar") || action.Contains("Restricción"))
                {
                    int firstQuote = line.IndexOf('"');
                    if (firstQuote != -1)
                    {
                        int secondQuote = line.IndexOf('"', firstQuote + 1);
                        if (secondQuote != -1) return line.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                    }
                }

                // Para acciones que involucran parámetros (geometría, vistas), extraemos lo que está entre paréntesis.
                int firstParen = line.IndexOf('(');
                if (firstParen != -1)
                {
                    int secondParen = line.LastIndexOf(')');
                    if (secondParen > firstParen) return line.Substring(firstParen + 1, secondParen - firstParen - 1).Trim();
                }
            }
            catch { /* Ignorar errores de parseo simples */ }

            return "N/A";
        }

        #endregion

        /// 
        /// ////////////////////////////////////////////////////////////////////////// Lógica de Seguimiento y Sesiones /////////////////////////////////////////////////////////////////////////
        /// 
        /// 
        #region Lógica de Seguimiento y Sesiones

        /// <summary>
        /// Prepara los temporizadores y variables para el seguimiento de actividad del usuario.
        /// </summary>
        private void InitializeActivityTracking()
        {
            isUserActive = false;

            inactivityTimer = new System.Timers.Timer(300000); // 5 minutos
            inactivityTimer.Elapsed += InactivityTimer_Elapsed;
            inactivityTimer.AutoReset = false;

            heartbeatTimer = new System.Timers.Timer(240000); // 4 minutos
            heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
            heartbeatTimer.AutoReset = true;

            LogToFile("Sistema de seguimiento de actividad (timers) inicializado.", "INFO");
        }

        /// <summary>
        /// Método central que se llama cada vez que detectamos una interacción del usuario.
        /// </summary>
        private void RecordUserActivity()
        {
            // Si el usuario estaba INACTIVO, esta acción marca el inicio de una nueva sesión de trabajo.
            if (!isUserActive)
            {
                isUserActive = true; // Lo marcamos como activo.
                LogToFile("Usuario activo detectado. Registrando 'Open' e iniciando temporizadores.", "INFO");

                // Registramos el inicio de la sesión de actividad.
                RegisterSolidWorksSession("Open");

                EnsurePreviousHistoryLogIsClosed();
                // Al iniciar una nueva sesión de actividad, escribimos "StartFile" en el log de historial.
                File.AppendAllText(historyActionLogPath, "StartFile\r\n");

                // Iniciamos el temporizador de pulsos.
                heartbeatTimer.Start();
            }

            // Ya sea que estuviera activo o no, cada nueva actividad reinicia el temporizador de INACTIVIDAD.
            // Esto le da al usuario otros 5 minutos antes de ser considerado inactivo.
            inactivityTimer.Stop();
            inactivityTimer.Start();
        }

        /// <summary>
        /// Se ejecuta cuando el temporizador de pulso (4 min) se dispara.
        /// </summary>
        private void HeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (isUserActive)
            {
                LogToFile("Pulso de actividad (Heartbeat) registrado.", "DEBUG");
                RegisterSolidWorksSession("ActivePulse");
            }
        }

        /// <summary>
        /// Se ejecuta cuando el temporizador de inactividad (5 min) se dispara.
        /// </summary>
        private void InactivityTimer_Tick(object sender, EventArgs e)
        {
            // Solo actuamos si el usuario estaba previamente activo.
            if (isUserActive)
            {
                isUserActive = false;       // Marcar al usuario como inactivo.
                inactivityTimer.Stop();     // Detener ambos temporizadores.
                heartbeatTimer.Stop();

                LogToFile("Inactividad detectada. Registrando sesión 'Close'.", "INFO");
                RegisterSolidWorksSession("Close");
            }
        }/// <summary>
         /// Se ejecuta cuando el temporizador de inactividad (5 min) se dispara.
         /// </summary>
        private void InactivityTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (isUserActive)
            {
                isUserActive = false;
                inactivityTimer.Stop();
                heartbeatTimer.Stop();
                LogToFile("Inactividad detectada. Registrando sesión 'Close'.", "INFO");
                RegisterSolidWorksSession("Close");
            }
        }

        /// <summary>
        /// Revisa si la sesión anterior terminó inesperadamente basándose en la existencia del archivo Journal.
        /// </summary>
        private void HandlePreviousSessionCrash()
        {
            try
            {
                if (!File.Exists(ConfigFilePath)) return;

                // Leemos la última línea no vacía del archivo de forma eficiente.
                var lastLine = File.ReadLines(ConfigFilePath).LastOrDefault(line => !string.IsNullOrWhiteSpace(line));
                if (lastLine == null) return;

                string[] parts = lastLine.TrimEnd(';').Split(',');
                if (parts.Length > 1)
                {
                    string lastAction = parts[1].Trim();

                    // Si la última acción fue 'Open' O 'ActivePulse', significa que el programa se cerró
                    // de forma inesperada mientras el usuario estaba trabajando.
                    if (lastAction.Equals("Open", StringComparison.OrdinalIgnoreCase) ||
                        lastAction.Equals("ActivePulse", StringComparison.OrdinalIgnoreCase))
                    {
                        LogToFile($"Detectada sesión previa no cerrada (última acción: {lastAction}). Registrando 'Crash'.", "WARNING");
                        RegisterSolidWorksSession("Crash");
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error al verificar si hubo un crash en la sesión anterior: {ex.Message}", "ERROR");
            }
        }

        /// <summary>
        /// Lee el archivo de historial de acciones, cuenta las ocurrencias de cada acción y
        /// escribe el resultado en un archivo de resumen.
        /// </summary>
        private void GenerateActionSummary()
        {
            try
            {
                LogToFile("Generando resumen de acciones...", "INFO");
                if (!File.Exists(historyActionLogPath))
                {
                    LogToFile("No se encontró HistoryAction.log para generar el resumen.", "WARNING");
                    return;
                }

                // Usamos un diccionario para contar cada acción.
                var actionCounts = new Dictionary<string, int>();

                // Leemos todas las líneas del log de historial.
                var historyLines = File.ReadLines(historyActionLogPath);

                foreach (var line in historyLines)
                {
                    // Ignoramos los marcadores de inicio y fin de sesión.
                    if (string.IsNullOrWhiteSpace(line) ||
                        line.Equals("StartFile", StringComparison.OrdinalIgnoreCase) ||
                        line.Equals("EndFile", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Extraemos el nombre de la acción (es la primera parte antes de la coma).
                    string action = line.Split(',')[0].Trim();

                    // Contamos la acción.
                    if (actionCounts.ContainsKey(action))
                    {
                        actionCounts[action]++;
                    }
                    else
                    {
                        actionCounts[action] = 1;
                    }
                }

                // Si no hay acciones que contar, no creamos el archivo.
                if (!actionCounts.Any())
                {
                    LogToFile("No se encontraron acciones para resumir.", "INFO");
                    return;
                }

                // Preparamos las líneas para el archivo de resumen.
                var summaryLines = new List<string>();
                foreach (var entry in actionCounts)
                {
                    summaryLines.Add($"{entry.Key}, {entry.Value}"); // Formato: Acción, Conteo
                }

                // Escribimos el archivo de resumen, sobrescribiendo el anterior.
                MessageBox.Show(summaryActionLogPath);
                File.WriteAllLines(summaryActionLogPath, summaryLines);
                LogToFile($"Resumen de acciones guardado exitosamente en: {summaryActionLogPath}", "INFO");
            }
            catch (Exception ex)
            {
                LogToFile($"Error al generar el resumen de acciones: {ex.ToString()}", "ERROR");
            }
        }
        #endregion
        /// 
        /// ////////////////////////////////////////////////////////////////////////// Trabajar con el archivo txt de sesiones /////////////////////////////////////////////////////////////////////////
        /// 
        /// 
        #region
        /// 
        /// Registra las sesiones en el archivo.
        /// 
        private void RegisterSolidWorksSession(string action)
        {
            try
            {
                string email = GetEmail();
                string userID = GetUserID();
                string sessionEntry = $"{DateTime.Now:yyyy/MM/dd HH:mm:ss},{action},{userID ?? "unknown"},{email ?? "unknown@email.com"};\r\n";

                // --- AÑADIR LOCK AQUÍ ---
                // Solo un hilo a la vez puede ejecutar el código dentro de este bloque.
                lock (_sessionFileLock)
                {
                    File.AppendAllText(ConfigFilePath, sessionEntry);
                }

                LogToFile($"Sesión '{action}' registrada en el archivo.", "INFO");
            }
            catch (Exception ex)
            {
                LogToFile($"Error Crítico al registrar la sesión '{action}': {ex.Message}", "ERROR");
            }
        }

        /// 
        /// Enviar las sesiones a la API.
        ///
        public async Task SendSesionSW()
        {
            string processedContent = "";

            try
            {
                // --- AÑADIR LOCK AQUÍ ---
                // Ponemos el candado ANTES de tocar el archivo.
                lock (_sessionFileLock)
                {
                    // Movemos la lógica de procesamiento y borrado DENTRO del candado.
                    processedContent = ProcessSessionFile();
                    if (!string.IsNullOrEmpty(processedContent))
                    {
                        // Si encontramos algo que enviar, borramos el archivo inmediatamente
                        // para no enviarlo dos veces si la llamada a la API falla.
                        ClearAlltext();
                    }
                } // El candado se libera aquí.

                if (string.IsNullOrEmpty(processedContent))
                {
                    LogToFile("No hay sesiones completas para enviar.", "INFO");
                    return;
                }

                // El resto de la lógica (llamada a la API) puede ir fuera del lock.
                LogToFile("Iniciando envío de sesiones procesadas...", "INFO");
                string token = await GetToken(TOKEN_Analitics_URL);
                if (string.IsNullOrEmpty(token)) { /* ... */ return; }

                var jsonPayload = new
                {
                    code = "r5ncccmGhzLG",
                    DeviceID = GetDeviceID(),
                    License = GetSerialnumber(),
                    SWVersion = GetSwVersion(),
                    file = processedContent
                };

                string requestBody = JsonConvert.SerializeObject(jsonPayload);
                var request = new HttpRequestMessage(HttpMethod.Post, API_Analitics_URL);
                request.Headers.Add("authorization", token);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                // ... (resto de tu lógica de manejo de respuesta) ...
            }
            catch (Exception ex)
            {
                LogToFile($"Error en SendSesionSW: {ex.ToString()}", "ERROR");
                // NOTA: Si la API falla, los datos ya fueron borrados del archivo principal.
                // Se podría añadir lógica para reintentar o guardar los datos fallidos en otro archivo.
                // Por ahora, lo mantenemos simple.
            }
        }


        /// <summary>
        /// Lee el archivo de sesiones crudo y lo procesa para crear pares limpios de Open/Close.
        /// Esta es la versión final, ajustada para el formato que espera la API.
        /// </summary>
        /// <returns>Una cadena de texto con las sesiones consolidadas, separadas por ';'.</returns>
        private string ProcessSessionFile()
        {
            try
            {
                if (!File.Exists(ConfigFilePath)) return "";
                var allLines = File.ReadAllLines(ConfigFilePath)
                                     .Where(l => !string.IsNullOrWhiteSpace(l))
                                     .ToList();

                if (!allLines.Any()) return "";

                var processedSessions = new List<string>();
                string currentOpenLine = null;
                string lastPulseLine = null;

                foreach (var line in allLines)
                {
                    var parts = line.Trim().TrimEnd(';').Split(',');
                    if (parts.Length < 2) continue; // Ignorar líneas malformadas
                    string action = parts[1].Trim();

                    if (action.Equals("Open", StringComparison.OrdinalIgnoreCase))
                    {
                        // Si encontramos un 'Open' y ya teníamos uno pendiente, cerramos el anterior como 'Crash'.
                        if (currentOpenLine != null)
                        {
                            string lineToClose = lastPulseLine ?? currentOpenLine;
                            string[] crashParts = lineToClose.Split(',');
                            crashParts[1] = "Crash";
                            processedSessions.Add(currentOpenLine.Trim().TrimEnd(';'));
                            processedSessions.Add(string.Join(",", crashParts));
                        }
                        currentOpenLine = line.Trim().TrimEnd(';');
                        lastPulseLine = null;
                    }
                    else if (action.Equals("ActivePulse", StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentOpenLine != null) lastPulseLine = line.Trim().TrimEnd(';');
                    }
                    else if (action.Equals("Close", StringComparison.OrdinalIgnoreCase) || action.Equals("Crash", StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentOpenLine != null)
                        {
                            processedSessions.Add(currentOpenLine);
                            // Si es un crash, usamos la fecha del último pulso para mayor precisión.
                            string lineToClose = (action.Equals("Crash") && lastPulseLine != null) ? lastPulseLine : line.Trim().TrimEnd(';');
                            string[] finalParts = lineToClose.Split(',');
                            finalParts[1] = action; // Asegura que la acción sea 'Close' o 'Crash'
                            processedSessions.Add(string.Join(",", finalParts));

                            currentOpenLine = null;
                            lastPulseLine = null;
                        }
                    }
                }

                // Si el bucle termina y queda una sesión abierta, la cerramos.
                if (currentOpenLine != null)
                {
                    string finalLineToClose = lastPulseLine ?? currentOpenLine;
                    string[] finalParts = finalLineToClose.Split(',');
                    finalParts[1] = "Close"; // Asumimos un cierre normal en la desconexión.
                    processedSessions.Add(currentOpenLine);
                    processedSessions.Add(string.Join(",", finalParts));
                }

                if (!processedSessions.Any()) return "";

                // Unimos todas las entradas procesadas en una sola cadena, usando ';' como separador.
                string finalPayload = string.Join(";", processedSessions) + ";";
                LogToFile($"ProcessSessionFile: Contenido procesado listo para enviar.", "DEBUG");
                return finalPayload;
            }
            catch (Exception ex)
            {
                LogToFile($"Error en ProcessSessionFile: {ex.ToString()}", "ERROR");
                return "";
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
            LogToFile("DisconnectFromSW: Iniciando desconexión...", "INFO");
            try
            {
                inactivityTimer?.Stop();
                heartbeatTimer?.Stop();
                if (journalFileWatcher != null)
                {
                    journalFileWatcher.EnableRaisingEvents = false; // Es bueno desactivarlo antes de liberarlo.
                }

                if (isUserActive)
                {
                    RegisterSolidWorksSession("Close");
                }
                File.AppendAllText(historyActionLogPath, "EndFile\r\n");
                GenerateActionSummary();

                // Desuscribirse de los eventos de SolidWorks.
                ((SldWorks)solidWorksApp).OnIdleNotify -= new DSldWorksEvents_OnIdleNotifyEventHandler(OnIdleNotifyHandler);

                // Liberar recursos desechables.
                journalFileWatcher?.Dispose();
                inactivityTimer?.Dispose();
                heartbeatTimer?.Dispose();

                // Liberar objetos COM y ponerlos en null para evitar su uso accidental.
                if (commandManager != null) { Marshal.ReleaseComObject(commandManager); commandManager = null; }
                if (solidWorksApp != null) { Marshal.ReleaseComObject(solidWorksApp); solidWorksApp = null; }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                LogToFile("Desconexión de SOLIDWORKS completada.", "INFO");
                return true;
            }
            catch (Exception ex)
            {
                LogToFile($"Error en DisconnectFromSW: {ex.ToString()}", "ERROR");
                return false;
            }
        }

        /// <summary>
        /// Define las rutas de trabajo (logs, config) en una ubicación segura y escribible.
        /// </summary>
        private void InitializePaths()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            baseApplicationDataFolder = Path.Combine(localAppData, AppName);
            Directory.CreateDirectory(baseApplicationDataFolder);

            LogFilePath = Path.Combine(baseApplicationDataFolder, AppName + "_log.txt");
            ConfigFilePath = Path.Combine(baseApplicationDataFolder, AppName + "_sessions.txt");
            historyActionLogPath = Path.Combine(baseApplicationDataFolder, "HistoryAction.log");
            summaryActionLogPath = Path.Combine(baseApplicationDataFolder, "LogAction.log");
            tempFolder = Path.Combine(baseApplicationDataFolder, "temp");
            Directory.CreateDirectory(tempFolder);
            LogToFile("Constructor: Rutas inicializadas.", "INFO");
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

                // ---------------------------  EXECUTION MODE -----------------------------------------
                // Creamos un objeto anónimo con nuestro modo dinámico.
                var payload = new { mode = this.executionMode };

                // Convertimos ese objeto a una cadena JSON de forma segura.
                string jsonPayload = JsonConvert.SerializeObject(payload);

                // Usamos la cadena JSON en el contenido de la petición.
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                // -------------------------------------------------------------------------------------

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

                if (testUpdate)
                {
                    VERSION = "1.0.4";
                    cambios = "Test 1";
                    link = "http://localhost/analytics/analytics-v1.0.4.zip";
                }
                else
                {
                    VERSION = lastInstaller["Version"]?.ToString();
                    cambios = lastInstaller["ReleaseNotes"]?.ToString();
                    link = lastInstaller["publicDllLink"]?.ToString();
                }




                if (string.IsNullOrEmpty(VERSION))
                {
                    LogToFile("No se pudo obtener el número de versión", "WARNING");
                    return;
                }

                // =================================================================================
                //  La lógica para lanzar el actualizador
                // =================================================================================
                if (CompareVersions()) // Si hay una nueva versión...
                {
                    LogToFile($"Nueva versión {this.VERSION} detectada. Creando hoja de trabajo para el actualizador.", "INFO");

                    // 1. Preparar la información en un objeto organizado.
                    string rutaInstalacion = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    rutaInstalacion = Path.Combine(GetSource(), AppName);

                    var jobInfo = new UpdateJobInfo
                    {
                        TargetDirectory = rutaInstalacion,
                        SourceUrl = this.link,
                        NewVersion = this.VERSION,
                        ReleaseNotes = this.cambios,
                        ProcessToClose = "SLDWORKS"
                    };

                    // 2. Preparar el actualizador en la carpeta temporal (esto no cambia).
                    string updaterOriginalPath = Path.Combine(rutaInstalacion, "updates.exe");
                    LogToFile($"La ruta updaterOriginalPath: {updaterOriginalPath}", "INFO");

                    if (!File.Exists(updaterOriginalPath)) { /*...manejar error...*/ return; }

                    string tempUpdaterDir = Path.Combine(Path.GetTempPath(), "analytics_update");
                    Directory.CreateDirectory(tempUpdaterDir);

                    string[] requiredFiles = { "updates.exe", "Newtonsoft.Json.dll" };

                    foreach (string fileName in requiredFiles)
                    {
                        string sourceFile = Path.Combine(rutaInstalacion, fileName);
                        string destFile = Path.Combine(tempUpdaterDir, fileName);

                        if (File.Exists(sourceFile))
                        {
                            File.Copy(sourceFile, destFile);
                            LogToFile($"Copiado archivo de dependencia: {fileName}", "DEBUG");
                        }
                        else
                        {
                            // Este log es importante. Si falta un archivo, lo sabrás.
                            LogToFile($"ADVERTENCIA: No se encontró el archivo requerido '{fileName}' en la carpeta de instalación para copiar.", "WARNING");
                        }
                    }

                    // Esta línea ya no copia, solo la usamos para tener la ruta al ejecutable para lanzarlo.
                    string updaterTempPath = Path.Combine(tempUpdaterDir, "updates.exe");

                    // 3. Crear la "Hoja de Trabajo" (el archivo JSON).
                    string jobFilePath = Path.Combine(tempUpdaterDir, "update_job.json");
                    string jsonContent = JsonConvert.SerializeObject(jobInfo, Formatting.Indented); // Usas Newtonsoft.Json que ya tienes.
                    File.WriteAllText(jobFilePath, jsonContent);
                    LogToFile($"Hoja de trabajo creada en: {jobFilePath}", "INFO");

                    // 4. Ejecutar el actualizador temporal, pasándole ÚNICAMENTE la ruta a su hoja de trabajo.
                    try
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = updaterTempPath,
                            Arguments = $"\"{jobFilePath}\"", // ¡El único argumento es la ruta al archivo JSON!
                            UseShellExecute = true
                        };

                        Process.Start(startInfo);
                        LogToFile($"Actualizador ejecutado con la hoja de trabajo.", "INFO");
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"Error al ejecutar el actualizador: {ex.Message}", "ERROR");
                    }
                }
                else
                {
                    LogToFile("El software ya está en su última versión.", "INFO");
                }
            }
            catch (Exception ex)
            {
                LogToFile($"Error fatal en CheckForUpdates: {ex.Message}", "ERROR");
            }
        }

        public class UpdateJobInfo
        {
            public string TargetDirectory { get; set; }
            public string SourceUrl { get; set; }
            public string NewVersion { get; set; }
            public string ReleaseNotes { get; set; }
            public string ProcessToClose { get; set; }
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
                LogToFile("--------------------------------------------------------------------------solidworksPath:", solidworksPath);

                await UploadIfExists(solidworksPath, "swxJRNL.swj");
                await UploadIfExists(solidworksPath, "swxJRNL.bak");
                await UploadIfExists(baseApplicationDataFolder, AppName + "_log.txt");
                await UploadIfExists(baseApplicationDataFolder, AppName + "_sessions.txt");
                await UploadIfExists(baseApplicationDataFolder, AppName + "_update_log.txt");

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
                string extention = fileKeyName.Split('.')[1];
                string keyName = $"{customerId}/{userId}/{extention}/{fecha}-{fileKeyName}";
                string encryptedKeyName = $"{customerIdEncryption}/{userIdEncryption}/{extention}/{fecha}-{fileKeyName}";

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
        /// <summary>
        /// Este método es llamado por SolidWorks cuando la aplicación está inactiva.
        /// Lo usaremos para ejecutar nuestras tareas de fondo de forma segura y una única vez.
        /// </summary>
        private int OnIdleNotifyHandler()
        {
            // Si ya hemos ejecutado nuestras tareas, no hacemos nada más.
            if (backgroundTasksStarted)
            {
                return 0;
            }

            // Marcamos que ya hemos comenzado, para no volver a ejecutar esto.
            backgroundTasksStarted = true;
            LogToFile("SolidWorks está inactivo (Idle). Iniciando tareas de fondo (actualización, envío de logs, etc.).", "INFO");

            //Inicia el monitoreo del archivo Journal.
            InitializeJournalMonitoring();

            //Revisa si la sesión anterior tuvo un crash.
            HandlePreviousSessionCrash();

            // Ahora, aquí lanzamos el Task.Run que antes estaba en ConnectToSW.
            // Estando aquí, el hilo tiene un entorno mucho más estable para ejecutarse.
            Task.Run(async () => {
                if (IsInternetAvailable())
                {
                    await CheckForUpdates();
                    await SendSesionSW();
                    await SendJwl();
                }
            });

            // Nos damos de baja del evento para no volver a ser llamados innecesariamente.
            ((SldWorks)solidWorksApp).OnIdleNotify -= new DSldWorksEvents_OnIdleNotifyEventHandler(OnIdleNotifyHandler);
            LogToFile("Tareas de fondo iniciadas. Dando de baja la suscripción al evento OnIdleNotify.", "INFO");

            return 0; // Se requiere devolver un entero.
        }
    }

}