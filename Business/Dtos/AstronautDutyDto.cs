namespace StargateAPI.Business.Dtos
{
    public class AstronautDutyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Assignment { get; set; } = string.Empty;
        public string Rank { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }
}