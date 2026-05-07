using System.Net.NetworkInformation;

namespace AxpigeonApp.Dao
{
    public class SingleMessageRespDao
    {
        public string status { get; set; }
        public string message { get; set; }

        public SingleMessageRspDataDao data;
    }
}
