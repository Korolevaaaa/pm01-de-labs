using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;

namespace VelosipedDrive;

public class OrderVm
{
    public int Id { get; set; }
    public int Code { get; set; }
    public string Composition { get; set; }
    public string OrderDate { get; set; }
    public string DeliveryDate { get; set; }
    public string Pickup { get; set; }
    public string ClientFIO { get; set; }
    public string Status { get; set; }
}

public partial class OrdersWindow : Window
{
    private readonly string _role;

    public OrdersWindow(string role)
    {
        InitializeComponent();
        _role = role;
        if (role == "Администратор") AdminPanel.Visibility = Visibility.Visible;
        LoadOrders();
    }

    private void LoadOrders()
    {
        var dt = Db.Query(
            @"SELECT o.*, p.Address FROM Orders o
              LEFT JOIN PickupPoints p ON p.Id=o.PickupPointId ORDER BY o.Id");
        var items = Db.Query("SELECT OrderId, Article, Qty FROM OrderItems")
            .Rows.Cast<DataRow>()
            .GroupBy(r => (int)r["OrderId"])
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(r => $"{r["Article"]}, {r["Qty"]}")));
        OrderList.ItemsSource = dt.Rows.Cast<DataRow>().Select(r => new OrderVm
        {
            Id = (int)r["Id"],
            Code = (int)r["Code"],
            OrderDate = r["OrderDate"] == DBNull.Value ? "" : Convert.ToDateTime(r["OrderDate"]).ToString("dd.MM.yyyy"),
            DeliveryDate = r["DeliveryDate"] == DBNull.Value ? "" : Convert.ToDateTime(r["DeliveryDate"]).ToString("dd.MM.yyyy"),
            Pickup = r["Address"].ToString(),
            ClientFIO = r["ClientFIO"].ToString(),
            Status = r["Status"].ToString(),
            Composition = items.TryGetValue((int)r["Id"], out var c) ? c : ""
        }).ToList();
    }

    private OrderVm Selected => OrderList.SelectedItem as OrderVm;

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (new OrderEditWindow(null).ShowDialog() == true) LoadOrders();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Selected == null) { MessageBox.Show("Выберите заказ."); return; }
        if (new OrderEditWindow(Selected.Id).ShowDialog() == true) LoadOrders();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Selected == null) { MessageBox.Show("Выберите заказ."); return; }
        if (MessageBox.Show($"Удалить заказ №{Selected.Id}?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        Db.Exec("DELETE FROM OrderItems WHERE OrderId=@id", new SqlParameter("@id", Selected.Id));
        Db.Exec("DELETE FROM Orders WHERE Id=@id", new SqlParameter("@id", Selected.Id));
        LoadOrders();
    }
}
