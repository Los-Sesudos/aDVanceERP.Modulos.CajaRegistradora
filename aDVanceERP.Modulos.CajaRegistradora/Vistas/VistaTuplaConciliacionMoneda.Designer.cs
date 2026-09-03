using Guna.UI2.WinForms;

using System.ComponentModel;

namespace aDVanceERP.Modulos.CajaRegistradora.Vistas {
    partial class VistaTuplaConciliacionMoneda {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            ComponentResourceManager resources = new ComponentResourceManager(typeof(VistaTuplaConciliacionMoneda));
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            layoutBase = new TableLayoutPanel();
            layoutVista = new TableLayoutPanel();
            fieldDiferencia = new Label();
            fieldMontoDeclarado = new Guna2TextBox();
            fieldMontoCalculado = new Label();
            fieldTituloMonedaCanal = new Label();
            separador1 = new Guna2Separator();
            layoutBase.SuspendLayout();
            layoutVista.SuspendLayout();
            SuspendLayout();
            // 
            // layoutBase
            // 
            layoutBase.BackColor = Color.White;
            layoutBase.ColumnCount = 1;
            layoutBase.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutBase.Controls.Add(layoutVista, 0, 0);
            layoutBase.Controls.Add(separador1, 0, 1);
            layoutBase.Dock = DockStyle.Fill;
            layoutBase.Location = new Point(0, 0);
            layoutBase.Name = "layoutBase";
            layoutBase.RowCount = 2;
            layoutBase.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutBase.RowStyles.Add(new RowStyle(SizeType.Absolute, 5F));
            layoutBase.Size = new Size(633, 47);
            layoutBase.TabIndex = 1;
            // 
            // layoutVista
            // 
            layoutVista.ColumnCount = 4;
            layoutVista.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            layoutVista.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layoutVista.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layoutVista.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            layoutVista.Controls.Add(fieldDiferencia, 3, 0);
            layoutVista.Controls.Add(fieldMontoDeclarado, 2, 0);
            layoutVista.Controls.Add(fieldMontoCalculado, 1, 0);
            layoutVista.Controls.Add(fieldTituloMonedaCanal, 0, 0);
            layoutVista.Dock = DockStyle.Top;
            layoutVista.Location = new Point(0, 0);
            layoutVista.Margin = new Padding(0);
            layoutVista.Name = "layoutVista";
            layoutVista.RowCount = 1;
            layoutVista.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutVista.Size = new Size(633, 42);
            layoutVista.TabIndex = 74;
            // 
            // fieldDiferencia
            // 
            fieldDiferencia.Dock = DockStyle.Fill;
            fieldDiferencia.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
            fieldDiferencia.ForeColor = Color.FromArgb(  64,   64,   64);
            fieldDiferencia.ImeMode = ImeMode.NoControl;
            fieldDiferencia.Location = new Point(513, 1);
            fieldDiferencia.Margin = new Padding(1);
            fieldDiferencia.Name = "fieldDiferencia";
            fieldDiferencia.Size = new Size(119, 40);
            fieldDiferencia.TabIndex = 40;
            fieldDiferencia.Text = "$ 0,00";
            fieldDiferencia.TextAlign = ContentAlignment.MiddleRight;
            // 
            // fieldMontoDeclarado
            // 
            fieldMontoDeclarado.Animated = true;
            fieldMontoDeclarado.AutoRoundedCorners = true;
            fieldMontoDeclarado.BorderColor = Color.Gainsboro;
            fieldMontoDeclarado.BorderRadius = 15;
            fieldMontoDeclarado.Cursor = Cursors.IBeam;
            fieldMontoDeclarado.CustomizableEdges = customizableEdges1;
            fieldMontoDeclarado.DefaultText = "";
            fieldMontoDeclarado.DisabledState.BorderColor = Color.White;
            fieldMontoDeclarado.DisabledState.ForeColor = Color.DimGray;
            fieldMontoDeclarado.DisabledState.PlaceholderForeColor = Color.DimGray;
            fieldMontoDeclarado.Dock = DockStyle.Fill;
            fieldMontoDeclarado.FocusedState.BorderColor = Color.SandyBrown;
            fieldMontoDeclarado.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
            fieldMontoDeclarado.ForeColor = Color.Black;
            fieldMontoDeclarado.HoverState.BorderColor = Color.SandyBrown;
            fieldMontoDeclarado.IconLeftOffset = new Point(10, 0);
            fieldMontoDeclarado.IconRight = (Image) resources.GetObject("fieldMontoDeclarado.IconRight");
            fieldMontoDeclarado.IconRightOffset = new Point(6, 0);
            fieldMontoDeclarado.IconRightSize = new Size(12, 12);
            fieldMontoDeclarado.Location = new Point(356, 5);
            fieldMontoDeclarado.Margin = new Padding(20, 5, 5, 5);
            fieldMontoDeclarado.Name = "fieldMontoDeclarado";
            fieldMontoDeclarado.PasswordChar = '\0';
            fieldMontoDeclarado.PlaceholderForeColor = Color.DimGray;
            fieldMontoDeclarado.PlaceholderText = "0,00";
            fieldMontoDeclarado.ReadOnly = true;
            fieldMontoDeclarado.SelectedText = "";
            fieldMontoDeclarado.ShadowDecoration.CustomizableEdges = customizableEdges2;
            fieldMontoDeclarado.Size = new Size(151, 32);
            fieldMontoDeclarado.TabIndex = 39;
            fieldMontoDeclarado.TextAlign = HorizontalAlignment.Right;
            fieldMontoDeclarado.TextOffset = new Point(5, 0);
            // 
            // fieldMontoCalculado
            // 
            fieldMontoCalculado.Dock = DockStyle.Fill;
            fieldMontoCalculado.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
            fieldMontoCalculado.ForeColor = Color.FromArgb(  64,   64,   64);
            fieldMontoCalculado.ImeMode = ImeMode.NoControl;
            fieldMontoCalculado.Location = new Point(161, 1);
            fieldMontoCalculado.Margin = new Padding(1);
            fieldMontoCalculado.Name = "fieldMontoCalculado";
            fieldMontoCalculado.Size = new Size(174, 40);
            fieldMontoCalculado.TabIndex = 28;
            fieldMontoCalculado.Text = "$ 0,00";
            fieldMontoCalculado.TextAlign = ContentAlignment.MiddleRight;
            // 
            // fieldTituloMonedaCanal
            // 
            fieldTituloMonedaCanal.Dock = DockStyle.Fill;
            fieldTituloMonedaCanal.Font = new Font("Segoe UI", 11.25F);
            fieldTituloMonedaCanal.ForeColor = Color.DimGray;
            fieldTituloMonedaCanal.ImageAlign = ContentAlignment.MiddleLeft;
            fieldTituloMonedaCanal.ImeMode = ImeMode.NoControl;
            fieldTituloMonedaCanal.Location = new Point(15, 5);
            fieldTituloMonedaCanal.Margin = new Padding(15, 5, 3, 3);
            fieldTituloMonedaCanal.Name = "fieldTituloMonedaCanal";
            fieldTituloMonedaCanal.Size = new Size(142, 34);
            fieldTituloMonedaCanal.TabIndex = 27;
            fieldTituloMonedaCanal.Text = "CUP (Transferencia)";
            fieldTituloMonedaCanal.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // separador1
            // 
            separador1.Dock = DockStyle.Fill;
            separador1.FillColor = Color.Gainsboro;
            separador1.Location = new Point(1, 43);
            separador1.Margin = new Padding(1);
            separador1.Name = "separador1";
            separador1.Size = new Size(631, 3);
            separador1.TabIndex = 73;
            // 
            // VistaTuplaConciliacionMoneda
            // 
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.White;
            ClientSize = new Size(633, 47);
            Controls.Add(layoutBase);
            Font = new Font("Segoe UI", 10.8F);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(4, 5, 4, 5);
            Name = "VistaTuplaConciliacionMoneda";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "VistaTuplaCajaRegistradora";
            layoutBase.ResumeLayout(false);
            layoutVista.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Guna2BorderlessForm formatoBase;
        private TableLayoutPanel layoutBase;
        private Guna2Separator separador1;
        private TableLayoutPanel layoutVista;
        private Label fieldDiferencia;
        private Guna2TextBox fieldMontoDeclarado;
        private Label fieldMontoCalculado;
        private Label fieldTituloMonedaCanal;
    }
}