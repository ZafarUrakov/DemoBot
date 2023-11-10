using DemoBot.Models;
using EFxceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace DemoBot.Brokers
{
    public class BotBroker : EFxceptionsContext
    {
        public DbSet<TelegramInf> TelegramInfs { get; set; }

        public BotBroker()
        {
            this.Database.EnsureCreated();
        }

        public IQueryable<TelegramInf> SelectAll()
        {
            return this.TelegramInfs.Select(telegramInf => telegramInf);
        }

        public void Insert(TelegramInf telegramInf)
        {
            if (telegramInf == null)
            {
                throw new ArgumentNullException(nameof(telegramInf));
            }

            this.TelegramInfs.Add(telegramInf);
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
