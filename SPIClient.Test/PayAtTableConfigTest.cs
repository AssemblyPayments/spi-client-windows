using Newtonsoft.Json.Linq;
using SPIClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Test
{
    public class PayAtTableConfigTest
    {
        [Fact]
        public void TestSetPayAtTableEnabled()
        {
            PayAtTableConfig config = new PayAtTableConfig();
            config.PayAtTableEnabled = true;

            Message msg = config.ToMessage("111");
            Assert.Equal(config.PayAtTableEnabled, msg.GetDataBoolValue("pay_at_table_enabled", false));
        }

        [Fact]
        public void TestSetOperatorIdEnabled()
        {
            PayAtTableConfig config = new PayAtTableConfig();
            config.OperatorIdEnabled = true;

            Message msg = config.ToMessage("111");
            Assert.Equal(config.OperatorIdEnabled, msg.GetDataBoolValue("operator_id_enabled", false));
        }

        [Fact]
        public void TestSetSplitByAmountEnabled()
        {
            PayAtTableConfig config = new PayAtTableConfig();
            config.SplitByAmountEnabled = true;

            Message msg = config.ToMessage("111");
            Assert.Equal(config.SplitByAmountEnabled, msg.GetDataBoolValue("split_by_amount_enabled", false));
        }

        [Fact]
        public void TestSetEqualSplitEnabled()
        {
            PayAtTableConfig config = new PayAtTableConfig();
            config.EqualSplitEnabled = true;

            Message msg = config.ToMessage("111");
            Assert.Equal(config.EqualSplitEnabled, msg.GetDataBoolValue("equal_split_enabled", false));
        }

        [Fact]
        public void testSetTippingEnabled()
        {
            PayAtTableConfig config = new PayAtTableConfig();
            config.TippingEnabled = true;

            Message msg = config.ToMessage("111");
            Assert.Equal(config.TippingEnabled, msg.GetDataBoolValue("tipping_enabled", false));
        }

        [Fact]
        public void TestSetSummaryReportEnabled()
        {
            PayAtTableConfig config = new PayAtTableConfig();
            config.SummaryReportEnabled = true;

            Message msg = config.ToMessage("111");
            Assert.Equal(config.SummaryReportEnabled, msg.GetDataBoolValue("summary_report_enabled", false));
        }

        [Fact]
        public void TestSetLabelPayButton()
        {
            PayAtTableConfig config = new PayAtTableConfig();
            config.LabelPayButton = "PAT";

            Message msg = config.ToMessage("111");
            Assert.Equal(config.LabelPayButton, msg.GetDataStringValue("pay_button_label"));
        }

        [Fact]
        public void TestSetLabelOperatorId()
        {
            PayAtTableConfig config = new PayAtTableConfig();
            config.LabelOperatorId = "12";

            Message msg = config.ToMessage("111");
            Assert.Equal(config.LabelOperatorId, msg.GetDataStringValue("operator_id_label"));
        }

        [Fact]
        public void TestSetLabelTableId()
        {
            PayAtTableConfig config = new PayAtTableConfig();
            config.LabelTableId = "12";

            Message msg = config.ToMessage("111");
            Assert.Equal(config.LabelTableId, msg.GetDataStringValue("table_id_label"));
        }

        [Fact]
        public void TestSetAllowedOperatorIds()
        {
            PayAtTableConfig config = new PayAtTableConfig();
            List<string> allowedOperatorIdList = new List<string>();
            allowedOperatorIdList.Add("1");
            allowedOperatorIdList.Add("2");
            config.AllowedOperatorIds = allowedOperatorIdList;

            Message msg = config.ToMessage("111");
            JArray operatorIdArray = (JArray)msg.Data["operator_id_list"];
            IList<string> operatorIdList = operatorIdArray.ToObject<IList<string>>();
            Assert.Equal(config.AllowedOperatorIds, operatorIdList);
            Assert.Equal(config.AllowedOperatorIds.Count, 2);
        }

        [Fact]
        public void TestSetTableRetrievalEnabled()
        {
            PayAtTableConfig config = new PayAtTableConfig();
            config.TableRetrievalEnabled = true;

            Message msg = config.ToMessage("111");
            Assert.Equal(config.TableRetrievalEnabled, msg.GetDataBoolValue("table_retrieval_enabled", false));
        }
    }
}
