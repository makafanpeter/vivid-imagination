using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PhoneNumbers;
using VividImagination.Shared.Infrastructure.Exceptions;
using VividImagination.Shared.Infrastructure.IdsGenerator;

namespace VividImagination.Shared.Infrastructure.Common;

 public static class CommonHelper
    {



        #region Fields

        //we use EmailValidator from FluentValidation. So let's keep them sync - https://github.com/JeremySkinner/FluentValidation/blob/master/src/FluentValidation/Validators/EmailValidator.cs
        private const string EmailExpression = @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-||_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+([a-z]+|\d|-|\.{0,1}|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])?([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))$";
        private const string PinExpression = @"^[0-9]*$";
        private const string CurrencyCodeExpression = "^[A-Za-z]{3}$";
        private const string CountryCodeExpression = "^\\+[0-9]{1,3}$";
        private const string AccountNumberExpression = @"^[0-9]*$";
        private const string HtmlColorExpression = "^#(?:[0-9a-fA-F]{3}){1,2}$";
        private const string NumericCurrencyCodeExpression = @"^\d{3}$";


        private static readonly Regex PinRegex;
        private static readonly Regex CurrencyCodeRegex;
        private static readonly Regex EmailRegex;
        private static readonly Regex CountryCodeRegex;
        private static readonly Regex AccountNumberRegex;
        private static readonly Regex HtmlColorRegex;
        private static readonly Regex NumericCurrencyCodeRegex;

        #endregion

        #region Ctor

        static CommonHelper()
        {
            EmailRegex = new Regex(EmailExpression, RegexOptions.IgnoreCase);
            PinRegex = new Regex(PinExpression);
            CurrencyCodeRegex = new Regex(CurrencyCodeExpression);
            CountryCodeRegex = new Regex(CountryCodeExpression);
            AccountNumberRegex = new Regex(AccountNumberExpression);
            HtmlColorRegex = new Regex(HtmlColorExpression);
            NumericCurrencyCodeRegex = new Regex(NumericCurrencyCodeExpression);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Ensures the subscriber email or throw.
        /// </summary>
        /// <param name="email">The email.</param>
        /// <returns></returns>
        public static string EnsureSubscriberEmailOrThrow(string email)
        {
            var output = EnsureNotNull(email);
            output = output.Trim();
            output = EnsureMaximumLength(output, 255);

            if (!IsValidEmail(output))
            {
                throw new AppException("Email is not valid.");
            }

            return output;
        }

        /// <summary>
        /// Verifies that a string is in valid e-mail format
        /// </summary>
        /// <param name="email">Email to verify</param>
        /// <returns>true if the string is a valid e-mail address and false if it's not</returns>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            email = email.Trim();

            return EmailRegex.IsMatch(email);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="pin"></param>
        /// <returns></returns>
        public static bool IsValidPin(string pin)
        {
            if (string.IsNullOrEmpty(pin))
                return false;

            pin = pin.Trim();

            return PinRegex.IsMatch(pin);
        }

       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currencyCode"></param>
        /// <returns></returns>
        public static bool IsValidCurrencyCode(string currencyCode)
        {
            if (string.IsNullOrEmpty(currencyCode))
                return false;

            currencyCode = currencyCode.Trim();

            return CurrencyCodeRegex.IsMatch(currencyCode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <returns></returns>
        public static bool IsValidAccountNumber(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return false;

            accountNumber = accountNumber.Trim();

            return AccountNumberRegex.IsMatch(accountNumber);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="countryCode"></param>
        /// <returns></returns>
        public static bool IsValidCountryCode(string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode))
                return false;

            countryCode = countryCode.Trim();
            var result = CountryCodeRegex.IsMatch(countryCode);
            return  result;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static bool IsValidHTMLColor(string color)
        {
            if (string.IsNullOrEmpty(color))
                return false;

            color = color.Trim();
            var result = HtmlColorRegex.IsMatch(color);
            return  result;
        }

        /// <summary>
        /// Verifies that string is an valid IP-Address
        /// </summary>
        /// <param name="ipAddress">IPAddress to verify</param>
        /// <returns>true if the string is a valid IpAddress and false if it's not</returns>
        public static bool IsValidIpAddress(string ipAddress)
        {
            return IPAddress.TryParse(ipAddress, out var _);
        }

        /// <summary>
        /// Generate random digit code
        /// </summary>
        /// <param name="length">Length</param>
        /// <returns>Result string</returns>
        public static string GenerateRandomDigitCode(int length)
        {
            using var random = new SecureRandomNumberGenerator();
            var str = string.Empty;
            for (var i = 0; i < length; i++)
                str = string.Concat(str, random.Next(10).ToString());
            return str;
        }

        /// <summary>
        /// Returns an random integer number within a specified rage
        /// </summary>
        /// <param name="min">Minimum number</param>
        /// <param name="max">Maximum number</param>
        /// <returns>Result</returns>
        public static int GenerateRandomInteger(int min = 0, int max = int.MaxValue)
        {
            using var random = new SecureRandomNumberGenerator();
            return random.Next(min, max);
        }

        /// <summary>
        /// Ensure that a string doesn't exceed maximum allowed length
        /// </summary>
        /// <param name="str">Input string</param>
        /// <param name="maxLength">Maximum length</param>
        /// <param name="postfix">A string to add to the end if the original string was shorten</param>
        /// <returns>Input string if its length is OK; otherwise, truncated input string</returns>
        public static string EnsureMaximumLength(string str, int maxLength, string postfix = null)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            if (str.Length <= maxLength)
                return str;

            var pLen = postfix?.Length ?? 0;

            var result = str[0..(maxLength - pLen)];
            if (!string.IsNullOrEmpty(postfix))
            {
                result += postfix;
            }

            return result;
        }

        /// <summary>
        /// Ensures that a string only contains numeric values
        /// </summary>
        /// <param name="str">Input string</param>
        /// <returns>Input string with only numeric values, empty string if input is null/empty</returns>
        public static string EnsureNumericOnly(string str)
        {
            return string.IsNullOrEmpty(str) ? string.Empty : new string(str.Where(char.IsDigit).ToArray());
        }

        /// <summary>
        /// Ensure that a string is not null
        /// </summary>
        /// <param name="str">Input string</param>
        /// <returns>Result</returns>
        public static string EnsureNotNull(string str)
        {
            return str ?? string.Empty;
        }

        /// <summary>
        /// Indicates whether the specified strings are null or empty strings
        /// </summary>
        /// <param name="stringsToValidate">Array of strings to validate</param>
        /// <returns>Boolean</returns>
        public static bool AreNullOrEmpty(params string[] stringsToValidate)
        {
            return stringsToValidate.Any(string.IsNullOrEmpty);
        }

        /// <summary>
        /// Compare two arrays
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="a1">Array 1</param>
        /// <param name="a2">Array 2</param>
        /// <returns>Result</returns>
        public static bool ArraysEqual<T>(T[] a1, T[] a2)
        {
            //also see Enumerable.SequenceEqual(a1, a2);
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            var comparer = EqualityComparer<T>.Default;
            return !a1.Where((t, i) => !comparer.Equals(t, a2[i])).Any();
        }

        /// <summary>
        /// Sets a property on an object to a value.
        /// </summary>
        /// <param name="instance">The object whose property to set.</param>
        /// <param name="propertyName">The name of the property to set.</param>
        /// <param name="value">The value to set the property to.</param>
        public static void SetProperty(object instance, string propertyName, object value)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            var instanceType = instance.GetType();
            var pi = instanceType.GetProperty(propertyName);
            if (pi == null)
                throw new AppException("No property '{0}' found on the instance of type '{1}'.", propertyName, instanceType);
            if (!pi.CanWrite)
                throw new AppException("The property '{0}' on the instance of type '{1}' does not have a setter.", propertyName, instanceType);
            if (value != null && !value.GetType().IsAssignableFrom(pi.PropertyType))
                value = To(value, pi.PropertyType);
            pi.SetValue(instance, value, Array.Empty<object>());
        }

        /// <summary>
        /// Converts a value to a destination type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="destinationType">The type to convert the value to.</param>
        /// <returns>The converted value.</returns>
        public static object To(object value, Type destinationType)
        {
            return To(value, destinationType, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a value to a destination type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="destinationType">The type to convert the value to.</param>
        /// <param name="culture">Culture</param>
        /// <returns>The converted value.</returns>
        public static object To(object value, Type destinationType, CultureInfo culture)
        {
            if (value == null)
                return null;

            var sourceType = value.GetType();

            var destinationConverter = TypeDescriptor.GetConverter(destinationType);
            if (destinationConverter.CanConvertFrom(value.GetType()))
                return destinationConverter.ConvertFrom(null!, culture, value);

            var sourceConverter = TypeDescriptor.GetConverter(sourceType);
            if (sourceConverter.CanConvertTo(destinationType))
                return sourceConverter.ConvertTo(null, culture, value, destinationType);

            if (destinationType.IsEnum && value is int)
                return Enum.ToObject(destinationType, (int)value);

            if (!destinationType.IsInstanceOfType(value))
                return Convert.ChangeType(value, destinationType, culture);

            return value;
        }

        /// <summary>
        /// Converts a value to a destination type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <returns>The converted value.</returns>
        public static T To<T>(object value)
        {
            //return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            return (T)To(value, typeof(T));
        }

        /// <summary>
        /// Convert enum for front-end
        /// </summary>
        /// <param name="str">Input string</param>
        /// <returns>Converted string</returns>
        public static string ConvertEnum(string str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            var result = string.Empty;
            foreach (var c in str)
                if (c.ToString() != c.ToString().ToLower())
                    result += " " + c.ToString();
                else
                    result += c.ToString();

            //ensure no spaces (e.g. when the first letter is upper case)
            result = result.TrimStart();
            return result;
        }

        /// <summary>
        /// Get difference in years
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public static int GetDifferenceInYears(DateTime startDate, DateTime endDate)
        {
            //source: http://stackoverflow.com/questions/9/how-do-i-calculate-someones-age-in-c
            //this assumes you are looking for the western idea of age and not using East Asian reckoning.
            var age = endDate.Year - startDate.Year;
            if (startDate > endDate.AddYears(-age))
                age--;
            return age;
        }



        public static bool IsValidPhoneNumber(string phoneNumber, string countryDialingCode = "+234")
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            bool valid = false;

            try
            {
                PhoneNumberUtil phoneNumberUtil = PhoneNumberUtil.GetInstance();
                string? regionCode = null;
                if (int.TryParse(countryDialingCode, out var dialingCode))
                {
                    regionCode = phoneNumberUtil.GetRegionCodeForCountryCode(dialingCode);
                }
                else if (!string.IsNullOrWhiteSpace(countryDialingCode) && countryDialingCode.Trim().Length == 2)
                {
                    regionCode = countryDialingCode.Trim().ToUpperInvariant();
                }

                regionCode ??= "NG";

                var number = phoneNumberUtil.Parse(phoneNumber, regionCode);
                valid = phoneNumberUtil.IsValidNumber(number);
            }
            catch (NumberParseException)
            {

            }

            return valid;
        }

        public static string FormatPhoneNumber(string phoneNumber, bool includePlusSign = false, string countryDialingCode = "+234")
        {
            try
            {
                PhoneNumberUtil phoneNumberUtil = PhoneNumberUtil.GetInstance();
                if (!IsValidPhoneNumber(phoneNumber, countryDialingCode))
                    return phoneNumber;

                string? regionCode = null;
                if (int.TryParse(countryDialingCode, out var dialingCode))
                {
                    regionCode = phoneNumberUtil.GetRegionCodeForCountryCode(dialingCode);
                }
                else if (!string.IsNullOrWhiteSpace(countryDialingCode) && countryDialingCode.Trim().Length == 2)
                {
                    regionCode = countryDialingCode.Trim().ToUpperInvariant();
                }

                regionCode ??= "NG";

                var number = phoneNumberUtil.Parse(phoneNumber, regionCode);
                var phone = phoneNumberUtil.Format(number, PhoneNumberFormat.E164);
                if (includePlusSign)
                {
                    return phone;
                }
                return phone.StartsWith("+") ? phone.Substring(1, phone.Length - 1) : phone;
            }
            catch (System.Exception)
            {

            }
            return phoneNumber;
        }



        public static string GenerateRandomDigitCodeFromDate(int length)
        {
            var random = new Random((int)DateTime.Now.Ticks);
            string str = string.Empty;
            for (int i = 0; i < length; i++)
                str = string.Concat(str, random.Next(10).ToString());
            return str;
        }




        public static string GetSha512(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            using (var sha512 = SHA512.Create())
            {
                byte[] bytesSha512In = Encoding.UTF8.GetBytes(text);
                byte[] bytesSha512Out = sha512.ComputeHash(bytesSha512In);
                string strSha512Out = BitConverter.ToString(bytesSha512Out);
                strSha512Out = strSha512Out.Replace("-", "");
                return strSha512Out;
            }
        }


        public static string Encrypt(string source, string key)
        {
            var desCryptoProvider = new TripleDESCryptoServiceProvider();
            var hashMd5Provider = new MD5CryptoServiceProvider();

            var byteHash = hashMd5Provider.ComputeHash(Encoding.UTF8.GetBytes(key));
            desCryptoProvider.Key = byteHash;
            desCryptoProvider.Mode = CipherMode.ECB; //CBC, CFB
            var byteBuff = Encoding.UTF8.GetBytes(source);

            var encoded =
                Convert.ToBase64String(desCryptoProvider.CreateEncryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
            return encoded;
        }


        public static string Decrypt(string encodedText, string key)
        {
            var desCryptoProvider = new TripleDESCryptoServiceProvider();
            var hashMd5Provider = new MD5CryptoServiceProvider();

            var byteHash = hashMd5Provider.ComputeHash(Encoding.UTF8.GetBytes(key));
            desCryptoProvider.Key = byteHash;
            desCryptoProvider.Mode = CipherMode.ECB; //CBC, CFB
            var byteBuff = Convert.FromBase64String(encodedText);

            var plaintext = Encoding.UTF8.GetString(desCryptoProvider.CreateDecryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
            return plaintext;
        }




        public static string GetFirstChars(string name, int length)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }
            var firstChars = name.Split(' ').Select(s => s[0]);
            var chars = firstChars.Take(length).ToArray();
            return chars.Count() < length ? new string(chars.ToArray()) + GenerateRandomDigitCode(length - chars.Count()) : new string(chars.ToArray());
        }








        public static string ToValidFileName(string input, string replacement = "_")
        {
            if (!string.IsNullOrEmpty(input))
            {
                return input;
            }
            var result = input ?? string.Empty;

            char[] invalidChars = { '/', ';', ' ' };

            return invalidChars.Aggregate(result, (current, c) => current.Replace(c.ToString(), replacement ?? "-"));
        }


        public static string Mask(this string value, int length = 0)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Substring(0, length) + new String('*', value.Length - length);
            return value;
        }
        public static string RemoveSpecialChar(string name, string replacement = "")
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            string filterExp = @"[^a-zA-Z0-9\s]";
            string result = Regex.Replace(name, filterExp, replacement);
            return result;
        }



        public static string Base64Encode(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }
            else
            {
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                return Convert.ToBase64String(plainTextBytes);
            }
        }

        public static string Base64Decode(string base64EncodedString)
        {
            if (string.IsNullOrEmpty(base64EncodedString))
            {
                return string.Empty;
            }
            else
            {
                var base64EncodedBytes = Convert.FromBase64String(base64EncodedString);
                return Encoding.UTF8.GetString(base64EncodedBytes);
            }
        }



        public static string Truncate(this string value, int maxLength = 150, string suffix = "")
        {
            if (suffix == null) throw new ArgumentNullException("suffix");
            if (maxLength <= 0) throw new ArgumentOutOfRangeException("maxLength");


            int subStringLength = maxLength - suffix.Length;

            if (subStringLength <= 0)
                throw new ArgumentException("Length of suffix string is greater or equal to maximumLength");

            if (value != null && value.Length > maxLength)
            {
                string truncatedString = value.Substring(0, subStringLength);
                // in case the last character is a space
                truncatedString = truncatedString.Trim();
                truncatedString += suffix;

                return truncatedString;
            }
            return value;
        }

        public static bool ValidateBvn(string bvn)
        {
            if (string.IsNullOrEmpty(bvn)) return false;

            Regex bvnMatch = new Regex(@"^[0-9]{11}$");

            var valid = bvnMatch.IsMatch(bvn);
            return valid;
        }

        public static bool ValidateCac(string cac)
        {
            if (string.IsNullOrEmpty(cac)) return false;

            Regex cacMatch = new Regex(@"^[A-Za-z]{2}[0-9]+$");

            var valid = cacMatch.IsMatch(cac);
            return valid;
        }

        public static bool ValidateTin(string tin)
        {
            if (string.IsNullOrEmpty(tin)) return false;

            Regex tinMatch = new Regex(@"^[0-9]{14}$");

            var valid = tinMatch.IsMatch(tin);
            return valid;
        }



        public static string GeneratePassword(int minLength,
                                  int maxLength)
        {
            // Make sure that input parameters are valid.
            if (minLength <= 0 || maxLength <= 0 || minLength > maxLength)
                return null;

            // Create a local array containing supported password characters
            // grouped by types. You can remove character groups from this
            // array, but doing so will weaken the password strength.
            var PASSWORD_CHARS_LCASE = "abcdefgijkmnopqrstwxyz";
            string PASSWORD_CHARS_UCASE = "ABCDEFGHJKLMNPQRSTWXYZ";
            string PASSWORD_CHARS_NUMERIC = "23456789";
            string PASSWORD_CHARS_SPECIAL = "*$-+?_&=!%{}/";
            char[][] charGroups = new char[][]
                {
            PASSWORD_CHARS_LCASE.ToCharArray(),
            PASSWORD_CHARS_UCASE.ToCharArray(),
            PASSWORD_CHARS_NUMERIC.ToCharArray(),
            PASSWORD_CHARS_SPECIAL.ToCharArray()
                };

            // Use this array to track the number of unused characters in each
            // character group.
            int[] charsLeftInGroup = new int[charGroups.Length];

            // Initially, all characters in each group are not used.
            for (int i = 0; i < charsLeftInGroup.Length; i++)
                charsLeftInGroup[i] = charGroups[i].Length;

            // Use this array to track (iterate through) unused character groups.
            int[] leftGroupsOrder = new int[charGroups.Length];

            // Initially, all character groups are not used.
            for (int i = 0; i < leftGroupsOrder.Length; i++)
                leftGroupsOrder[i] = i;

            // Because we cannot use the default randomizer, which is based on the
            // current time (it will produce the same "random" number within a
            // second), we will use a random number generator to seed the
            // randomizer.

            // Use a 4-byte array to fill it with random bytes and convert it then
            // to an integer value.
            byte[] randomBytes = new byte[4];

            // Generate 4 random bytes.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(randomBytes);

            // Convert 4 bytes into a 32-bit integer value.
            int seed = BitConverter.ToInt32(randomBytes, 0);

            // Now, this is real randomization.
            Random random = new Random(seed);

            // This array will hold password characters.
            char[] password;

            // Allocate appropriate memory for the password.
            if (minLength < maxLength)
                password = new char[random.Next(minLength, maxLength + 1)];
            else
                password = new char[minLength];

            // Index of the next character to be added to password.
            int nextCharIdx;

            // Index of the next character group to be processed.
            int nextGroupIdx;

            // Index which will be used to track not processed character groups.
            int nextLeftGroupsOrderIdx;

            // Index of the last non-processed character in a group.
            int lastCharIdx;

            // Index of the last non-processed group.
            int lastLeftGroupsOrderIdx = leftGroupsOrder.Length - 1;

            // Generate password characters one at a time.
            for (int i = 0; i < password.Length; i++)
            {
                // If only one character group remained unprocessed, process it;
                // otherwise, pick a random character group from the unprocessed
                // group list. To allow a special character to appear in the
                // first position, increment the second parameter of the Next
                // function call by one, i.e. lastLeftGroupsOrderIdx + 1.
                if (lastLeftGroupsOrderIdx == 0)
                    nextLeftGroupsOrderIdx = 0;
                else
                    nextLeftGroupsOrderIdx = random.Next(0,
                                                         lastLeftGroupsOrderIdx);

                // Get the actual index of the character group, from which we will
                // pick the next character.
                nextGroupIdx = leftGroupsOrder[nextLeftGroupsOrderIdx];

                // Get the index of the last unprocessed characters in this group.
                lastCharIdx = charsLeftInGroup[nextGroupIdx] - 1;

                // If only one unprocessed character is left, pick it; otherwise,
                // get a random character from the unused character list.
                if (lastCharIdx == 0)
                    nextCharIdx = 0;
                else
                    nextCharIdx = random.Next(0, lastCharIdx + 1);

                // Add this character to the password.
                password[i] = charGroups[nextGroupIdx][nextCharIdx];

                // If we processed the last character in this group, start over.
                if (lastCharIdx == 0)
                    charsLeftInGroup[nextGroupIdx] =
                                              charGroups[nextGroupIdx].Length;
                // There are more unprocessed characters left.
                else
                {
                    // Swap processed character with the last unprocessed character
                    // so that we don't pick it until we process all characters in
                    // this group.
                    if (lastCharIdx != nextCharIdx)
                    {
                        char temp = charGroups[nextGroupIdx][lastCharIdx];
                        charGroups[nextGroupIdx][lastCharIdx] =
                                    charGroups[nextGroupIdx][nextCharIdx];
                        charGroups[nextGroupIdx][nextCharIdx] = temp;
                    }
                    // Decrement the number of unprocessed characters in
                    // this group.
                    charsLeftInGroup[nextGroupIdx]--;
                }

                // If we processed the last group, start all over.
                if (lastLeftGroupsOrderIdx == 0)
                    lastLeftGroupsOrderIdx = leftGroupsOrder.Length - 1;
                // There are more unprocessed groups left.
                else
                {
                    // Swap processed group with the last unprocessed group
                    // so that we don't pick it until we process all groups.
                    if (lastLeftGroupsOrderIdx != nextLeftGroupsOrderIdx)
                    {
                        int temp = leftGroupsOrder[lastLeftGroupsOrderIdx];
                        leftGroupsOrder[lastLeftGroupsOrderIdx] =
                                    leftGroupsOrder[nextLeftGroupsOrderIdx];
                        leftGroupsOrder[nextLeftGroupsOrderIdx] = temp;
                    }
                    // Decrement the number of unprocessed groups.
                    lastLeftGroupsOrderIdx--;
                }
            }

            // Convert password characters into a string and return the result.
            return new string(password);
        }



        /// <summary>
        /// Create a data hash
        /// </summary>
        /// <param name="data">The data for calculating the hash</param>
        /// <param name="hashAlgorithm">Hash algorithm</param>
        /// <param name="trimByteCount">The number of bytes, which will be used in the hash algorithm; leave 0 to use all array</param>
        /// <returns>Data hash</returns>
        public static string CreateHash(byte[] data, string hashAlgorithm, int trimByteCount = 0)
        {
            if (string.IsNullOrEmpty(hashAlgorithm))
                throw new ArgumentNullException(nameof(hashAlgorithm));

            var algorithm = (HashAlgorithm)CryptoConfig.CreateFromName(hashAlgorithm);
            if (algorithm == null)
                throw new ArgumentException("Unrecognized hash name");

            if (trimByteCount > 0 && data.Length > trimByteCount)
            {
                var newData = new byte[trimByteCount];
                Array.Copy(data, newData, trimByteCount);

                return BitConverter.ToString(algorithm.ComputeHash(newData)).Replace("-", string.Empty);
            }

            return BitConverter.ToString(algorithm.ComputeHash(data)).Replace("-", string.Empty);
        }

        #endregion

        public static string ParseDomain(string host)
        {
           // return DomainName.TryParse(host, out var outDomain) ? $"{outDomain.Domain}.{outDomain.TLD}" : host;
           
           return host;
        }
        
        
        public static bool IsValidDomainName(string name)
        {
            return Uri.CheckHostName(name) != UriHostNameType.Unknown;
        }

        public static bool IsValidHostName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            return Uri.TryCreate(name, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

        }

        public static string ToTitleCase(string text, string culture = "en-US")
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            var myTI = new CultureInfo(culture, false).TextInfo;
            return myTI.ToTitleCase(text.ToLower());
        }

/*
        public static List<CountryInfoDto> GetCountries(List<string>? filters)
        {
            ICountryProvider countryProvider = new CountryProvider();
    
            var countries = countryProvider.GetCountries();
    
            // If filters are provided, filter the countries
            if (filters is { Count: > 0 })
            {
                countries = countries.Where(c => filters.Contains(c.Alpha2Code.ToString()));
            }
            
            return countries.Select( x=> new CountryInfoDto()
            {
                Alpha2Code =  x.Alpha2Code.ToString(),
                Alpha3Code =  x.Alpha3Code.ToString(),
                NumericCode = x.NumericCode.ToString(),
                CommonName = x.CommonName,
                OfficialName = x.OfficialName,
                Currencies = x.Currencies.Select(c => c.IsoCode)   
                    .ToList(),
                CurrencyNumericCodes =x.Currencies.Select(c => c.NumericCode).ToList()
            }).ToList();
        }
        
        
        public static CountryInfoDto? GetCountry(string? country)
        {
            if (string.IsNullOrWhiteSpace(country))
                return null;

            ICountryProvider countryProvider = new CountryProvider();
            var countries = countryProvider.GetCountries();

            var countryInfo = countries.FirstOrDefault(c =>
                c.CommonName.Equals(country, StringComparison.OrdinalIgnoreCase) ||
                c.OfficialName.Equals(country, StringComparison.OrdinalIgnoreCase) ||
                c.Alpha2Code.ToString().Equals(country, StringComparison.OrdinalIgnoreCase) ||
                c.Alpha3Code.ToString().Equals(country, StringComparison.OrdinalIgnoreCase));
            return countryInfo != null
                ? new CountryInfoDto()
                {
                    Alpha2Code = countryInfo.Alpha2Code.ToString(),
                    Alpha3Code = countryInfo.Alpha3Code.ToString(),
                    NumericCode = countryInfo.NumericCode.ToString(),
                    CommonName = countryInfo.CommonName,
                    OfficialName = countryInfo.OfficialName,
                    Currencies = countryInfo.Currencies.Select(c => c.IsoCode)
                        .ToList(),
                    CurrencyNumericCodes = countryInfo.Currencies.Select(c => c.NumericCode).ToList()
                }
                : null;
        }
        
        */
        public static bool IsValidNumericCurrencyCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return false;

            code = code.Trim();
            var result = NumericCurrencyCodeRegex.IsMatch(code);
            return  result;
        }
        
    }