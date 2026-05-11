namespace WizardGrower.Economy
{
    public readonly struct CurrencyAuthorityResult
    {
        public CurrencyAuthorityResult(bool success, int balanceAfter, string message = "")
        {
            Success = success;
            BalanceAfter = balanceAfter;
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public int BalanceAfter { get; }
        public string Message { get; }
    }
}
