namespace SPIClient
{
    public static class PurchaseHelper
    {
        public static PurchaseRequest CreatePurchaseRequest(int amountCents, string purchaseId)
        {
            return new PurchaseRequest(amountCents, purchaseId);
        }

        public static PurchaseRequest CreatePurchaseRequestV2(string posRefId, int purchaseAmount, int tipAmount, int cashoutAmount, bool promptForCashout, int surchargeAmount)
        {
            var pr = new PurchaseRequest(purchaseAmount, posRefId)
            {
                CashoutAmount = cashoutAmount,
                TipAmount = tipAmount,
                PromptForCashout = promptForCashout,
                SurchargeAmount = surchargeAmount
            };
            return pr;
        }

        public static RefundRequest CreateRefundRequest(int amountCents, string purchaseId, bool suppressMerchantPassword)
        {
            return new RefundRequest(amountCents, purchaseId, suppressMerchantPassword);
        }

    }
}