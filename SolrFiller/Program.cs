using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CsQuery;
using CsQuery.ExtensionMethods;

namespace UsaDownloader
{
	class Program
	{
		static void Main(string[] args)
		{
			GetAndSaveUsaCompanies();
		}

		private static void GetAndSaveUsaCompanies()
		{
			const string wikiUrl = "http://en.wikipedia.org";
			string mainWikiListOfCompaniesPage = string.Format("{0}/wiki/List_of_companies_of_the_United_States", wikiUrl);

			List<IDomObject> wikiListOfCompanies =
				CQ.CreateFromUrl(mainWikiListOfCompaniesPage).Select("ul li a[href^=/wiki]").ToList();
			int zildjianCompany = wikiListOfCompanies.IndexOf(x => x.InnerText.ToLower().Contains("zildjian"));
			wikiListOfCompanies.RemoveRange(zildjianCompany + 1, wikiListOfCompanies.Count - zildjianCompany - 1);

			var listOfCompanies = new List<Company>();

			foreach (var wikiPage in wikiListOfCompanies)
			{
				var company = new Company {Name = wikiPage.InnerText.Replace("amp;", "&")};

				GetHeadquarter(wikiUrl, wikiPage, company);

				Console.WriteLine("Found {0} - {1}", company.Name, company.Address);

				listOfCompanies.Add(company);
			}

			var fileinfo = new FileInfo("data.txt");
			using (var stream = fileinfo.CreateText())
			{
				foreach (var company in listOfCompanies)
				{
					stream.WriteLine("{0} - {1}", company.Name, company.Address);
					stream.Flush();
				}
			}
		}

		private static void GetHeadquarter(string wikiUrl, IDomObject wikiPage, Company company)
		{
			string companyUrl = string.Format("{0}{1}", wikiUrl, wikiPage.Attributes["href"]);
			IEnumerable<IDomObject> headquarterSearch =
				CQ.CreateFromUrl(companyUrl)
					.Select("table.infobox tr")
					.Where(x => x.InnerHTML.ToLower().Contains("headquarter"))
					.ToList();

			IDomObject headquarter = headquarterSearch.FirstOrDefault();
			if (headquarter != null)
			{
				company.Address = string.Empty;

				const string regexPattern = "<[^>]*>";
				var cleanedAddress =
					Regex.Replace(headquarter.InnerHTML, regexPattern, "").Replace('\n', ' ').Replace("Headquarters", "").Trim();
				company.Address = cleanedAddress;
			}
			else
			{
				company.Address = "(null)";
			}
		}

		public class Company
		{
			public string Name { get; set; }
			public string Address { get; set; }
		}
	}
}
