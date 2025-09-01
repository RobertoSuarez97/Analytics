namespace instalador
{
    partial class frminstalador
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frminstalador));
            this.lyrBienvenida = new System.Windows.Forms.Panel();
            this.NotagroupBox = new System.Windows.Forms.GroupBox();
            this.linkNota = new System.Windows.Forms.Label();
            this.labelNota = new System.Windows.Forms.Label();
            this.lblBienbenidaDesc = new System.Windows.Forms.Label();
            this.lblBienvenidaTit = new System.Windows.Forms.Label();
            this.imgXpertAnalitics = new System.Windows.Forms.PictureBox();
            this.lyrAviso = new System.Windows.Forms.Panel();
            this.ckbAviso = new System.Windows.Forms.CheckBox();
            this.lblAvisoTit = new System.Windows.Forms.Label();
            this.lblAvisoSubtit = new System.Windows.Forms.Label();
            this.rtbTerminos = new System.Windows.Forms.RichTextBox();
            this.lyrValidacion = new System.Windows.Forms.Panel();
            this.lblLikRegistro = new System.Windows.Forms.Label();
            this.lblRegistro = new System.Windows.Forms.Label();
            this.txtContraseña = new System.Windows.Forms.TextBox();
            this.lblContraseña = new System.Windows.Forms.Label();
            this.txtEmailValid = new System.Windows.Forms.TextBox();
            this.lblEmailValid = new System.Windows.Forms.Label();
            this.lblDescValid = new System.Windows.Forms.Label();
            this.lblValidTit = new System.Windows.Forms.Label();
            this.imgXperAnaliticsValid = new System.Windows.Forms.PictureBox();
            this.lyrConfigUsuario = new System.Windows.Forms.Panel();
            this.btncarpeta = new System.Windows.Forms.Button();
            this.txtruta = new System.Windows.Forms.TextBox();
            this.lblRutaInstalacion = new System.Windows.Forms.Label();
            this.txtDevice = new System.Windows.Forms.TextBox();
            this.lblDevice = new System.Windows.Forms.Label();
            this.cbInstitucion = new System.Windows.Forms.ComboBox();
            this.lblInstitucion = new System.Windows.Forms.Label();
            this.lblConfigTitulo = new System.Windows.Forms.Label();
            this.imgXpermeAnaliticsConfig = new System.Windows.Forms.PictureBox();
            this.lyrSeleccion = new System.Windows.Forms.Panel();
            this.lbldescConfig = new System.Windows.Forms.Label();
            this.lblConfigSoft = new System.Windows.Forms.Label();
            this.lyrInstalacion = new System.Windows.Forms.Panel();
            this.lbprogreso = new System.Windows.Forms.Label();
            this.pbinstalacion = new System.Windows.Forms.ProgressBar();
            this.lyrConfirmacion = new System.Windows.Forms.Panel();
            this.lblBienvenidaConfirmacion = new System.Windows.Forms.Label();
            this.lblConfirmacionTexto = new System.Windows.Forms.Label();
            this.lblConfirmacionTitulo = new System.Windows.Forms.Label();
            this.pnbotones = new System.Windows.Forms.Panel();
            this.btnatras = new System.Windows.Forms.Button();
            this.btnsiguiente = new System.Windows.Forms.Button();
            this.CopyRightLabel = new System.Windows.Forms.Label();
            this.lyrBienvenida.SuspendLayout();
            this.NotagroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.imgXpertAnalitics)).BeginInit();
            this.lyrAviso.SuspendLayout();
            this.lyrValidacion.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.imgXperAnaliticsValid)).BeginInit();
            this.lyrConfigUsuario.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.imgXpermeAnaliticsConfig)).BeginInit();
            this.lyrSeleccion.SuspendLayout();
            this.lyrInstalacion.SuspendLayout();
            this.lyrConfirmacion.SuspendLayout();
            this.pnbotones.SuspendLayout();
            this.SuspendLayout();
            // 
            // lyrBienvenida
            // 
            this.lyrBienvenida.Controls.Add(this.NotagroupBox);
            this.lyrBienvenida.Controls.Add(this.lblBienbenidaDesc);
            this.lyrBienvenida.Controls.Add(this.lblBienvenidaTit);
            this.lyrBienvenida.Controls.Add(this.imgXpertAnalitics);
            this.lyrBienvenida.Location = new System.Drawing.Point(0, 0);
            this.lyrBienvenida.Name = "lyrBienvenida";
            this.lyrBienvenida.Size = new System.Drawing.Size(800, 428);
            this.lyrBienvenida.TabIndex = 1;
            // 
            // NotagroupBox
            // 
            this.NotagroupBox.Controls.Add(this.linkNota);
            this.NotagroupBox.Controls.Add(this.labelNota);
            this.NotagroupBox.Location = new System.Drawing.Point(400, 280);
            this.NotagroupBox.Name = "NotagroupBox";
            this.NotagroupBox.Size = new System.Drawing.Size(380, 80);
            this.NotagroupBox.TabIndex = 4;
            this.NotagroupBox.TabStop = false;
            this.NotagroupBox.Text = "Nota";
            // 
            // linkNota
            // 
            this.linkNota.AutoSize = true;
            this.linkNota.Cursor = System.Windows.Forms.Cursors.Hand;
            this.linkNota.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.linkNota.ForeColor = System.Drawing.Color.Blue;
            this.linkNota.Location = new System.Drawing.Point(295, 20);
            this.linkNota.Name = "linkNota";
            this.linkNota.Size = new System.Drawing.Size(43, 16);
            this.linkNota.TabIndex = 1;
            this.linkNota.Text = "video";
            this.linkNota.Click += new System.EventHandler(this.lblLikRegistro_Click);
            // 
            // labelNota
            // 
            this.labelNota.Location = new System.Drawing.Point(10, 20);
            this.labelNota.Name = "labelNota";
            this.labelNota.Size = new System.Drawing.Size(285, 50);
            this.labelNota.TabIndex = 0;
            this.labelNota.Text = "Si tienes dudas en la instalación revisa este\r\nó envíanos un correo a soporte@xpertme.com";
            // 
            // lblBienbenidaDesc
            // 
            this.lblBienbenidaDesc.Location = new System.Drawing.Point(400, 140);
            this.lblBienbenidaDesc.Name = "lblBienbenidaDesc";
            this.lblBienbenidaDesc.Size = new System.Drawing.Size(380, 120);
            this.lblBienbenidaDesc.TabIndex = 3;
            this.lblBienbenidaDesc.Text = "Xpertme Analytics es una solución para conocer el uso real de cada licencia de SolidWorks proporcionando un reporte preciso y automático.\r\n\r\nCon Xpertme Analytics IA, obtendrás recomendaciones detalladas que ayudan a mejorar el desempeño en el uso de SolidWorks y potenciar tus habilidades de diseño.";
            // 
            // lblBienvenidaTit
            // 
            this.lblBienvenidaTit.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold);
            this.lblBienvenidaTit.Location = new System.Drawing.Point(400, 50);
            this.lblBienvenidaTit.Name = "lblBienvenidaTit";
            this.lblBienvenidaTit.Size = new System.Drawing.Size(380, 70);
            this.lblBienvenidaTit.TabIndex = 2;
            this.lblBienvenidaTit.Text = "¡Bienvenido al instalador de\r\nXpertme Analytics!";
            // 
            // imgXpertAnalitics
            // 
            this.imgXpertAnalitics.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("Banner")));
            this.imgXpertAnalitics.Location = new System.Drawing.Point(20, 20);
            this.imgXpertAnalitics.Name = "imgXpertAnalitics";
            this.imgXpertAnalitics.Size = new System.Drawing.Size(350, 408);
            this.imgXpertAnalitics.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.imgXpertAnalitics.TabIndex = 0;
            this.imgXpertAnalitics.TabStop = false;
            // 
            // lyrAviso
            // 
            this.lyrAviso.Controls.Add(this.ckbAviso);
            this.lyrAviso.Controls.Add(this.lblAvisoTit);
            this.lyrAviso.Controls.Add(this.lblAvisoSubtit);
            this.lyrAviso.Controls.Add(this.rtbTerminos);
            this.lyrAviso.Location = new System.Drawing.Point(0, 0);
            this.lyrAviso.Name = "lyrAviso";
            this.lyrAviso.Size = new System.Drawing.Size(800, 440);
            this.lyrAviso.TabIndex = 2;
            // 
            // ckbAviso
            // 
            this.ckbAviso.AutoSize = true;
            this.ckbAviso.Location = new System.Drawing.Point(20, 410);
            this.ckbAviso.Name = "ckbAviso";
            this.ckbAviso.Size = new System.Drawing.Size(200, 20);
            this.ckbAviso.TabIndex = 1;
            this.ckbAviso.Text = "Aceptar términos y condiciones";
            this.ckbAviso.UseVisualStyleBackColor = true;
            this.ckbAviso.CheckedChanged += new System.EventHandler(this.ckbAviso_CheckedChanged);
            // 
            // lblAvisoTit
            // 
            //this.lblAvisoTit.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold);
            //this.lblAvisoTit.Location = new System.Drawing.Point(20, 70);
            //this.lblAvisoTit.Name = "lblAvisoTit";
            //this.lblAvisoTit.Size = new System.Drawing.Size(760, 43);
            //this.lblAvisoTit.TabIndex = 3;
            //this.lblAvisoTit.Text = "TÉRMINOS Y CONDICIONES DE USO DE XPERTME ANALYTICS";
            //this.lblAvisoTit.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // lblAvisoSubtit
            // 
            this.lblAvisoSubtit.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.lblAvisoSubtit.Location = new System.Drawing.Point(20, 20);
            this.lblAvisoSubtit.Name = "lblAvisoSubtit";
            this.lblAvisoSubtit.Size = new System.Drawing.Size(760, 70);
            this.lblAvisoSubtit.TabIndex = 2;
            this.lblAvisoSubtit.Text = "¡Estás a un paso de integrarte a la comunidad más grande de aprendizaje especializado en la industria!";
            this.lblAvisoSubtit.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // rtbTerminos
            // 
            this.rtbTerminos.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtbTerminos.Location = new System.Drawing.Point(20, 90);
            this.rtbTerminos.Name = "rtbTerminos";
            this.rtbTerminos.ReadOnly = true;
            this.rtbTerminos.Size = new System.Drawing.Size(760, 300);
            this.rtbTerminos.TabIndex = 0;
            this.rtbTerminos.Text = resources.GetString("rtbTerminos.Text");
            // 
            // lyrValidacion
            // 
            this.lyrValidacion.Controls.Add(this.lblLikRegistro);
            this.lyrValidacion.Controls.Add(this.lblRegistro);
            this.lyrValidacion.Controls.Add(this.txtContraseña);
            this.lyrValidacion.Controls.Add(this.lblContraseña);
            this.lyrValidacion.Controls.Add(this.txtEmailValid);
            this.lyrValidacion.Controls.Add(this.lblEmailValid);
            this.lyrValidacion.Controls.Add(this.lblDescValid);
            this.lyrValidacion.Controls.Add(this.lblValidTit);
            this.lyrValidacion.Controls.Add(this.imgXperAnaliticsValid);
            this.lyrValidacion.Location = new System.Drawing.Point(0, 0);
            this.lyrValidacion.Name = "lyrValidacion";
            this.lyrValidacion.Size = new System.Drawing.Size(800, 428);
            this.lyrValidacion.TabIndex = 3;
            // 
            // lblValidTit
            // 
            this.lblValidTit.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblValidTit.Location = new System.Drawing.Point(400, 20);
            this.lblValidTit.Name = "lblValidTit";
            this.lblValidTit.Size = new System.Drawing.Size(350, 29);
            this.lblValidTit.TabIndex = 8;
            this.lblValidTit.Text = "Identifícate";
            // 
            // lblLikRegistro
            // 
            this.lblLikRegistro.AutoSize = true;
            this.lblLikRegistro.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblLikRegistro.ForeColor = System.Drawing.Color.Blue;
            this.lblLikRegistro.Location = new System.Drawing.Point(400, 380);
            this.lblLikRegistro.Name = "lblLikRegistro";
            this.lblLikRegistro.Size = new System.Drawing.Size(101, 16);
            this.lblLikRegistro.TabIndex = 7;
            this.lblLikRegistro.Text = "Regístrate aquí.";
            this.lblLikRegistro.Click += new System.EventHandler(this.lblLikRegistro_Click);
            // 
            // lblRegistro
            // 
            this.lblRegistro.AutoSize = true;
            this.lblRegistro.Location = new System.Drawing.Point(400, 290);
            this.lblRegistro.Name = "lblRegistro";
            this.lblRegistro.Size = new System.Drawing.Size(330, 64);
            this.lblRegistro.TabIndex = 6;
            this.lblRegistro.Text = "Si ya habías estado registrado previamente en la\r\nplataforma xpertme.com automáticamente reconoceremos\r\ntu correo, si eres usuario nuevo, crearemos una cuenta y\r\nal ingresar por primera vez te pediremos completes algunos\r\ndatos de tu registro";
            // 
            // txtContraseña
            // 
            this.txtContraseña.Location = new System.Drawing.Point(400, 250);
            this.txtContraseña.Name = "txtContraseña";
            this.txtContraseña.Size = new System.Drawing.Size(351, 22);
            this.txtContraseña.TabIndex = 5;
            this.txtContraseña.UseSystemPasswordChar = true;
            // 
            // lblContraseña
            // 
            this.lblContraseña.AutoSize = true;
            this.lblContraseña.Location = new System.Drawing.Point(400, 224);
            this.lblContraseña.Name = "lblContraseña";
            this.lblContraseña.Size = new System.Drawing.Size(79, 16);
            this.lblContraseña.TabIndex = 4;
            this.lblContraseña.Text = "Contraseña:";
            // 
            // txtEmailValid
            // 
            this.txtEmailValid.Location = new System.Drawing.Point(400, 191);
            this.txtEmailValid.Name = "txtEmailValid";
            this.txtEmailValid.Size = new System.Drawing.Size(351, 22);
            this.txtEmailValid.TabIndex = 3;
            // 
            // lblEmailValid
            // 
            this.lblEmailValid.AutoSize = true;
            this.lblEmailValid.Location = new System.Drawing.Point(400, 165);
            this.lblEmailValid.Name = "lblEmailValid";
            this.lblEmailValid.Size = new System.Drawing.Size(295, 16);
            this.lblEmailValid.TabIndex = 2;
            this.lblEmailValid.Text = "Correo con el que te registraste en la plataforma:";
            // 
            // lblDescValid
            // 
            this.lblDescValid.Location = new System.Drawing.Point(400, 55);
            this.lblDescValid.Name = "lblDescValid";
            this.lblDescValid.Size = new System.Drawing.Size(360, 80);
            this.lblDescValid.TabIndex = 1;
            this.lblDescValid.Text = "Para activar el uso de Xpertme Analytics, si perteneces a una empresa usa tu correo con dominio corporativo, si eres un usuario independiente, usa tu correo particular.";
            // 
            // imgXperAnaliticsValid
            // 
            this.imgXperAnaliticsValid.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("Banner")));
            this.imgXperAnaliticsValid.Location = new System.Drawing.Point(20, 20);
            this.imgXperAnaliticsValid.Name = "imgXperAnaliticsValid";
            this.imgXperAnaliticsValid.Size = new System.Drawing.Size(350, 408);
            this.imgXperAnaliticsValid.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.imgXperAnaliticsValid.TabIndex = 0;
            this.imgXperAnaliticsValid.TabStop = false;
            // 
            // lyrConfigUsuario
            // 
            this.lyrConfigUsuario.Controls.Add(this.btncarpeta);
            this.lyrConfigUsuario.Controls.Add(this.txtruta);
            this.lyrConfigUsuario.Controls.Add(this.lblRutaInstalacion);
            this.lyrConfigUsuario.Controls.Add(this.txtDevice);
            this.lyrConfigUsuario.Controls.Add(this.lblDevice);
            this.lyrConfigUsuario.Controls.Add(this.cbInstitucion);
            this.lyrConfigUsuario.Controls.Add(this.lblInstitucion);
            this.lyrConfigUsuario.Controls.Add(this.lblConfigTitulo);
            this.lyrConfigUsuario.Controls.Add(this.imgXpermeAnaliticsConfig);
            this.lyrConfigUsuario.Location = new System.Drawing.Point(0, 0);
            this.lyrConfigUsuario.Name = "lyrConfigUsuario";
            this.lyrConfigUsuario.Size = new System.Drawing.Size(800, 428);
            this.lyrConfigUsuario.TabIndex = 4;
            // 
            // btncarpeta
            // 
            this.btncarpeta.Location = new System.Drawing.Point(640, 320);
            this.btncarpeta.Name = "btncarpeta";
            this.btncarpeta.Size = new System.Drawing.Size(130, 23);
            this.btncarpeta.TabIndex = 8;
            this.btncarpeta.Text = "Seleccionar";
            this.btncarpeta.UseVisualStyleBackColor = true;
            this.btncarpeta.Click += new System.EventHandler(this.btncarpeta_Click);
            // 
            // txtruta
            // 
            this.txtruta.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.txtruta.Location = new System.Drawing.Point(400, 320);
            this.txtruta.Name = "txtruta";
            this.txtruta.Size = new System.Drawing.Size(230, 23);
            this.txtruta.TabIndex = 7;
            // 
            // lblRutaInstalacion
            // 
            this.lblRutaInstalacion.AutoSize = true;
            this.lblRutaInstalacion.Location = new System.Drawing.Point(400, 290);
            this.lblRutaInstalacion.Name = "lblRutaInstalacion";
            this.lblRutaInstalacion.Size = new System.Drawing.Size(161, 16);
            this.lblRutaInstalacion.TabIndex = 6;
            this.lblRutaInstalacion.Text = "Confirma la ruta de instalación";
            // 
            // txtDevice
            // 
            this.txtDevice.Location = new System.Drawing.Point(400, 250);
            this.txtDevice.Name = "txtDevice";
            this.txtDevice.Size = new System.Drawing.Size(350, 22);
            this.txtDevice.TabIndex = 5;
            this.txtDevice.Text = "Nombre";
            // 
            // lblDevice
            // 
            this.lblDevice.AutoSize = true;
            this.lblDevice.Location = new System.Drawing.Point(400, 220);
            this.lblDevice.Name = "lblDevice";
            this.lblDevice.Size = new System.Drawing.Size(262, 16);
            this.lblDevice.TabIndex = 4;
            this.lblDevice.Text = "Etiqueta el equipo en que vas a estar trabajando";
            // 
            // cbInstitucion
            // 
            this.cbInstitucion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbInstitucion.FormattingEnabled = true;
            this.cbInstitucion.Location = new System.Drawing.Point(400, 180);
            this.cbInstitucion.Name = "cbInstitucion";
            this.cbInstitucion.Size = new System.Drawing.Size(350, 24);
            this.cbInstitucion.TabIndex = 3;
            // 
            // lblInstitucion
            // 
            this.lblInstitucion.AutoSize = true;
            this.lblInstitucion.Location = new System.Drawing.Point(400, 150);
            this.lblInstitucion.Name = "lblInstitucion";
            this.lblInstitucion.Size = new System.Drawing.Size(127, 16);
            this.lblInstitucion.TabIndex = 2;
            this.lblInstitucion.Text = "Empresa/Universidad";
            // 
            // lblConfigTitulo
            // 
            this.lblConfigTitulo.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold);
            this.lblConfigTitulo.Location = new System.Drawing.Point(400, 70);
            this.lblConfigTitulo.Name = "lblConfigTitulo";
            this.lblConfigTitulo.Size = new System.Drawing.Size(380, 50);
            this.lblConfigTitulo.TabIndex = 1;
            this.lblConfigTitulo.Text = "Personaliza tu instalación";
            // 
            // imgXpermeAnaliticsConfig
            // 
            this.imgXpermeAnaliticsConfig.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("Banner")));
            this.imgXpermeAnaliticsConfig.Location = new System.Drawing.Point(20, 20);
            this.imgXpermeAnaliticsConfig.Name = "imgXpermeAnaliticsConfig";
            this.imgXpermeAnaliticsConfig.Size = new System.Drawing.Size(350, 408);
            this.imgXpermeAnaliticsConfig.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.imgXpermeAnaliticsConfig.TabIndex = 0;
            this.imgXpermeAnaliticsConfig.TabStop = false;
            // 
            // lyrSeleccion
            // 
            this.lyrSeleccion.Controls.Add(this.lbldescConfig);
            this.lyrSeleccion.Controls.Add(this.lblConfigSoft);
            this.lyrSeleccion.Location = new System.Drawing.Point(0, 0);
            this.lyrSeleccion.Name = "lyrSeleccion";
            this.lyrSeleccion.Size = new System.Drawing.Size(800, 428);
            this.lyrSeleccion.TabIndex = 5;
            // 
            // lbldescConfig
            // 
            this.lbldescConfig.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.lbldescConfig.Location = new System.Drawing.Point(20, 60);
            this.lbldescConfig.Name = "lbldescConfig";
            this.lbldescConfig.Size = new System.Drawing.Size(760, 30);
            this.lbldescConfig.TabIndex = 1;
            this.lbldescConfig.Text = "Selecciona de la lista las tareas que realizarás con mayor frecuencia.";
            // 
            // lblConfigSoft
            // 
            this.lblConfigSoft.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold);
            this.lblConfigSoft.Location = new System.Drawing.Point(20, 20);
            this.lblConfigSoft.Name = "lblConfigSoft";
            this.lblConfigSoft.Size = new System.Drawing.Size(760, 35);
            this.lblConfigSoft.TabIndex = 0;
            this.lblConfigSoft.Text = "Uso de SolidWORKS";
            // 
            // lyrInstalacion
            // 
            this.lyrInstalacion.Controls.Add(this.lbprogreso);
            this.lyrInstalacion.Controls.Add(this.pbinstalacion);
            this.lyrInstalacion.Location = new System.Drawing.Point(0, 0);
            this.lyrInstalacion.Name = "lyrInstalacion";
            this.lyrInstalacion.Size = new System.Drawing.Size(800, 428);
            this.lyrInstalacion.TabIndex = 6;
            // 
            // lbprogreso
            // 
            this.lbprogreso.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F);
            this.lbprogreso.Location = new System.Drawing.Point(20, 180);
            this.lbprogreso.Name = "lbprogreso";
            this.lbprogreso.Size = new System.Drawing.Size(760, 29);
            this.lbprogreso.TabIndex = 1;
            this.lbprogreso.Text = "2 de 15 módulos instalados";
            this.lbprogreso.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pbinstalacion
            // 
            this.pbinstalacion.Location = new System.Drawing.Point(20, 220);
            this.pbinstalacion.Maximum = 140;
            this.pbinstalacion.Name = "pbinstalacion";
            this.pbinstalacion.Size = new System.Drawing.Size(760, 41);
            this.pbinstalacion.TabIndex = 0;
            // 
            // lyrConfirmacion
            // 
            this.lyrConfirmacion.Controls.Add(this.lblBienvenidaConfirmacion);
            this.lyrConfirmacion.Controls.Add(this.lblConfirmacionTexto);
            this.lyrConfirmacion.Controls.Add(this.lblConfirmacionTitulo);
            this.lyrConfirmacion.Location = new System.Drawing.Point(0, 0);
            this.lyrConfirmacion.Name = "lyrConfirmacion";
            this.lyrConfirmacion.Size = new System.Drawing.Size(800, 428);
            this.lyrConfirmacion.TabIndex = 7;
            // 
            // lblBienvenidaConfirmacion
            // 
            this.lblBienvenidaConfirmacion.Location = new System.Drawing.Point(20, 260);
            this.lblBienvenidaConfirmacion.Name = "lblBienvenidaConfirmacion";
            this.lblBienvenidaConfirmacion.Size = new System.Drawing.Size(760, 50);
            this.lblBienvenidaConfirmacion.TabIndex = 3;
            this.lblBienvenidaConfirmacion.Text = "Bienvenido a la comunidad Xpertme Analytics!";
            this.lblBienvenidaConfirmacion.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblConfirmacionTexto
            // 
            this.lblConfirmacionTexto.Location = new System.Drawing.Point(20, 120);
            this.lblConfirmacionTexto.Name = "lblConfirmacionTexto";
            this.lblConfirmacionTexto.Size = new System.Drawing.Size(760, 100);
            this.lblConfirmacionTexto.TabIndex = 2;
            this.lblConfirmacionTexto.Text = "Para conocer más acerca de esta solución te invitamos a que revises el siguiente video, que te dará un breve tour sobre nuestra solución.";
            this.lblConfirmacionTexto.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblConfirmacionTitulo
            // 
            this.lblConfirmacionTitulo.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold);
            this.lblConfirmacionTitulo.Location = new System.Drawing.Point(20, 50);
            this.lblConfirmacionTitulo.Name = "lblConfirmacionTitulo";
            this.lblConfirmacionTitulo.Size = new System.Drawing.Size(760, 50);
            this.lblConfirmacionTitulo.TabIndex = 1;
            this.lblConfirmacionTitulo.Text = "¡Felicidades!";
            this.lblConfirmacionTitulo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pnbotones
            // 
            this.pnbotones.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.pnbotones.Controls.Add(this.btnatras);
            this.pnbotones.Controls.Add(this.btnsiguiente);
            this.pnbotones.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnbotones.Location = new System.Drawing.Point(0, 428);
            this.pnbotones.Name = "pnbotones";
            this.pnbotones.Size = new System.Drawing.Size(800, 72);
            this.pnbotones.TabIndex = 1;
            // 
            // btnatras
            // 
            this.btnatras.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnatras.Location = new System.Drawing.Point(525, 15);
            this.btnatras.Name = "btnatras";
            this.btnatras.Size = new System.Drawing.Size(120, 42);
            this.btnatras.TabIndex = 1;
            this.btnatras.Text = "&Atrás";
            this.btnatras.UseVisualStyleBackColor = true;
            this.btnatras.Click += new System.EventHandler(this.btnatras_Click);
            // 
            // btnsiguiente
            // 
            this.btnsiguiente.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnsiguiente.Location = new System.Drawing.Point(660, 15);
            this.btnsiguiente.Name = "btnsiguiente";
            this.btnsiguiente.Size = new System.Drawing.Size(120, 42);
            this.btnsiguiente.TabIndex = 0;
            this.btnsiguiente.Text = "Siguiente";
            this.btnsiguiente.UseVisualStyleBackColor = true;
            this.btnsiguiente.Click += new System.EventHandler(this.btnsiguiente_Click);
            // 
            // CopyRightLabel
            // 
            this.CopyRightLabel.AutoSize = true;
            this.CopyRightLabel.Location = new System.Drawing.Point(12, 470);
            this.CopyRightLabel.Name = "CopyRightLabel";
            this.CopyRightLabel.Size = new System.Drawing.Size(223, 16);
            this.CopyRightLabel.TabIndex = 0;
            this.CopyRightLabel.Text = "Copyright © 2000-2025 por Xpertme®";
            // 
            // frminstalador
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.Controls.Add(this.CopyRightLabel);
            this.Controls.Add(this.pnbotones);
            this.Controls.Add(this.lyrBienvenida);
            this.Controls.Add(this.lyrAviso);
            this.Controls.Add(this.lyrValidacion);
            this.Controls.Add(this.lyrConfigUsuario);
            this.Controls.Add(this.lyrSeleccion);
            this.Controls.Add(this.lyrInstalacion);
            this.Controls.Add(this.lyrConfirmacion);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MaximizeBox = false;
            this.Name = "frminstalador";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Cargando....";
            this.btnatras.Enabled = false;
            this.btnsiguiente.Enabled = false;
            this.Load += new System.EventHandler(this.frminstalador_Load);
            this.lyrBienvenida.ResumeLayout(false);
            this.NotagroupBox.ResumeLayout(false);
            this.NotagroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.imgXpertAnalitics)).EndInit();
            this.lyrAviso.ResumeLayout(false);
            this.lyrAviso.PerformLayout();
            this.lyrValidacion.ResumeLayout(false);
            this.lyrValidacion.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.imgXperAnaliticsValid)).EndInit();
            this.lyrConfigUsuario.ResumeLayout(false);
            this.lyrConfigUsuario.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.imgXpermeAnaliticsConfig)).EndInit();
            this.lyrSeleccion.ResumeLayout(false);
            this.lyrInstalacion.ResumeLayout(false);
            this.lyrInstalacion.PerformLayout();
            this.lyrConfirmacion.ResumeLayout(false);
            this.lyrConfirmacion.PerformLayout();
            this.pnbotones.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        // Paneles principales
        private System.Windows.Forms.Panel lyrBienvenida;
        private System.Windows.Forms.Panel lyrAviso;
        private System.Windows.Forms.Panel lyrValidacion;
        private System.Windows.Forms.Panel lyrConfigUsuario;
        private System.Windows.Forms.Panel lyrSeleccion;
        private System.Windows.Forms.Panel lyrInstalacion;
        private System.Windows.Forms.Panel lyrConfirmacion;

        // Controles para el panel de Bienvenida
        private System.Windows.Forms.PictureBox imgXpertAnalitics;
        private System.Windows.Forms.Label lblBienvenidaTit;
        private System.Windows.Forms.Label lblBienbenidaDesc;
        private System.Windows.Forms.GroupBox NotagroupBox;
        private System.Windows.Forms.Label labelNota;
        private System.Windows.Forms.Label linkNota;

        // Controles para el panel de Aviso (Términos y Condiciones)
        private System.Windows.Forms.RichTextBox rtbTerminos;
        private System.Windows.Forms.Label lblAvisoTit;
        private System.Windows.Forms.Label lblAvisoSubtit;
        private System.Windows.Forms.CheckBox ckbAviso;

        // Controles para el panel de Validación
        private System.Windows.Forms.PictureBox imgXperAnaliticsValid;
        private System.Windows.Forms.Label lblDescValid;
        private System.Windows.Forms.Label lblEmailValid;
        private System.Windows.Forms.TextBox txtEmailValid;
        private System.Windows.Forms.Label lblContraseña;
        private System.Windows.Forms.TextBox txtContraseña;
        private System.Windows.Forms.Label lblRegistro;
        private System.Windows.Forms.Label lblLikRegistro;
        private System.Windows.Forms.Label lblValidTit;

        // Controles para el panel de Configuración de usuario (unificado con la ruta de instalación)
        private System.Windows.Forms.PictureBox imgXpermeAnaliticsConfig;
        private System.Windows.Forms.Label lblConfigTitulo;
        private System.Windows.Forms.Label lblInstitucion;
        private System.Windows.Forms.ComboBox cbInstitucion;
        private System.Windows.Forms.Label lblDevice;
        private System.Windows.Forms.TextBox txtDevice;
        private System.Windows.Forms.Label lblRutaInstalacion;
        private System.Windows.Forms.TextBox txtruta;
        private System.Windows.Forms.Button btncarpeta;

        // Controles para el panel de Selección de tareas
        private System.Windows.Forms.Label lblConfigSoft;
        private System.Windows.Forms.Label lbldescConfig;

        // Controles para el panel de Instalación
        private System.Windows.Forms.Label lbprogreso;
        private System.Windows.Forms.ProgressBar pbinstalacion;

        // Controles para el panel de Confirmación
        private System.Windows.Forms.Label lblConfirmacionTitulo;
        private System.Windows.Forms.Label lblConfirmacionTexto;
        private System.Windows.Forms.Label lblBienvenidaConfirmacion;

        // Controles de botones y copyright
        private System.Windows.Forms.Label CopyRightLabel;
        private System.Windows.Forms.Panel pnbotones;
        private System.Windows.Forms.Button btnatras;
        private System.Windows.Forms.Button btnsiguiente;
    }
}