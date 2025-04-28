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

namespace analytics_AddIn
{
    [ComVisible(true)]
    [Guid("31b803e0-7a01-4841-a0de-895b726625c9")]
    [DisplayName("Analitics")]
    [Description("Analitics SOLIDWORKS Add-In")]
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
        private const string ConfigFilePath = "C:\\ProgramData\\SOLIDWORKS\\"+ AppName +"_sessions.txt";
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
                var addInTitle = t.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? t.ToString();
                var addInDesc = t.GetCustomAttribute<DescriptionAttribute>()?.Description ?? t.ToString();
                var addInKeyPath = string.Format(ADDIN_KEY_TEMPLATE, t.GUID);

                using (var addInKey = Registry.LocalMachine.CreateSubKey(addInKeyPath))
                {
                    addInKey.SetValue(ADD_IN_TITLE_REG_KEY_NAME, addInTitle);
                    addInKey.SetValue(ADD_IN_DESCRIPTION_REG_KEY_NAME, addInDesc);

                    // 📌 Ruta dinámica del icono dentro del directorio del Add-in
                    string pathInstall = GetSource();
                    string iconPath = Path.Combine(pathInstall, AppName,"AddinIcon.bmp");
                    addInKey.SetValue("Icon Path", iconPath, RegistryValueKind.String);  // Ruta del icono
                }

                var addInStartupKeyPath = string.Format(ADDIN_STARTUP_KEY_TEMPLATE, t.GUID);
                using (var addInStartupKey = Registry.CurrentUser.CreateSubKey(addInStartupKeyPath))
                {
                    addInStartupKey.SetValue(null, 1, RegistryValueKind.DWord);
                }

                Console.WriteLine("Add-in registrado correctamente con ícono.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al registrar el Add-in: " + ex.Message);
            }
        }


        [ComUnregisterFunction]
        public static void UnregisterFunction(Type t)
        {
            try
            {
                Registry.LocalMachine.DeleteSubKey(string.Format(ADDIN_KEY_TEMPLATE, t.GUID));
                Registry.CurrentUser.DeleteSubKey(string.Format(ADDIN_STARTUP_KEY_TEMPLATE, t.GUID));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while unregistering the addin: " + e.Message);
            }
        }

        public bool ConnectToSW(object solidWorksInstance, int addInId)
        {
            solidWorksApp = solidWorksInstance as ISldWorks;
            addInCookie = addInId;
            solidWorksApp.SetAddinCallbackInfo(0, this, addInCookie);
            commandManager = solidWorksApp.GetCommandManager(addInCookie);
            InitializeEventHandlers();

            if (IsInternetAvailable())
            {
                solidWorksApp.SendMsgToUser("Si hay internet");
                Task.Run(async () => { await CheckForUpdates(); await SendSesionSW(); SendJwl(); });
            }
            else
            {
                solidWorksApp.SendMsgToUser("No hay conexión a internet. No se pudo verificar la versión.");
            }

            try
            {
                if (DoesConfigFileExist())
                {
                    ReadConfigFile();
                }
                else
                {
                    CreateConfigFile();
                }
            }
            catch (Exception ex)
            {
                solidWorksApp.SendMsgToUser("Error al verificar el archivo de configuración: " + ex.Message);
            }
            return true;
        }

        /// 
        /// Configura los manejadores de eventos para capturar eventos de SOLIDWORKS.
        /// 
        private void InitializeEventHandlers()
        {
            solidWorksEventHandler = (SldWorks)solidWorksApp;
            openDocuments = new Hashtable();
            //ValidateLicense();
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
                    Debug.Print("Error: No se pudo obtener el token.");
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

                // 🔹 Corregir encabezado de autorización (Agregar "Bearer ")
                request.Headers.Add("Authorization", $"{token}");

                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.Print($"Error en la solicitud: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    return;
                }

                string responseText = await response.Content.ReadAsStringAsync();
                JObject jsonResponse = JObject.Parse(responseText);

                // 🔹 Validar si "message" existe antes de acceder a él
                if (jsonResponse.ContainsKey("message"))
                {
                    JArray messageArray = (JArray)jsonResponse["message"];
                    JToken firstItem = messageArray?.FirstOrDefault();

                    if (firstItem != null && firstItem["statusInsert"]?.ToString() == "Insertado")
                    {
                        ClearAlltext();
                    }
                }
                else
                {
                    Debug.Print("Advertencia: No se encontró 'message' en la respuesta.");
                }
            }
            catch (HttpRequestException httpEx)
            {
                Debug.Print("Error de red en SendSesionSW: " + httpEx.Message);
            }
            catch (Exception ex)
            {
                Debug.Print("Error en SendSesionSW: " + ex.Message);
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

        /// 
        /// Obtiene la version actual de analitycs y la compara con la ultima.
        ///
        private bool CompareVersions()
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

        /// 
        /// Obtiene el token de forma dinamica.
        ///
        private async Task<string> GetToken(string url, string requestBody = "")
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                // Si la URL es TOKEN_URL, usa autenticación básica
                if (url == TOKEN_Xpertme_URL)
                {
                    request.Headers.Add("Authorization", "Basic YW5hbHl0aWNzX3N3OlRtMFF6QTlR");
                    request.Content = new FormUrlEncodedContent(new[]
                    {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("scope", "analytics:installers:get")
                });
                }
                else if (!string.IsNullOrEmpty(requestBody))
                {
                    // Si tiene un cuerpo de solicitud, lo envía como JSON
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                }
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                JObject jsonObject = JObject.Parse(responseBody);

                return jsonObject["token"]?.ToString() ?? jsonObject["accessToken"]?.ToString();
            }
            catch (HttpRequestException httpEx)
            {
                Debug.Print("Error HTTP: " + httpEx.Message);
                return null;
            }
            catch (Exception ex)
            {
                Debug.Print("Error general: " + ex.Message);
                return null;
            }

        }
        #endregion

        /// 
        /// ////////////////////////////////////////////////////////////////////////// Enviar archivos a AWS /////////////////////////////////////////////////////////////////////////
        /// 

        private void SendJwl()
        {
            string baseVersion, currentVersion, hotfixes;
            solidWorksApp.GetBuildNumbers2(out baseVersion, out currentVersion, out hotfixes);

            string versionSW = ExtractSWVersion(baseVersion);
            string userFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            string solidworksPath = Path.Combine(userFolder, "SOLIDWORKS", $"SOLIDWORKS {versionSW}");

            UploadIfExists(solidworksPath, "swxJRNL.swj");
            UploadIfExists(solidworksPath, "swxJRNL.bak");
        }

        private string ExtractSWVersion(string baseVersion)
        {
            int startIndex = baseVersion.IndexOf("20");
            int endIndex = baseVersion.IndexOf("_");

            return (startIndex != -1 && endIndex != -1)
                ? baseVersion.Substring(startIndex, endIndex - startIndex)
                : "UnknownVersion";
        }

        private void UploadIfExists(string directory, string fileName)
        {
            DateTime date = new DateTime();
            string filePath = Path.Combine(directory, fileName, date.ToString());
            if (File.Exists(filePath))
            {
                UploadToS3(filePath, fileName);
            } 
        }

        private async Task UploadToS3(string filePath, string fileKeyName)
        {
            string bucketName = "dev-nc-swapp";
            string userId = GetUserID();
            string customerId = GetCustomerID();
            DateTime date = new DateTime();
            string keyName = $"{customerId}/{userId}/{fileKeyName}/{date}";
            // Encriptar el keyName
            string encryptionKey = "dT7eY93vKLO82n9/ZPiC1m0HKoA9f5FZ6+dx6E5m7PA="; // asegúrate de tener una clave de 32 caracteres
            string encryptedKeyName = EncryptString(keyName, encryptionKey);
            Debug.Print($"Subiendo archivo a S3: {filePath}");

            try
            {
                // Usar SharedCredentialsFile para obtener las credenciales del perfil 'default'
                var chain = new CredentialProfileStoreChain();
                AWSCredentials credentials;

                if (!chain.TryGetAWSCredentials("default", out credentials))
                {
                    throw new Exception("No se pudieron obtener las credenciales de AWS.");
                }

                using (var s3Client = new AmazonS3Client(credentials, RegionEndpoint.USEast1))
                using (var fileTransferUtility = new TransferUtility(s3Client))
                {
                    fileTransferUtility.Upload(filePath, bucketName, encryptedKeyName);
                    Debug.Print("Archivo subido correctamente a AWS S3.");
                    await InsertLogs(keyName);
                }
            }
            catch (AmazonS3Exception e)
            {
                Debug.Print($"Error en AWS S3: {e.Message}");
            }
            catch (Exception e)
            {
                Debug.Print($"Error desconocido: {e.Message}");
            }
        }

        public async Task InsertLogs(string FilesLogs)
        {
            string token = await GetToken(TOKEN_Analitics_URL);

            if (string.IsNullOrEmpty(token))
            {
                Debug.Print("Error: No se pudo obtener el token.");
                return;
            }

            string url = "https://api-ncsw.xpertme.com/api/createLogs";
            string device = GetDeviceID();
            string UserID = GetUserID();
            var Json = new
            {
                code = "r5ncccmGhzLG",
                deviceID = device,
                UserID = UserID,
                UserMail = 0,
                logList = FilesLogs
            };

            string requestBody = JsonConvert.SerializeObject(Json, Formatting.Indented);
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            
            request.Headers.Add("Authorization", $"Bearer {token}");

            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Debug.Print($"Error en la solicitud: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                return;
            }

            string responseText = await response.Content.ReadAsStringAsync();
            JObject jsonResponse = JObject.Parse(responseText);
            Debug.Print(jsonResponse.ToString());
        }

        private string EncryptString(string plainText, string key)
        {
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

            return Convert.ToBase64String(array);
        }

        
    }
}