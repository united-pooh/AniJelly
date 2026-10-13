using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Emby.Naming.TV
{
    /// <summary>
    /// Class to parse season paths.
    /// </summary>
    public static partial class SeasonPathParser
    {
        private const string SeasonKeywordPattern =
            @"시즌|シーズン|сезон" +
            @"|season|sæson|saison|staffel|series|stagione|säsong|seizoen|seasong" +
            @"|sezon|sezona|sezóna|sezonul|série|séria|serie|seria|temporada|kausi";

        private static readonly Regex CleanNameRegex = new(@"[ ._\-\[\]]", RegexOptions.Compiled);

        [GeneratedRegex(@"^\s*((?<seasonnumber>(?>\d+))(?:st|nd|rd|th|\.)*(?!\s*[Ee]\d+))\s*(?:" + SeasonKeywordPattern + @")\s*(?<rightpart>.*)$", RegexOptions.IgnoreCase)]
        private static partial Regex ProcessPre();

        [GeneratedRegex(@"^\s*(?:" + SeasonKeywordPattern + @")\s*(?<seasonnumber>\d+?)(?=\d{3,4}p|[^\d]|$)(?!\s*[Ee]\d)(?<rightpart>.*)$", RegexOptions.IgnoreCase)]
        private static partial Regex ProcessPost();

        [GeneratedRegex(@"[sS](\d{1,4})(?!\d|[eE]\d)(?=\.|_|-|\[|\]|\s|$)", RegexOptions.None)]
        private static partial Regex SeasonPrefix();

        [GeneratedRegex(SeasonKeywordPattern, RegexOptions.IgnoreCase)]
        private static partial Regex SeasonKeyword();

        [GeneratedRegex(@"^第(?<seasonnumber>[0-9]+|[零〇一二三四五六七八九两兩]|[一二三四五六七八九两兩]?十[一二三四五六七八九]?)[季期](?!第?[0-9零〇一二三四五六七八九十两兩]+[集话話]|[Ee][0-9])")]
        private static partial Regex ChineseSeason();

        /// <summary>
        /// Attempts to parse season number from path.
        /// </summary>
        /// <param name="path">Path to season.</param>
        /// <param name="parentPath">Folder name of the parent.</param>
        /// <param name="supportSpecialAliases">Support special aliases when parsing.</param>
        /// <param name="supportNumericSeasonFolders">Support numeric season folders when parsing.</param>
        /// <returns>Returns <see cref="SeasonPathParserResult"/> object.</returns>
        public static SeasonPathParserResult Parse(string path, string? parentPath, bool supportSpecialAliases, bool supportNumericSeasonFolders)
        {
            var result = new SeasonPathParserResult();
            var parentFolderName = parentPath is null ? null : new DirectoryInfo(parentPath).Name;

            var (seasonNumber, isSeasonFolder) = GetSeasonNumberFromPath(path, parentFolderName, supportSpecialAliases, supportNumericSeasonFolders);

            result.SeasonNumber = seasonNumber;

            if (result.SeasonNumber.HasValue)
            {
                result.Success = true;
                result.IsSeasonFolder = isSeasonFolder;
            }

            return result;
        }

        /// <summary>
        /// Gets the season number from path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="parentFolderName">The parent folder name.</param>
        /// <param name="supportSpecialAliases">if set to <c>true</c> [support special aliases].</param>
        /// <param name="supportNumericSeasonFolders">if set to <c>true</c> [support numeric season folders].</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        private static (int? SeasonNumber, bool IsSeasonFolder) GetSeasonNumberFromPath(
            string path,
            string? parentFolderName,
            bool supportSpecialAliases,
            bool supportNumericSeasonFolders)
        {
            var fileName = Path.GetFileName(path);

            var seasonPrefixMatch = SeasonPrefix().Match(fileName);
            if (seasonPrefixMatch.Success &&
                int.TryParse(seasonPrefixMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
            {
                return (val, true);
            }

            string filename = CleanNameRegex.Replace(fileName, string.Empty);

            if (parentFolderName is not null)
            {
                var cleanParent = CleanNameRegex.Replace(parentFolderName, string.Empty);
                filename = filename.Replace(cleanParent, string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            if (supportSpecialAliases &&
                (filename.Equals("specials", StringComparison.OrdinalIgnoreCase) ||
                 filename.Equals("extras", StringComparison.OrdinalIgnoreCase) ||
                 filename.Equals("sp", StringComparison.OrdinalIgnoreCase) ||
                 filename.Equals("sps", StringComparison.OrdinalIgnoreCase) ||
                 filename is "特典" or "特别篇" or "特別篇"))
            {
                return (0, true);
            }

            var chineseMatch = ChineseSeason().Match(filename);
            if (chineseMatch.Success)
            {
                var number = chineseMatch.Groups["seasonnumber"].ValueSpan;
                if (char.IsAsciiDigit(number[0]))
                {
                    return int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out val)
                        ? (val, true)
                        : (null, false);
                }

                var tensIndex = number.IndexOf('十');
                var seasonNumber = tensIndex < 0
                    ? GetChineseDigit(number[0])
                    : (tensIndex == 0 ? 1 : GetChineseDigit(number[0])) * 10
                        + (tensIndex == number.Length - 1 ? 0 : GetChineseDigit(number[^1]));
                return (seasonNumber, true);
            }

            if (supportNumericSeasonFolders &&
                int.TryParse(filename, NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
            {
                return (val, true);
            }

            bool isMixedLibrary = !supportNumericSeasonFolders && !supportSpecialAliases;
            var preMatch = ProcessPre().Match(filename);
            if (preMatch.Success)
            {
                if (isMixedLibrary && !SeasonKeyword().IsMatch(fileName))
                {
                    return (null, false);
                }

                return CheckMatch(preMatch);
            }
            else
            {
                var postMatch = ProcessPost().Match(filename);
                if (postMatch.Success && isMixedLibrary && !SeasonKeyword().IsMatch(fileName))
                {
                    return (null, false);
                }

                return CheckMatch(postMatch);
            }
        }

        private static int GetChineseDigit(char digit) => digit switch
        {
            '两' or '兩' => 2,
            '〇' => 0,
            _ => "零一二三四五六七八九".IndexOf(digit, StringComparison.Ordinal)
        };

        private static (int? SeasonNumber, bool IsSeasonFolder) CheckMatch(Match match)
        {
            var numberString = match.Groups["seasonnumber"];
            if (numberString.Success)
            {
                if (int.TryParse(numberString.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seasonNumber))
                {
                    return (seasonNumber, true);
                }
            }

            return (null, false);
        }

        /// <summary>
        /// Extracts the season number from the second half of the Season folder name (everything after "Season", or "Staffel").
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        private static (int? SeasonNumber, bool IsSeasonFolder) GetSeasonNumberFromPathSubstring(ReadOnlySpan<char> path)
        {
            var numericStart = -1;
            var length = 0;

            var hasOpenParenthesis = false;
            var isSeasonFolder = true;

            // Find out where the numbers start, and then keep going until they end
            for (var i = 0; i < path.Length; i++)
            {
                if (char.IsNumber(path[i]))
                {
                    if (!hasOpenParenthesis)
                    {
                        if (numericStart == -1)
                        {
                            numericStart = i;
                        }

                        length++;
                    }
                }
                else if (numericStart != -1)
                {
                    // There's other stuff after the season number, e.g. episode number
                    isSeasonFolder = false;
                    break;
                }

                var currentChar = path[i];
                if (currentChar == '(')
                {
                    hasOpenParenthesis = true;
                }
                else if (currentChar == ')')
                {
                    hasOpenParenthesis = false;
                }
            }

            if (numericStart == -1)
            {
                return (null, isSeasonFolder);
            }

            return (int.Parse(path.Slice(numericStart, length), provider: CultureInfo.InvariantCulture), isSeasonFolder);
        }
    }
}
