using System;
using System.Text;
using Standart.Hash.xxHash;

namespace Cache.Core
{
    internal class UtilAlg
    {
        public static ulong KyeToHash(object key)
        {
            switch (key)
            {
                case ulong k:
                    return k;
                case byte[] k:
                    return xxHash64.ComputeHash(k, k.Length);
                case string k:
                    var b = Encoding.UTF8.GetBytes(k);
                    return xxHash64.ComputeHash(b, b.Length);
                case int k:
                    return (ulong) k;
                case byte k:
                    return k;
                case ushort k:
                    return k;
                case long k:
                    return (ulong) k;
                default:
                    throw new NotSupportedException("key not support");
            }
        }
    }
}