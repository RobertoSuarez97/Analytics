namespace updates
{
    partial class formUpdate
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(formUpdate));
            this.label1 = new System.Windows.Forms.Label();
            this.lbversion = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.rtbcambios = new System.Windows.Forms.RichTextBox();
            this.btnactualizar = new System.Windows.Forms.Button();
            this.lblVersionActual = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            //
            // label1 (Título principal)
            //
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F); // Tamaño de fuente ligeramente reducido
            this.label1.Location = new System.Drawing.Point(20, 20); // Ajuste ligero de posición
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(502, 49); // El tamaño se ajustará por AutoSize
            this.label1.TabIndex = 0;
            this.label1.Text = "Nueva versión disponible";
            //
            // lblVersionActual (Etiqueta versión actual)
            //
            this.lblVersionActual.AutoSize = true;
            this.lblVersionActual.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.lblVersionActual.Location = new System.Drawing.Point(25, 80); // Subido
            this.lblVersionActual.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblVersionActual.Name = "lblVersionActual";
            this.lblVersionActual.Size = new System.Drawing.Size(201, 31);
            this.lblVersionActual.TabIndex = 5;
            this.lblVersionActual.Text = "Versión actual: ";
            //
            // lbversion (Etiqueta nueva versión)
            //
            this.lbversion.AutoSize = true;
            this.lbversion.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.lbversion.Location = new System.Drawing.Point(25, 120); // Subido
            this.lbversion.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbversion.Name = "lbversion";
            this.lbversion.Size = new System.Drawing.Size(114, 31);
            this.lbversion.TabIndex = 1;
            this.lbversion.Text = "Versión:";
            //
            // label2 (Etiqueta Cambios)
            //
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label2.Location = new System.Drawing.Point(25, 170); // Subido y alineado
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(129, 31);
            this.label2.TabIndex = 2;
            this.label2.Text = "Cambios:";
            //
            // rtbcambios (RichTextBox para cambios)
            //
            this.rtbcambios.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.rtbcambios.Location = new System.Drawing.Point(30, 210); // Subido
            this.rtbcambios.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.rtbcambios.Name = "rtbcambios";
            this.rtbcambios.ReadOnly = true;
            // *** Altura reducida significativamente ***
            this.rtbcambios.Size = new System.Drawing.Size(380, 370);
            this.rtbcambios.TabIndex = 3;
            this.rtbcambios.Text = "";
            //
            // pictureBox1 (Imagen)
            //
            this.pictureBox1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox1.BackgroundImage")));
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom; // Cambiado a Zoom para que quepa
            this.pictureBox1.Location = new System.Drawing.Point(430, 210); // Movido a la derecha y subido
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pictureBox1.Name = "pictureBox1";
            // *** Tamaño ajustado para estar al lado del RichTextBox y con altura reducida ***
            this.pictureBox1.Size = new System.Drawing.Size(400, 370);
            this.pictureBox1.TabIndex = 7;
            this.pictureBox1.TabStop = false;
            //
            // progressBar1 (Barra de progreso)
            //
            // *** Anclada a la izquierda, derecha y abajo ***
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(30, 600); // Posicionada debajo de los controles anteriores
            this.progressBar1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.progressBar1.Name = "progressBar1";
            // *** Ancho ajustado (el anclaje se encargará del ancho final), Alto reducido ***
            this.progressBar1.Size = new System.Drawing.Size(790, 25);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar1.TabIndex = 6;
            this.progressBar1.Visible = false;
            //
            // btnactualizar (Botón Actualizar)
            //
            // *** Anclado a la derecha y abajo ***
            this.btnactualizar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnactualizar.Location = new System.Drawing.Point(630, 640); // Posicionado debajo de la barra, a la derecha
            this.btnactualizar.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnactualizar.Name = "btnactualizar";
            // *** Tamaño del botón reducido ***
            this.btnactualizar.Size = new System.Drawing.Size(200, 45);
            this.btnactualizar.TabIndex = 4;
            this.btnactualizar.Text = "Actualizar";
            this.btnactualizar.UseVisualStyleBackColor = true;
            this.btnactualizar.Click += new System.EventHandler(this.btnactualizar_Click);
            //
            // formUpdate (El formulario principal)
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F); // Esto usualmente se mantiene
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font; // Esto se mantiene
            // *** Tamaño del cliente reducido ***
            this.ClientSize = new System.Drawing.Size(850, 700);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.lblVersionActual);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.btnactualizar);
            this.Controls.Add(this.rtbcambios);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lbversion);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle; // Mantenido
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false; // Mantenido
            this.Name = "formUpdate";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen; // Mantenido
            this.Text = "Actualizaciones";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lbversion;
        private System.Windows.Forms.Label lblVersionActual;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.RichTextBox rtbcambios;
        private System.Windows.Forms.Button btnactualizar;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}
