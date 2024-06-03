using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLIB;
using System.IO;

public static class DatabaseInitializer
{
    private static SQLiteConnection _dbConnection;
    private static SQLiteConnection _deathsDbConnection;
    private static SQLiteConnection _survivedDbConnection;
    private static SQLiteConnection _winsDbConnection;
    private static SQLiteConnection _fourKDbConnection;
    private static SQLiteConnection _lostMoneyDbConnection;

    public static void InitializeDatabase()
    {
        SQLiteConnection _dbConnection = new SQLiteConnection("Data Source=C:\\JaiBot Stuff\\SQLite\\database\\JaiCoin.db;Version=3;");
        _dbConnection.Open();

        SQLiteConnection _deathsDbConnection = new SQLiteConnection("Data Source=C:\\JaiBot Stuff\\SQLite\\database\\DeathsCounter.db;Version=3;");
        _deathsDbConnection.Open();
        using (var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS UserBalances (Username TEXT PRIMARY KEY, Balance INTEGER)", _deathsDbConnection))
        {
            command.ExecuteNonQuery();
        }

        SQLiteConnection _lostMoneyDbConnection = new SQLiteConnection("Data Source=C:\\JaiBot Stuff\\SQLite\\database\\LostMoney.db;Version=3;");
        _lostMoneyDbConnection.Open();
        using (var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS LostMoney (Username TEXT, LostAmount INTEGER)", _lostMoneyDbConnection))
        {
            command.ExecuteNonQuery();
        }

        SQLiteConnection _survivedDbConnection = new SQLiteConnection("Data Source=C:\\JaiBot Stuff\\SQLite\\database\\SurvivedCounter.db;Version=3;");
        _survivedDbConnection.Open();
        using (var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS UserBalances (Username TEXT PRIMARY KEY, Balance INTEGER)", _survivedDbConnection))
        {
            command.ExecuteNonQuery();
        }

        SQLiteConnection _winsDbConnection = new SQLiteConnection("Data Source=C:\\JaiBot Stuff\\SQLite\\database\\WinsCounter.db;Version=3;");
        _winsDbConnection.Open();
        using (var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS UserBalances (Username TEXT PRIMARY KEY, Balance INTEGER)", _winsDbConnection))
        {
            command.ExecuteNonQuery();
        }

        SQLiteConnection _fourKDbConnection = new SQLiteConnection("Data Source=C:\\JaiBot Stuff\\SQLite\\database\\FourKCounter.db;Version=3;");
        _fourKDbConnection.Open();
        using (var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS UserBalances (Username TEXT PRIMARY KEY, Balance INTEGER)", _fourKDbConnection))
        {
            command.ExecuteNonQuery();
        }

        using (var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS UserBalances (Username TEXT PRIMARY KEY, Balance INTEGER)", _dbConnection))
        {
            command.ExecuteNonQuery();
        }
    }
}
