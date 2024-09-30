using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SupplyChain.Processor;

public class Functions {
    public static readonly TextInfo CultureTest = new CultureInfo("en-US", false).TextInfo;

    public static bool IsEmailValid(string email)
    {
        var trimmedEmail = email.Trim();

        if (trimmedEmail.EndsWith("."))
        {
            return false; // suggested by @TK-421
        }
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == trimmedEmail;
        }
        catch
        {
            return false;
        }
    }

    public static string GenerateRandomString(int length) {
        var random = new Random();
        const string chars = "0123456789";
        while(true) {
            var randText =
                new string(Enumerable.Repeat(chars, length)
                    .Select(s => s[random.Next(s.Length)])
                    .ToArray());
            var md5 = MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes(randText);
            var hashBytes = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            foreach(var t in hashBytes) {
                sb.Append(t.ToString("X2"));
            }

            var randomTopicName = sb.ToString()[..length];
            if(randomTopicName.StartsWith("0"))
                continue;

            return randomTopicName;
        }
    }

    public class RandomNameGenerator
    {
        private static Random random = new Random();

        // Sample lists of first names and last names
        private static List<string> firstNames = new List<string>
    {
        "John", "Mary", "David", "Sarah", "Michael", "Emily", "James", "Emma", "William", "Olivia"
    };

        private static List<string> lastNames = new List<string>
    {
        "Smith", "Johnson", "Brown", "Davis", "Miller", "Wilson", "Moore", "Taylor", "Anderson", "Thomas"
    };

        public static string GenerateRandomName()
        {
            string firstName = firstNames[random.Next(0, firstNames.Count)];
            string lastName = lastNames[random.Next(0, lastNames.Count)];

            return $"{firstName} {lastName}";
        }
    }
    public static string RandomDigits(int length)
    {
        var random = new Random();
        string s = string.Empty;
        for (int i = 0; i < length; i++)
            s = String.Concat(s, random.Next(10).ToString());
        return s;
    }

    public static string GetDeptUrl(string departmentName) {
        var urlNameSplit = departmentName.Split(" ");
        var urlString = "";
        var count = 1;
        foreach(var split in urlNameSplit) {
            urlString += $"{(count != 1 ? "_" : null)}{split}";
            count++;
        }

        return urlString;
    }
    public static string ToSentenceCase(string str)
    {
        // Check if the string is null or empty
        if (string.IsNullOrEmpty(str))
            return str;

        // Convert the string to lowercase
        str = str.ToLower();

        // Capitalize the first letter
        str = char.ToUpper(str[0]) + str.Substring(1);

        return str;
    }
    public static string GetDeptFromUrl(string urlString) {
        var splitUrl = urlString.Split("_");
        var toReturnDept = "";
        var count = 1;
        foreach(var split in splitUrl) {
            toReturnDept += $"{(count != 1 ? " " : null)}{split}";
            count++;
        }

        return toReturnDept;
    }
}