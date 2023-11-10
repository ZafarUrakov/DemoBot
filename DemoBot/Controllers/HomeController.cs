using DemoBot.Brokers;
using DemoBot.Models;
using Microsoft.AspNetCore.Mvc;

namespace DemoBot.Controllers
{
    public class HomeController : Controller
    {
        private readonly StudentBroker studentBroker;

        public HomeController(StudentBroker studentBroker)
        {
            this.studentBroker = studentBroker;
        }

        public IActionResult Index()
        {
            return Ok();
        }

        [HttpPost]
        public ActionResult<Student> PostStudent(Student student)
        {
            this.studentBroker.Students.Add(student);

            return Ok("Welcome!");
        }
    }
}
