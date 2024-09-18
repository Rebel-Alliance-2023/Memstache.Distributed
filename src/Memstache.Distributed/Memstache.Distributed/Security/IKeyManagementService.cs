namespace MemStache.Distributed.Security
{
    public interface IKeyManagementService
    {
        Task<MasterKey> GenerateMasterKeyAsync();
        Task<DerivedKey> GenerateDerivedKeyAsync(string masterKeyId = null);
        Task<MasterKey> GetMasterKeyAsync(string keyId = null);
        Task<DerivedKey> GetDerivedKeyAsync(string keyId = null);
        Task<MasterKey> RotateMasterKeyAsync(string masterKeyId = null);
    }
}
