using System.Drawing.Printing;
using MshMsh.Domain;
using MshMsh.Services;

namespace MshMsh
{
    public partial class Form1 : Form
    {
        private readonly ProductService _productService = new();
        private readonly OrderService _orderService = new();
        private readonly CustomerService _customerService = new();

        private Panel mainContentPanel = null!;
        private Panel pnlPos = null!;
        private Panel pnlReturns = null!;
        private Panel pnlInventory = null!;
        private Panel pnlCustomers = null!;
        private Panel pnlReports = null!;

        private Panel pnlWelcome = null!;
        // عناصر الكاشير
        private TextBox txtBarcodeScan = null!;
        private TextBox txtSearchProduct = null!;
        private ListBox lstProductSearch = null!;
        private TextBox txtCustomerName = null!;
        private TextBox txtCustomerPhone = null!;
        private Button btnSelectCustomer = null!;
        private NumericUpDown numDiscount = null!;
        private DataGridView dgvPosCart = null!;
        private Label lblPosSubTotal = null!;
        private Label lblPosTotal = null!;

        private class CartRowItem
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = "";
            public decimal UnitPrice { get; set; }
            public int Quantity { get; set; }
            public int MaxStock { get; set; }
            public decimal Total => Quantity * UnitPrice;
        }

        private readonly List<CartRowItem> _cartItems = new();
        private List<Product> _allCachedProducts = new();
        private Order? _lastSavedOrder = null;

        // عناصر المرتجع
        private DataGridView dgvOrdersListForReturn = null!;
        private DataGridView dgvOrderItemsDetails = null!;
        private NumericUpDown numReturnQty = null!;
        private int _selectedOrderIdForReturn = 0;

        // عناصر المخزن
        private DataGridView dgvProducts = null!;
        private TextBox txtProdName = null!;
        private NumericUpDown numCostPrice = null!;
        private NumericUpDown numSellingPrice = null!;
        private NumericUpDown numStock = null!;
        private Label lblSelectedProdId = null!;
        private Button btnAddProd = null!;
        private Button btnUpdateProd = null!;
        private Button btnDeleteProd = null!;

        // عناصر العملاء
        private DataGridView dgvCustomers = null!;
        private TextBox txtCustMgmtName = null!;
        private TextBox txtCustMgmtPhone = null!;
        private Label lblSelectedCustId = null!;
        private Button btnAddCust = null!;
        private Button btnUpdateCust = null!;
        private Button btnViewPurchases = null!;

        // عناصر التقارير
        private DateTimePicker dtpFromDate = null!;
        private DateTimePicker dtpToDate = null!;
        private Label lblTotalSales = null!;
        private Label lblTotalCost = null!;
        private Label lblTotalProfit = null!;
        private Label lblTotalRefunds = null!;

        public Form1()
        {
            InitializeComponent();
            SetupDashboardUI();
            this.Load += async (s, e) =>
            {
                await RefreshProductsDataAsync();
                await RefreshCustomersDataAsync();
                await RefreshReportsDataAsync();
            };
        }

        private void SetupDashboardUI()
        {
            this.Text = "نظام مشمش POS - المنظومة التجارية المتكاملة";
            this.WindowState = FormWindowState.Maximized;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Segoe UI", 10, FontStyle.Regular);

            // 1. القائمة الجانبية (Sidebar)
            var sidebar = new Panel
            {
                Dock = DockStyle.Right,
                Width = 230,
                BackColor = Color.FromArgb(33, 43, 54)
            };

            var btnLogout = new Button
            {
                Text = "تسجيل خروج",
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnLogout.Click += (s, e) => { CurrentSession.Logout(); this.Close(); };
            sidebar.Controls.Add(btnLogout);

            var pnlNavButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(10, 15, 10, 0),
                WrapContents = false,
                AutoScroll = true
            };

            var lblLogo = new Label
            {
                Text = "MshMsh POS\nنظام الكاشير الذكي",
                ForeColor = Color.Orange,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Width = 205,
                Height = 65,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 0, 15)
            };

            var btnNavPos = CreateNavButton("🛒 نقطة البيع (الكاشير)");
            var btnNavReturns = CreateNavButton("🔄 شاشة المرتجعات");
            var btnNavInventory = CreateNavButton("📦 إدارة المخزن");
            var btnNavCustomers = CreateNavButton("👥 سجل العملاء");
            var btnNavReports = CreateNavButton("📊 تقارير الأرباح");

            btnNavPos.Click += (s, e) => { ShowModule(pnlPos); txtBarcodeScan.Focus(); };
            btnNavReturns.Click += async (s, e) => { ShowModule(pnlReturns); await LoadOrdersForReturnScreenAsync(); };
            btnNavInventory.Click += (s, e) =>
            {
                if (!CurrentSession.IsAdmin)
                {
                    MessageBox.Show("إدارة المخزن مخصصة للمدير فقط!", "صلاحية غير كافية", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                ShowModule(pnlInventory);
            };
            btnNavCustomers.Click += async (s, e) => { ShowModule(pnlCustomers); await RefreshCustomersDataAsync(); };
            btnNavReports.Click += async (s, e) =>
            {
                if (!CurrentSession.IsAdmin)
                {
                    MessageBox.Show("تقارير الأرباح مخصصة للمدير فقط!", "صلاحية غير كافية", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                await RefreshReportsDataAsync();
                ShowModule(pnlReports);
            };

            pnlNavButtons.Controls.AddRange(new Control[] { lblLogo, btnNavPos, btnNavReturns, btnNavInventory, btnNavCustomers, btnNavReports });
            sidebar.Controls.Add(pnlNavButtons);

            // 2. الشريط العلوي
            var topHeader = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.White };
            var roleTitle = CurrentSession.IsAdmin ? "مدير النظام (أدمن)" : "كاشير";
            var lblSession = new Label
            {
                Text = $"👤 الحساب الحالي: {CurrentSession.CurrentUser?.FullName} | الصلاحية: {roleTitle}",
                ForeColor = Color.FromArgb(50, 50, 50),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };
            topHeader.Controls.Add(lblSession);

            // 3. الحاوية المركزية
            mainContentPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(244, 246, 248) };

            BuildPosModule();
            BuildReturnsModule();
            BuildInventoryModule();
            BuildCustomersModule();
            BuildReportsModule();
            BuildWelcomeModule();

            this.Controls.Add(mainContentPanel);
            this.Controls.Add(topHeader);
            this.Controls.Add(sidebar);

            ShowModule(pnlWelcome);
        }

        private void BuildWelcomeModule()
        {
            pnlWelcome = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(10, 15, 25)
            };

            string imagePath = System.IO.Path.Combine(Application.StartupPath, "bg_mshmsh.jpg");
            if (System.IO.File.Exists(imagePath))
            {
                pnlWelcome.BackgroundImage = Image.FromFile(imagePath);
                pnlWelcome.BackgroundImageLayout = ImageLayout.Zoom;
            }

            mainContentPanel.Controls.Add(pnlWelcome);
        }

        private Button CreateNavButton(string text)
        {
            return new Button
            {
                Text = text,
                Width = 205,
                Height = 48,
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Color.FromArgb(41, 53, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };
        }

        private void ShowModule(Panel panelToShow)
        {
            pnlWelcome.Visible = false;
            pnlPos.Visible = false;
            pnlReturns.Visible = false;
            pnlInventory.Visible = false;
            pnlCustomers.Visible = false;
            pnlReports.Visible = false;

            panelToShow.Visible = true;
            panelToShow.BringToFront();
        }

        // ======================= 1. موديول الكاشير المطور (POS) =======================
        private void BuildPosModule()
        {
            pnlPos = new Panel { Dock = DockStyle.Fill };

            var tblTop = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.White,
                ColumnCount = 9,
                RowCount = 2,
                Padding = new Padding(10, 10, 10, 5)
            };
            tblTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            tblTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            tblTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tblTop.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var lblCName = new Label { Text = "اسم العميل:", AutoSize = true, Anchor = AnchorStyles.Right };
            txtCustomerName = new TextBox { Width = 140, Text = "عميل نقدي" };

            btnSelectCustomer = new Button
            {
                Text = "👥 اختيار عميل",
                Width = 110,
                Height = 29,
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            btnSelectCustomer.Click += async (s, e) => await OpenCustomerSelectionDialogAsync();

            var lblCPhone = new Label { Text = "رقم الهاتف:", AutoSize = true, Anchor = AnchorStyles.Right };
            txtCustomerPhone = new TextBox { Width = 130, Text = "01000000000" };

            var btnDelItem = new Button { Text = "❌ حذف الصنف", Width = 115, Height = 30, BackColor = Color.FromArgb(231, 76, 60), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnDelItem.Click += (s, e) => RemoveSelectedItem();

            var btnClearAll = new Button { Text = "🗑️ إلغاء الفاتورة", Width = 115, Height = 30, BackColor = Color.DarkGray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnClearAll.Click += (s, e) => ClearCart();

            var lblScan = new Label { Text = "⚡ سكانر باركود:", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), Anchor = AnchorStyles.Right };
            txtBarcodeScan = new TextBox { Width = 140, BackColor = Color.LightYellow };
            txtBarcodeScan.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    HandleBarcodeScan(txtBarcodeScan.Text.Trim());
                }
            };

            var lblSearch = new Label { Text = "🔍 بحث بالاسم:", AutoSize = true, Anchor = AnchorStyles.Right };
            txtSearchProduct = new TextBox { Width = 210 };
            txtSearchProduct.TextChanged += (s, e) => FilterProductList(txtSearchProduct.Text);

            // الصف الأول
            tblTop.Controls.Add(lblCName, 0, 0);
            tblTop.Controls.Add(txtCustomerName, 1, 0);
            tblTop.Controls.Add(btnSelectCustomer, 2, 0);
            tblTop.Controls.Add(lblCPhone, 3, 0);
            tblTop.Controls.Add(txtCustomerPhone, 4, 0);
            tblTop.Controls.Add(btnDelItem, 5, 0);
            tblTop.Controls.Add(btnClearAll, 6, 0);

            // الصف الثاني
            tblTop.Controls.Add(lblScan, 0, 1);
            tblTop.Controls.Add(txtBarcodeScan, 1, 1);
            tblTop.Controls.Add(lblSearch, 3, 1);
            tblTop.Controls.Add(txtSearchProduct, 4, 1);

            lstProductSearch = new ListBox { Width = 210, Height = 180, Visible = false };
            lstProductSearch.DoubleClick += (s, e) => AddSelectedFromSearch();

            // شريط الإجمالي والسداد السفلي
            var tblFooter = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 85,
                BackColor = Color.White,
                ColumnCount = 6,
                RowCount = 1,
                Padding = new Padding(15, 12, 15, 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            tblFooter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblFooter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblFooter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblFooter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tblFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            var btnCheckout = new Button { Text = "💾 حفظ الفاتورة", Width = 150, Height = 55, BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 15, 0) };
            btnCheckout.Click += async (s, e) => await HandleCheckoutAsync(printImmediately: false);

            var btnCheckoutPrint = new Button { Text = "🖨️ حفظ وطباعة", Width = 160, Height = 55, BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 30, 0) };
            btnCheckoutPrint.Click += async (s, e) => await HandleCheckoutAsync(printImmediately: true);

            var lblDisc = new Label { Text = "قيمة الخصم:", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Anchor = AnchorStyles.Right, Margin = new Padding(0, 18, 5, 0) };
            numDiscount = new NumericUpDown { Width = 95, Maximum = 100000, DecimalPlaces = 2, Margin = new Padding(0, 15, 30, 0) };
            numDiscount.ValueChanged += (s, e) => RecalculateTotals();

            lblPosSubTotal = new Label { Text = "المجموع: 0.00 ج.م", Font = new Font("Segoe UI", 11, FontStyle.Regular), ForeColor = Color.Gray, AutoSize = true, Anchor = AnchorStyles.Right, Margin = new Padding(0, 18, 20, 0) };
            lblPosTotal = new Label { Text = "الصافي: 0.00 ج.م", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.DarkSlateBlue, AutoSize = true, Anchor = AnchorStyles.Right, Margin = new Padding(0, 12, 0, 0) };

            tblFooter.Controls.Add(btnCheckout, 0, 0);
            tblFooter.Controls.Add(btnCheckoutPrint, 1, 0);
            tblFooter.Controls.Add(lblDisc, 2, 0);
            tblFooter.Controls.Add(numDiscount, 3, 0);
            tblFooter.Controls.Add(lblPosSubTotal, 4, 0);
            tblFooter.Controls.Add(lblPosTotal, 5, 0);

            // جدول سلة الفاتورة
            dgvPosCart = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                AllowUserToAddRows = false
            };

            var colId = new DataGridViewTextBoxColumn { Name = "ProdId", HeaderText = "الكود", ReadOnly = true, Width = 70 };
            var colName = new DataGridViewTextBoxColumn { Name = "ProdName", HeaderText = "اسم الصنف", ReadOnly = true };
            var colPrice = new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "سعر البيع ", ReadOnly = !CurrentSession.IsAdmin };
            var colQty = new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "الكمية " };
            var colTotal = new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "الإجمالي", ReadOnly = true };

            dgvPosCart.Columns.AddRange(new DataGridViewColumn[] { colId, colName, colPrice, colQty, colTotal });

            // تحديث الكمية أو السعر المعدل من الجدول في الـ Memory فورياً
            dgvPosCart.CellEndEdit += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _cartItems.Count) return;
                var item = _cartItems[e.RowIndex];

                if (e.ColumnIndex == dgvPosCart.Columns["Quantity"].Index)
                {
                    if (int.TryParse(dgvPosCart.Rows[e.RowIndex].Cells["Quantity"].Value?.ToString(), out int newQty) && newQty > 0)
                    {
                        if (newQty > item.MaxStock)
                        {
                            MessageBox.Show($"المتاح بالمخزن هو {item.MaxStock} فقط!", "تنبيه المخزن", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            dgvPosCart.Rows[e.RowIndex].Cells["Quantity"].Value = item.Quantity;
                        }
                        else
                        {
                            item.Quantity = newQty;
                        }
                    }
                    else
                    {
                        dgvPosCart.Rows[e.RowIndex].Cells["Quantity"].Value = item.Quantity;
                    }
                }

                if (e.ColumnIndex == dgvPosCart.Columns["UnitPrice"].Index && CurrentSession.IsAdmin)
                {
                    if (decimal.TryParse(dgvPosCart.Rows[e.RowIndex].Cells["UnitPrice"].Value?.ToString(), out decimal newPrice) && newPrice > 0)
                    {
                        item.UnitPrice = newPrice;
                    }
                    else
                    {
                        dgvPosCart.Rows[e.RowIndex].Cells["UnitPrice"].Value = item.UnitPrice;
                    }
                }

                RefreshCartGridUI();
            };

            pnlPos.Controls.Add(dgvPosCart);
            pnlPos.Controls.Add(lstProductSearch);
            pnlPos.Controls.Add(tblFooter);
            pnlPos.Controls.Add(tblTop);

            mainContentPanel.Controls.Add(pnlPos);
        }

        // نافذة اختيار العميل المنبثقة
        private async Task OpenCustomerSelectionDialogAsync()
        {
            var customers = await _customerService.GetAllCustomersAsync();

            using var dialog = new Form
            {
                Text = "اختيار عميل مسجل",
                Size = new Size(500, 450),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                RightToLeft = RightToLeft.Yes,
                RightToLeftLayout = true
            };

            var txtSearch = new TextBox { Dock = DockStyle.Top, Height = 30, PlaceholderText = "🔍 اكتب اسم العميل أو الهاتف للبحث السريع..." };

            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AllowUserToAddRows = false,
                DataSource = customers.Select(c => new { كود = c.Id, الاسم = c.Name, الهاتف = c.Phone }).ToList()
            };

            txtSearch.TextChanged += (s, e) =>
            {
                var q = txtSearch.Text.Trim().ToLower();
                grid.DataSource = customers
                    .Where(c => c.Name.ToLower().Contains(q) || c.Phone.Contains(q))
                    .Select(c => new { كود = c.Id, الاسم = c.Name, الهاتف = c.Phone })
                    .ToList();
            };

            var btnSelect = new Button { Text = "✅ اختيار العميل", Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            void SelectCustomerAction()
            {
                if (grid.CurrentRow != null)
                {
                    txtCustomerName.Text = grid.CurrentRow.Cells["الاسم"].Value?.ToString() ?? "";
                    txtCustomerPhone.Text = grid.CurrentRow.Cells["الهاتف"].Value?.ToString() ?? "";
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                }
            }

            btnSelect.Click += (s, e) => SelectCustomerAction();
            grid.DoubleClick += (s, e) => SelectCustomerAction();

            dialog.Controls.Add(grid);
            dialog.Controls.Add(txtSearch);
            dialog.Controls.Add(btnSelect);

            dialog.ShowDialog(this);
        }

        private void FilterProductList(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                lstProductSearch.Visible = false;
                return;
            }

            var matches = _allCachedProducts
                .Where(p => p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) || p.Id.ToString() == searchText.Trim())
                .Take(10)
                .ToList();

            if (matches.Any())
            {
                lstProductSearch.DataSource = matches;
                lstProductSearch.DisplayMember = "Name";
                lstProductSearch.ValueMember = "Id";
                lstProductSearch.Location = new Point(txtSearchProduct.Location.X, txtSearchProduct.Bottom + 5);
                lstProductSearch.Visible = true;
                lstProductSearch.BringToFront();
            }
            else
            {
                lstProductSearch.Visible = false;
            }
        }

        private void HandleBarcodeScan(string barcodeText)
        {
            if (string.IsNullOrWhiteSpace(barcodeText)) return;

            var cleanCode = barcodeText.Replace("*", "").Replace("MSH-", "").TrimStart('0');
            if (int.TryParse(cleanCode, out int prodId))
            {
                var product = _allCachedProducts.FirstOrDefault(p => p.Id == prodId);
                if (product != null)
                {
                    AddProductToCart(product);
                    txtBarcodeScan.Clear();
                    txtBarcodeScan.Focus();
                    return;
                }
            }

            MessageBox.Show("لم يتم العثور على صنف بهذا الباركود!", "غير موجود", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtBarcodeScan.SelectAll();
        }

        private void AddSelectedFromSearch()
        {
            if (lstProductSearch.SelectedItem is Product selectedProduct)
            {
                AddProductToCart(selectedProduct);
                lstProductSearch.Visible = false;
                txtSearchProduct.Clear();
                txtBarcodeScan.Focus();
            }
        }

        private void AddProductToCart(Product product)
        {
            var existing = _cartItems.FirstOrDefault(x => x.ProductId == product.Id);
            if (existing != null)
            {
                if (existing.Quantity + 1 > product.StockQuantity)
                {
                    MessageBox.Show($"المتاح في المخزن ({product.StockQuantity}) لا يكفي!", "المخزون نفد", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                existing.Quantity += 1;
            }
            else
            {
                if (product.StockQuantity < 1)
                {
                    MessageBox.Show("هذا المنتج غير متوفر في المخزن حالياً!", "نفد المخزون", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _cartItems.Add(new CartRowItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.SellingPrice,
                    Quantity = 1,
                    MaxStock = product.StockQuantity
                });
            }

            RefreshCartGridUI();
        }

        private void RemoveSelectedItem()
        {
            if (dgvPosCart.CurrentRow == null) return;
            int index = dgvPosCart.CurrentRow.Index;
            if (index >= 0 && index < _cartItems.Count)
            {
                _cartItems.RemoveAt(index);
                RefreshCartGridUI();
            }
        }

        private void ClearCart()
        {
            if (_cartItems.Count == 0) return;
            if (MessageBox.Show("هل أنت متأكد من إلغاء وتفريغ الفاتورة بالكامل؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _cartItems.Clear();
                numDiscount.Value = 0;
                RefreshCartGridUI();
            }
        }

        private void RefreshCartGridUI()
        {
            dgvPosCart.Rows.Clear();
            foreach (var item in _cartItems)
            {
                dgvPosCart.Rows.Add(item.ProductId, item.ProductName, item.UnitPrice.ToString("N2"), item.Quantity, item.Total.ToString("N2"));
            }
            RecalculateTotals();
        }

        private void RecalculateTotals()
        {
            decimal subTotal = _cartItems.Sum(x => x.Total);
            decimal discount = numDiscount.Value;
            decimal total = Math.Max(0, subTotal - discount);

            lblPosSubTotal.Text = $"المجموع: {subTotal:N2} ج.م";
            lblPosTotal.Text = $"الصافي: {total:N2} ج.م";
        }

        // حفظ الفاتورة بالأسعار الفعلية المعدلة من الجدول
        private async Task HandleCheckoutAsync(bool printImmediately)
        {
            if (_cartItems.Count == 0)
            {
                MessageBox.Show("الفاتورة فارغة!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var customer = await _customerService.GetOrCreateCustomerAsync(txtCustomerName.Text, txtCustomerPhone.Text);

                // تمرير كود الصنف + الكمية + سعر البيع الفعلي المعدل في الجدول
                var items = _cartItems.Select(c => (c.ProductId, c.Quantity, c.UnitPrice)).ToList();
                _lastSavedOrder = await _orderService.CreateOrderAsync(customer.Id, numDiscount.Value, items);

                MessageBox.Show($"تم حفظ الفاتورة بنجاح رقم #{_lastSavedOrder.Id}\nالصافي المطلوب: {_lastSavedOrder.TotalAmount:N2} ج.م", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (printImmediately)
                {
                    PrintReceiptPreview();
                }

                _cartItems.Clear();
                numDiscount.Value = 0;
                RefreshCartGridUI();
                await RefreshProductsDataAsync();
                txtBarcodeScan.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ في الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintReceiptPreview()
        {
            if (_lastSavedOrder == null) return;

            var printDoc = new PrintDocument();
            printDoc.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics!;
                float y = 15;
                var fontHeader = new Font("Segoe UI", 12, FontStyle.Bold);
                var fontBold = new Font("Segoe UI", 9, FontStyle.Bold);
                var fontNormal = new Font("Segoe UI", 9);

                g.DrawString("MshMsh Store - إيصال بيع", fontHeader, Brushes.Black, 60, y);
                y += 25;
                g.DrawString($"رقم الفاتورة: #{_lastSavedOrder.Id}", fontBold, Brushes.Black, 15, y);
                y += 18;
                g.DrawString($"التاريخ: {_lastSavedOrder.OrderDate:yyyy-MM-dd HH:mm}", fontNormal, Brushes.Black, 15, y);
                y += 18;
                g.DrawString($"العميل: {txtCustomerName.Text}", fontNormal, Brushes.Black, 15, y);
                y += 20;
                g.DrawLine(Pens.Black, 15, y, 260, y);
                y += 5;

                g.DrawString("الصنف", fontBold, Brushes.Black, 15, y);
                g.DrawString("الكمية", fontBold, Brushes.Black, 140, y);
                g.DrawString("الإجمالي", fontBold, Brushes.Black, 200, y);
                y += 18;

                foreach (var item in _lastSavedOrder.Items)
                {
                    var prod = _allCachedProducts.FirstOrDefault(p => p.Id == item.ProductId);
                    string name = prod?.Name ?? $"صنف #{item.ProductId}";
                    g.DrawString(name.Length > 15 ? name.Substring(0, 14) + ".." : name, fontNormal, Brushes.Black, 15, y);
                    g.DrawString(item.Quantity.ToString(), fontNormal, Brushes.Black, 150, y);
                    g.DrawString(item.TotalPrice.ToString("N2"), fontNormal, Brushes.Black, 200, y);
                    y += 18;
                }

                y += 5;
                g.DrawLine(Pens.Black, 15, y, 260, y);
                y += 8;
                if (_lastSavedOrder.Discount > 0)
                {
                    g.DrawString($"الخصم: {_lastSavedOrder.Discount:N2} EGP", fontNormal, Brushes.DarkRed, 30, y);
                    y += 18;
                }
                g.DrawString($"الصافي: {_lastSavedOrder.TotalAmount:N2} EGP", fontHeader, Brushes.DarkBlue, 30, y);
                y += 30;
                g.DrawString("شكراً لزيارتكم ونتشرف بخدمتكم دائماً", fontNormal, Brushes.Black, 35, y);
            };

            var printPreview = new PrintPreviewDialog { Document = printDoc, Width = 450, Height = 600 };
            printPreview.ShowDialog();
        }

        // ======================= 2. موديول المرتجع =======================
        private void BuildReturnsModule()
        {
            pnlReturns = new Panel { Dock = DockStyle.Fill };

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 280
            };

            var pnlTopOrders = new Panel { Dock = DockStyle.Fill };
            var lblOrdTitle = new Label { Text = "📋 اختر الفاتورة المراد الاسترجاع منها من القائمة:", Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI", 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };

            dgvOrdersListForReturn = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false
            };

            dgvOrdersListForReturn.CellClick += async (s, e) =>
            {
                if (dgvOrdersListForReturn.CurrentRow != null)
                {
                    _selectedOrderIdForReturn = Convert.ToInt32(dgvOrdersListForReturn.CurrentRow.Cells["رقم_الفاتورة"].Value);
                    await LoadOrderItemsForReturnAsync(_selectedOrderIdForReturn);
                }
            };

            pnlTopOrders.Controls.Add(dgvOrdersListForReturn);
            pnlTopOrders.Controls.Add(lblOrdTitle);
            split.Panel1.Controls.Add(pnlTopOrders);

            var pnlBottomItems = new Panel { Dock = DockStyle.Fill };

            var tblReturnBar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.WhiteSmoke,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(15, 10, 15, 10)
            };
            tblReturnBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblReturnBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblReturnBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var btnConfirmReturn = new Button
            {
                Text = "🔄 إرجاع الصنف المحدد",
                Width = 190,
                Height = 38,
                BackColor = Color.FromArgb(230, 126, 34),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(0, 0, 20, 0)
            };

            var lblQtyRet = new Label { Text = "الكمية المرتجعة:", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Anchor = AnchorStyles.Right, Margin = new Padding(0, 10, 10, 0) };
            numReturnQty = new NumericUpDown { Width = 95, Minimum = 1, Value = 1, Margin = new Padding(0, 8, 0, 0) };

            btnConfirmReturn.Click += async (s, e) =>
            {
                if (_selectedOrderIdForReturn == 0 || dgvOrderItemsDetails.CurrentRow == null)
                {
                    MessageBox.Show("يرجى اختيار الفاتورة أولاً ثم الصنف المراد إرجاعه!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int prodId = Convert.ToInt32(dgvOrderItemsDetails.CurrentRow.Cells["كود_الصنف"].Value);
                int qty = (int)numReturnQty.Value;

                try
                {
                    await _orderService.ProcessItemReturnAsync(_selectedOrderIdForReturn, prodId, qty);
                    MessageBox.Show("تم استرجاع الصنف وإعادته للمخزن بنجاح!", "نجاح المرتجع", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadOrderItemsForReturnAsync(_selectedOrderIdForReturn);
                    await RefreshProductsDataAsync();
                    await RefreshReportsDataAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "خطأ في المرتجع", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            tblReturnBar.Controls.Add(btnConfirmReturn, 0, 0);
            tblReturnBar.Controls.Add(lblQtyRet, 1, 0);
            tblReturnBar.Controls.Add(numReturnQty, 2, 0);

            dgvOrderItemsDetails = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AllowUserToAddRows = false
            };

            pnlBottomItems.Controls.Add(dgvOrderItemsDetails);
            pnlBottomItems.Controls.Add(tblReturnBar);
            split.Panel2.Controls.Add(pnlBottomItems);

            pnlReturns.Controls.Add(split);
            mainContentPanel.Controls.Add(pnlReturns);
        }

        private async Task LoadOrdersForReturnScreenAsync()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            var list = orders.Select(o => new
            {
                رقم_الفاتورة = o.Id,
                العميل = o.Customer.Name,
                الهاتف = o.Customer.Phone,
                التاريخ = o.OrderDate.ToString("yyyy-MM-dd HH:mm"),
                الخصم = o.Discount.ToString("N2") + " ج.م",
                الصافي = o.TotalAmount.ToString("N2") + " ج.م"
            }).ToList();

            dgvOrdersListForReturn.DataSource = null;
            dgvOrdersListForReturn.DataSource = list;
            dgvOrderItemsDetails.DataSource = null;
            _selectedOrderIdForReturn = 0;
        }

        private async Task LoadOrderItemsForReturnAsync(int orderId)
        {
            var order = await _orderService.GetOrderWithDetailsAsync(orderId);
            if (order == null) return;

            var items = order.Items.Select(i => new
            {
                كود_الصنف = i.ProductId,
                اسم_الصنف = i.Product.Name,
                سعر_البيع = i.UnitSellingPrice.ToString("N2") + " ج.م",
                الكمية_المشتراة = i.Quantity,
                الإجمالي = i.TotalPrice.ToString("N2") + " ج.م"
            }).ToList();

            dgvOrderItemsDetails.DataSource = null;
            dgvOrderItemsDetails.DataSource = items;
        }

        // ======================= 3. موديول المخزن =======================
        private void BuildInventoryModule()
        {
            pnlInventory = new Panel { Dock = DockStyle.Fill };

            var tblInv = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Color.White,
                ColumnCount = 8,
                RowCount = 2,
                Padding = new Padding(15, 10, 15, 10)
            };
            tblInv.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblInv.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            tblInv.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblInv.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tblInv.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblInv.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tblInv.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblInv.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            lblSelectedProdId = new Label { Text = "0", Visible = false };

            var lblN = new Label { Text = "اسم الصنف:", AutoSize = true, Anchor = AnchorStyles.Right };
            txtProdName = new TextBox { Width = 165 };

            var lblC = new Label { Text = "سعر الشراء:", AutoSize = true, Anchor = AnchorStyles.Right };
            numCostPrice = new NumericUpDown { Width = 100, Maximum = 100000, DecimalPlaces = 2 };

            var lblS = new Label { Text = "سعر البيع:", AutoSize = true, Anchor = AnchorStyles.Right };
            numSellingPrice = new NumericUpDown { Width = 100, Maximum = 100000, DecimalPlaces = 2 };

            var lblStk = new Label { Text = "المخزون:", AutoSize = true, Anchor = AnchorStyles.Right };
            numStock = new NumericUpDown { Width = 90, Maximum = 10000 };

            tblInv.Controls.Add(lblN, 0, 0);
            tblInv.Controls.Add(txtProdName, 1, 0);
            tblInv.Controls.Add(lblC, 2, 0);
            tblInv.Controls.Add(numCostPrice, 3, 0);
            tblInv.Controls.Add(lblS, 4, 0);
            tblInv.Controls.Add(numSellingPrice, 5, 0);
            tblInv.Controls.Add(lblStk, 6, 0);
            tblInv.Controls.Add(numStock, 7, 0);

            var pnlInvButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 5, 0, 0)
            };

            btnAddProd = new Button { Text = "➕ إضافة", Width = 95, Height = 34, BackColor = Color.SeaGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 10, 0) };
            btnUpdateProd = new Button { Text = "✏️ تعديل", Width = 95, Height = 34, BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Enabled = false, Margin = new Padding(0, 0, 10, 0) };
            btnDeleteProd = new Button { Text = "❌ حذف", Width = 95, Height = 34, BackColor = Color.IndianRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Enabled = false, Margin = new Padding(0, 0, 10, 0) };
            var btnClear = new Button { Text = "تفريغ", Width = 85, Height = 34, BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 10, 0) };
            var btnPrintBarcode = new Button { Text = "🏷️ طباعة باركود", Width = 140, Height = 34, BackColor = Color.FromArgb(142, 68, 173), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            btnPrintBarcode.Click += (s, e) => PrintBarcodeLabel();

            btnAddProd.Click += async (s, e) =>
            {
                try
                {
                    await _productService.AddProductAsync(txtProdName.Text, numCostPrice.Value, numSellingPrice.Value, (int)numStock.Value);
                    MessageBox.Show("تمت الإضافة بنجاح!");
                    ResetProdFields();
                    await RefreshProductsDataAsync();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };

            btnUpdateProd.Click += async (s, e) =>
            {
                try
                {
                    int id = int.Parse(lblSelectedProdId.Text);
                    await _productService.UpdateProductAsync(id, txtProdName.Text, numCostPrice.Value, numSellingPrice.Value, (int)numStock.Value);
                    MessageBox.Show("تم تعديل المنتج بنجاح!");
                    ResetProdFields();
                    await RefreshProductsDataAsync();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };

            btnDeleteProd.Click += async (s, e) =>
            {
                if (MessageBox.Show("تأكيد حذف الصنف؟", "حذف", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    int id = int.Parse(lblSelectedProdId.Text);
                    await _productService.DeleteProductAsync(id);
                    ResetProdFields();
                    await RefreshProductsDataAsync();
                }
            };

            btnClear.Click += (s, e) => ResetProdFields();

            pnlInvButtons.Controls.AddRange(new Control[] { btnAddProd, btnUpdateProd, btnDeleteProd, btnClear, btnPrintBarcode });
            tblInv.SetColumnSpan(pnlInvButtons, 8);
            tblInv.Controls.Add(pnlInvButtons, 0, 1);

            dgvProducts = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AllowUserToAddRows = false
            };
            dgvProducts.CellClick += (s, e) =>
            {
                if (dgvProducts.CurrentRow?.DataBoundItem is Product p)
                {
                    lblSelectedProdId.Text = p.Id.ToString();
                    txtProdName.Text = p.Name;
                    numCostPrice.Value = p.CostPrice;
                    numSellingPrice.Value = p.SellingPrice;
                    numStock.Value = p.StockQuantity;

                    btnAddProd.Enabled = false;
                    btnUpdateProd.Enabled = true;
                    btnDeleteProd.Enabled = true;
                }
            };

            pnlInventory.Controls.Add(dgvProducts);
            pnlInventory.Controls.Add(tblInv);
            mainContentPanel.Controls.Add(pnlInventory);
        }

        private void PrintBarcodeLabel()
        {
            if (lblSelectedProdId.Text == "0")
            {
                MessageBox.Show("الرجاء اختيار صنف أولاً لطباعة الباركود الخاص به!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var printDoc = new PrintDocument();
            printDoc.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics!;
                var titleFont = new Font("Segoe UI", 10, FontStyle.Bold);
                var barcodeFont = new Font("Courier New", 18, FontStyle.Bold);
                var priceFont = new Font("Segoe UI", 12, FontStyle.Bold);

                g.DrawString("MshMsh Store", titleFont, Brushes.Black, 30, 10);
                g.DrawString(txtProdName.Text, new Font("Segoe UI", 9), Brushes.Black, 30, 30);
                g.DrawString($"*MSH-{lblSelectedProdId.Text.PadLeft(5, '0')}*", barcodeFont, Brushes.Black, 20, 50);
                g.DrawString($"السعر: {numSellingPrice.Value:N2} EGP", priceFont, Brushes.DarkGreen, 30, 80);
            };

            var printPreview = new PrintPreviewDialog { Document = printDoc, Width = 500, Height = 400 };
            printPreview.ShowDialog();
        }

        private void ResetProdFields()
        {
            lblSelectedProdId.Text = "0";
            txtProdName.Clear();
            numCostPrice.Value = 0;
            numSellingPrice.Value = 0;
            numStock.Value = 0;
            btnAddProd.Enabled = true;
            btnUpdateProd.Enabled = false;
            btnDeleteProd.Enabled = false;
        }

        // ======================= 4. موديول إدارة العملاء =======================
        private void BuildCustomersModule()
        {
            pnlCustomers = new Panel { Dock = DockStyle.Fill };

            var tblCust = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.White,
                ColumnCount = 7,
                RowCount = 1,
                Padding = new Padding(15, 15, 15, 10)
            };
            tblCust.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblCust.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            tblCust.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblCust.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            tblCust.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblCust.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblCust.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            lblSelectedCustId = new Label { Text = "0", Visible = false };

            var lblName = new Label { Text = "اسم العميل:", AutoSize = true, Anchor = AnchorStyles.Right };
            txtCustMgmtName = new TextBox { Width = 170 };

            var lblPhone = new Label { Text = "رقم الهاتف:", AutoSize = true, Anchor = AnchorStyles.Right };
            txtCustMgmtPhone = new TextBox { Width = 150 };

            btnAddCust = new Button { Text = "➕ إضافة عميل", Width = 110, Height = 34, BackColor = Color.SeaGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 10, 0) };
            btnUpdateCust = new Button { Text = "✏️ تعديل", Width = 90, Height = 34, BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Enabled = false, Margin = new Padding(0, 0, 10, 0) };
            btnViewPurchases = new Button { Text = "🔍 مشتريات العميل", Width = 140, Height = 34, BackColor = Color.DarkSlateBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Enabled = false };

            btnAddCust.Click += async (s, e) =>
            {
                try
                {
                    await _customerService.AddCustomerAsync(txtCustMgmtName.Text, txtCustMgmtPhone.Text);
                    MessageBox.Show("تم إضافة العميل بنجاح!");
                    txtCustMgmtName.Clear();
                    txtCustMgmtPhone.Clear();
                    await RefreshCustomersDataAsync();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };

            btnUpdateCust.Click += async (s, e) =>
            {
                try
                {
                    int id = int.Parse(lblSelectedCustId.Text);
                    await _customerService.UpdateCustomerAsync(id, txtCustMgmtName.Text, txtCustMgmtPhone.Text);
                    MessageBox.Show("تم تعديل بيانات العميل بنجاح!");
                    txtCustMgmtName.Clear();
                    txtCustMgmtPhone.Clear();
                    btnUpdateCust.Enabled = false;
                    btnViewPurchases.Enabled = false;
                    btnAddCust.Enabled = true;
                    await RefreshCustomersDataAsync();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            };

            btnViewPurchases.Click += async (s, e) =>
            {
                int id = int.Parse(lblSelectedCustId.Text);
                var (totalSpent, count, orders) = await _customerService.GetCustomerPurchasesAsync(id);

                string details = $"كشف حساب العميل: {txtCustMgmtName.Text}\n" +
                                 $"إجمالي عدد الفواتير: {count}\n" +
                                 $"إجمالي المشتريات: {totalSpent:N2} ج.م\n\n" +
                                 "تفاصيل آخر الفواتير:\n";

                foreach (var ord in orders.Take(5))
                {
                    details += $"فاتورة #{ord.Id} - تاريخ: {ord.OrderDate:yyyy-MM-dd} - صافي: {ord.TotalAmount:N2} ج.م\n";
                }

                MessageBox.Show(details, "سجل مشتريات العميل", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            tblCust.Controls.Add(lblName, 0, 0);
            tblCust.Controls.Add(txtCustMgmtName, 1, 0);
            tblCust.Controls.Add(lblPhone, 2, 0);
            tblCust.Controls.Add(txtCustMgmtPhone, 3, 0);
            tblCust.Controls.Add(btnAddCust, 4, 0);
            tblCust.Controls.Add(btnUpdateCust, 5, 0);
            tblCust.Controls.Add(btnViewPurchases, 6, 0);

            dgvCustomers = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true,
                AllowUserToAddRows = false
            };
            dgvCustomers.CellClick += (s, e) =>
            {
                if (dgvCustomers.CurrentRow?.DataBoundItem is Customer c)
                {
                    lblSelectedCustId.Text = c.Id.ToString();
                    txtCustMgmtName.Text = c.Name;
                    txtCustMgmtPhone.Text = c.Phone;

                    btnAddCust.Enabled = false;
                    btnUpdateCust.Enabled = true;
                    btnViewPurchases.Enabled = true;
                }
            };

            pnlCustomers.Controls.Add(dgvCustomers);
            pnlCustomers.Controls.Add(tblCust);
            mainContentPanel.Controls.Add(pnlCustomers);
        }

        // ======================= 5. موديول تقارير الأرباح =======================
        private void BuildReportsModule()
        {
            pnlReports = new Panel { Dock = DockStyle.Fill };

            var tblFilter = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.White,
                ColumnCount = 5,
                RowCount = 1,
                Padding = new Padding(15, 15, 15, 10)
            };
            tblFilter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblFilter.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            tblFilter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tblFilter.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            tblFilter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var lblFrom = new Label { Text = "من تاريخ:", AutoSize = true, Anchor = AnchorStyles.Right };
            dtpFromDate = new DateTimePicker { Width = 140, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-7) };

            var lblTo = new Label { Text = "إلى تاريخ:", AutoSize = true, Anchor = AnchorStyles.Right };
            dtpToDate = new DateTimePicker { Width = 140, Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            var btnFilter = new Button { Text = "🔍 تطبيق الفلترة", Width = 130, Height = 32, BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnFilter.Click += async (s, e) => await RefreshReportsDataAsync();

            tblFilter.Controls.Add(lblFrom, 0, 0);
            tblFilter.Controls.Add(dtpFromDate, 1, 0);
            tblFilter.Controls.Add(lblTo, 2, 0);
            tblFilter.Controls.Add(dtpToDate, 3, 0);
            tblFilter.Controls.Add(btnFilter, 4, 0);

            var cardsFlow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 180, Padding = new Padding(20) };
            var pnlCard1 = CreateReportCard("إجمالي المبيعات (بعد الخصم)", out lblTotalSales, Color.FromArgb(41, 128, 185));
            var pnlCard2 = CreateReportCard("إجمالي التكلفة", out lblTotalCost, Color.FromArgb(192, 57, 43));
            var pnlCard3 = CreateReportCard("إجمالي المرتجعات", out lblTotalRefunds, Color.FromArgb(230, 126, 34));
            var pnlCard4 = CreateReportCard("صافي الربح الفعلي", out lblTotalProfit, Color.FromArgb(39, 174, 96));

            cardsFlow.Controls.AddRange(new Control[] { pnlCard1, pnlCard2, pnlCard3, pnlCard4 });

            pnlReports.Controls.Add(cardsFlow);
            pnlReports.Controls.Add(tblFilter);
            mainContentPanel.Controls.Add(pnlReports);
        }

        private Panel CreateReportCard(string title, out Label lblValue, Color headerColor)
        {
            var pnl = new Panel { Width = 230, Height = 130, BackColor = Color.White, Margin = new Padding(10) };
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 35, BackColor = headerColor };
            var lblTitle = new Label { Text = title, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            pnlHeader.Controls.Add(lblTitle);

            lblValue = new Label { Text = "0.00 ج.م", Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = Color.FromArgb(40, 40, 40), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };

            pnl.Controls.Add(lblValue);
            pnl.Controls.Add(pnlHeader);
            return pnl;
        }

        private async Task RefreshReportsDataAsync()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            var returns = await _orderService.GetAllReturnsAsync();

            var startDate = dtpFromDate.Value.Date;
            var endDate = dtpToDate.Value.Date.AddDays(1).AddTicks(-1);

            var filteredOrders = orders.Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate).ToList();
            var filteredReturns = returns.Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate).ToList();

            decimal totalSales = filteredOrders.Sum(o => o.TotalAmount);
            decimal totalCost = filteredOrders.Sum(o => o.TotalCost);
            decimal totalRefunds = filteredReturns.Sum(r => r.RefundAmount);

            decimal totalProfit = Math.Max(0, filteredOrders.Sum(o => o.TotalProfit) - totalRefunds);

            lblTotalSales.Text = $"{totalSales:N2} ج.م";
            lblTotalCost.Text = $"{totalCost:N2} ج.م";
            lblTotalRefunds.Text = $"{totalRefunds:N2} ج.م";
            lblTotalProfit.Text = $"{totalProfit:N2} ج.م";
        }

        private async Task RefreshProductsDataAsync()
        {
            _allCachedProducts = await _productService.GetAllProductsAsync();
            dgvProducts.DataSource = null;
            dgvProducts.DataSource = _allCachedProducts;

            if (dgvProducts.Columns["Id"] != null) dgvProducts.Columns["Id"]!.HeaderText = "الكود";
            if (dgvProducts.Columns["Name"] != null) dgvProducts.Columns["Name"]!.HeaderText = "اسم المنتج";
            if (dgvProducts.Columns["CostPrice"] != null) dgvProducts.Columns["CostPrice"]!.HeaderText = "سعر الشراء";
            if (dgvProducts.Columns["SellingPrice"] != null) dgvProducts.Columns["SellingPrice"]!.HeaderText = "سعر البيع";
            if (dgvProducts.Columns["StockQuantity"] != null) dgvProducts.Columns["StockQuantity"]!.HeaderText = "المخزون";
        }

        private async Task RefreshCustomersDataAsync()
        {
            var customers = await _customerService.GetAllCustomersAsync();
            dgvCustomers.DataSource = null;
            dgvCustomers.DataSource = customers;

            if (dgvCustomers.Columns["Id"] != null) dgvCustomers.Columns["Id"]!.HeaderText = "كود العميل";
            if (dgvCustomers.Columns["Name"] != null) dgvCustomers.Columns["Name"]!.HeaderText = "اسم العميل";
            if (dgvCustomers.Columns["Phone"] != null) dgvCustomers.Columns["Phone"]!.HeaderText = "رقم الهاتف";
        }
    }
}