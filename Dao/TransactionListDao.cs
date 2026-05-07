namespace AxpigeonApp.Dao
{
    public class TransactionListDao
    {
        public Guid id { get; set; }
        public string transactionId { get; set; }
        public string phone { get; set; }

        public string brand_name { get; set; }

        public string merchant_name { get; set; }

        public string message { get; set; }

        public string messageEncrypt { get; set; }

        public string operator_name { get; set; }
        public string provider_name { get; set; }

        public string pov_transaction_id { get; set; }

        public string schedule_date { get; set; }

        public DateTime sent_at { get; set; }

        public string sms_type { get; set; }
        public string pov_campaign_id { get; set; }

        public string transaction_id { get; set; }

        public bool is_send_now { get; set; }

        public string transaction_tmp_id { get; set; }

        public string status { get; set; }

        public string msgPassword { get; set; }


        public DateTime created_at { get; set; }
    }
}
