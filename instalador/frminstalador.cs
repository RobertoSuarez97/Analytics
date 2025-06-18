using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net.Http;
// para ponerlo en el menú de windows
using IWshRuntimeLibrary;
// para el registro
using Microsoft.Win32;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Net.NetworkInformation;
using System.Linq;

namespace instalador
{
    public partial class frminstalador : Form
    {
        private const string AppName = "analytics";
        private static readonly HttpClient client = new HttpClient();
        WebClient wc = new WebClient();
        List<Panel> paginas = new List<Panel>(); // para los paneles
        Panel panelCheckBoxContainer = new Panel();
        System.Windows.Forms.CheckBox chk = new System.Windows.Forms.CheckBox();
        int indice = 0; // indice del panel
        string ruta = "", pathToExe = "", temp = "";
        string VERSION = "", AssemblyVERSION = "", ARCHIVO = "", URL = "", LINK = "";
        // datos que se guardan
        string email = "", password = "", UserID = "", deviceID = "", deviceAlias = "", deviceName = "", code = "", osVersion = "", deviceMAC = "", swActivies = "", campus = "";
        string mode = "prod";
        string DllAddIn = "analytics_AddIn";
        private Dictionary<string, int> customerMapping = new Dictionary<string, int>();
        private int selectedCustomerID = 0; // Aquí guardarás el CustomerID del seleccionado
        private string LogFilePath = GetSource() + "\\" + AppName + "\\Instalador_log.txt";

        public frminstalador()
        {
            InitializeComponent();
            // No llames async aquí
            this.Load += frminstalador_Load;
        }

        private async void frminstalador_Load(object sender, EventArgs e)
        {
            await Task.Delay(100); // opcional, solo si quieres darle tiempo al form de cargar
            await setValoresIniciales();
        }

        /////////////////////////////////////////////////////////////////////------------Acotaciones-----------///////////////////////////////////////////////////////////////////////
        // Valores iniciales
        // **Carpeta de descarga
        // **Seteo de instalacion y descomprimir archivo
        private async Task setValoresIniciales()
        {
            // primero de todo comprobamos la última versión
            getAssemblyVersion();
            await getVersion();

            // ahora detectamos el link al archivo

            ARCHIVO = AppName + "-v" + AssemblyVERSION + ".zip";
            
            // URL = this.LINK + "/" + AppName + "/" + ARCHIVO;
            URL = this.LINK;
            // MessageBox.Show(URL);
            txtmenuinicio.Text = AppName;

            // añadimos a la lista de páginas
            paginas.Add(lyrBienvenida);
            paginas.Add(lyrAviso);
            paginas.Add(lyrValidacion);
            paginas.Add(lyrConfigUsuario);
            paginas.Add(lyrSeleccion);
            paginas.Add(panel3);
            paginas.Add(panel4);
            paginas.Add(panel5);
            paginas.Add(lyrNotUser);

            // navegamos a la primera (Bienvenida)
            indice = 0;
            pagina();

            // ruta predeterminada de programas ("C:\Program Files\")
            ruta = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            txtruta.Text = ruta;

            // para la descarga del programa e registros en el regedit
            wc.DownloadProgressChanged += (s, e) =>
            {
                pbinstalacion.Value = e.ProgressPercentage;
            };

            wc.DownloadFileCompleted += (s, e) =>
            {
                if (!e.Cancelled)
                {
                    // si no se ha cancelado, extraemos el archivo en la carpeta de instalación

                    string carpeta = Path.Combine(ruta, AppName);
                    if (Directory.Exists(carpeta)) eliminarCarpeta(carpeta);
                    Directory.CreateDirectory(carpeta);

                    rtbprogreso.AppendText("\nExtrayendo/copiando archivos");
                    ZipFile.ExtractToDirectory(temp, carpeta); // extraemos el archivo

                    // ✅ REGISTRAR LA DLL AUTOMÁTICAMENTE
                    string dllPath = Path.Combine(carpeta, "Dll", DllAddIn + ".dll");
                    if (System.IO.File.Exists(dllPath))
                    {
                        rtbprogreso.AppendText("\nRegistrando la DLL...");
                        RegisterDLL(dllPath);
                    }

                    rtbprogreso.AppendText("\nAgregando al registro...");
                    // agregando al menú de inicio
                    pathToExe = Path.Combine(carpeta, AppName + ".exe");

                    // la agregamos al registro
                    RegistryKey __key = Registry.LocalMachine.OpenSubKey("Software", true)
                    .OpenSubKey("Microsoft", true).OpenSubKey("Windows", true)
                    .OpenSubKey("CurrentVersion", true);

                    RegistryKey key = __key.OpenSubKey("App Paths", true);

                    key.CreateSubKey(AppName + ".exe", true);
                    key = key.OpenSubKey(AppName + ".exe", true);
                    key.SetValue("", Path.Combine(carpeta, AppName + ".exe"));
                    key.SetValue("Path", carpeta);

                    // lo mismo para desinstalarla
                    RegistryKey unins = __key.OpenSubKey("Uninstall", true);
                    unins.CreateSubKey(AppName, true);
                    unins = unins.OpenSubKey(AppName, true);

                    unins.SetValue("DisplayName", AppName);
                    unins.SetValue("DisplayVersion", VERSION);
                    unins.SetValue("UninstallString", Path.Combine(carpeta, "uninstall.exe"));

                    pbinstalacion.Value = pbinstalacion.Maximum;

                    // Llamamos a la función que agrega el registro de usuario
                    registerUserRegedit();

                    MessageBox.Show("Instalación completada");
                    Environment.Exit(0);
                }
            };
        }

        public void registerUserRegedit()
        {
            string Keypath1 = @"SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName";

            try
            {
                RegistryKey Key1 = Registry.LocalMachine.CreateSubKey(Keypath1);
                Key1.SetValue("CustomerID", code);
                Key1.Close();

                RegistryKey key2 = Registry.LocalMachine.CreateSubKey(Keypath1);
                key2.SetValue("UserID", UserID);
                key2.Close();

                RegistryKey key3 = Registry.LocalMachine.CreateSubKey(Keypath1);
                key3.SetValue("deviceID", deviceID);
                key3.Close();

                RegistryKey key4 = Registry.LocalMachine.CreateSubKey(Keypath1);
                key4.SetValue("Email", email);
                key4.Close();

                RegistryKey key5 = Registry.LocalMachine.CreateSubKey(Keypath1);
                key5.SetValue("Source", ruta);
                key5.Close();
            }
            catch (Exception ex)
            {
                if (ex.Message.StartsWith("Se denegó el acceso a la clave de Registro"))
                {
                    MessageBox.Show("Se denegó el acceso, cancela y ejecuta como administrador");
                }
            }
        }

        // ✅ FUNCIÓN PARA REGISTRAR LA DLL AUTOMÁTICAMENTE
        private void RegisterDLL(string dllPath)
        {
            try
            {
                string regasmPath = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe";
                string argument = $"\"{dllPath}\" /tlb /codebase";

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

                    rtbprogreso.AppendText("\nDLL registrada correctamente.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al registrar la DLL: {ex.Message}");
            }
        }

        public void GenerarCheckBoxes()
        {
            // Limpiar los controles previos del panel
            //lyrSeleccion.Controls.Clear();
            List<string> opciones = new List<string>
            {
                "Diseño Estructural",
                "Diseño Aeronáutico",
                "Diseño de moldes",
                "Diseño de dispositivos médicos",
                "Diseño de herramientas",
                "Diseño de equipos industriales",
                "Diseño de productos de consumo",
                "Diseño de envases",
                "Diseño Automotriz",
                "Diseño de empaques",
                "Diseño de maquinaria",
                "Diseño de muebles",
                "Generación de planos 2D"
            };

            // Crear un Panel contenedor
            panelCheckBoxContainer.BackColor = System.Drawing.Color.White; // Fondo blanco
            panelCheckBoxContainer.AutoScroll = true; // Permitir scroll si es necesario
            panelCheckBoxContainer.Size = new System.Drawing.Size(900, 400); // Ajusta el tamaño según sea necesario
            panelCheckBoxContainer.Location = new System.Drawing.Point(20, 150); // Posición dentro de lyrSeleccion

            int yPos = 10; // Posición inicial dentro del panel
            int xPos = 10;
            int columnHeight = 330; // Ajustar según el tamaño del panel
            int columnWidth = 450; // Ancho de cada columna

            foreach (var opcion in opciones)
            {
                // Si se llega al límite de la columna, mover a la siguiente
                if (yPos >= columnHeight)
                {
                    xPos += columnWidth; // Mover a la siguiente columna
                    yPos = 10; // Reiniciar la posición vertical
                }

                // Crear un nuevo CheckBox en cada iteración
                CheckBox chk = new CheckBox();
                chk.Text = opcion;
                chk.Size = new System.Drawing.Size(400, 40);
                chk.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
                chk.Location = new System.Drawing.Point(xPos, yPos);
                chk.BackColor = System.Drawing.Color.White;

                // Agregar el CheckBox al Panel contenedor
                panelCheckBoxContainer.Controls.Add(chk);

                // Incrementar la posición vertical
                yPos += 40;
            }

            // Agregar el Panel contenedor a lyrSeleccion
            lyrSeleccion.Controls.Add(panelCheckBoxContainer);
        }

        private List<Dictionary<string, string>> ObtenerCheckBoxesSeleccionados()
        {
            List<Dictionary<string, string>> actividadesSeleccionadas = new List<Dictionary<string, string>>();

            foreach (Control control in panelCheckBoxContainer.Controls)
            {
                // Validar primero si es un CheckBox
                if (control is CheckBox)
                {
                    CheckBox chk = (CheckBox)control;

                    // Ahora validar si está seleccionado
                    if (chk.Checked)
                    {
                        actividadesSeleccionadas.Add(new Dictionary<string, string>
            {
                { "activiti", chk.Text }
            });
                    }
                }
            }

            return actividadesSeleccionadas;
        }

        private void ckbAviso_CheckedChanged(object sender, EventArgs e)
        {
            // código para (no) aceptar la licencia
            if (ckbAviso.Checked)
            {
                // no la acepta
                btnsiguiente.Enabled = true;
            }
            else btnsiguiente.Enabled = false;
        }

        public static string GetMacAddress()
        {
            try
            {
                // Obtiene todas las interfaces de red
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                // Filtra la interfaz activa que esté conectada y no sea loopback
                var activeInterface = networkInterfaces
                    .FirstOrDefault(nic => nic.OperationalStatus == OperationalStatus.Up &&
                                           nic.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                // Si encontró una interfaz activa, retorna su dirección MAC
                if (activeInterface != null)
                {
                    return string.Join(":", activeInterface.GetPhysicalAddress()
                        .GetAddressBytes()
                        .Select(b => b.ToString("X2")));
                }
                else
                {
                    return "No se encontró ninguna interfaz de red activa.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener la MAC Address: {ex.Message}");
                return "Error al obtener la MAC Address";
            }
        }

        // Obtiene el sistema operativo del equipo
        public string GetOperatingSystem()
        {

            string windowsProductName = string.Empty;

            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

            if (key != null)
            {
                windowsProductName = key.GetValue("ProductName")?.ToString();
                key.Close();
                return windowsProductName;
            }
            else
            {
                return "ERROR!!";
            }


        }

        private void lblLikRegistro_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://accounts.xpertme.com/user/registerView",
                UseShellExecute = true // Necesario para evitar problemas con seguridad en algunas versiones de Windows
            });
        }

        private void lblNotUserClick_click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://accounts.xpertme.com/user/registerView",
                UseShellExecute = true // Necesario para evitar problemas con seguridad en algunas versiones de Windows
            });
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

        /////////////////////////////////////////////////////////////////////------------Obtener tokens-----------///////////////////////////////////////////////////////////////////////
        public static async Task<string> GetToken(string authHeader, string scope, bool useLocalAuth = false)
        {
            // Forzar el uso de TLS 1.2 para evitar problemas de seguridad
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            string tokenUrl;
            if (useLocalAuth)
            {
                tokenUrl = "https://api-ncsw.xpertme.com/api/auth"; // URL local
            }
            else
            {
                tokenUrl = "https://api-academy.xpertcad.com/v2/system/oauth/token"; // URL de Xpertme
            }

            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

                // Si es la URL de Xpertme, agregamos el header de autorización
                if (!useLocalAuth)
                {
                    request.Headers.Add("Authorization", $"Basic {authHeader}");
                    request.Content = new FormUrlEncodedContent(new[]
                    {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", scope)
            });
                }
                else
                {
                    // Si es la URL local, no es necesario el header de autorización ni los datos
                    request.Content = new StringContent(""); // Se puede ajustar si la API espera otros datos
                }

                try
                {
                    HttpResponseMessage response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();
                    JObject jsonObject = JObject.Parse(responseBody);
                    string accessToken = jsonObject["accessToken"]?.ToString();
                    if (string.IsNullOrEmpty(accessToken))
                    {
                        accessToken = jsonObject["token"]?.ToString();  // Si no se encuentra "accessToken", verifica "token"
                    }

                    if (string.IsNullOrEmpty(accessToken))
                    {
                        throw new Exception("Token no encontrado");
                    }
                    return accessToken;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al obtener el token en petición GetToken: {ex}");
                    return null;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////------------Obtener version actual-----------///////////////////////////////////////////////////////////////////////
        public async Task getVersion()
        {
            string token = await GetToken("YW5hbHl0aWNzX3N3OlRtMFF6QTlR", "analytics:installers:get");
            if (token != null)
            {
                string response = await getAnalyticsInstallers(token);
                JObject jsonObject = JObject.Parse(response);
                this.LINK = jsonObject["data"]?["lastInstaller"]?["publicDllLink"]?.ToString();
                this.VERSION = jsonObject["data"]?["lastInstaller"]?["Version"]?.ToString();
                Text = $"Instalación Xpertme Analytics {VERSION}";
            }
            else
            {
                Console.WriteLine("No se pudo obtener el token desde get version.");
            }
        }

        private void getAssemblyVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            this.AssemblyVERSION = fvi.FileVersion;
            lbversion.Text = AssemblyVERSION;
            
        }

        public static async Task<string> getAnalyticsInstallers(string token)
        {
            string analyticsUrl = "https://api-academy.xpertcad.com/v2/analytics/users/getAnalyticsInstallers";
            var request = new HttpRequestMessage(HttpMethod.Post, analyticsUrl);
            request.Headers.Add("Authorization", $"Bearer {token}");

            // El encabezado "Content-Type" se define dentro de `StringContent`, no en `Headers`
            var requestBody = "{ \"mode\": \"dev\" }";
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener los datos//: {ex.Message}");
                return null;
            }
        }

        /////////////////////////////////////////////////////////////////////------------Validacion de usuario-----------///////////////////////////////////////////////////////////////////////
        private async Task<bool> getUserData()
        {
            if (string.IsNullOrWhiteSpace(txtEmailValid.Text) || string.IsNullOrWhiteSpace(txtContraseña.Text))
            {
                MessageBox.Show("Por favor, ingrese su correo y contraseña.", "Campos requeridos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string status = await ValidUser(txtEmailValid.Text, txtContraseña.Text, mode);

            if (string.IsNullOrEmpty(status) || status == "0")
            {
                indice = 8;
                pagina();
                return false;
            }

            if (status == "-1")
            {
                // Seteamos valores
                code = "null";
                campus = "null";

                lblInstitucion.Visible = false;
                cbInstitucion.Visible = false;
                lblCode.Visible = false;
                txtCode.Visible = false;
                return true;
            }

            if (status == "1")
            {
                lblInstitucion.Visible = true;
                cbInstitucion.Visible = true;
                lblCode.Visible = false;
                txtCode.Visible = false;
                return true;
            }

            return true;
        }

        public async Task<string> ValidUser(string email, string password, string mode)
        {
            string token = await GetToken("bmNfc3c6ekgwUXpPOUc=", "access:userA:Analytics");
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Error: No se pudo obtener el token.");
                return null;
            }

            string requestUrl = "https://api-academy.xpertcad.com/v2/nctech/users/userAuth";

            using (HttpClient client = new HttpClient())
            {
                var requestBody = new
                {
                    UserEmail = email,
                    Password = password,
                    mode = mode
                };

                var jsonBody = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                {
                    Content = content
                };
                request.Headers.Add("Authorization", $"Bearer {token}");

                try
                {
                    HttpResponseMessage response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    string responseText = await response.Content.ReadAsStringAsync();
                    JObject jsonObject = JObject.Parse(responseText);
                    string statusCustomer = jsonObject["data"]?["hasCustomer"]?.ToString();

                    LogToFile(jsonObject.ToString(Newtonsoft.Json.Formatting.None));

                    if (string.IsNullOrEmpty(statusCustomer))
                    {
                        return "0"; // No existe hasCustomer → 0
                    }

                    UserID = jsonObject["data"]?["ac_UserID"]?.ToString();
                    customerMapping.Clear();
                    List<string> customerNames = new List<string>();
                    foreach (var customer in jsonObject["data"]["customersInfo"])
                    {
                        string customerName = customer["CustomerName"]?.ToString();
                        string customerIDStr = customer["CustomerID"]?.ToString();

                        if (!string.IsNullOrEmpty(customerName))
                        {
                            customerNames.Add(customerName);
                            customerMapping[customerName] = int.Parse(customerIDStr); // Mapeo del nombre al ID
                        }
                    }

                    // Actualizar el ComboBox en el hilo de la UI
                    cbInstitucion.Invoke((MethodInvoker)delegate
                    {
                        cbInstitucion.Items.Clear(); // Limpiar el ComboBox antes de llenarlo
                        cbInstitucion.Items.AddRange(customerNames.ToArray());
                        if (cbInstitucion.Items.Count > 0)
                        {
                            cbInstitucion.SelectedIndex = 0; // Seleccionar el primer valor por defecto
                            campus = cbInstitucion.SelectedItem.ToString();
                            selectedCustomerID = customerMapping[campus]; // Guardar el CustomerID asociado
                        } else
                        {
                            campus = null;
                            selectedCustomerID = -1;
                        }
                    });

                    // Manejar cambios en la selección del ComboBox
                    cbInstitucion.SelectedIndexChanged += (s, e) =>
                    {
                        campus = cbInstitucion.SelectedItem.ToString();
                        if (customerMapping.ContainsKey(campus))
                        {
                            selectedCustomerID = customerMapping[campus];
                        }
                    };


                    if (string.IsNullOrEmpty(statusCustomer))
                    {
                        return "0"; // No existe hasCustomer → 0
                    }
                    else if (statusCustomer == "-1")
                    {
                        return statusCustomer; // Usuario autodidacta → null
                    }
                    else if (statusCustomer == "1")
                    {
                        // Ya estamos llenando el ComboBox arriba
                        return "1";
                    }
                    else
                    {
                        return "0"; // Cualquier otro caso
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en ValidUser: {ex.Message}");
                    return "0";
                }
            }
        }
       

        /////////////////////////////////////////////////////////////////////------------Create device-----------///////////////////////////////////////////////////////////////////////
        public static async Task<string> createDevice(string json)
        {
            string token = await GetToken("", "", true);
            string analyticsUrl = "https://api-ncsw.xpertme.com/api/createDevice";

            var request = new HttpRequestMessage(HttpMethod.Post, analyticsUrl);
            request.Headers.Add("authorization", $"{token}");

            // El encabezado "Content-Type" se define dentro de `StringContent`, no en `Headers`
            var requestBody = json;
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Parsear la respuesta JSON
                JObject jsonResponse = JObject.Parse(responseBody);

                // Extraer el valor de "insertId"
                string insertId = jsonResponse["insertId"]?.ToString() ?? null;

                return insertId; // Devuelve el insertId
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener los datos--: {ex.Message}");
                return null;
            }
        }

        /////////////////////////////////////////////////////////////////////------------Botones-----------///////////////////////////////////////////////////////////////////////
        void pagina()
        {
            // mostramos la página actual
            paginas[indice].BringToFront();
            switch (indice)
            {
                case 0:
                    // bienvenida
                    btnatras.Enabled = false;
                    btnsiguiente.Enabled = true;
                    break;
                case 1:
                    // aviso
                    ckbAviso.Checked = false;
                    btnatras.Enabled = true;
                    btnsiguiente.Enabled = false;
                    break;
                case 2:
                    // validacion
                    btnatras.Enabled = true;
                    btnsiguiente.Enabled = true;
                    break;
                case 3:
                    // config
                    btnatras.Enabled = true;
                    btnsiguiente.Enabled = true;
                    break;
                case 4:
                    // seleccion
                    btnatras.Enabled = true;
                    btnsiguiente.Enabled = true;
                    break;
                case 5:
                    // ruta
                    btnsiguiente.Text = "Siguiente";
                    btnatras.Enabled = true;
                    btnsiguiente.Enabled = true;
                    break;
                case 6:
                    // para instalar
                    btnsiguiente.Text = "Instalar";
                    break;
                case 7:
                    btnsiguiente.Text = "Instalando...";
                    btnsiguiente.Enabled = false;
                    btnatras.Text = "Cancelar";
                    break;
                case 8:
                    btnsiguiente.Enabled = false;
                    btnatras.Enabled = true;
                    lblNotUserClick.BringToFront();
                    break;
                default: break;
            }
        }

        private async void btnsiguiente_Click(object sender, EventArgs e)
        {
            if (indice == 2) // Página de validación de usuario
            {
                // txtEmailValid.Text = "";
                // txtContraseña.Text = "";
                //txtEmailValid.Text = "supervisor@nctech.com.mx";
                //txtContraseña.Text = "#2024Xpert";
                if (!await getUserData()) {return;} 
            }

            if (indice == 3) // Capturamos los datos
            {
                email = txtEmailValid.Text;
                password = txtContraseña.Text;
                deviceAlias = txtDevice.Text;
                code = selectedCustomerID.ToString();
                GenerarCheckBoxes();
            }

            if (indice == 4) // Campuramos los activities
            {
                var seleccionados = ObtenerCheckBoxesSeleccionados();
                string json = JsonConvert.SerializeObject(seleccionados, Formatting.Indented);
                swActivies = json;
                string format = "yyyy-MM-dd HH:mm:ss";
                string Currenttinme = DateTime.Now.ToString(format);
                deviceMAC = GetMacAddress();
                osVersion = GetOperatingSystem();
                deviceName = Environment.MachineName;

                var requestBodyData = new
                {
                    code = "r5ncccmGhzLG",
                    created_at = Currenttinme,
                    UserEmail = email,
                    userID = UserID,
                    appCustomerID = code,
                    deviceAlias = deviceAlias,
                    deviceName = deviceName,
                    deviceMAC = deviceMAC,
                    soVersion = osVersion,
                    swActivies = swActivies,
                    Campus = campus,
                    Career = "null"
                };

                // Convertir a string (JSON)
                string jsonString = JsonConvert.SerializeObject(requestBodyData);
                // Usa await para obtener el resultado de la tarea
                deviceID = await createDevice(jsonString);
            }

            if (indice == paginas.Count - 1) Environment.Exit(0);
            ++indice;
            pagina();

            if (btnsiguiente.Text == "Instalando...") // Página de instalacion
            {
                temp = Path.Combine(Path.GetTempPath(), AppName);
                if (Directory.Exists(temp)) eliminarCarpeta(temp);
                Directory.CreateDirectory(temp);
                temp = Path.Combine(temp, ARCHIVO);
                wc.DownloadFileAsync(new Uri(URL), temp);
                rtbprogreso.Text = "Descargando " + URL;
                lbprogreso.Text = "Instalando...";
            }
        }

        private void btnatras_Click(object sender, EventArgs e)
        {
            if (indice == 0) return;

            if (indice == 8) { indice = 2; pagina(); return; }
            
            if (btnatras.Text == "Cancelar")
            {
                // pedimos confirmación para salir
                var d = MessageBox.Show("Seguro que quieres salir?", "Instalación", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk);
                if (d == DialogResult.Yes)
                {
                    // cancelamos y salimos
                    if (wc.IsBusy) wc.CancelAsync();
                    Environment.Exit(0);
                }
            }
            --indice;
            pagina();
        }

        // para ponerlo en el menú de inicio
        private void addInicio()
        {
            string commonStartMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
            string appStartMenuPath = Path.Combine(commonStartMenuPath, "Programs", txtmenuinicio.Text);

            if (!Directory.Exists(appStartMenuPath))
                Directory.CreateDirectory(appStartMenuPath);

            string shortcutLocation = Path.Combine(appStartMenuPath, AppName + ".lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

            shortcut.Description = "Muestra un mensaje";
            shortcut.IconLocation = $@"{ruta}\{AppName}.ico"; //uncomment to set the icon of the shortcut
            shortcut.TargetPath = pathToExe;
            shortcut.Save();
        }

        // para seleccionar la carpeta de instalación
        private void btncarpeta_Click(object sender, EventArgs e)
        {
            // seleccionamos otra carpeta para la instalación
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtruta.Text = fbd.SelectedPath;
                ruta = fbd.SelectedPath;
                pathToExe = Path.Combine(ruta, AppName + ".exe");
            }
        }

        // método para eliminar una carpeta por completo
        private void eliminarCarpeta(string ruta)
        {
            foreach (string ar in Directory.GetFiles(ruta)) System.IO.File.Delete(ar);
            foreach (string ca in Directory.GetDirectories(ruta)) eliminarCarpeta(ca);

            Directory.Delete(ruta);
        }

        public void LogToFile(string message, string level = "INFO")
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
        private static string GetSource()
        {
            string defaultSource = @"C:\ProgramData\SOLIDWORKS\analytics"; // Ruta por defecto
            string source = null; // Inicialmente nulo

            try
            {
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
            catch (Exception e)
            {
                Debug.Print("Error al obtener el valor 'Source' del Registro: " + e.ToString());
            }
            if (string.IsNullOrEmpty(source))
            {
                source = defaultSource;
            }

            return source;
        }
    }
}
