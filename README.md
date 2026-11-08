# Лабораторные работы ПМ.01 (ДЭ 09.02.07)

- **VelosipedDrive/** — ООО «ВелосипедДрайв»: модули 1, 2, 3 (авторизация, роли, список товаров, CRUD + изображения)
- **MebelOrg/** — ООО «МебельОрг» (вариант 4): модули 1, 2, 3
- **sql/** — скрипты создания и наполнения БД (MS SQL Server)

## Запуск
1. Восстановить БД: `sqlcmd -S "(localdb)\MSSQLLocalDB" -i sql/velosiped.sql` (таблицы уже есть в LocalDB)
2. `dotnet build -c Release` в папке проекта
3. Запуск exe или `dotnet VelosipedDrive.dll --as Администратор`
