using System;

namespace DemoBot.Models
{
    public class Student
    {
        public Guid Id { get; set; }
        public  long TelegramId { get; set; }
        public string PhoneNumber { get; set; }
    }
}
