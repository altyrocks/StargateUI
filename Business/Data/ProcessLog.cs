namespace StargateAPI.Business.Data
{
    public class ProcessLog
    {
        public int Id { get; set; }

        public DateTime TimestampUtc { get; set; }

        // INFO or ERROR
        public string Level { get; set; } = string.Empty;

        // GetPersonByNameHandler
        public string Source { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        // Stack trace, JSON of payload
        public string? Details { get; set; }
    }
}