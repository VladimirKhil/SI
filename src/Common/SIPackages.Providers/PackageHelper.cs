using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIPackages.Providers
{
	/// <summary>
	/// Класс, генерирующий случайные пакеты
	/// </summary>
	public static class PackageHelper
	{
		private static readonly Random Rand = new Random();

		/// <summary>
		/// Индикатор случайного пакета
		/// </summary>
		public const string RandomIndicator = "{random}";

		public static async Task<SIDocument> GenerateRandomPackage(IPackagesProvider provider, string name, string author, string roundNameFormat, int roundsCount = 3, int themesCount = 6, int baseCost = 100, Stream stream = null)
		{
			var doc = SIDocument.Create(name, author, stream);
			return await GenerateCore(provider, roundsCount, themesCount, baseCost, doc, roundNameFormat);
		}

		public static async Task<SIDocument> GenerateRandomPackage(IPackagesProvider provider, string folder, string name, string author, string roundNameFormat, int roundsCount = 3, int themesCount = 6, int baseCost = 100)
		{
			var doc = SIDocument.Create(name, author, folder);
			return await GenerateCore(provider, roundsCount, themesCount, baseCost, doc, roundNameFormat);
		}

		private static async Task<SIDocument> GenerateCore(IPackagesProvider provider, int roundsCount, int themesCount, int baseCost, SIDocument doc, string roundNameFormat)
		{
			var files = (await provider.GetPackages()).ToList();

			var packageComments = new StringBuilder(RandomIndicator); // Информация для отчёта об игре

			for (var i = 0; i < roundsCount; i++)
			{
				doc.Package.Rounds.Add(new Round { Type = RoundTypes.Standart, Name = string.Format(roundNameFormat, i + 1) });
				for (int j = 0; j < themesCount; j++)
				{
					if (files.Count == 0)
						break;

					if (!await ExtractTheme(provider, doc, files, i, round => round.Type == RoundTypes.Standart && round.Themes.Count > 0/*, usedThemes*/, packageComments, baseCost))
					{
						j--;
						continue;
					}
				}

				if (files.Count == 0)
					break;
			}

			doc.Package.Rounds.Add(new Round { Type = RoundTypes.Final, Name = "ФИНАЛ" });
			for (var j = 0; j < 7; j++)
			{
				if (files.Count == 0)
					break;

				if (!await ExtractTheme(provider, doc, files, roundsCount, round => round.Type == RoundTypes.Final && round.Themes.Count > 0/*, usedThemes*/, packageComments, 0))
				{
					j--;
					continue;
				}
			}

			// В пакете могут быть и свои комментарии; допишем туда
			doc.Package.Info.Comments.Text += packageComments.ToString();

			return doc;
		}

		private static async Task<bool> ExtractTheme(IPackagesProvider provider, SIDocument doc, List<string> files, int roundIndex, Func<Round, bool> predicate/*, List<int> usedThemes*/, StringBuilder packageComments, int baseCost)
		{
			var fIndex = Rand.Next(files.Count);
			var doc2 = await provider.GetPackage(files[fIndex]);
			if (doc2 == null)
			{
				throw new PackageNotFoundException(files[fIndex]);
			}

			using (doc2)
			{
				var normal = doc2.Package.Rounds.Where(predicate).ToList();
				var count = normal.Count;
				if (count == 0)
				{
					files.RemoveAt(fIndex);
					return false;
				}

				var rIndex = Rand.Next(count);
				var r = normal[rIndex];
				var tIndex = Rand.Next(r.Themes.Count);

				var theme = r.Themes[tIndex];

				// Исключим повторения имён тем
				if (doc.Package.Rounds.Any(round => round.Themes.Any(th => th.Name == theme.Name)))
					return false;

				// Нужно перенести в пакет необходимых авторов, источники, медиаконтент
				InheritAuthors(doc2, r, theme);
				InheritSources(doc2, r, theme);

				for (int i = 0; i < theme.Questions.Count; i++)
				{
					theme.Questions[i].Price = (roundIndex + 1) * (i + 1) * baseCost;

					InheritAuthors(doc2, theme.Questions[i]);
					InheritSources(doc2, theme.Questions[i]);
					await InheritContent(doc, doc2, theme.Questions[i]);
				}

				doc.Package.Rounds[roundIndex].Themes.Add(theme);
				packageComments.AppendFormat("{0}:{1}:{2};", files[fIndex], doc2.Package.Rounds.IndexOf(r), tIndex);
				return true;
			}
		}

		private static void InheritAuthors(SIDocument doc2, Question question)
		{
			var authors = question.Info.Authors;
			if (authors.Count > 0)
			{
				for (int i = 0; i < authors.Count; i++)
				{
					var link = doc2.GetLink(question.Info.Authors, i, out string tail);
					if (link != null)
						question.Info.Authors[i] = link + tail;
				}
			}
		}

		private static void InheritSources(SIDocument doc2, Question question)
		{
			var sources = question.Info.Sources;
			if (sources.Count > 0)
			{
				for (int i = 0; i < sources.Count; i++)
				{
					var link = doc2.GetLink(question.Info.Sources, i, out string tail);
					if (link != null)
						question.Info.Sources[i] = link + tail;
				}
			}
		}

		private static async Task InheritContent(SIDocument doc, SIDocument doc2, Question question)
		{
			foreach (var atom in question.Scenario)
			{
				if (atom.Type == AtomTypes.Text || atom.Type == AtomTypes.Oral)
					continue;

				var link = doc2.GetLink(atom);
				if (link.GetStream != null)
				{
					DataCollection collection = null;
					switch (atom.Type)
					{
						case AtomTypes.Video:
							collection = doc.Video;
							break;

						case AtomTypes.Audio:
							collection = doc.Audio;
							break;

						case AtomTypes.Image:
							collection = doc.Images;
							break;
					}

					if (collection != null)
					{
						using (var stream = link.GetStream().Stream)
						{
							await collection.AddFile(link.Uri, stream);
						}
					}
				}
			}
		}

		private static void InheritAuthors(SIDocument doc2, Round round, Theme theme)
		{
			var authors = theme.Info.Authors;
			if (authors.Count == 0)
			{
				authors = round.Info.Authors;
				if (authors.Count == 0)
					authors = doc2.Package.Info.Authors;

				if (authors.Count > 0)
				{
					var realAuthors = doc2.GetRealAuthors(authors);
					theme.Info.Authors.Clear();

					foreach (var item in realAuthors)
					{
						theme.Info.Authors.Add(item);
					}
				}
			}
			else
			{
				for (int i = 0; i < authors.Count; i++)
				{
					var link = doc2.GetLink(theme.Info.Authors, i, out string tail);
					if (link != null)
					{
						theme.Info.Authors[i] = link + tail;
					}
				}
			}
		}

		private static void InheritSources(SIDocument doc2, Round round, Theme theme)
		{
			var sources = theme.Info.Sources;
			if (sources.Count == 0)
			{
				sources = round.Info.Sources;
				if (sources.Count == 0)
					sources = doc2.Package.Info.Sources;

				if (sources.Count > 0)
				{
					var realSources = doc2.GetRealSources(sources);
					theme.Info.Sources.Clear();

					foreach (var item in realSources)
					{
						theme.Info.Sources.Add(item);
					}
				}
			}
			else
			{
				for (int i = 0; i < sources.Count; i++)
				{
					var link = doc2.GetLink(theme.Info.Sources, i, out string tail);
					if (link != null)
					{
						theme.Info.Sources[i] = link + tail;
					}
				}
			}
		}
	}
}
