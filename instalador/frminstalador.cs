using IWshRuntimeLibrary;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace instalador
{
    public partial class frminstalador : Form
    {
        private const string AppName = "analytics";
        private static readonly HttpClient client = new HttpClient();
        WebClient wc = new WebClient();
        List<Panel> paginas = new List<Panel>();
        int indice = 0;
        string ruta = "", pathToExe = "", temp = "";
        string VERSION = "", AssemblyVERSION = "", ARCHIVO = "", URL = "", LINK = "";
        string email = "", password = "", UserID = "", deviceID = "", deviceAlias = "", deviceName = "", code = "", osVersion = "", deviceMAC = "", swActivies = "", campus = "";
        string mode = "prod";
        string DllAddIn = "analytics_AddIn";
        private Dictionary<string, int> customerMapping = new Dictionary<string, int>();
        private int selectedCustomerID = 0;
        private string LogFilePath = GetSource() + "\\" + AppName + "\\Instalador_log.txt";

        public frminstalador()
        {
            indice = 0;            
            InitializeComponent();
            this.Load += frminstalador_Load;
        }

        private async void frminstalador_Load(object sender, EventArgs e)
        {
            this.rtbTerminos.SelectAll();
            this.rtbTerminos.SelectionAlignment = HorizontalAlignment.Center;
            this.rtbTerminos.Select(0, 0);
            await Task.Delay(100);
            await setValoresIniciales();
        }

        //------------------ LÓGICA PRINCIPAL ------------------//
        private async Task setValoresIniciales()
        {
            getAssemblyVersion();
            await getVersion();

            ARCHIVO = AppName + "-v" + AssemblyVERSION + ".zip";
            URL = this.LINK;

            paginas.Add(lyrBienvenida);
            paginas.Add(lyrAviso);
            paginas.Add(lyrValidacion);
            paginas.Add(lyrConfigUsuario);
            paginas.Add(lyrSeleccion);
            paginas.Add(lyrInstalacion);
            paginas.Add(lyrConfirmacion);

            indice = 0;
            pagina();

            ruta = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            txtruta.Text = Path.Combine(ruta, AppName);

            wc.DownloadProgressChanged += (s, e) =>
            {
                pbinstalacion.Value = e.ProgressPercentage;
                lbprogreso.Text = $"Descargando archivos ({e.ProgressPercentage}%)";
            };

            wc.DownloadFileCompleted += async (s, e) =>
            {
                if (!e.Cancelled)
                {
                    string carpeta = txtruta.Text;
                    if (Directory.Exists(carpeta)) eliminarCarpeta(carpeta);
                    Directory.CreateDirectory(carpeta);
                    ZipFile.ExtractToDirectory(temp, carpeta);

                    string dllPath = Path.Combine(carpeta, "Dll", DllAddIn + ".dll");
                    if (System.IO.File.Exists(dllPath))
                    {
                        RegisterDLL(dllPath);
                    }

                    lbprogreso.Text = "Registro y finalización...";
                    pbinstalacion.Value = pbinstalacion.Maximum;

                    registerUserRegedit();

                    await Task.Delay(1000);
                    indice = paginas.IndexOf(lyrConfirmacion);
                    pagina();
                }
            };
        }

        private void pagina()
        {
            paginas[indice].BringToFront();
            switch (indice)
            {
                case 0:
                    btnatras.Enabled = true;
                    btnsiguiente.Enabled = true;
                    btnsiguiente.Text = "Siguiente";
                    btnatras.Text = "Cancelar";
                    break;
                case 1:
                    ckbAviso.Checked = false;
                    btnatras.Enabled = true;
                    btnsiguiente.Enabled = false;
                    btnatras.Text = "Atras";
                    btnsiguiente.Text = "Siguiente";
                    break;
                case 2:
                    btnatras.Enabled = true;
                    btnsiguiente.Enabled = true;
                    btnsiguiente.Text = "Siguiente";
                    break;
                case 3:
                    btnatras.Enabled = true;
                    btnsiguiente.Enabled = true;
                    btnsiguiente.Text = "Siguiente";
                    break;
                case 4:
                    btnatras.Enabled = true;
                    btnsiguiente.Enabled = true;
                    btnsiguiente.Text = "Instalar";
                    break;
                case 5:
                    btnatras.Enabled = true;
                    btnsiguiente.Text = "Instalando...";
                    btnsiguiente.Enabled = false;
                    btnatras.Text = "Cancelar";
                    break;
                case 6:
                    btnatras.Enabled = false;
                    btnsiguiente.Text = "Cerrar";
                    btnsiguiente.Enabled = true;
                    break;
                default:
                    break;
            }
        }

        public static async Task<string> createDevice(string json)
        {
            string token = await GetToken("", "", true);
            string analyticsUrl = "https://api-ncsw.xpertme.com/api/createDevice";

            var request = new HttpRequestMessage(HttpMethod.Post, analyticsUrl);
            request.Headers.Add("authorization", $"{token}");

            var requestBody = json;
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject jsonResponse = JObject.Parse(responseBody);
                string insertId = jsonResponse["insertId"]?.ToString() ?? null;

                return insertId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener los datos--: {ex.Message}");
                return null;
            }
        }

        private async void btnsiguiente_Click(object sender, EventArgs e)
        {
            if (indice == 1 && !ckbAviso.Checked) return;

            if (indice == 2)
            {
                txtDevice.Text = Environment.MachineName;
                if (!await getUserData()) { return; }
            }

            if (indice == 3)
            {
                email = txtEmailValid.Text;
                deviceAlias = txtDevice.Text;
                code = selectedCustomerID.ToString();

                GenerarAcordeonDeActividades();
            }

            if (indice == 4) // Capturamos las actividades seleccionadas y creamos el dispositivo
            {
                var seleccionados = ObtenerCheckBoxesSeleccionados();
                string json = JsonConvert.SerializeObject(seleccionados, Formatting.Indented).Replace("\r\n", "");
                swActivies = json;

                if (seleccionados.Count == 0)
                {
                    MessageBox.Show("Debes seleccionar al menos una actividad.", "Actividades requeridas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return; // Detenemos la ejecución para que el usuario pueda seleccionar una actividad
                }


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
                    osVersion = osVersion,
                    swActivies = swActivies,
                    Campus = campus,
                    Career = "null"
                };
                string jsonString = JsonConvert.SerializeObject(requestBodyData);
                deviceID = await createDevice(jsonString);
            }

            if (btnsiguiente.Text == "Instalar")
            {
                temp = Path.Combine(Path.GetTempPath(), AppName);
                if (Directory.Exists(temp)) eliminarCarpeta(temp);
                Directory.CreateDirectory(temp);
                temp = Path.Combine(temp, ARCHIVO);

                lbprogreso.Text = "Descargando archivos...";
                btnsiguiente.Enabled = false;
                btnatras.Text = "Cancelar";

                wc.DownloadFileAsync(new Uri(URL), temp);

                indice++;
                pagina();
                return;
            }

            if (btnsiguiente.Text == "Cerrar")
            {
                Environment.Exit(0);
            }

            indice++;
            pagina();
        }

        private void btnatras_Click(object sender, EventArgs e)
        {
            if (indice == 0)
            {
                var d = MessageBox.Show("¿Seguro que quieres salir?", "Instalación", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk);
                if (d == DialogResult.Yes)
                {
                    Environment.Exit(0);
                    return;
                }
                else
                {
                    indice = 0;
                    pagina();
                    return;
                }
            }

            if (btnatras.Text == "Cancelar" && indice != 0)
            {
                var d = MessageBox.Show("¿Seguro que quieres salir?", "Instalación", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk);
                if (d == DialogResult.Yes)
                {
                    if (wc.IsBusy)
                    {
                        wc.CancelAsync();                        
                    }
                    Environment.Exit(0);
                    return;
                }
                else
                {
                    indice = 5;
                    pagina();
                    return;
                }
            }
            if (indice == paginas.IndexOf(lyrConfirmacion))
            {
                return;
            }
            --indice;
            pagina();
        }

        private void ckbAviso_CheckedChanged(object sender, EventArgs e)
        {
            btnsiguiente.Enabled = ckbAviso.Checked;
        }

        //------------------ MÉTODOS AUXILIARES ------------------//
        public void registerUserRegedit()
        {
            string Keypath1 = @"SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName";

            try
            {
                RegistryKey key1 = Registry.LocalMachine.CreateSubKey(Keypath1);
                key1.SetValue("CustomerID", code);
                key1.Close();

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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al registrar la DLL: {ex.Message}");
            }
        }

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
                MessageBox.Show("No se encontró tu cuenta. Por favor, verifica tus datos o regístrate.", "Usuario no encontrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            if (status == "-1")
            {
                code = "null";
                campus = "null";

                lblInstitucion.Visible = false;
                cbInstitucion.Visible = false;
                return true;
            }

            if (status == "1")
            {
                lblInstitucion.Visible = true;
                cbInstitucion.Visible = true;
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

                    if (statusCustomer == "0" || string.IsNullOrEmpty(statusCustomer)) return "0";

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
                            customerMapping[customerName] = int.Parse(customerIDStr);
                        }
                    }

                    cbInstitucion.Invoke((MethodInvoker)delegate
                    {
                        cbInstitucion.Items.Clear();
                        cbInstitucion.Items.AddRange(customerNames.ToArray());
                        if (cbInstitucion.Items.Count > 0)
                        {
                            cbInstitucion.SelectedIndex = 0;
                            campus = cbInstitucion.SelectedItem.ToString();
                            selectedCustomerID = customerMapping[campus];
                        }
                        else
                        {
                            campus = null;
                            selectedCustomerID = -1;
                        }
                    });

                    cbInstitucion.SelectedIndexChanged += (s, e) =>
                    {
                        campus = cbInstitucion.SelectedItem.ToString();
                        if (customerMapping.ContainsKey(campus))
                        {
                            selectedCustomerID = customerMapping[campus];
                        }
                    };

                    if (statusCustomer == "-1") return statusCustomer;
                    if (statusCustomer == "1") return "1";

                    return "0";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en ValidUser: {ex.Message}");
                    return "0";
                }
            }
        }

        public static async Task<string> GetToken(string authHeader, string scope, bool useLocalAuth = false)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            string tokenUrl;
            if (useLocalAuth)
            {
                tokenUrl = "https://api-ncsw.xpertme.com/api/auth";
            }
            else
            {
                tokenUrl = "https://api-academy.xpertcad.com/v2/system/oauth/token";
            }

            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
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
                    request.Content = new StringContent("");
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
                        accessToken = jsonObject["token"]?.ToString();
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
        }

        public static async Task<string> getAnalyticsInstallers(string token)
        {
            string analyticsUrl = "https://api-academy.xpertcad.com/v2/analytics/users/getAnalyticsInstallers";
            var request = new HttpRequestMessage(HttpMethod.Post, analyticsUrl);
            request.Headers.Add("Authorization", $"Bearer {token}");
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

        private void lblLikRegistro_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://accounts.xpertme.com/user/registerView",
                UseShellExecute = true
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
                    source = o?.ToString();
                }
            }
            catch (Exception e)
            {
                Debug.Print("Error de obtener usserID!!! " + e.ToString());
            }
            return source;
        }

        public void GenerarAcordeonDeActividades()
        {
            this.lyrSeleccion.Controls.Clear();
            this.lyrSeleccion.Controls.Add(this.lblConfigSoft);
            this.lyrSeleccion.Controls.Add(this.lbldescConfig);

            var scrollContainer = new Panel();
            scrollContainer.Location = new Point(20, 80); // empieza desde Y=80
            scrollContainer.Size = new Size(this.lyrSeleccion.Width - 40, this.lyrSeleccion.Height - 100);
            scrollContainer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; // 🔥 se adapta
            scrollContainer.AutoScroll = true;
            scrollContainer.BackColor = Color.WhiteSmoke;

            this.lyrSeleccion.Controls.Add(scrollContainer);


            var actividades = new Dictionary<string, List<Tuple<string, string>>>
            {
                {"Diseño", new List<Tuple<string, string>>
                    {
                        Tuple.Create("Part Modeling (Modelos de partes)", "Diseño de piezas prismáticas y complejas en 3D"),
                        Tuple.Create("Surface Modeling (Modelado de superficies)", "Diseño avanzado usando superficies abiertas, ideal para geometrías complejas como automotrices o de consumo."),
                        Tuple.Create("Sheet Metal (Chapa Metálica)", "Diseño de piezas en lámina doblada, con herramientas para dobleces, cortes y planos de fabricación."),
                        Tuple.Create("Weldments (Estructuras Soldadas)", "Diseño de estructuras con perfiles estructurales (tubulares, ángulos, etc.), con cortes automáticos y listas de corte."),
                        Tuple.Create("Mold Tools (Diseño de Moldes)", "Creación de cavidades, líneas de partición y herramientas específicas para moldes de inyección."),
                        Tuple.Create("Multibody Part Design", "Permite crear múltiples cuerpos sólidos en una sola pieza para estudios o separación posterior."),
                        Tuple.Create("3D Sketching", "Croquizado en 3D, útil para trayectorias, estruFcturas, tubos y recorridos complejos.")
                    }},
                {"Ensambles", new List<Tuple<string, string>>
                    {
                        Tuple.Create("Modelado ascendente de ensambles", "Modelado de cada parte de forma individual que posteriormente se insertan en un archivo de ensamble."),
                        Tuple.Create("Modelado descendente de ensambles", "Creacion y/o modificacion de piezas dentro del propio entorno del ensamblaje utilizando la geometria de otros componentes como referencia."),
                        Tuple.Create("Modelado de ensambles a partir de un modelo maestro", "Definicion basica de las piezas de un ensamble como solidos dentro de un documento de pieza que posteriormente se convierten y detallan en un ensamble vinculado."),
                        Tuple.Create("Modelado de ensambles a partir de un layout", "Se inicia con un croquis en el ensamble que define el esqueleto del diseño, las posiciones y geometria basica de las piezas que lo componen.")
                    }}
            };

            foreach (var categoria in actividades)
            {
                // Panel contenedor de cada acordeón
                var accordionPanel = new Panel();
                accordionPanel.Dock = DockStyle.Top;
                accordionPanel.AutoSize = true;
                accordionPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;

                // ===== HEADER =====
                var headerPanel = new Panel();
                headerPanel.Dock = DockStyle.Top;
                headerPanel.Height = 40;
                headerPanel.BackColor = System.Drawing.Color.White;
                headerPanel.Padding = new Padding(10, 10, 10, 0);

                var lblCategoria = new Label();
                lblCategoria.Text = categoria.Key;
                lblCategoria.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
                lblCategoria.Dock = DockStyle.Left;
                lblCategoria.Cursor = Cursors.Hand;
                lblCategoria.Click += HeaderLabel_Click;
                headerPanel.Controls.Add(lblCategoria);

                var arrowLabel = new Label();
                arrowLabel.Text = "▼";
                arrowLabel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
                arrowLabel.Dock = DockStyle.Right;
                arrowLabel.TextAlign = ContentAlignment.MiddleRight;
                arrowLabel.Width = 30;
                arrowLabel.Cursor = Cursors.Hand;
                arrowLabel.Click += HeaderLabel_Click;
                headerPanel.Controls.Add(arrowLabel);

                // ===== CONTENT =====
                var contentPanel = new Panel();
                contentPanel.Dock = DockStyle.Top;
                contentPanel.BackColor = System.Drawing.Color.White;
                contentPanel.Padding = new Padding(10);
                contentPanel.Height = 0; // inicia colapsado

                foreach (var actividad in categoria.Value)
                {
                    var checkItemPanel = new Panel();
                    checkItemPanel.Dock = DockStyle.Top;
                    checkItemPanel.Height = 70;
                    checkItemPanel.BackColor = System.Drawing.Color.White;
                    checkItemPanel.Margin = new Padding(0, 0, 0, 5);

                    var chk = new CheckBox();
                    chk.Text = "";
                    chk.Location = new System.Drawing.Point(0, 10);
                    chk.Size = new System.Drawing.Size(30, 20);
                    checkItemPanel.Controls.Add(chk);

                    var lblTitulo = new Label();
                    lblTitulo.Text = actividad.Item1;
                    lblTitulo.Location = new System.Drawing.Point(25, 5);
                    lblTitulo.Size = new System.Drawing.Size(680, 20);
                    lblTitulo.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
                    checkItemPanel.Controls.Add(lblTitulo);

                    var lblDescripcion = new Label();
                    lblDescripcion.Text = actividad.Item2;
                    lblDescripcion.Location = new System.Drawing.Point(25, 30);
                    lblDescripcion.Size = new System.Drawing.Size(680, 35);
                    lblDescripcion.Font = new System.Drawing.Font("Segoe UI", 9F);
                    lblDescripcion.ForeColor = System.Drawing.Color.DimGray;
                    checkItemPanel.Controls.Add(lblDescripcion);

                    contentPanel.Controls.Add(checkItemPanel);
                    contentPanel.Controls.SetChildIndex(checkItemPanel, 0);
                }

                // Guardamos altura real en Tag
                contentPanel.Tag = categoria.Value.Count * 75;

                // Agregar en orden correcto
                accordionPanel.Controls.Add(contentPanel);
                accordionPanel.Controls.Add(headerPanel);

                scrollContainer.Controls.Add(accordionPanel);
                scrollContainer.Controls.SetChildIndex(accordionPanel, 0);
            }
        }

        private void HeaderLabel_Click(object sender, EventArgs e)
        {
            var clickedLabel = sender as Label;
            if (clickedLabel == null) return;

            var headerPanel = clickedLabel.Parent as Panel;
            if (headerPanel == null) return;

            var accordionPanel = headerPanel.Parent as Panel;
            if (accordionPanel == null) return;

            // El contentPanel siempre está debajo dentro del accordionPanel
            var contentPanel = accordionPanel.Controls.OfType<Panel>().FirstOrDefault(p => p != headerPanel);

            if (contentPanel != null)
            {
                if (contentPanel.Height == 0)
                {
                    // expandir
                    int fullHeight = (int)contentPanel.Tag;
                    contentPanel.Height = fullHeight;

                    var arrow = headerPanel.Controls.OfType<Label>()
                                    .FirstOrDefault(l => l.Text == "▼" || l.Text == "▲");
                    if (arrow != null) arrow.Text = "▲";
                }
                else
                {
                    // colapsar
                    contentPanel.Height = 0;

                    var arrow = headerPanel.Controls.OfType<Label>()
                                    .FirstOrDefault(l => l.Text == "▼" || l.Text == "▲");
                    if (arrow != null) arrow.Text = "▼";
                }
            }
        }

        private List<Dictionary<string, string>> ObtenerCheckBoxesSeleccionados()
        {
            var actividadesSeleccionadas = new List<Dictionary<string, string>>();

            foreach (Control control in lyrSeleccion.Controls)
            {
                // Buscar el contenedor scroll (Panel dinámico)
                if (control is Panel scrollContainer)
                {
                    foreach (Control accordionPanel in scrollContainer.Controls)
                    {
                        if (accordionPanel is Panel)
                        {
                            // Buscar el contentPanel (donde están los checkItemPanel)
                            foreach (Control contentPanel in accordionPanel.Controls)
                            {
                                // contentPanel válido
                                if (contentPanel is Panel && contentPanel.Tag != null)
                                {
                                    foreach (Control checkItemPanel in contentPanel.Controls)
                                    {
                                        if (checkItemPanel is Panel)
                                        {
                                            // Buscar el CheckBox
                                            var chk = checkItemPanel.Controls.OfType<CheckBox>().FirstOrDefault();
                                            var lblTitulo = checkItemPanel.Controls.OfType<Label>().FirstOrDefault();

                                            if (chk != null && chk.Checked && lblTitulo != null)
                                            {
                                                actividadesSeleccionadas.Add(new Dictionary<string, string>
                                        {
                                            { "activiti", lblTitulo.Text }
                                        });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return actividadesSeleccionadas;
        }

        public static string GetMacAddress()
        {
            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                var activeInterface = networkInterfaces.FirstOrDefault(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback);
                if (activeInterface != null)
                {
                    return string.Join(":", activeInterface.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));
                }
                return "No se encontró ninguna interfaz de red activa.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener la MAC Address: {ex.Message}");
                return "Error al obtener la MAC Address";
            }
        }

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
            return "ERROR!!";
        }

        private void btncarpeta_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtruta.Text = fbd.SelectedPath;
                ruta = fbd.SelectedPath;
                pathToExe = Path.Combine(ruta, AppName + ".exe");
            }
        }

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
            string defaultSource = @"C:\ProgramData\SOLIDWORKS\analytics";
            string source = null;
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