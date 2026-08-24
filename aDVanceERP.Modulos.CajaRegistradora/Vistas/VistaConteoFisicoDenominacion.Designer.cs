namespace aDVanceERP.Modulos.CajaRegistradora.Vistas {
    partial class VistaConteoFisicoDenominacion {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            components = new System.ComponentModel.Container();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges7 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges8 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            formatoBase = new Guna.UI2.WinForms.Guna2BorderlessForm(components);
            panelDenominacion1 = new Guna.UI2.WinForms.Guna2Panel();
            layoutDenominacion1 = new TableLayoutPanel();
            fieldMontoTotal = new Label();
            fieldTituloDenominacion = new Label();
            fieldConteoDenominacion = new Guna.UI2.WinForms.Guna2TextBox();
            panelDenominacion1.SuspendLayout();
            layoutDenominacion1.SuspendLayout();
            SuspendLayout();
            // 
            // formatoBase
            // 
            formatoBase.ContainerControl = this;
            formatoBase.DockIndicatorTransparencyValue = 0.6D;
            formatoBase.DragForm = false;
            formatoBase.HasFormShadow = false;
            formatoBase.TransparentWhileDrag = true;
            // 
            // panelDenominacion1
            // 
            panelDenominacion1.BackColor = Color.Transparent;
            panelDenominacion1.BorderColor = Color.Gainsboro;
            panelDenominacion1.BorderRadius = 8;
            panelDenominacion1.BorderThickness = 1;
            panelDenominacion1.Controls.Add(layoutDenominacion1);
            panelDenominacion1.CustomizableEdges = customizableEdges5;
            panelDenominacion1.Dock = DockStyle.Fill;
            panelDenominacion1.FillColor = SystemColors.ButtonFace;
            panelDenominacion1.Location = new Point(0, 0);
            panelDenominacion1.Margin = new Padding(5);
            panelDenominacion1.Name = "panelDenominacion1";
            panelDenominacion1.ShadowDecoration.BorderRadius = 8;
            panelDenominacion1.ShadowDecoration.CustomizableEdges = customizableEdges6;
            panelDenominacion1.ShadowDecoration.Depth = 10;
            panelDenominacion1.Size = new Size(306, 45);
            panelDenominacion1.TabIndex = 65;
            // 
            // layoutDenominacion1
            // 
            layoutDenominacion1.BackColor = Color.Transparent;
            layoutDenominacion1.ColumnCount = 3;
            layoutDenominacion1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65F));
            layoutDenominacion1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
            layoutDenominacion1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutDenominacion1.Controls.Add(fieldMontoTotal, 2, 0);
            layoutDenominacion1.Controls.Add(fieldTituloDenominacion, 0, 0);
            layoutDenominacion1.Controls.Add(fieldConteoDenominacion, 1, 0);
            layoutDenominacion1.Dock = DockStyle.Fill;
            layoutDenominacion1.Location = new Point(0, 0);
            layoutDenominacion1.Name = "layoutDenominacion1";
            layoutDenominacion1.RowCount = 1;
            layoutDenominacion1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutDenominacion1.Size = new Size(306, 45);
            layoutDenominacion1.TabIndex = 0;
            // 
            // fieldMontoTotal
            // 
            fieldMontoTotal.Dock = DockStyle.Fill;
            fieldMontoTotal.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
            fieldMontoTotal.ForeColor = Color.Gray;
            fieldMontoTotal.ImeMode = ImeMode.NoControl;
            fieldMontoTotal.Location = new Point(136, 1);
            fieldMontoTotal.Margin = new Padding(1, 1, 5, 1);
            fieldMontoTotal.Name = "fieldMontoTotal";
            fieldMontoTotal.Size = new Size(165, 43);
            fieldMontoTotal.TabIndex = 41;
            fieldMontoTotal.Text = "= $ 0,00";
            fieldMontoTotal.TextAlign = ContentAlignment.MiddleRight;
            // 
            // fieldTituloDenominacion
            // 
            fieldTituloDenominacion.Dock = DockStyle.Fill;
            fieldTituloDenominacion.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
            fieldTituloDenominacion.ForeColor = Color.FromArgb(  64,   64,   64);
            fieldTituloDenominacion.ImeMode = ImeMode.NoControl;
            fieldTituloDenominacion.Location = new Point(1, 1);
            fieldTituloDenominacion.Margin = new Padding(1);
            fieldTituloDenominacion.Name = "fieldTituloDenominacion";
            fieldTituloDenominacion.Size = new Size(63, 43);
            fieldTituloDenominacion.TabIndex = 40;
            fieldTituloDenominacion.Text = "$ 1";
            fieldTituloDenominacion.TextAlign = ContentAlignment.MiddleRight;
            // 
            // fieldConteoDenominacion
            // 
            fieldConteoDenominacion.Animated = true;
            fieldConteoDenominacion.AutoRoundedCorners = true;
            fieldConteoDenominacion.BorderColor = Color.Gainsboro;
            fieldConteoDenominacion.BorderRadius = 16;
            fieldConteoDenominacion.Cursor = Cursors.IBeam;
            fieldConteoDenominacion.CustomizableEdges = customizableEdges7;
            fieldConteoDenominacion.DefaultText = "";
            fieldConteoDenominacion.DisabledState.BorderColor = Color.White;
            fieldConteoDenominacion.DisabledState.ForeColor = Color.DimGray;
            fieldConteoDenominacion.DisabledState.PlaceholderForeColor = Color.DimGray;
            fieldConteoDenominacion.Dock = DockStyle.Fill;
            fieldConteoDenominacion.FocusedState.BorderColor = Color.SandyBrown;
            fieldConteoDenominacion.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold);
            fieldConteoDenominacion.ForeColor = Color.Black;
            fieldConteoDenominacion.HoverState.BorderColor = Color.SandyBrown;
            fieldConteoDenominacion.IconLeftOffset = new Point(10, 0);
            fieldConteoDenominacion.IconRightOffset = new Point(6, 0);
            fieldConteoDenominacion.IconRightSize = new Size(12, 12);
            fieldConteoDenominacion.Location = new Point(70, 5);
            fieldConteoDenominacion.Margin = new Padding(5);
            fieldConteoDenominacion.Name = "fieldConteoDenominacion";
            fieldConteoDenominacion.PasswordChar = '\0';
            fieldConteoDenominacion.PlaceholderForeColor = Color.DimGray;
            fieldConteoDenominacion.PlaceholderText = "0";
            fieldConteoDenominacion.SelectedText = "";
            fieldConteoDenominacion.ShadowDecoration.CustomizableEdges = customizableEdges8;
            fieldConteoDenominacion.Size = new Size(60, 35);
            fieldConteoDenominacion.TabIndex = 39;
            fieldConteoDenominacion.TextAlign = HorizontalAlignment.Right;
            // 
            // VistaConteoFisicoDenominacion
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(306, 45);
            Controls.Add(panelDenominacion1);
            FormBorderStyle = FormBorderStyle.None;
            Name = "VistaConteoFisicoDenominacion";
            Text = "VistaConteoFisicoDenominacion";
            panelDenominacion1.ResumeLayout(false);
            layoutDenominacion1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Guna.UI2.WinForms.Guna2BorderlessForm formatoBase;
        private Guna.UI2.WinForms.Guna2Panel panelDenominacion1;
        private TableLayoutPanel layoutDenominacion1;
        private Label fieldMontoTotal;
        private Label fieldTituloDenominacion;
        private Guna.UI2.WinForms.Guna2TextBox fieldConteoDenominacion;
    }
}