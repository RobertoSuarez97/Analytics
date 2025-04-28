namespace analytics_AddIn
{
    partial class update
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(update));
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
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F);
            this.label1.Location = new System.Drawing.Point(12, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(399, 39);
            this.label1.TabIndex = 0;
            this.label1.Text = "Nueva versión disponible";
            // 
            // lbversion
            // 
            this.lbversion.AutoSize = true;
            this.lbversion.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.lbversion.Location = new System.Drawing.Point(23, 111);
            this.lbversion.Name = "lbversion";
            this.lbversion.Size = new System.Drawing.Size(71, 20);
            this.lbversion.TabIndex = 1;
            this.lbversion.Text = "Versión:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label2.Location = new System.Drawing.Point(23, 140);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 20);
            this.label2.TabIndex = 2;
            this.label2.Text = "Cambios:";
            // 
            // rtbcambios
            // 
            this.rtbcambios.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.rtbcambios.Location = new System.Drawing.Point(27, 177);
            this.rtbcambios.Name = "rtbcambios";
            this.rtbcambios.ReadOnly = true;
            this.rtbcambios.Size = new System.Drawing.Size(268, 346);
            this.rtbcambios.TabIndex = 3;
            this.rtbcambios.Text = "";
            // 
            // btnactualizar
            // 
            this.btnactualizar.Location = new System.Drawing.Point(398, 564);
            this.btnactualizar.Name = "btnactualizar";
            this.btnactualizar.Size = new System.Drawing.Size(231, 46);
            this.btnactualizar.TabIndex = 4;
            this.btnactualizar.Text = "Actualizar";
            this.btnactualizar.UseVisualStyleBackColor = true;
            this.btnactualizar.Click += new System.EventHandler(this.btnactualizar_Click);
            // 
            // lblVersionActual
            // 
            this.lblVersionActual.AutoSize = true;
            this.lblVersionActual.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.lblVersionActual.Location = new System.Drawing.Point(23, 76);
            this.lblVersionActual.Name = "lblVersionActual";
            this.lblVersionActual.Size = new System.Drawing.Size(126, 20);
            this.lblVersionActual.TabIndex = 5;
            this.lblVersionActual.Text = "Versión actual: ";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(27, 529);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(638, 25);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar1.TabIndex = 6;
            this.progressBar1.Visible = false;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBox1.BackgroundImage")));
            this.pictureBox1.Location = new System.Drawing.Point(301, 177);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(364, 346);
            this.pictureBox1.TabIndex = 7;
            this.pictureBox1.TabStop = false;
            // 
            // update
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(692, 624);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.lblVersionActual);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.btnactualizar);
            this.Controls.Add(this.rtbcambios);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lbversion);
            this.Controls.Add(this.label1);
            this.Name = "update";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
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