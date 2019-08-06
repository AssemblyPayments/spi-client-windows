using Newtonsoft.Json.Linq;
using SPIClient;
using System.Collections.Generic;
using Xunit;

namespace Test
{
    public class PayAtTableConfigTest
    {
        [Fact]
        public void PayAtTableEnabled_OnValidRequest_IsSet()
        {
            // arrange
            var config = new PayAtTableConfig();
            config.PayAtTableEnabled = true;

            // act
            var msg = config.ToMessage("111");

            // assert
            Assert.Equal(config.PayAtTableEnabled, msg.GetDataBoolValue("pay_at_table_enabled", false));
        }

        [Fact]
        public void OperatorIdEnabled_OnValidRequest_IsSet()
        {
            // arrange
            var config = new PayAtTableConfig();
            config.OperatorIdEnabled = true;

            // act
            var msg = config.ToMessage("111");

            // assert
            Assert.Equal(config.OperatorIdEnabled, msg.GetDataBoolValue("operator_id_enabled", false));
        }

        [Fact]
        public void SplitByAmountEnabled_OnValidRequest_IsSet()
        {
            // arrange
            var config = new PayAtTableConfig();
            config.SplitByAmountEnabled = true;

            // act
            var msg = config.ToMessage("111");

            // assert
            Assert.Equal(config.SplitByAmountEnabled, msg.GetDataBoolValue("split_by_amount_enabled", false));
        }

        [Fact]
        public void EqualSplitEnabled_OnValidRequest_IsSet()
        {
            // arrange
            var config = new PayAtTableConfig();
            config.EqualSplitEnabled = true;

            // act
            var msg = config.ToMessage("111");

            // assert
            Assert.Equal(config.EqualSplitEnabled, msg.GetDataBoolValue("equal_split_enabled", false));
        }

        [Fact]
        public void TippingEnabled_OnValidRequest_IsSet()
        {
            // arrange
            var config = new PayAtTableConfig();
            config.TippingEnabled = true;

            // act
            var msg = config.ToMessage("111");

            // assert
            Assert.Equal(config.TippingEnabled, msg.GetDataBoolValue("tipping_enabled", false));
        }

        [Fact]
        public void SummaryReportEnabled_OnValidRequest_IsSet()
        {
            // arrange
            var config = new PayAtTableConfig();
            config.SummaryReportEnabled = true;

            // act
            var msg = config.ToMessage("111");

            // assert
            Assert.Equal(config.SummaryReportEnabled, msg.GetDataBoolValue("summary_report_enabled", false));
        }

        [Fact]
        public void LabelPayButton_OnValidRequest_IsSet()
        {
            // arrange
            var config = new PayAtTableConfig();
            config.LabelPayButton = "PAT";

            // act
            var msg = config.ToMessage("111");

            // assert
            Assert.Equal(config.LabelPayButton, msg.GetDataStringValue("pay_button_label"));
        }

        [Fact]
        public void LabelOperatorId_OnValidRequest_IsSet()
        {
            // arrange
            var config = new PayAtTableConfig();
            config.LabelOperatorId = "12";

            // act
            var msg = config.ToMessage("111");

            // assert
            Assert.Equal(config.LabelOperatorId, msg.GetDataStringValue("operator_id_label"));
        }

        [Fact]
        public void LabelTableId_OnValidRequest_IsSet()
        {
            // arrange
            var config = new PayAtTableConfig();
            config.LabelTableId = "12";

            // act
            var msg = config.ToMessage("111");

            // assert
            Assert.Equal(config.LabelTableId, msg.GetDataStringValue("table_id_label"));
        }

        [Fact]
        public void AllowedOperatorIds_OnValidRequest_IsSet()
        {
            // arrange
            var config = new PayAtTableConfig();
            var allowedOperatorIdList = new List<string>();
            allowedOperatorIdList.Add("1");
            allowedOperatorIdList.Add("2");
            config.AllowedOperatorIds = allowedOperatorIdList;

            // act
            var msg = config.ToMessage("111");
            var operatorIdArray = (JArray)msg.Data["operator_id_list"];
            var operatorIdList = operatorIdArray.ToObject<IList<string>>();

            // assert
            Assert.Equal(config.AllowedOperatorIds, operatorIdList);
            Assert.Equal(config.AllowedOperatorIds.Count, 2);
        }

        [Fact]
        public void TableRetrievalEnabled_OnValidRequest_IsSet()
        {
            // arrange
            var config = new PayAtTableConfig();
            config.TableRetrievalEnabled = true;

            // act
            var msg = config.ToMessage("111");

            // assert
            Assert.Equal(config.TableRetrievalEnabled, msg.GetDataBoolValue("table_retrieval_enabled", false));
        }

        [Fact]
        public void FeatureDisableMessage_OnValidRequest_IsSet()
        {
            // arrange
            var config = new PayAtTableConfig();

            // act
            var msg = PayAtTableConfig.FeatureDisableMessage("111");

            // assert
            Assert.False(config.PayAtTableEnabled);
            Assert.Equal(msg.EventName, "set_table_config");
        }
    }
}
