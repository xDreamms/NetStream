using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NetStream
{
    public class Helper
    {
        public static List<string> Regexes
        {
            get
            {
                List<string> regexes = new List<string>();
                regexes.Add("[sS][0-9]+[eE][0-9]+-*[eE]*[0-9]*");
                regexes.Add("[0-9]+[xX][0-9]+");

                return regexes;
            }
        }

        public static int GetSeasonNumberFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return -2;
            }

            try
            {
                foreach (string regex in Regexes)
                {
                    Match match = Regex.Match(fileName, regex);
                    if (match.Success)
                    {
                        string matched = match.Value.ToLower();
                        if (regex.Contains("e")) //SDDEDD
                        {
                            matched = matched.Replace("s", "");
                            int eIndex = matched.IndexOf("e");
                            if (eIndex < 0) continue;

                            matched = matched.Substring(0, eIndex);
                            if (int.TryParse(matched, out int seasonNumber))
                            {
                                return seasonNumber;
                            }
                        }
                        else if (regex.Contains("x")) //DDXDD
                        {
                            int xIndex = matched.IndexOf("x");
                            if (xIndex < 0) continue;

                            matched = matched.Substring(0, xIndex);
                            if (int.TryParse(matched, out int seasonNumber))
                            {
                                return seasonNumber;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting season number: {ex.Message}");
            }

            return -2;
        }

        public static int GetEpisodeNumberFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return -2;
            }

            try
            {
                foreach (string regex in Regexes)
                {
                    Match match = Regex.Match(fileName, regex);
                    if (match.Success)
                    {
                        string matched = match.Value.ToLower();
                        if (regex.Contains("e")) //SDDEDD
                        {
                            int eIndex = matched.IndexOf("e");
                            if (eIndex < 0) continue;

                            matched = matched.Substring(eIndex + 1);

                            if (matched.Contains("e") || matched.Contains("-"))
                            {
                                int secondIndex = matched.Contains("e") ?
                                    matched.IndexOf("e") : matched.IndexOf("-");

                                if (secondIndex < 0) continue;

                                matched = matched.Substring(0, secondIndex).Replace("-", "");
                            }

                            if (int.TryParse(matched, out int episodeNumber))
                            {
                                return episodeNumber;
                            }
                        }
                        else if (regex.Contains("x")) //DDXDD
                        {
                            int xIndex = matched.IndexOf("x");
                            if (xIndex < 0) continue;

                            matched = matched.Substring(xIndex + 1);
                            if (int.TryParse(matched, out int episodeNumber))
                            {
                                return episodeNumber;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting episode number: {ex.Message}");
            }

            return -2;
        }
    }
}

