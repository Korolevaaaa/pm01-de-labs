using System.Windows;
using Microsoft.Data.SqlClient;

namespace MebelOrg;

public partial class LoginWindow : Window
{
    public LoginWindow() { InitializeComponent(); }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        var dt = Db.Query(
            "SELECT Role, FIO FROM Users WHERE Login=@l AND Password=@p",
            new SqlParameter("@l", LoginBox.Text.Trim()),
            new SqlParameter("@p", PasswordBox.Password));

        if (dt.Rows.Count == 0)
        {
            ErrorText.Text = "Неверный логин или пароль.";
            return;
        }

        var role = dt.Rows[0]["Role"].ToString();
        var fio = dt.Rows[0]["FIO"].ToString();
        OpenProducts(role, fio);
    }

    private void Guest_Click(object sender, RoutedEventArgs e) => OpenProducts("Гость", "Гость");

    private void OpenProducts(string role, string fio)
    {
        new ProductsWindow(role, fio).Show();
        Close();
    }
}
