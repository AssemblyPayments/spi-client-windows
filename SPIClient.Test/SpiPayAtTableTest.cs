using Newtonsoft.Json.Linq;
using SPIClient;
using System.Collections.Generic;
using Xunit;

namespace Test
{
    public class SpiPayAtTableTest
    {
        [Fact]
        public void BillStatusResponse_OnValidResponse_ReturnObjects()
        {
            // arrange
            var a = new BillStatusResponse
            {
                BillId = "1",
                OperatorId = "12",
                TableId = "2",
                OutstandingAmount = 10000,
                TotalAmount = 20000,
                BillData = "Ww0KICAgICAgICAgICAgICAgIHsNCiAgICAgICAgICAgICAgICAgICAgInBheW1lbnRfdHlwZSI6ImNhc2giLCAgICAgICAgICAgICAgICAgICAgICAgICAgDQogICAgICAgICAgICAgICAgICAgICJwYXltZW50X3N1bW1hcnkiOnsgICAgICAgICAgICAgICAgICAgICAgICAgICANCiAgICAgICAgICAgICAgICAgICAgICAgICJiYW5rX2RhdGUiOiIxMjAzMjAxOCIsICAgICAgICAgICAgICAgICAgDQogICAgICAgICAgICAgICAgICAgICAgICAiYmFua190aW1lIjoiMDc1NDAzIiwgICAgICAgICAgICAgICAgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgInB1cmNoYXNlX2Ftb3VudCI6MTIzNCwgICAgICAgICAgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgInRlcm1pbmFsX2lkIjoiUDIwMTUwNzEiLCAgICAgICAgICAgICAgICAgDQogICAgICAgICAgICAgICAgICAgICAgICAidGVybWluYWxfcmVmX2lkIjoic29tZSBzdHJpbmciLCAgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgInRpcF9hbW91bnQiOjAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgDQogICAgICAgICAgICAgICAgICAgIH0NCiAgICAgICAgICAgICAgICB9LA0KICAgICAgICAgICAgICAgIHsNCiAgICAgICAgICAgICAgICAgICAgInBheW1lbnRfdHlwZSI6ImNhcmQiLCAgICAgICAgICAgICAgICAgICAgICAgICAgDQogICAgICAgICAgICAgICAgICAgICJwYXltZW50X3N1bW1hcnkiOnsgICAgICAgICAgICAgICAgICAgICAgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgImFjY291bnRfdHlwZSI6IkNIRVFVRSIsICAgICAgICAgICAgICAgICAgICAgICANCiAgICAgICAgICAgICAgICAgICAgICAgICJhdXRoX2NvZGUiOiIwOTQyMjQiLCAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICANCiAgICAgICAgICAgICAgICAgICAgICAgICJiYW5rX2RhdGUiOiIxMjAzMjAxOCIsICAgICAgICAgICAgICAgICAgICAgICAgICAgICANCiAgICAgICAgICAgICAgICAgICAgICAgICJiYW5rX3RpbWUiOiIwNzU0NDciLCAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAiaG9zdF9yZXNwb25zZV9jb2RlIjoiMDAwIiwgICAgICAgICAgICAgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgImhvc3RfcmVzcG9uc2VfdGV4dCI6IkFQUFJPVkVEIiwgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgIm1hc2tlZF9wYW4iOiIuLi4uLi4uLi4uLi40MzUxIiwgICAgICAgICAgICAgICAgICAgDQogICAgICAgICAgICAgICAgICAgICAgICAicHVyY2hhc2VfYW1vdW50IjoxMjM0LCAgICAgICAgICAgICAgICAgICAgICAgICANCiAgICAgICAgICAgICAgICAgICAgICAgICJycm4iOiIxODAzMTIwMDAzNzkiLCAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICANCiAgICAgICAgICAgICAgICAgICAgICAgICJzY2hlbWVfbmFtZSI6IkFtZXgiLCAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgInRlcm1pbmFsX2lkIjoiMTAwNFAyMDE1MDcxIiwgICAgICAgICAgICAgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgInRlcm1pbmFsX3JlZl9pZCI6InNvbWUgc3RyaW5nIiwgICAgICAgICAgICAgICAgICAgIA0KICAgICAgICAgICAgICAgICAgICAgICAgInRpcF9hbW91bnQiOjEyMzQgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgDQogICAgICAgICAgICAgICAgICAgIH0NCiAgICAgICAgICAgICAgICB9DQogICAgICAgICAgICBd"
            };

            // act
            var m = a.ToMessage("d");

            // assert
            Assert.Equal("bill_details", m.EventName);
            Assert.Equal(a.BillId, m.GetDataStringValue("bill_id"));
            Assert.Equal(a.TableId, m.GetDataStringValue("table_id"));
            Assert.Equal(a.OutstandingAmount, m.GetDataIntValue("bill_outstanding_amount"));
            Assert.Equal(a.TotalAmount, m.GetDataIntValue("bill_total_amount"));
            Assert.Equal(a.getBillPaymentHistory()[0].GetTerminalRefId(), "some string");
        }

        [Fact]
        public void GetOpenTablesResponse_OnValidResponse_ReturnObjects()
        {
            // arrange
            var openTablesEntries = new List<OpenTablesEntry>();
            var openTablesEntry = new OpenTablesEntry();
            openTablesEntry.TableId = "1";
            openTablesEntry.Label = "1";
            openTablesEntry.BillOutstandingAmount = 2000;
            openTablesEntries.Add(openTablesEntry);

            openTablesEntry = new OpenTablesEntry();
            openTablesEntry.TableId = "2";
            openTablesEntry.Label = "2";
            openTablesEntry.BillOutstandingAmount = 2500;
            openTablesEntries.Add(openTablesEntry);

            // act
            var getOpenTablesResponse = new GetOpenTablesResponse();
            getOpenTablesResponse.OpenTablesEntries = openTablesEntries;
            var m = getOpenTablesResponse.ToMessage("1234");
            var getOpenTablesArray = (JArray)m.Data["tables"];
            var getOpenTablesList = getOpenTablesArray.ToObject<List<OpenTablesEntry>>();

            // assert
            Assert.Equal(openTablesEntries.Count, getOpenTablesList.Count);
            Assert.Equal(2, openTablesEntries.Count);
        }

        [Fact]
        public void GetOpenTables_OnValidResponse_IsSet()
        {
            // arrange
            var openTablesEntries = new List<OpenTablesEntry>();
            var openTablesEntry = new OpenTablesEntry();
            openTablesEntry.TableId = "1";
            openTablesEntry.Label = "1";
            openTablesEntry.BillOutstandingAmount = 2000;
            openTablesEntries.Add(openTablesEntry);

            // act
            var getOpenTablesResponse = new GetOpenTablesResponse();
            getOpenTablesResponse.OpenTablesEntries = openTablesEntries;
            var openTablesEntriesResponse = getOpenTablesResponse.GetOpenTables();

            // assert
            Assert.Equal(openTablesEntries.Count, openTablesEntriesResponse.Count);
        }

        [Fact]
        public void GetOpenTables_OnValidResponseNull_IsSet()
        {
            // arrange
            var getOpenTablesResponse = new GetOpenTablesResponse();

            // act
            var openTablesEntriesResponse = getOpenTablesResponse.GetOpenTables();

            // assert
            Assert.NotNull(openTablesEntriesResponse);
            Assert.Null(getOpenTablesResponse.OpenTablesEntries);
        }

        [Fact]
        public void BillPaymentFlowEndedResponse_OnValidResponse_ReturnObjects()
        {
            // arrange
            var secrets = SpiClientTestUtils.SetTestSecrets();
            const string jsonStr = @"{""message"":{""data"":{""bill_id"":""1554246591041.23"",""bill_outstanding_amount"":1000,""bill_total_amount"":1000,""card_total_amount"":0,""card_total_count"":0,""cash_total_amount"":0,""cash_total_count"":0,""operator_id"":""1"",""table_id"":""1""},""datetime"":""2019-04-03T10:11:21.328"",""event"":""bill_payment_flow_ended"",""id"":""C12.4""}}";

            // act
            var msg = Message.FromJson(jsonStr, secrets);
            var response = new BillPaymentFlowEndedResponse(msg);

            // assert
            Assert.Equal("bill_payment_flow_ended", msg.EventName);
            Assert.Equal("1554246591041.23", response.BillId);
            Assert.Equal(1000, response.BillOutstandingAmount);
            Assert.Equal(1000, response.BillTotalAmount);
            Assert.Equal("1", response.TableId);
            Assert.Equal("1", response.OperatorId);
            Assert.Equal(0, response.CardTotalCount);
            Assert.Equal(0, response.CardTotalAmount);
            Assert.Equal(0, response.CashTotalCount);
            Assert.Equal(0, response.CashTotalAmount);

            // act
            response = new BillPaymentFlowEndedResponse();

            // assert
            Assert.Null(response.BillId);
            Assert.Equal(0, response.BillOutstandingAmount);
        }

        [Fact]
        public void SpiPayAtTable_OnValidRequest_ReturnStatus()
        {
            // arrange
            var spi = new Spi();

            // act
            var spiPay = new SpiPayAtTable(spi);

            // assert
            Assert.NotNull(spiPay.Config);

            // act
            var spi2 = (Spi)SpiClientTestUtils.GetInstanceField(spiPay.GetType(), spiPay, "_spi");

            // assert
            Assert.Equal(spi.CurrentStatus, spi2.CurrentStatus);

            // arrange
            spiPay = new SpiPayAtTable();

            // act
            var spi3 = (Spi)SpiClientTestUtils.GetInstanceField(spiPay.GetType(), spiPay, "_spi");

            // assert
            Assert.Null(spi3);
        }
    }
}
