using System.Windows;

namespace MebelOrg;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Window w;
        if (e.Args.Length >= 2 && e.Args[0] == "--as")
        {
            var role = e.Args[1];
            if (e.Args.Length >= 4 && e.Args[2] == "--win" && e.Args[3] == "orders")
                w = new OrdersWindow(role);
            else if (e.Args.Length >= 4 && e.Args[2] == "--win" && e.Args[3] == "edit")
                w = new ProductEditWindow(null);
            else
                w = new ProductsWindow(role, role);
        }
        else w = new LoginWindow();
        MainWindow = w;
        w.Show();
    }
}
