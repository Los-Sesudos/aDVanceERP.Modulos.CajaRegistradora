using Guna.UI2.WinForms;

using System.ComponentModel;

namespace aDVanceERP.Modulos.CajaRegistradora.Vistas {
    partial class VistaTuplaTurno {
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
            ComponentResourceManager resources = new ComponentResourceManager(typeof(VistaTuplaTurno));
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            layoutBase = new TableLayoutPanel();
            layoutVista = new TableLayoutPanel();
            btnAnularTurno = new Guna2Button();
            fieldEstado = new Guna2Button();
            fieldFechaApertura = new Label();
            fieldUsuarioApertura = new Label();
            fieldCodigo = new Label();
            fieldDiferenciaEfectivo = new Label();
            fieldEfectivoCalculado = new Label();
            fieldEfectivoDeclarado = new Label();
            fieldAlmacen = new Label();
            btnVerDetalleTurno = new Guna2Button();
            layoutBase.SuspendLayout();
            layoutVista.SuspendLayout();
            SuspendLayout();
            // 
            // layoutBase
            // 
            layoutBase.BackColor = Color.Gainsboro;
            layoutBase.ColumnCount = 1;
            layoutBase.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layoutBase.Controls.Add(layoutVista, 0, 0);
            layoutBase.Dock = DockStyle.Fill;
            layoutBase.Location = new Point(0, 0);
            layoutBase.Name = "layoutBase";
            layoutBase.RowCount = 1;
            layoutBase.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layoutBase.Size = new Size(1241, 42);
            layoutBase.TabIndex = 1;
            // 
            // layoutVista
            // 
            layoutVista.BackColor = Color.White;
            layoutVista.ColumnCount = 10;
            layoutVista.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            layoutVista.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layoutVista.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layoutVista.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            layoutVista.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            layoutVista.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            layoutVista.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            layoutVista.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            layoutVista.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            layoutVista.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            layoutVista.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            layoutVista.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            layoutVista.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            layoutVista.Controls.Add(btnAnularTurno, 9, 0);
            layoutVista.Controls.Add(fieldEstado, 7, 0);
            layoutVista.Controls.Add(fieldFechaApertura, 3, 0);
            layoutVista.Controls.Add(fieldUsuarioApertura, 2, 0);
            layoutVista.Controls.Add(fieldCodigo, 0, 0);
            layoutVista.Controls.Add(fieldDiferenciaEfectivo, 6, 0);
            layoutVista.Controls.Add(fieldEfectivoCalculado, 4, 0);
            layoutVista.Controls.Add(fieldEfectivoDeclarado, 5, 0);
            layoutVista.Controls.Add(fieldAlmacen, 1, 0);
            layoutVista.Controls.Add(btnVerDetalleTurno, 8, 0);
            layoutVista.Dock = DockStyle.Fill;
            layoutVista.Location = new Point(0, 0);
            layoutVista.Margin = new Padding(0, 0, 0, 1);
            layoutVista.Name = "layoutVista";
            layoutVista.RowCount = 1;
            layoutVista.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutVista.Size = new Size(1241, 41);
            layoutVista.TabIndex = 19;
            // 
            // btnAnularTurno
            // 
            btnAnularTurno.Animated = true;
            btnAnularTurno.BorderColor = Color.Gainsboro;
            btnAnularTurno.BorderRadius = 16;
            btnAnularTurno.BorderThickness = 1;
            btnAnularTurno.CustomImages.HoveredImage = (Image) resources.GetObject("resource.HoveredImage");
            btnAnularTurno.CustomImages.Image = (Image) resources.GetObject("resource.Image");
            btnAnularTurno.CustomImages.ImageAlign = HorizontalAlignment.Center;
            btnAnularTurno.CustomizableEdges = customizableEdges1;
            btnAnularTurno.Dock = DockStyle.Fill;
            btnAnularTurno.FillColor = Color.White;
            btnAnularTurno.Font = new Font("Segoe UI", 9.75F);
            btnAnularTurno.ForeColor = Color.White;
            btnAnularTurno.HoverState.BorderColor = Color.PeachPuff;
            btnAnularTurno.HoverState.FillColor = Color.PeachPuff;
            btnAnularTurno.HoverState.ForeColor = Color.White;
            btnAnularTurno.Location = new Point(1203, 3);
            btnAnularTurno.Name = "btnAnularTurno";
            btnAnularTurno.ShadowDecoration.CustomizableEdges = customizableEdges2;
            btnAnularTurno.Size = new Size(35, 35);
            btnAnularTurno.TabIndex = 41;
            // 
            // fieldEstado
            // 
            fieldEstado.AutoRoundedCorners = true;
            fieldEstado.BorderColor = Color.Gainsboro;
            fieldEstado.BorderRadius = 11;
            fieldEstado.BorderThickness = 1;
            fieldEstado.CustomizableEdges = customizableEdges3;
            fieldEstado.DisabledState.BorderColor = Color.Gainsboro;
            fieldEstado.DisabledState.CustomBorderColor = Color.Gainsboro;
            fieldEstado.DisabledState.FillColor = Color.Gainsboro;
            fieldEstado.DisabledState.ForeColor = Color.DimGray;
            fieldEstado.Dock = DockStyle.Fill;
            fieldEstado.Enabled = false;
            fieldEstado.FillColor = Color.Gainsboro;
            fieldEstado.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            fieldEstado.ForeColor = Color.DimGray;
            fieldEstado.HoverState.BorderColor = Color.PeachPuff;
            fieldEstado.HoverState.FillColor = Color.PeachPuff;
            fieldEstado.HoverState.ForeColor = Color.Black;
            fieldEstado.Location = new Point(1038, 8);
            fieldEstado.Margin = new Padding(8);
            fieldEstado.Name = "fieldEstado";
            fieldEstado.ShadowDecoration.CustomizableEdges = customizableEdges4;
            fieldEstado.Size = new Size(114, 25);
            fieldEstado.TabIndex = 40;
            fieldEstado.Text = "● estado";
            fieldEstado.TextOffset = new Point(0, -1);
            // 
            // fieldFechaApertura
            // 
            fieldFechaApertura.Dock = DockStyle.Fill;
            fieldFechaApertura.Font = new Font("Segoe UI", 11.25F);
            fieldFechaApertura.ForeColor = Color.DimGray;
            fieldFechaApertura.ImeMode = ImeMode.NoControl;
            fieldFechaApertura.Location = new Point(505, 1);
            fieldFechaApertura.Margin = new Padding(5, 1, 1, 1);
            fieldFechaApertura.Name = "fieldFechaApertura";
            fieldFechaApertura.Size = new Size(154, 39);
            fieldFechaApertura.TabIndex = 39;
            fieldFechaApertura.Text = "00/00/0000 00:00";
            fieldFechaApertura.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // fieldUsuarioApertura
            // 
            fieldUsuarioApertura.AutoEllipsis = true;
            fieldUsuarioApertura.Dock = DockStyle.Fill;
            fieldUsuarioApertura.Font = new Font("Segoe UI", 11.25F);
            fieldUsuarioApertura.ForeColor = Color.DimGray;
            fieldUsuarioApertura.ImeMode = ImeMode.NoControl;
            fieldUsuarioApertura.Location = new Point(345, 1);
            fieldUsuarioApertura.Margin = new Padding(5, 1, 1, 1);
            fieldUsuarioApertura.Name = "fieldUsuarioApertura";
            fieldUsuarioApertura.Size = new Size(154, 39);
            fieldUsuarioApertura.TabIndex = 37;
            fieldUsuarioApertura.Text = "apertura";
            fieldUsuarioApertura.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // fieldCodigo
            // 
            fieldCodigo.Dock = DockStyle.Fill;
            fieldCodigo.Font = new Font("Segoe UI", 11.25F);
            fieldCodigo.ForeColor = Color.DimGray;
            fieldCodigo.ImeMode = ImeMode.NoControl;
            fieldCodigo.Location = new Point(1, 1);
            fieldCodigo.Margin = new Padding(1);
            fieldCodigo.Name = "fieldCodigo";
            fieldCodigo.Size = new Size(178, 39);
            fieldCodigo.TabIndex = 13;
            fieldCodigo.Text = "codigo";
            fieldCodigo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // fieldDiferenciaEfectivo
            // 
            fieldDiferenciaEfectivo.Dock = DockStyle.Fill;
            fieldDiferenciaEfectivo.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point,  0);
            fieldDiferenciaEfectivo.ForeColor = Color.Black;
            fieldDiferenciaEfectivo.ImeMode = ImeMode.NoControl;
            fieldDiferenciaEfectivo.Location = new Point(921, 1);
            fieldDiferenciaEfectivo.Margin = new Padding(1);
            fieldDiferenciaEfectivo.Name = "fieldDiferenciaEfectivo";
            fieldDiferenciaEfectivo.Size = new Size(108, 39);
            fieldDiferenciaEfectivo.TabIndex = 34;
            fieldDiferenciaEfectivo.Text = "difEfectivo";
            fieldDiferenciaEfectivo.TextAlign = ContentAlignment.MiddleRight;
            // 
            // fieldEfectivoCalculado
            // 
            fieldEfectivoCalculado.Dock = DockStyle.Fill;
            fieldEfectivoCalculado.Font = new Font("Segoe UI", 11.25F);
            fieldEfectivoCalculado.ForeColor = Color.Black;
            fieldEfectivoCalculado.ImeMode = ImeMode.NoControl;
            fieldEfectivoCalculado.Location = new Point(661, 1);
            fieldEfectivoCalculado.Margin = new Padding(1);
            fieldEfectivoCalculado.Name = "fieldEfectivoCalculado";
            fieldEfectivoCalculado.Size = new Size(128, 39);
            fieldEfectivoCalculado.TabIndex = 35;
            fieldEfectivoCalculado.Text = "efCalculado";
            fieldEfectivoCalculado.TextAlign = ContentAlignment.MiddleRight;
            // 
            // fieldEfectivoDeclarado
            // 
            fieldEfectivoDeclarado.Dock = DockStyle.Fill;
            fieldEfectivoDeclarado.Font = new Font("Segoe UI", 11.25F);
            fieldEfectivoDeclarado.ForeColor = Color.Black;
            fieldEfectivoDeclarado.ImeMode = ImeMode.NoControl;
            fieldEfectivoDeclarado.Location = new Point(791, 1);
            fieldEfectivoDeclarado.Margin = new Padding(1);
            fieldEfectivoDeclarado.Name = "fieldEfectivoDeclarado";
            fieldEfectivoDeclarado.Size = new Size(128, 39);
            fieldEfectivoDeclarado.TabIndex = 36;
            fieldEfectivoDeclarado.Text = "efDeclarado";
            fieldEfectivoDeclarado.TextAlign = ContentAlignment.MiddleRight;
            // 
            // fieldAlmacen
            // 
            fieldAlmacen.AutoEllipsis = true;
            fieldAlmacen.Dock = DockStyle.Fill;
            fieldAlmacen.Font = new Font("Segoe UI", 11.25F);
            fieldAlmacen.ForeColor = Color.DimGray;
            fieldAlmacen.ImeMode = ImeMode.NoControl;
            fieldAlmacen.Location = new Point(181, 1);
            fieldAlmacen.Margin = new Padding(1);
            fieldAlmacen.Name = "fieldAlmacen";
            fieldAlmacen.Size = new Size(158, 39);
            fieldAlmacen.TabIndex = 17;
            fieldAlmacen.Text = "almacen";
            fieldAlmacen.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnVerDetalleTurno
            // 
            btnVerDetalleTurno.Animated = true;
            btnVerDetalleTurno.BorderColor = Color.Gainsboro;
            btnVerDetalleTurno.BorderRadius = 16;
            btnVerDetalleTurno.BorderThickness = 1;
            btnVerDetalleTurno.CustomImages.HoveredImage = (Image) resources.GetObject("resource.HoveredImage1");
            btnVerDetalleTurno.CustomImages.Image = (Image) resources.GetObject("resource.Image1");
            btnVerDetalleTurno.CustomImages.ImageAlign = HorizontalAlignment.Center;
            btnVerDetalleTurno.CustomizableEdges = customizableEdges5;
            btnVerDetalleTurno.Dock = DockStyle.Fill;
            btnVerDetalleTurno.FillColor = Color.White;
            btnVerDetalleTurno.Font = new Font("Segoe UI", 9.75F);
            btnVerDetalleTurno.ForeColor = Color.White;
            btnVerDetalleTurno.HoverState.BorderColor = Color.PeachPuff;
            btnVerDetalleTurno.HoverState.FillColor = Color.PeachPuff;
            btnVerDetalleTurno.HoverState.ForeColor = Color.White;
            btnVerDetalleTurno.Location = new Point(1163, 3);
            btnVerDetalleTurno.Name = "btnVerDetalleTurno";
            btnVerDetalleTurno.ShadowDecoration.CustomizableEdges = customizableEdges6;
            btnVerDetalleTurno.Size = new Size(34, 35);
            btnVerDetalleTurno.TabIndex = 22;
            // 
            // VistaTuplaTurno
            // 
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.White;
            ClientSize = new Size(1241, 42);
            Controls.Add(layoutBase);
            Font = new Font("Segoe UI", 10.8F);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(4, 5, 4, 5);
            Name = "VistaTuplaTurno";
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
        private TableLayoutPanel layoutVista;
        private Label fieldCodigo;
        private Label fieldAlmacen;
        private Label fieldDiferenciaEfectivo;
        private Label fieldEfectivoCalculado;
        private Label fieldEfectivoDeclarado;
        private Label fieldUsuarioApertura;
        private Guna2Button btnVerDetalleTurno;
        private Label fieldFechaApertura;
        private Guna2Button fieldEstado;
        private Guna2Button btnAnularTurno;
    }
}