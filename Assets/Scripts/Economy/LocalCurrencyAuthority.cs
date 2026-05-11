using System.Threading.Tasks;

namespace WizardGrower.Economy
{
    public sealed class LocalCurrencyAuthority : ICurrencyAuthority
    {
        public bool IsServerAuthoritative => false;

        public Task<CurrencyAuthorityResult> GrantAsync(string kind, int amount, string reason, string source)
        {
            return Task.FromResult(new CurrencyAuthorityResult(true, -1));
        }

        public Task<CurrencyAuthorityResult> SpendAsync(string kind, int amount, string reason)
        {
            return Task.FromResult(new CurrencyAuthorityResult(true, -1));
        }
    }
}
