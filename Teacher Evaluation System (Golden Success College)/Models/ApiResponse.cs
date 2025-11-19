namespace Teacher_Evaluation_System__Golden_Success_College_.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }        // true/false
        public string Message { get; set; }      // your message
        public T Data { get; set; }
    }
}
