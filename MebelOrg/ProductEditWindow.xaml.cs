using System;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace MebelOrg;

public partial class ProductEditWindow : Window
{
    private readonly string _article;   // null => добавление
    private FileInfo _selectedImage;    // выбранное новое фото (пока только на диске)
    private bool _photoRemoved;

    private string ImagesDir => Path.Combine(AppContext.BaseDirectory, "Images");

    public ProductEditWindow(string article)
    {
        InitializeComponent();
        _article = article;
        if (article == null)
        {
            Header.Text = "Добавление товара";
            PhotoPreview.Source = LoadImageNoLock(Path.Combine(ImagesDir, "picture.png"));
            return;
        }

        var dt = Db.Query("SELECT * FROM Products WHERE Article=@a", new SqlParameter("@a", article));
        if (dt.Rows.Count == 0) { Close(); return; }
        var r = dt.Rows[0];
        ArticleBox.Text = r["Article"].ToString(); ArticleBox.IsEnabled = false;
        NameBox.Text = r["Name"].ToString();
        UnitBox.Text = r["Unit"].ToString();
        PriceBox.Text = r["Price"].ToString();
        SupplierBox.Text = r["Supplier"].ToString();
        ManufacturerBox.Text = r["Manufacturer"].ToString();
        CategoryBox.Text = r["Category"].ToString();
        DiscountBox.Text = r["Discount"].ToString();
        StockBox.Text = r["Stock"].ToString();
        DescriptionBox.Text = r["Description"].ToString();
        var photo = r["Photo"].ToString();
        PhotoPathText.Text = photo;
        PhotoPreview.Source = LoadImageNoLock(
            File.Exists(Path.Combine(ImagesDir, photo))
                ? Path.Combine(ImagesDir, photo)
                : Path.Combine(ImagesDir, "picture.png"));
    }

    // Загрузка картинки без удержания файлового потока —
    // иначе файл нельзя будет удалить/заменить, пока живёт UI.
    private static BitmapImage LoadImageNoLock(string path)
    {
        var bi = new BitmapImage();
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.StreamSource = fs;
            bi.EndInit();
        }
        bi.Freeze();
        return bi;
    }

    private void PickPhoto_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp" };
        if (dlg.ShowDialog() != true) return;
        _selectedImage = new FileInfo(dlg.FileName);
        _photoRemoved = false;
        PhotoPreview.Source = LoadImageNoLock(_selectedImage.FullName);
        PhotoPathText.Text = _selectedImage.FullName;
    }

    private void ClearPhoto_Click(object sender, RoutedEventArgs e)
    {
        _selectedImage = null;
        _photoRemoved = true;
        PhotoPathText.Text = "";
        PhotoPreview.Source = null;
    }

    // Безопасное копирование файла изображения в папку приложения.
    // Если файл с таким именем уже есть — делаем уникальное имя.
    private string SaveImage(FileInfo src)
    {
        var name = src.Name;
        var dest = Path.Combine(ImagesDir, name);
        if (File.Exists(dest))
            name = Path.GetFileNameWithoutExtension(src.Name) + "_" +
                   DateTime.Now.ToString("yyyyMMddHHmmss") + src.Extension;
        dest = Path.Combine(ImagesDir, name);
        try
        {
            src.CopyTo(dest, overwrite: true);
        }
        catch (IOException ex)
        {
            MessageBox.Show("Не удалось сохранить файл изображения: " + ex.Message);
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show("Нет прав на запись изображения: " + ex.Message);
            throw;
        }
        return name;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ArticleBox.Text) || string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show("Заполните артикул и наименование.");
            return;
        }
        if (!decimal.TryParse(PriceBox.Text, out var price) || price < 0)
        {
            MessageBox.Show("Цена должна быть неотрицательным числом.");
            return;
        }
        if (!int.TryParse(DiscountBox.Text, out var disc) || disc < 0 || disc > 100)
        {
            MessageBox.Show("Скидка должна быть целым числом от 0 до 100.");
            return;
        }
        if (!int.TryParse(StockBox.Text, out var stock) || stock < 0)
        {
            MessageBox.Show("Количество на складе должно быть целым неотрицательным числом.");
            return;
        }

        string photo = null;
        if (_photoRemoved) photo = null;
        else if (_selectedImage != null) photo = SaveImage(_selectedImage);
        else photo = PhotoPathText.Text is { Length: > 0 } p ? p : null;

        object photoParam = string.IsNullOrEmpty(photo) ? DBNull.Value : photo;

        if (_article == null)
        {
            Db.Exec(
                @"INSERT INTO Products(Article,Name,Unit,Price,Supplier,Manufacturer,Category,Discount,Stock,Description,Photo)
                  VALUES(@a,@n,@u,@pr,@s,@m,@c,@d,@st,@de,@ph)",
                new SqlParameter("@a", ArticleBox.Text.Trim()), new SqlParameter("@n", NameBox.Text), new SqlParameter("@u", UnitBox.Text),
                new SqlParameter("@pr", price), new SqlParameter("@s", SupplierBox.Text), new SqlParameter("@m", ManufacturerBox.Text),
                new SqlParameter("@c", CategoryBox.Text), new SqlParameter("@d", disc), new SqlParameter("@st", stock),
                new SqlParameter("@de", DescriptionBox.Text), new SqlParameter("@ph", photoParam));
        }
        else
        {
            Db.Exec(
                @"UPDATE Products SET Name=@n,Unit=@u,Price=@pr,Supplier=@s,Manufacturer=@m,
                  Category=@c,Discount=@d,Stock=@st,Description=@de,Photo=@ph WHERE Article=@a",
                new SqlParameter("@a", _article), new SqlParameter("@n", NameBox.Text), new SqlParameter("@u", UnitBox.Text),
                new SqlParameter("@pr", price), new SqlParameter("@s", SupplierBox.Text), new SqlParameter("@m", ManufacturerBox.Text),
                new SqlParameter("@c", CategoryBox.Text), new SqlParameter("@d", disc), new SqlParameter("@st", stock),
                new SqlParameter("@de", DescriptionBox.Text), new SqlParameter("@ph", photoParam));
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
