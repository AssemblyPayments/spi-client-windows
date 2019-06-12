using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using SPIClient;
using Xunit;

namespace Test
{
    public class PayAtTableTest
    {
        [Fact]
        public void TestGetOpenTablesResponse()
        {
            List<OpenTablesEntry> openTablesEntries = new List<OpenTablesEntry>();
            OpenTablesEntry openTablesEntry = new OpenTablesEntry();
            openTablesEntry.TableId = "1";
            openTablesEntry.Label = "1";
            openTablesEntry.BillOutstandingAmount = 2000;
            openTablesEntries.Add(openTablesEntry);

            openTablesEntry = new OpenTablesEntry();
            openTablesEntry.TableId = "2";
            openTablesEntry.Label = "2";
            openTablesEntry.BillOutstandingAmount = 2500;
            openTablesEntries.Add(openTablesEntry);

            GetOpenTablesResponse getOpenTablesResponse = new GetOpenTablesResponse();
            getOpenTablesResponse.OpenTablesEntries = openTablesEntries;
            Message m = getOpenTablesResponse.ToMessage("1234");

            JArray getOpenTablesArray = (JArray)m.Data["tables"];
            List<OpenTablesEntry> getOpenTablesList = getOpenTablesArray.ToObject<List<OpenTablesEntry>>();
            Assert.Equal(openTablesEntries.ToArray(), openTablesEntries.ToArray());
            Assert.Equal(openTablesEntries.Count, 2);
        }
    }
}
