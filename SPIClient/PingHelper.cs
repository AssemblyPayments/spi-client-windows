namespace SPIClient
{
    public static class PongHelper
    {
        public static Message GeneratePongResponse(Message ping)
        {
            return new Message(ping.Id, Events.Pong, null, true);
        }
    }
    
    public static class PingHelper
    {
        public static Message GeneratePingRequest()
        {
            return new Message(RequestIdHelper.Id("ping"), Events.Ping, null, true);
        }
    }

}