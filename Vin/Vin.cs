#region license
// Vin
// .NET Library for Validating Vehicle Identification Numbers
// Copyright 2016 Dale Newman
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//       http://www.apache.org/licenses/LICENSE-2.0
//   
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DaleNewman
{

    public static class Vin
    {

        private static readonly object LockObject = new object();

        private const int ValidVinLength = 17;
        private const int CheckDigitIndex = 8;
        // Character weights for 17 characters in VIN
        private static readonly int[] CharacterWeights = { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 };
        private static readonly int DefaultYear = (DateTime.Now.Year / 30) * 30;
        private static readonly int NextYear = DateTime.Now.Year + 1;

        //private static 

        private static readonly Dictionary<char, int> Years = new Dictionary<char, int> {
            { 'A', 0 },
            { 'B', 1 },
            { 'C', 2 },
            { 'D', 3 },
            { 'E', 4 },
            { 'F', 5 },
            { 'G', 6 },
            { 'H', 7 },
            { 'J', 8 },
            { 'K', 9 },
            { 'L', 10 },
            { 'M', 11 },
            { 'N', 12 },
            { 'P', 13 },
            { 'R', 14 },
            { 'S', 15 },
            { 'T', 16 },
            { 'V', 17 },
            { 'W', 18 },
            { 'X', 19 },
            { 'Y', 20 },
            { '1', 21 },
            { '2', 22 },
            { '3', 23 },
            { '4', 24 },
            { '5', 25 },
            { '6', 26 },
            { '7', 27 },
            { '8', 28 },
            { '9', 29 }
        };

        private static readonly Dictionary<char, int> ValidCheckCharacters = new Dictionary<char, int> {
            {'0',0},
            {'1',1},
            {'2',2},
            {'3',3},
            {'4',4},
            {'5',5},
            {'6',6},
            {'7',7},
            {'8',8},
            {'9',9},
            {'X',10}
        };

        // Character transliteration, how to get from characters to numbers
        private static readonly Dictionary<char, int> CharacterTransliteration = new Dictionary<char, int> {
            {'A', 1},
            {'B', 2},
            {'C', 3},
            {'D', 4},
            {'E', 5},
            {'F', 6},
            {'G', 7},
            {'H', 8},
            {'J', 1},
            {'K', 2},
            {'L', 3},
            {'M', 4},
            {'N', 5},
            {'P', 7},
            {'R', 9},
            {'S', 2},
            {'T', 3},
            {'U', 4},
            {'V', 5},
            {'W', 6},
            {'X', 7},
            {'Y', 8},
            {'Z', 9},
            {'1', 1},
            {'2', 2},
            {'3', 3},
            {'4', 4},
            {'5', 5},
            {'6', 6},
            {'7', 7},
            {'8', 8},
            {'9', 9},
            {'0', 0}
        };

        // lock and load wmi if GetWorldManufacturer is used
        private static Dictionary<string, string> _wmi;
        private static Dictionary<string, string> WorldManufacturerIdentifiers
        {
            get
            {
                if (_wmi == null)
                {
                    lock (LockObject)
                    {
                        var data = File.ReadAllText("Data/manufacturers.json");
                        var deserealized = JsonSerializer.Deserialize<CodeValue[]>(data);

                        _wmi = deserealized.DistinctBy(d => d.Code)
                            .ToDictionary(value => value.Code, value => value.Name);


                    }
                }
                return _wmi;
            }
        }

        public static bool IsValid(string vin)
        {

            var value = 0;

            if (vin?.Length != ValidVinLength)
            {
                return false;
            }

            var checkCharacter = vin[CheckDigitIndex];
            if (!ValidCheckCharacters.ContainsKey(checkCharacter))
            {
                return false;
            }

            for (var i = 0; i < ValidVinLength; i++)
            {
                if (!CharacterTransliteration.ContainsKey(vin[i]))
                {
                    return false;
                }
                value += (CharacterWeights[i] * (CharacterTransliteration[vin[i]]));
            }

            return (value % 11) == ValidCheckCharacters[checkCharacter];
        }

        public static string GetWorldManufacturer(string vinOrWmi)
        {
            if (string.IsNullOrEmpty(vinOrWmi))
                return string.Empty;

            if (vinOrWmi.Length < 2)
                return string.Empty;

            if (vinOrWmi.Length > 2 && WorldManufacturerIdentifiers.ContainsKey(vinOrWmi.Substring(0, 3)))
            {
                return WorldManufacturerIdentifiers[vinOrWmi.Substring(0, 3)];
            }


            return WorldManufacturerIdentifiers.Keys.Any(d => d.StartsWith(vinOrWmi.Substring(0, 2)))
                ? WorldManufacturerIdentifiers.FirstOrDefault(pair => pair.Key.StartsWith(vinOrWmi.Substring(0, 2))).Value
                : string.Empty;
        }

        private static int GetModelYear(char yearCharacter, int startYear = 0)
        {
            if (startYear == 0)
            {
                startYear = DefaultYear;
            }

            if (Years.TryGetValue(yearCharacter, out var year1))
            {
                var year = startYear + year1;
                if (year > NextYear)
                {
                    year -= 30;
                }
                return year;
            }

            return 0;

        }

        public static int GetModelYear(string vin, int startYear = 0)
        {

            if (string.IsNullOrEmpty(vin))
                return 0;

            if (vin.Length < 10)
            {
                return 0;
            }

            var yearCharacter = vin[9];

            return GetModelYear(yearCharacter, startYear);
        }

        public static int GetVinYear(string vin)
        {
            var letters = "ABCDEFGHJKLMNPRSTVWXY123456789";
            var yearStr = vin[9];

            var currentYear = System.DateTime.Now.Year;

            var result = new List<int>();

            var yearCounter = 1980;
            var lettersCounter = 0;

            while (yearCounter != currentYear)
            {
                var letter = letters[lettersCounter];

                if (letter == yearStr)
                {
                    result.Add(yearCounter);
                }

                if (lettersCounter == letters.Length - 1)
                {
                    lettersCounter = 0;
                }
                else
                {
                    lettersCounter += 1;
                }

                yearCounter += 1;
            }

            result.Sort();
            result.Reverse();

            return result.FirstOrDefault();
        }

    }
}
