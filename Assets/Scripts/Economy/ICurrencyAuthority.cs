using System.Threading.Tasks;

namespace WizardGrower.Economy
{
    public interface ICurrencyAuthority
    {
        bool IsServerAuthoritative { get; }
        bool IsBusy { get; }
        Task<CurrencyAuthorityResult> GrantAsync(string kind, int amount, string reason, string source);
        Task<CurrencyAuthorityResult> SpendAsync(string kind, int amount, string reason);
    }
}
