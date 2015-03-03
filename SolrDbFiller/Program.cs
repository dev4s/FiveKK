using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using SolrDbFiller.Db;

namespace SolrDbFiller
{
	class Program
	{
		static void Main()
		{
			var file = new FileInfo("data.txt");
			var names = new List<string>();
			var addresses = new List<string>();
			
			using (var reader = file.OpenText())
			{
				while (!reader.EndOfStream)
				{
					var line = reader.ReadLine();
					if (line == null) continue;

					var nameAddress = line.Replace("&", string.Empty).Split('-');

					var name = nameAddress[0].Trim().Split(' ');
					names.AddRange(name.Select(x => x.Trim().Replace(",", string.Empty)));

					if (nameAddress[1].Trim().Equals("(null)")) continue;

					var address = nameAddress[1].Trim().Split(',');
					addresses.AddRange(address.Select(x => x.Trim()));
				}
			}

			addresses.RemoveAll(string.IsNullOrEmpty);

			var sw = Stopwatch.StartNew();

			for (int j = 0; j < 50; j++)
			{
				var tasks = new List<Task>();
				for (int i = 0; i < 10; i++)
				{
					tasks.Add(SaveToDatabase(names, addresses));
				}

				Task.WaitAll(tasks.ToArray(), -1);
			}

			sw.Stop();
			Console.WriteLine("Total time (for 5kk): {0}:{1}:{2}", sw.Elapsed.Hours, sw.Elapsed.Minutes, sw.Elapsed.Seconds);
			Console.ReadKey();
		}

		private static async Task SaveToDatabase(IReadOnlyList<string> names, IReadOnlyList<string> addresses)
		{
			const int count = 10000;

			using (var dbContext = new SolrDbContext())
			{
				dbContext.Configuration.AutoDetectChangesEnabled = false;

				for (int i = 1; i <= count; i++)
				{
					var n1 = RandomGen.Next(names.Count);
					var n2 = RandomGen.Next(names.Count);
					var n3 = RandomGen.Next(names.Count);

					var a1 = RandomGen.Next(addresses.Count);
					var a2 = RandomGen.Next(addresses.Count);
					var a3 = RandomGen.Next(addresses.Count);

					var company = new Company
					{
						Name = string.Format("{0} {1} {2}", names[n1], names[n2], names[n3]),
						Address = string.Format("{0}, {1}, {2}", addresses[a1], addresses[a2], addresses[a3])
					};

					dbContext.Companies.Add(company);

					if (i % 50 == 0)
					{
						await dbContext.SaveChangesAsync();
					}

					Console.WriteLine("{0}: {1} - {2}", i, company.Name, company.Address);
				}
			}
		}

		private static class RandomGen
		{
			private static readonly RNGCryptoServiceProvider Global = new RNGCryptoServiceProvider();
			[ThreadStatic]
			private static Random local;

			public static int Next(int maxValue)
			{
				Random inst = local;
				if (inst != null) return inst.Next(maxValue);

				var buffer = new byte[4];
				Global.GetBytes(buffer);
				local = inst = new Random(BitConverter.ToInt32(buffer, 0));
				return inst.Next(maxValue);
			}
		}
	}
}
