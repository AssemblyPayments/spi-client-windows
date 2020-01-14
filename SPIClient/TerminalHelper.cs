using System.Collections.Generic;

namespace SPIClient
{
    internal static class TerminalHelper
    {
        internal static bool IsPrinterAvailable(string terminalModel)
        {
            // 
            var terminalsWithoutPrinter = new List<string>
            {
                "E355"
            };

            if (terminalsWithoutPrinter.Contains(terminalModel))
            {
                return false;
            }

            return true;
        }
    }
}
