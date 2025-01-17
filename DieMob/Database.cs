﻿using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Data;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace DieMob;

public static class Database
{
	public static IDbConnection db;
	public static List<DieMobRegion> DieMobRegions = new();

	public static void Connect()
	{
		switch (TShock.Config.Settings.StorageType.ToLower())
		{
			case "mysql":
				string[] dbHost = TShock.Config.Settings.MySqlHost.Split(':');
				db = new MySqlConnection()
				{
					ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
						dbHost[0],
						dbHost.Length == 1 ? "3306" : dbHost[1],
						TShock.Config.Settings.MySqlDbName,
						TShock.Config.Settings.MySqlUsername,
						TShock.Config.Settings.MySqlPassword)

				};
				break;

			case "sqlite":
				string sql = Path.Combine(TShock.SavePath, "DieMob.sqlite");
				db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
				break;

		}

		SqlTableCreator sqlcreator = new(Database.db, Database.db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

		sqlcreator.EnsureTableStructure(new SqlTable("DieMobRegions",
			new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, Unique = true, AutoIncrement = true, Length = 6 },
			new SqlColumn("Region", MySqlDbType.VarChar) { Length = 30 },
			new SqlColumn("WorldID", MySqlDbType.Int32),
			new SqlColumn("AffectFriendlyNPCs", MySqlDbType.Int32),
			new SqlColumn("AffectStatueSpawns", MySqlDbType.Int32),
			new SqlColumn("ReplaceMobs", MySqlDbType.Text),
			new SqlColumn("Type", MySqlDbType.Int32)));

	}
	public static void DieMob_Read()
	{
		QueryResult reader;

		reader = db.QueryReader("SELECT * FROM DieMobRegions WHERE WorldID=@0", Main.worldID);
		List<string> obsoleteRegions = new();
		while (reader.Read())
		{
			var regionName = reader.Get<string>("Region");
			var region = TShock.Regions.GetRegionByName(regionName);
			if (region != null && region.Name != "")
			{
				DieMobRegions.Add(new DieMobRegion(region)
				{
					AffectFriendlyNPCs = reader.Get<bool>("AffectFriendlyNPCs"),
					AffectStatueSpawns = reader.Get<bool>("AffectStatueSpawns"),
					ReplaceMobs = JsonConvert.DeserializeObject<Dictionary<int, int>>(reader.Get<string>("ReplaceMobs")),
					Type = (RegionType)reader.Get<int>("Type")
				});
			}
			else
			{
				obsoleteRegions.Add(regionName);
			}
		}
		reader.Dispose();
		foreach (string region in obsoleteRegions)
		{
			Console.WriteLine("Deleting region from DB: " + region);
			db.Query("DELETE FROM DieMobRegions WHERE Region=@0 AND WorldID=@1", region, Main.worldID);

		}
	}
	public static bool DieMob_Add(string name)
	{
		db.Query("INSERT INTO DieMobRegions (Region, WorldID, AffectFriendlyNPCs, AffectStatueSpawns, Type, ReplaceMobs) VALUES (@0, @1, 0, 0, 0, @2)",
			name, Main.worldID, JsonConvert.SerializeObject(new Dictionary<int, int>()));
		return true;
	}
	public static void DieMob_Delete(string name)
	{
		db.Query("DELETE FROM DieMobRegions WHERE Region=@0 AND WorldID=@1", name, Main.worldID);
		for (int i = 0; i < DieMobRegions.Count; i++)
		{
			if (DieMobRegions[i].TSRegion.Name == name)
			{

				DieMobRegions.RemoveAt(i);
				break;
			}
		}
	}
	public static void Diemob_Update(DieMobRegion region)
	{
		db.Query("UPDATE DieMobRegions SET AffectFriendlyNPCs=@2, AffectStatueSpawns=@3, Type=@4, ReplaceMobs=@5 WHERE Region=@0 AND WorldID=@1",
			region.TSRegion.Name, Main.worldID, region.AffectFriendlyNPCs, region.AffectStatueSpawns,
			(int)region.Type, JsonConvert.SerializeObject(region.ReplaceMobs));
	}

	public static DieMobRegion GetRegionByName(string name)
	{
		return DieMobRegions.FirstOrDefault(e => e.TSRegion.Name == name);
	}
}
