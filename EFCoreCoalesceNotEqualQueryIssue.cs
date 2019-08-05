using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EFCoreCoalesceNotEqualQueryIssue
{
    public class Data
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int? Id { get; set; }

        public int? TestId { get; set; }

        public string Name { get; set; }
    }

    class TestDbContext : DbContext
    {
        private readonly ILoggerFactory loggerFactory;

        public TestDbContext( ILoggerFactory loggerFactory )
        {
            this.loggerFactory = loggerFactory;
        }

        public DbSet<Data> Data { get; set; }

        protected override void OnConfiguring( DbContextOptionsBuilder optionsBuilder )
        {
            base.OnConfiguring( optionsBuilder );
            optionsBuilder.UseSqlite( new Microsoft.Data.Sqlite.SqliteConnection("Data Source=test.db") );
            optionsBuilder.UseLoggerFactory( loggerFactory );
        }
    }

    class Repro
    {
        static void Main( )
        {
            var context = new TestDbContext( new LoggerFactory( ).AddConsole( LogLevel.Debug ) );
            if( context.Database.EnsureCreated( ) )
            {
                context.Data.Add( new Data { Name = "HasTestId", TestId = 123 } );
                context.Data.Add( new Data { Name = "ZeroTestId", TestId = 0 } );
                context.Data.Add( new Data { Name = "NullTestId", TestId = null } );

                context.SaveChanges( );
            }

            var noMainLinked = context.Data.Where( o => (o.TestId ?? 0) != 0)
                .Select( o => o.Name )
                .ToList( );

            var expected = new HashSet<string>( new [] { "HasTestId" } );
            Debug.Assert( expected.SetEquals( noMainLinked ), "Failed to get expected data");
            return;
        }
    }
}
