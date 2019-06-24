using Moq;
using Newtonsoft.Json.Linq;
using SPIClient;
using System.Collections.Generic;
using Xunit;

namespace Test
{
    public class SpiPayAtTableTest
    {
        [Fact]
        public void TestBillSTatusResponseToMessage()
        {

            var a = new BillStatusResponse
            {
                BillId = "1",
                OperatorId = "12",
                TableId = "2",
                OutstandingAmount = 10000,
                TotalAmount = 20000,
                BillData = "Ww0KICAgICAgICAgICAgICAgIHsNCiAgICAgICAgICAgICAgICAgICAgInBheW1lbnRfdHlwZSI6ImNhc2giLCAgICAgICAgICAgICAgICAgICAgICAgICAgDQogICAgICAgICAgICAgICAgICAgICJwYXltZW50X3N1bW1hcnkiOnsgICAgICAgICAgICAgICAgICAgICAgICAgICANCiAgICAgICAgICAgICAgICAgICAgICAgICJiYW5rX2RhdGUiOiIxMjAzMjAxOCIsICAgICAgICAgICAgICAgICAgDQogICAgICAgICAgICAgICAgICAgICAgICAiYmFua190aW1lIjoiMDc1NDAzIiwgICAgICAgICAgICAgICAgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgInB1cmNoYXNlX2Ftb3VudCI6MTIzNCwgICAgICAgICAgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgInRlcm1pbmFsX2lkIjoiUDIwMTUwNzEiLCAgICAgICAgICAgICAgICAgDQogICAgICAgICAgICAgICAgICAgICAgICAidGVybWluYWxfcmVmX2lkIjoic29tZSBzdHJpbmciLCAgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgInRpcF9hbW91bnQiOjAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgDQogICAgICAgICAgICAgICAgICAgIH0NCiAgICAgICAgICAgICAgICB9LA0KICAgICAgICAgICAgICAgIHsNCiAgICAgICAgICAgICAgICAgICAgInBheW1lbnRfdHlwZSI6ImNhcmQiLCAgICAgICAgICAgICAgICAgICAgICAgICAgDQogICAgICAgICAgICAgICAgICAgICJwYXltZW50X3N1bW1hcnkiOnsgICAgICAgICAgICAgICAgICAgICAgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgImFjY291bnRfdHlwZSI6IkNIRVFVRSIsICAgICAgICAgICAgICAgICAgICAgICANCiAgICAgICAgICAgICAgICAgICAgICAgICJhdXRoX2NvZGUiOiIwOTQyMjQiLCAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICANCiAgICAgICAgICAgICAgICAgICAgICAgICJiYW5rX2RhdGUiOiIxMjAzMjAxOCIsICAgICAgICAgICAgICAgICAgICAgICAgICAgICANCiAgICAgICAgICAgICAgICAgICAgICAgICJiYW5rX3RpbWUiOiIwNzU0NDciLCAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAiaG9zdF9yZXNwb25zZV9jb2RlIjoiMDAwIiwgICAgICAgICAgICAgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgImhvc3RfcmVzcG9uc2VfdGV4dCI6IkFQUFJPVkVEIiwgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgIm1hc2tlZF9wYW4iOiIuLi4uLi4uLi4uLi40MzUxIiwgICAgICAgICAgICAgICAgICAgDQogICAgICAgICAgICAgICAgICAgICAgICAicHVyY2hhc2VfYW1vdW50IjoxMjM0LCAgICAgICAgICAgICAgICAgICAgICAgICANCiAgICAgICAgICAgICAgICAgICAgICAgICJycm4iOiIxODAzMTIwMDAzNzkiLCAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICANCiAgICAgICAgICAgICAgICAgICAgICAgICJzY2hlbWVfbmFtZSI6IkFtZXgiLCAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgInRlcm1pbmFsX2lkIjoiMTAwNFAyMDE1MDcxIiwgICAgICAgICAgICAgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgInRlcm1pbmFsX3JlZl9pZCI6InNvbWUgc3RyaW5nIiwgICAgICAgICAgICAgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgInRpcF9hbW91bnQiOjEyMzQgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgDQogICAgICAgICAgICAgICAgICAgIH0NCiAgICAgICAgICAgICAgICB9DQogICAgICAgICAgICBd"
            };

            var m = a.ToMessage("d");

            Assert.Equal(m.EventName, "bill_details");
            Assert.Equal(a.BillId, m.GetDataStringValue("bill_id"));
            Assert.Equal(a.TableId, m.GetDataStringValue("table_id"));
            Assert.Equal(a.OutstandingAmount, m.GetDataIntValue("bill_outstanding_amount"));
            Assert.Equal(a.TotalAmount, m.GetDataIntValue("bill_total_amount"));
            Assert.Equal(a.getBillPaymentHistory()[0].GetTerminalRefId(), "some string");
        }

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
            Assert.Equal(openTablesEntries.Count, getOpenTablesList.Count);
            Assert.Equal(openTablesEntries.Count, 2);
        }

        [Fact]
        public void TestBillPaymentFlowEndedResponse()
        {
            Secrets secrets = SpiClientTestUtils.SetTestSecrets();

            string jsonStr = @"{""message"":{""data"":{""bill_id"":""1554246591041.23"",""bill_outstanding_amount"":1000,""bill_total_amount"":1000,""card_total_amount"":0,""card_total_count"":0,""cash_total_amount"":0,""cash_total_count"":0,""operator_id"":""1"",""table_id"":""1""},""datetime"":""2019-04-03T10:11:21.328"",""event"":""bill_payment_flow_ended"",""id"":""C12.4""}}";

            Message msg = Message.FromJson(jsonStr, secrets);
            BillPaymentFlowEndedResponse response = new BillPaymentFlowEndedResponse(msg);

            Assert.Equal(msg.EventName, "bill_payment_flow_ended");
            Assert.Equal(response.BillId, "1554246591041.23");
            Assert.Equal(response.BillOutstandingAmount, 1000);
            Assert.Equal(response.BillTotalAmount, 1000);
            Assert.Equal(response.TableId, "1");
            Assert.Equal(response.OperatorId, "1");
            Assert.Equal(response.CardTotalCount, 0);
            Assert.Equal(response.CardTotalAmount, 0);
            Assert.Equal(response.CashTotalCount, 0);
            Assert.Equal(response.CashTotalAmount, 0);

            response = new BillPaymentFlowEndedResponse();
            Assert.Null(response.BillId);
            Assert.Equal(response.BillOutstandingAmount, 0);
        }

        [Fact]
        public void TestSpiPayAtTable()
        {
            Spi spi = new Spi();
            SpiPayAtTable spiPay = new SpiPayAtTable(spi);

            Assert.NotNull(spiPay.Config);
            Spi spi2 = (Spi)SpiClientTestUtils.GetInstanceField(spiPay.GetType(), spiPay, "_spi");
            Assert.Equal(spi.CurrentStatus, spi2.CurrentStatus);

            spiPay = new SpiPayAtTable();
            Spi spi3 = (Spi)SpiClientTestUtils.GetInstanceField(spiPay.GetType(), spiPay, "_spi");
            Assert.Null(spi3);
        }
    }
}
