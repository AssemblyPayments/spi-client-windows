using SPIClient.Service;

namespace SPIClient
{
    internal static class TransactionReportHelper
    {
        internal static TransactionReport CreateTransactionReportEnvelope(string posVendorId, string posVersion, string libraryLanguage, string libraryVersion, string serialNumber)
        {
            var transactionReport = new TransactionReport()
            {
                PosVendorId = posVendorId,
                PosVersion = posVersion,
                LibraryLanguage = libraryLanguage,
                LibraryVersion = libraryVersion,
                SerialNumber = serialNumber,
            };

            return transactionReport;
        }
    }
}
