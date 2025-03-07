using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.Secp256k1;
using NostrLib.Models;

namespace NostrLib
{
    public static class NostrEventExtensions
    {
        public static ECXOnlyPubKey GetPublicKey(this INostrEvent nostrEvent)
        {
            if (nostrEvent is null)
            {
                throw new ArgumentNullException(nameof(nostrEvent));
            }

            return ParsePubKey(nostrEvent.PublicKey);
        }

        public static ECPrivKey ParseKey(this string key)
        {
            return ECPrivKey.Create(key.DecodHexData());
        }

        public static ECXOnlyPubKey ParsePubKey(this string key)
        {
            return Context.Instance.CreateXOnlyPubKey(key.DecodHexData());
        }

        public static string ToJson(this INostrEvent<string> nostrEvent, bool withoutId)
        {
            if (nostrEvent is null)
            {
                throw new ArgumentNullException(nameof(nostrEvent));
            }

            var retVal =
                $"[{(withoutId ? 0 : $"\"{nostrEvent.Id}\"")},\"{nostrEvent.PublicKey}\",{nostrEvent.CreatedAt?.ToUnixTimeSeconds()},{(int)nostrEvent.Kind},[{string.Join(',', nostrEvent.Tags.Select(tag => tag))}],\"{nostrEvent.Content}\"]";

            return retVal;
        }

        public static bool Verify(this INostrEvent<string> nostrEvent)
        {
            if (nostrEvent is null)
            {
                throw new ArgumentNullException(nameof(nostrEvent));
            }

            var hash = nostrEvent.ToJson(true).ComputeSha256Hash();
            if (hash.ToHex() != nostrEvent.Id)
            {
                return false;
            }

            var pub = nostrEvent.GetPublicKey();
            if (!SecpSchnorrSignature.TryCreate(nostrEvent.Signature.DecodHexData(), out var sig))
            {
                return false;
            }

            return pub.SigVerifyBIP340(sig, hash);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2008:Do not create tasks without passing a TaskScheduler", Justification = "<Pending>")]
        public static async Task<TResult[]> WhenAll<TResult>(this IEnumerable<Task<TResult>> tasks, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout).ContinueWith(_ => default(TResult)) as Task<TResult>;
            var completedTasks =
                        (await Task.WhenAll(tasks.Select(task => Task.WhenAny(task, timeoutTask)))).
                        Where(task => task != timeoutTask);
            return await Task.WhenAll(completedTasks);
        }
    }

    public static class StringExtensions
    {
        /// <summary>
        /// convert a secure string into a normal plain text string
        /// </summary>
        /// <param name="secureStr"></param>
        /// <returns></returns>
        public static string ToPlainString(this System.Security.SecureString secureStr)
        {
            string plainStr = new System.Net.NetworkCredential(string.Empty,
                              secureStr).Password;
            return plainStr;
        }

        /// <summary>
        ///     A String extension method that converts the @this to a secure string.
        /// </summary>
        /// <param name="str">The @this to act on.</param>
        /// <returns>str as a SecureString.</returns>
        public static SecureString ToSecureString(this string str)
        {
            if (str is null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            var secureString = new SecureString();
            foreach (var c in str)
                secureString.AppendChar(c);

            return secureString;
        }

        public static int CharToDec(this char c)
        {
            if ('0' <= c && c <= '9')
            {
                return c - '0';
            }
            else if ('a' <= c && c <= 'f')
            {
                return c - 'a' + 10;
            }
            else if ('A' <= c && c <= 'F')
            {
                return c - 'A' + 10;
            }
            else
            {
                return -1;
            }
        }

        public static byte[] ComputeSha256Hash(this string rawData)
        {
            // Create a SHA256
            using var sha256Hash = System.Security.Cryptography.SHA256.Create();
            // ComputeHash - returns byte array
            return sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        }

        public static byte[] DecodHexData(this string encoded)
        {
            if (encoded == null)
                throw new ArgumentNullException(nameof(encoded));
            if (encoded.Length % 2 == 1)
                throw new FormatException("Invalid Hex String");

            var result = new byte[encoded.Length / 2];
            for (int i = 0, j = 0; i < encoded.Length; i += 2, j++)
            {
                var a = CharToDec(encoded[i]);
                var b = CharToDec(encoded[i + 1]);
                if (a == -1 || b == -1)
                    throw new FormatException("Invalid Hex String");
                result[j] = (byte)(((uint)a << 4) | (uint)b);
            }

            return result;
        }

        public static string ToHex(this byte[] bytes)
        {
            if (bytes is null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            var builder = new StringBuilder();
            foreach (var t in bytes)
            {
                builder.Append(t.ToHex());
            }

            return builder.ToString();
        }

        public static string ToHex(this Span<byte> bytes)
        {
            var builder = new StringBuilder();
            foreach (var t in bytes)
            {
                builder.Append(t.ToHex());
            }

            return builder.ToString();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>")]
        private static string ToHex(this byte b)
        {
            return b.ToString("x2");
        }
    }
}
