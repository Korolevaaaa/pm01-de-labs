using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MebelOrg;

public class ProductVm
{
    public string Article { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public string Manufacturer { get; set; }
    public string Supplier { get; set; }
    public string Unit { get; set; }
    public string PhotoPath { get; set; }
    public string PhotoFile { get; set; }
    public string PriceOld { get; set; }
    public string PriceNew { get; set; }
    public string DiscountText { get; set; }
    public int Stock { get; set; }
    public int Discount { get; set; }
    public bool BigDiscount => Discount > 15;
    public decimal Price { get; set; }
}

public partial class ProductsWindow : Window
{
    private readonly string _role;
    private List<ProductVm> _items = new();

    public ProductsWindow(string role, string fio)
    {
        InitializeComponent();
        _role = role;
        UserInfo.Text = $"{fio} — {role}";

        bool manager = role == "Менеджер";
        bool admin = role == "Администратор";

        if (manager || admin)
        {
            ControlsPanel.Visibility = Visibility.Visible;
            OrdersBtn.Visibility = Visibility.Visible;
            LoadFilter();
        }
        if (admin)
        {
            AddBtn.Visibility = Visibility.Visible;
            EditBtn.Visibility = Visibility.Visible;
            DeleteBtn.Visibility = Visibility.Visible;
        }
        LoadProducts();
    }

    private void LoadFilter()
    {
        var dt = Db.Query("SELECT DISTINCT Category FROM Products ORDER BY Category");
        FilterBox.Items.Add("Все");
        foreach (DataRow r in dt.Rows) FilterBox.Items.Add(r[0].ToString());
        FilterBox.SelectedIndex = 0;
        SortBox.SelectedIndex = 0;
    }

    private void LoadProducts()
    {
        var dt = Db.Query("SELECT * FROM Products");
        _items = dt.Rows.Cast<DataRow>().Select(r =>
        {
            decimal price = Convert.ToDecimal(r["Price"]);
            int disc = r["Discount"] == DBNull.Value ? 0 : Convert.ToInt32(r["Discount"]);
            var photo = r["Photo"]?.ToString();
            var path = !string.IsNullOrWhiteSpace(photo) && File.Exists(Path.Combine(AppContext.BaseDirectory, "Images", photo))
                ? Path.Combine(AppContext.BaseDirectory, "Images", photo)
                : Path.Combine(AppContext.BaseDirectory, "Images", "picture.png");
            return new ProductVm
            {
                Article = r["Article"].ToString(),
                Name = r["Name"].ToString(),
                Category = r["Category"].ToString(),
                Description = r["Description"].ToString(),
                Manufacturer = r["Manufacturer"].ToString(),
                Supplier = r["Supplier"].ToString(),
                Unit = r["Unit"].ToString(),
                Stock = r["Stock"] == DBNull.Value ? 0 : Convert.ToInt32(r["Stock"]),
                Discount = disc,
                Price = price,
                PriceOld = disc > 0 ? price.ToString("0.##") + " ₽" : "",
                PriceNew = (price * (100 - disc) / 100m).ToString("0.##") + " ₽",
                DiscountText = disc > 0 ? $"скидка {disc}%" : "",
                PhotoPath = path,
                PhotoFile = photo
            };
        }).ToList();
        ApplyView();
    }

    private void ApplyView()
    {
        IEnumerable<ProductVm> v = _items;
        if (ControlsPanel.Visibility == Visibility.Visible)
        {
            var s = SearchBox.Text.Trim();
            if (s.Length > 0)
                v = v.Where(p => p.Name.Contains(s, StringComparison.OrdinalIgnoreCase)
                              || p.Description.Contains(s, StringComparison.OrdinalIgnoreCase));
            if (FilterBox.SelectedIndex > 0)
                v = v.Where(p => p.Category == FilterBox.SelectedItem.ToString());
            v = SortBox.SelectedIndex switch
            {
                1 => v.OrderBy(p => p.Price),
                2 => v.OrderByDescending(p => p.Price),
                _ => v
            };
        }
        ProductList.ItemsSource = v.ToList();
    }

    private void Refresh_Changed(object sender, EventArgs e) { if (_items != null) ApplyView(); }

    private void Orders_Click(object sender, RoutedEventArgs e) => new OrdersWindow(_role).ShowDialog();

    private ProductVm Selected => ProductList.SelectedItem as ProductVm;

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (new ProductEditWindow(null).ShowDialog() == true) LoadProducts();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Selected == null) { MessageBox.Show("Выберите товар."); return; }
        if (new ProductEditWindow(Selected.Article).ShowDialog() == true) LoadProducts();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Selected == null) { MessageBox.Show("Выберите товар."); return; }
        if (MessageBox.Show($"Удалить товар {Selected.Article}?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        Db.Exec("DELETE FROM OrderItems WHERE Article=@a", new SqlParameter("@a", Selected.Article));
        Db.Exec("DELETE FROM Products WHERE Article=@a", new SqlParameter("@a", Selected.Article));
        try
        {
            if (!string.IsNullOrWhiteSpace(Selected.PhotoFile))
            {
                var img = Path.Combine(AppContext.BaseDirectory, "Images", Selected.PhotoFile);
                if (File.Exists(img)) File.Delete(img);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        LoadProducts();
    }
}
