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

namespace analytics_AddIn
{
    [ComVisible(true)]
    [Guid("31b803e0-7a01-4841-a0de-895b726625c9")]
    [DisplayName("Analytics")]
    [Description("Analytics SOLIDWORKS Add-In Description")]
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
        private string ConfigFilePath = GetSource() + "\\" + AppName + "\\" + AppName + "_sessions.txt";
        private string LogFilePath = GetSource() + "\\" + AppName + "\\" + AppName + "_log.txt";
        private string tempFolder = GetSource() + "\\" + AppName + "\\temp\\";
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
        
        [ComRegisterFunction]
        public static void RegisterFunction(Type t)
        {
            try
            {
                var logger = new Class1();
                var addInTitle = t.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? t.ToString();
                var addInDesc = t.GetCustomAttribute<DescriptionAttribute>()?.Description ?? t.ToString();
                var addInKeyPath = string.Format(ADDIN_KEY_TEMPLATE, t.GUID);

                using (var addInKey = Registry.LocalMachine.CreateSubKey(addInKeyPath))
                {
                    addInKey.SetValue(ADD_IN_TITLE_REG_KEY_NAME, addInTitle);
                    addInKey.SetValue(ADD_IN_DESCRIPTION_REG_KEY_NAME, addInDesc);

                    string pathInstall = GetSource();
                    string iconPath = Path.Combine(pathInstall, AppName, AppName + ".bmp");
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
                var logger = new Class1();
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
                solidWorksApp = solidWorksInstance as ISldWorks;
                addInCookie = addInId;
                solidWorksApp.SetAddinCallbackInfo(0, this, addInCookie);
                commandManager = solidWorksApp.GetCommandManager(addInCookie);
                InitializeEventHandlers();

                if (IsInternetAvailable())
                {
                    LogToFile("Conexión a internet detectada. Verificando actualizaciones...");
                    Task.Run(async () =>
                    {
                        try
                        {
                            await CheckForUpdates();
                            await SendSesionSW();
                            await SendJwl();
                        }
                        catch (Exception asyncEx)
                        {
                            LogToFile("Error en tareas asincrónicas de conexión: " + asyncEx.Message);
                        }
                    });
                }
                else
                {
                    LogToFile("No hay conexión a internet. No se pudo verificar la versión.");
                }

                if (DoesConfigFileExist())
                {
                    ReadConfigFile();
                }
                else
                {
                    CreateConfigFile();
                }

                return true;
            }
            catch (Exception ex)
            {
                LogToFile("Error en ConnectToSW: " + ex.Message);
                return false;
            }
        }


        /// 
        /// Configura los manejadores de eventos para capturar eventos de SOLIDWORKS.
        /// 
        private void InitializeEventHandlers()
        {
            try
            {
                solidWorksEventHandler = (SldWorks)solidWorksApp;
                openDocuments = new Hashtable();
                //ValidateLicense();
            }
            catch (Exception ex)
            {
                LogToFile("Error al inicializar manejadores de eventos: " + ex.Message);
            }
        }

        private void LogToFile(string message, string level = "INFO")
        {
            try
            {
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
                    logEntry = $"{timeStamp} [INFO] - {message}";
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
            return File.Exists(ConfigFilePath);
        }

        /// 
        /// Lee el contenido del archivo de configuración.
        /// 
        private void ReadConfigFile()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    throw new FileNotFoundException("El archivo de configuración no existe.");
                }
                string fileContent = File.ReadAllText(ConfigFilePath);
                if (string.IsNullOrWhiteSpace(fileContent))
                {
                    RegisterSolidWorksSession("Open");
                }
                else
                {
                    string lastLine = ReadLastLine(ConfigFilePath);
                    string[] parts = lastLine.TrimEnd(';', ',').Split(',');
                    string sessionStatus = parts[1].Trim();
                    if (sessionStatus == "Open")
                    {
                        RegisterSolidWorksSession("Crash");
                    }
                    RegisterSolidWorksSession("Open");
                }
            }
            catch (Exception ex)
            {
                Debug.Print("Error al leer el archivo de configuración: " + ex.Message);
            }
        }

        /// 
        /// Crea un nuevo archivo de configuración si no existe.
        /// 
        private void CreateConfigFile()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
                using (FileStream fs = File.Create(ConfigFilePath))
                {
                    Console.WriteLine("Archivo de configuración creado exitosamente.");
                }
                SetFilePermissions(ConfigFilePath);
                RegisterSolidWorksSession("Open");
            }
            catch (Exception ex)
            {
                Debug.Print("Error al crear el archivo de configuración: " + ex.Message);
            }
        }

        /// 
        /// Crea los permisos para leer el archivo.
        /// 
        private void SetFilePermissions(string filePath)
        {
            try
            {
                FileSecurity fileSecurity = File.GetAccessControl(filePath);
                string currentUser = WindowsIdentity.GetCurrent().Name;
                fileSecurity.AddAccessRule(new FileSystemAccessRule(
                    currentUser,
                    FileSystemRights.Read | FileSystemRights.Write,
                    InheritanceFlags.None,
                    PropagationFlags.None,
                    AccessControlType.Allow
                ));
                File.SetAccessControl(filePath, fileSecurity);
            }
            catch (Exception ex)
            {
                Debug.Print("Error al configurar permisos del archivo: " + ex.ToString());
            }
        }

        /// 
        /// Registra las sesiones en el archivo.
        /// 
        private void RegisterSolidWorksSession(string action)
        {
            string email = GetEmail();
            string userID = GetUserID();
            try
            {
                string sessionEntry = $@"{DateTime.Now:yyyy/MM/dd HH:mm:ss},{action},{userID},{email};";
                File.AppendAllText(ConfigFilePath, sessionEntry);
            }
            catch (Exception ex)
            {
                Debug.Print("Error al registrar la sesión en SOLIDWORKS: " + ex.Message);
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
            }
            return lastLine;
        }

        /// 
        /// Enviar las sesiones a la API.
        ///
        public async Task SendSesionSW()
        {
            try
            {
                string token = await GetToken(TOKEN_Analitics_URL);
                if (string.IsNullOrEmpty(token))
                {
                    string msg = "Error: No se pudo obtener el token.";
                    Debug.Print(msg);
                    LogToFile(msg);
                    return;
                }

                string fileContent = getTextFile();
                string serialNumber = GetSerialnumber();
                string swVersion = GetSwVersion();
                string deviceId = GetDeviceID();

                var jsonPayload = new
                {
                    code = "r5ncccmGhzLG",
                    DeviceID = deviceId,
                    License = serialNumber,
                    SWVersion = swVersion,
                    file = fileContent
                };

                string requestBody = JsonConvert.SerializeObject(jsonPayload, Formatting.Indented);

                var request = new HttpRequestMessage(HttpMethod.Post, API_Analitics_URL);
                request.Headers.Add("authorization", token);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                string responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string errorMsg = $"Error en la solicitud: {response.StatusCode} - {responseText}";
                    Debug.Print(errorMsg);
                    LogToFile(errorMsg);
                    return;
                }

                JObject jsonResponse = JObject.Parse(responseText);

                if (jsonResponse.ContainsKey("message"))
                {
                    JArray messageArray = (JArray)jsonResponse["message"];
                    JToken firstItem = messageArray?.FirstOrDefault();

                    if (firstItem != null && firstItem["statusInsert"]?.ToString() == "Insertado")
                    {
                        ClearAlltext();
                        LogToFile("Sesión enviada correctamente.");
                    }
                    else
                    {
                        LogToFile("Advertencia: La sesión no fue insertada correctamente.");
                    }
                }
                else
                {
                    string msg = "Advertencia: No se encontró 'message' en la respuesta.";
                    Debug.Print(msg);
                    LogToFile(msg);
                }
            }
            catch (HttpRequestException httpEx)
            {
                string msg = "Error de red en SendSesionSW: " + httpEx.Message;
                Debug.Print(msg);
                LogToFile(msg);
            }
            catch (Exception ex)
            {
                string msg = "Error en SendSesionSW: " + ex.Message;
                Debug.Print(msg);
                LogToFile(msg);
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
                return "";
            }
        }

        /// 
        /// Vacia el archivo.
        ///
        public void ClearAlltext()
        {
            File.WriteAllText(ConfigFilePath, string.Empty);
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
            RegisterSolidWorksSession("Close");
            System.Runtime.InteropServices.Marshal.ReleaseComObject(commandManager);
            commandManager = null;
            System.Runtime.InteropServices.Marshal.ReleaseComObject(solidWorksApp);
            solidWorksApp = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return true;
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
                        return key.GetValue("Source")?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener la ruta del icono del registro: " + ex.Message);
            }
            return string.Empty;
        }

        /// 
        /// Validar que el usuario tenga licencia de analitics.
        ///
        private bool ValidateLicense()
        {
            // Lógica pendiente de implementación
            return true;
        }

        /// 
        /// Verifica si hay conexión a internet.
        /// 
        private bool IsInternetAvailable()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = client.GetAsync("https://www.google.com").Result;
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
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
                RegistryKey key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ComputerName");
                if (key != null)
                {
                    object o = key.GetValue("Email");
                    email = o.ToString();
                }
            }
            catch (Exception e)
            {
                Debug.Print("Error de obtener el email!!! " + e.ToString());
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
                RegistryKey key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ComputerName");
                if (key != null)
                {
                    object o = key.GetValue("UserID");
                    userID = o.ToString();
                }
            }
            catch (Exception e)
            {
                Debug.Print("Error de obtener usserID!!! " + e.ToString());
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
                RegistryKey key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ComputerName");
                if (key != null)
                {
                    object o = key.GetValue("CustomerID");
                    customerID = o.ToString();
                }
            }
            catch (Exception e)
            {
                Debug.Print("Error de obtener customerID!!! " + e.ToString());
            }
            return customerID;
        }

        /// 
        /// Obtiene el numero serial.
        ///
        public string GetSerialnumber()
        {
            string keyPath = @"SOFTWARE\SolidWorks\Licenses\Serial Numbers";
            List<string> ValueList = new List<string>();
            // Open the HKEY_LOCAL_MACHINE\Software registry key
            using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(keyPath))
            {
                if (baseKey != null)
                {
                    // Get all the value names (including REG_SZ) under the specified key
                    string[] valueNames = baseKey.GetValueNames();

                    foreach (string valueName in valueNames)
                    {
                        // Check if the value is of type REG_SZ (string value)
                        if (baseKey.GetValueKind(valueName) == RegistryValueKind.String)
                        {
                            // Retrieve the value data as a string
                            string valueData = baseKey.GetValue(valueName).ToString();
                            ValueList.Add($"{valueName}: {valueData}");
                        }
                    }
                    string result = string.Join(System.Environment.NewLine, ValueList);
                    return result;
                }
                else
                {
                    Console.WriteLine("Registry key not found.");
                }
                return null;
            }
        }

        /// 
        /// Obtiene la version de SW.
        ///
        public string GetSwVersion()
        {
            string fullversin = "";
            string BaserVersion;
            string CurrentVersion;
            string hotfixed;
            // MessageBox.Show("Version Number SOLIDWROKS:" + iSwApp.RevisionNumber());
            solidWorksApp.GetBuildNumbers2(out BaserVersion, out CurrentVersion, out hotfixed);
            //MessageBox.Show($"SOLIDWORKS Mjor revision number:{BaserVersion} Current: {CurrentVersion}  Hotfix: {hotfixed}");
            fullversin = $"SOLIDWORKS:{BaserVersion} Current:{CurrentVersion}  Hotfix:{hotfixed}";
            return fullversin;
        }

        /// 
        /// Obtiene el ID del Device.
        ///
        private string GetDeviceID()
        {
            string deviceID = null;
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ComputerName");
                if (key != null)
                {
                    object o = key.GetValue("deviceID");
                    deviceID = o.ToString();
                }
            }
            catch (Exception e)
            {
                Debug.Print("Error de obtener deviceID!!! " + e.ToString());
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
                string token = await GetToken(TOKEN_Xpertme_URL);
                if (token != null)
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, API_Xpertme_URL);
                    request.Headers.Add("Authorization", $"Bearer {token}");
                    request.Content = new StringContent("{ \"mode\": \"dev\" }", Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    JObject jsonObject = JObject.Parse(responseBody);
                    var lastInstaller = jsonObject["data"]?["lastInstaller"];

                    VERSION = lastInstaller?["Version"]?.ToString();
                    cambios = lastInstaller?["ReleaseNotes"]?.ToString();
                    link = lastInstaller?["publicDllLink"]?.ToString();

                    if (!string.IsNullOrEmpty(VERSION) && CompareVersions())
                    {
                        var d = new update();
                        d.versionActual = versionActual;
                        d.versionNueva = VERSION;
                        d.cambios = cambios;
                        d.link = link;
                        d.ShowDialog();
                        d.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMsg = "Error al verificar actualizaciones: " + ex.Message;
                LogToFile(errorMsg);  // Agregar el error al log
                Debug.Print(errorMsg);
                LogToFile(errorMsg);
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
                var v1 = assemblyVersion.Split('.').Select(int.Parse).ToList();
                versionActual = assemblyVersion;
                var v2 = VERSION.Split('.').Select(int.Parse).ToList();

                for (int i = 0; i < v1.Count; i++)
                {
                    if (v2[i] > v1[i]) return true;
                    if (v2[i] < v1[i]) return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                string errorMsg = "Error al comparar versiones: " + ex.Message;
                LogToFile(errorMsg);  // Agregar el error al log
                Debug.Print(errorMsg);
                LogToFile(errorMsg);
                return false; // Por defecto, no hay actualización
            }
        }


        /// 
        /// Obtiene el token de forma dinamica.
        ///
        private async Task<string> GetToken(string url, string requestBody = "")
        {
            try
            {
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
                }
                else if (url == TOKEN_Analitics_URL)
                {
                    request.Content = new StringContent("", Encoding.UTF8, "application/x-www-form-urlencoded");
                }
                else if (!string.IsNullOrEmpty(requestBody))
                {
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                }

                var response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string errorMsg = $"Error al obtener token: {response.StatusCode} - {responseBody}";
                    LogToFile(errorMsg);  // Agregar el error al log
                    Debug.Print(errorMsg);
                    LogToFile(errorMsg);
                    return null;
                }

                JObject jsonObject = JObject.Parse(responseBody);

                string token = jsonObject["token"]?.ToString() ?? jsonObject["accessToken"]?.ToString();

                if (string.IsNullOrEmpty(token))
                {
                    string errorMsg = "Error: El token no se encontró en la respuesta.";
                    LogToFile(errorMsg);  // Agregar el error al log
                    Debug.Print(errorMsg);
                    LogToFile(errorMsg);
                    return null;
                }

                Debug.Print("Token obtenido correctamente.");
                return token;
            }
            catch (HttpRequestException httpEx)
            {
                string errorMsg = "Error HTTP en GetToken: " + httpEx.Message;
                LogToFile(errorMsg);  // Agregar el error al log
                Debug.Print(errorMsg);
                LogToFile(errorMsg);
                return null;
            }
            catch (Exception ex)
            {
                string errorMsg = "Error general en GetToken: " + ex.Message;
                LogToFile(errorMsg);  // Agregar el error al log
                Debug.Print(errorMsg);
                LogToFile(errorMsg);
                return null;
            }
        }



        #endregion

        /// 
        /// ////////////////////////////////////////////////////////////////////////// Enviar archivos a AWS /////////////////////////////////////////////////////////////////////////
        /// 

        private async Task SendJwl()
        {
            try
            {
                string baseVersion, currentVersion, hotfixes;
                solidWorksApp.GetBuildNumbers2(out baseVersion, out currentVersion, out hotfixes);

                string versionSW = ExtractSWVersion(baseVersion);
                string userFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                string solidworksPath = Path.Combine(userFolder, "SOLIDWORKS", $"SOLIDWORKS {versionSW}");
                LogToFile("mando llamar al sendJwl");
                await UploadIfExists(solidworksPath, "swxJRNL.swj");
                await UploadIfExists(solidworksPath, "swxJRNL.bak");
            }
            catch (Exception ex)
            {
                string errorMsg = "Error al enviar archivos JRNL: " + ex.Message;
                LogToFile(errorMsg);  // Log error
                Debug.Print(errorMsg);
                LogToFile(errorMsg);
            }
        }

        private string ExtractSWVersion(string baseVersion)
        {
            try
            {
                int startIndex = baseVersion.IndexOf("20");
                int endIndex = baseVersion.IndexOf("_");

                return (startIndex != -1 && endIndex != -1)
                    ? baseVersion.Substring(startIndex, endIndex - startIndex)
                    : "UnknownVersion";
            }
            catch (Exception ex)
            {
                string errorMsg = "Error al extraer la versión de SOLIDWORKS: " + ex.Message;
                LogToFile(errorMsg);  // Log error
                Debug.Print(errorMsg);
                LogToFile(errorMsg);
                return "UnknownVersion";
            }
        }

        private async Task UploadIfExists(string directory, string fileName)
        {
            try
            {
                // DateTime date = new DateTime();
                string filePath = Path.Combine(directory, fileName);
                LogToFile("mando llamar al upload" + filePath);
                if (File.Exists(filePath))
                {
                    await UploadToS3(filePath, fileName);
                } else
                {
                    LogToFile("Archivo no existe: " + filePath);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = "Error al subir archivo si existe: " + ex.Message;
                LogToFile(errorMsg);  // Log error
                Debug.Print(errorMsg);
                LogToFile(errorMsg);
            }
        }

        private async Task UploadToS3(string filePath, string fileKeyName)
        {
            try
            {
                string bucketName = "dev-nc-swapp";
                string userId = GetUserID();
                string customerId = GetCustomerID();
                DateTime date = DateTime.Now;

                // Crear carpeta temporal si no existe
                if (!Directory.Exists(tempFolder))
                    Directory.CreateDirectory(tempFolder);

                // Crear ruta del archivo temporal
                string tempFilePath = Path.Combine(tempFolder, Path.GetFileName(filePath));

                // Copiar el archivo a la carpeta temporal
                File.Copy(filePath, tempFilePath, true);

                // Encriptar los valores
                string encryptionKey = "dT7eY93vKLO82n90ZPiC1m0HKoA9f5FZ6+dx6E5m7PA=";
                string userIdEncryption = EncryptString(userId, encryptionKey).Replace("/", "0");
                string customerIdEncryption = EncryptString(customerId, encryptionKey).Replace("/", "0");
                string fileKeyNameEncryption = EncryptString(fileKeyName, encryptionKey).Replace("/", "0");
                string fecha = date.ToString().Replace("/", "-");
                string keyName = $"{customerId}/{userId}/{fileKeyName}/{fecha}/{fileKeyName}";
                string encryptedKeyName = $"{customerIdEncryption}/{userIdEncryption}/{fileKeyNameEncryption}/{fecha}/{fileKeyName}";

                Debug.Print($"Subiendo archivo a S3: {tempFilePath}");

                var chain = new CredentialProfileStoreChain();
                AWSCredentials credentials;

                if (!chain.TryGetAWSCredentials("default", out credentials))
                {
                    LogToFile("No se pudieron obtener las credenciales de AWS.");
                    throw new Exception("No se pudieron obtener las credenciales de AWS.");
                }

                using (var s3Client = new AmazonS3Client(credentials, RegionEndpoint.USEast1))
                using (var fileTransferUtility = new TransferUtility(s3Client))
                {
                    fileTransferUtility.Upload(tempFilePath, bucketName, encryptedKeyName);
                    Debug.Print("Archivo subido correctamente a AWS S3.");
                    LogToFile("Archivo subido correctamente a AWS S3.");
                    await InsertLogs(keyName);
                }
            }
            catch (AmazonS3Exception e)
            {
                string errorMsg = $"Error en AWS S3: {e.Message}";
                LogToFile(errorMsg);
                Debug.Print(errorMsg);
            }
            catch (Exception e)
            {
                string errorMsg = $"Error desconocido en UploadToS3: {e.Message}";
                LogToFile(errorMsg);
                Debug.Print(errorMsg);
            }
            finally
            {
                // Borrar carpeta temporal completa
                try
                {
                    if (Directory.Exists(tempFolder))
                        Directory.Delete(tempFolder, true);
                }
                catch (Exception ex)
                {
                    LogToFile("Error al eliminar la carpeta temporal: " + ex.Message);
                }
            }
        }

        public async Task InsertLogs(string FilesLogs)
        {
            try
            {
                // Obtener el token de autenticación
                string token = await GetToken(TOKEN_Analitics_URL);

                if (string.IsNullOrEmpty(token))
                {
                    string errorMsg = "Error: No se pudo obtener el token.";
                    LogToFile(errorMsg);  // Log error
                    Debug.Print(errorMsg);
                    LogToFile(errorMsg);
                    return;
                }

                // Preparar la URL de la API
                string url = "https://api-ncsw.xpertme.com/api/createLogs";  // URL de la API de logs

                // Obtener los datos del dispositivo y usuario
                string device = GetDeviceID();
                string UserID = GetUserID();  // Asegúrate de que si UserID es null, sea manejado adecuadamente
                string UserMail = GetEmail();  // Asegúrate de obtener el correo del usuario

                // Preparar los datos de la solicitud
                var Json = new
                {
                    code = "r5ncccmGhzLG",
                    deviceID = device,
                    UserID = (UserID == null ? (object)null : UserID),  // Si es null, pasamos un null explícito
                    UserMail = UserMail,
                    logList = new[]
                    {
                new { nameFile = FilesLogs }  // Aquí convertimos FilesLogs en un objeto con la propiedad nameFile
            }
                };

                // Serializar los datos en formato JSON
                string requestBody = JsonConvert.SerializeObject(Json, Formatting.Indented);

                // Crear la solicitud HTTP
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"{token}");  // Añadir el token en el encabezado
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                // Enviar la solicitud
                var response = await client.SendAsync(request);

                // Comprobar si la respuesta es exitosa
                if (!response.IsSuccessStatusCode)
                {
                    string errorMsg = $"Error en la solicitud: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
                    LogToFile(errorMsg);  // Log error
                    Debug.Print(errorMsg);
                    LogToFile(errorMsg);
                    return;
                }

                // Procesar la respuesta de la API
                string responseText = await response.Content.ReadAsStringAsync();
                JObject jsonResponse = JObject.Parse(responseText);
                Debug.Print(jsonResponse.ToString());
                LogToFile(jsonResponse.ToString());
            }
            catch (Exception ex)
            {
                string errorMsg = "Error al insertar logs: " + ex.Message;
                LogToFile(errorMsg);  // Log error
                Debug.Print(errorMsg);
                LogToFile(errorMsg);
            }
        }

        private string EncryptString(string plainText, string key)
        {
            try
            {
                // Reemplazar las diagonales ("/") por "0"
                plainText = plainText.Replace("/", "0");

                byte[] iv = new byte[16];
                byte[] array;

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
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
                LogToFile(encryptedString);

                return encryptedString;
            }
            catch (Exception ex)
            {
                string errorMsg = "Error al encriptar la cadena: " + ex.Message;
                LogToFile(errorMsg);  // Log error
                Debug.Print(errorMsg);
                LogToFile(errorMsg);
                return string.Empty;
            }
        }

    }
}