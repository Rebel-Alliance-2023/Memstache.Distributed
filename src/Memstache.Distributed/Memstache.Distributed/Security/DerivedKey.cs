using System;
using System.Threading.Tasks;

namespace MemStache.Distributed.Security
{

    public class DerivedKey
    {
        public string Id { get; set; }
        public string MasterKeyId { get; set; }
        public byte[] PublicKey { get; set; }
        public byte[] PrivateKey { get; set; }
    }
}
