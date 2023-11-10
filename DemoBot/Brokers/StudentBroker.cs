using DemoBot.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using EFxceptions;

namespace DemoBot.Brokers
{
    public class StudentBroker : EFxceptionsContext
    {
        public DbSet<Student> Students { get; set; }

        public StudentBroker()
        {
            this.Database.EnsureCreated();
        }

        public IQueryable<Student> SelectAll()
        {
            return this.Students.Select(telegramInf => telegramInf);
        }

        public void Insert(Student student)
        {
            if (student == null)
            {
                throw new ArgumentNullException(nameof(student));
            }

            this.Students.Add(student);
            this.SaveChanges();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = "Data source = Smart.db";
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            optionsBuilder.UseSqlite(connectionString);
        }
    }
}
