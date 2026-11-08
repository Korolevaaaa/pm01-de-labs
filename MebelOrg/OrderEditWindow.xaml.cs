using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace MebelOrg;

public partial class OrderEditWindow : Window
{
    private readonly int? _id;
    private readonly List<(string Article, int Qty)> _items = new();

    public OrderEditWindow(int? id)
    {
        InitializeComponent();
        _id = id;

        var pp = Db.Query("SELECT Id, Address FROM PickupPoints ORDER BY Id");
        PickupBox.ItemsSource = pp.Rows.Cast<DataRow>()
            .Select(r => new { Id = (int)r["Id"], Address = r["Address"].ToString() }).ToList();
        PickupBox.DisplayMemberPath = "Address";
        PickupBox.SelectedValuePath = "Id";
        StatusBox.SelectedIndex = 0;

        if (id == null)
        {
            Header.Text = "Добавление заказа";
            OrderDatePicker.SelectedDate = DateTime.Today;
            DeliveryDatePicker.SelectedDate = DateTime.Today.AddDays(3);
            return;
        }

        var dt = Db.Query("SELECT * FROM Orders WHERE Id=@id", new SqlParameter("@id", id));
        if (dt.Rows.Count == 0) { Close(); return; }
        var r = dt.Rows[0];
        if (r["OrderDate"] != DBNull.Value) OrderDatePicker.SelectedDate = (DateTime)r["OrderDate"];
        if (r["DeliveryDate"] != DBNull.Value) DeliveryDatePicker.SelectedDate = (DateTime)r["DeliveryDate"];
        if (r["PickupPointId"] != DBNull.Value) PickupBox.SelectedValue = (int)r["PickupPointId"];
        ClientBox.Text = r["ClientFIO"].ToString();
        CodeBox.Text = r["Code"].ToString();
        StatusBox.Text = r["Status"].ToString().Trim();

        foreach (DataRow i in Db.Query("SELECT Article, Qty FROM OrderItems WHERE OrderId=@id", new SqlParameter("@id", id)).Rows)
            _items.Add((i["Article"].ToString(), (int)i["Qty"]));
        RefreshItems();
    }

    private void RefreshItems() =>
        ItemsList.ItemsSource = _items.Select(i => $"{i.Article}, {i.Qty}").ToList();

    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        var art = ItemArticle.Text.Trim();
        if (art.Length == 0 || !int.TryParse(ItemQty.Text, out var qty) || qty <= 0)
        {
            MessageBox.Show("Введите артикул и количество.");
            return;
        }
        _items.Add((art, qty));
        ItemArticle.Clear(); ItemQty.Clear();
        RefreshItems();
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsList.SelectedIndex >= 0) { _items.RemoveAt(ItemsList.SelectedIndex); RefreshItems(); }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(CodeBox.Text, out var code)) { MessageBox.Show("Код должен быть числом."); return; }
        var od = OrderDatePicker.SelectedDate ?? DateTime.Today;
        var dd = DeliveryDatePicker.SelectedDate ?? od;
        var status = (StatusBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString()
                     ?? StatusBox.Text ?? "Новый";

        if (_id == null)
        {
            var newId = Convert.ToInt32(Db.Scalar("SELECT ISNULL(MAX(Id),0)+1 FROM Orders"));
            Db.Exec(
                @"INSERT INTO Orders(Id,OrderDate,DeliveryDate,PickupPointId,ClientFIO,Code,Status)
                  VALUES(@id,@od,@dd,@pp,@f,@c,@s)",
                new SqlParameter("@id", newId), new SqlParameter("@od", od), new SqlParameter("@dd", dd),
                new SqlParameter("@pp", PickupBox.SelectedValue ?? DBNull.Value),
                new SqlParameter("@f", ClientBox.Text), new SqlParameter("@c", code), new SqlParameter("@s", status));
            foreach (var (art, qty) in _items)
                Db.Exec("INSERT INTO OrderItems(OrderId,Article,Qty) VALUES(@o,@a,@q)",
                    new SqlParameter("@o", newId), new SqlParameter("@a", art), new SqlParameter("@q", qty));
        }
        else
        {
            Db.Exec(
                @"UPDATE Orders SET OrderDate=@od,DeliveryDate=@dd,PickupPointId=@pp,
                  ClientFIO=@f,Code=@c,Status=@s WHERE Id=@id",
                new SqlParameter("@id", _id), new SqlParameter("@od", od), new SqlParameter("@dd", dd),
                new SqlParameter("@pp", PickupBox.SelectedValue ?? DBNull.Value),
                new SqlParameter("@f", ClientBox.Text), new SqlParameter("@c", code), new SqlParameter("@s", status));
            Db.Exec("DELETE FROM OrderItems WHERE OrderId=@id", new SqlParameter("@id", _id));
            foreach (var (art, qty) in _items)
                Db.Exec("INSERT INTO OrderItems(OrderId,Article,Qty) VALUES(@o,@a,@q)",
                    new SqlParameter("@o", _id), new SqlParameter("@a", art), new SqlParameter("@q", qty));
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
