using Microsoft.EntityFrameworkCore;
namespace HealthCalendar.DAL;

using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using HealthCalendar.Models;
using HealthCalendar.Shared;
using Microsoft.AspNetCore.Identity;
using SQLitePCL;

public static class DbInit
{
    // Seeds users to Db
    public static async Task DbSeed(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // a UserManager, used to add Users securely
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        // an AuthDbContext, used to check if User table is empty
        AuthDbContext authContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        // an HealthCalendarDbContext, used to do db operations
        HealthCalendarDbContext context = scope.ServiceProvider.GetRequiredService<HealthCalendarDbContext>();

        /* ----- Seeding User table: ----- */

        // Only seeds if User table is empty
        if (!authContext.Users.Any())
        {
            var admin = new User
            {
                Name = "Baifan",
                UserName = "baifan@gmail.com",
                // Password = "Aaaa4@"
                Role = Roles.Admin
            };
            await addUser(userManager, admin, "Aaaa4@");

            var worker1 = new User
            {
                Name = "Bong",
                UserName = "bong@gmail.com",
                // Password = "Aaaa4@"
                Role = Roles.Worker
            };
            await addUser(userManager, worker1, "Aaaa4@");

            var worker2 = new User
            {
                Name = "olav",
                UserName = "olav@gmail.com",
                // Password = "Aaaa4@"
                Role = Roles.Worker
            };
            await addUser(userManager, worker2, "Aaaa4@");

            var patient1 = new User
            {
                Name = "Lars",
                UserName = "lars@gmail.com",
                // Password = "Aaaa4@"
                Role = Roles.Patient,
                WorkerId = worker1.Id,
                Worker = worker1
            };
            await addUser(userManager, patient1, "Aaaa4@");

            var patient2 = new User
            {
                Name = "Karl",
                UserName = "karl@gmail.com",
                // Password = "Aaaa4@"
                Role = Roles.Patient,
                WorkerId = worker1.Id,
                Worker = worker1
            };
            await addUser(userManager, patient2, "Aaaa4@");

            var patient3 = new User
            {
                Name = "Bengt",
                UserName = "bengt@gmail.com",
                // Password = "Aaaa4@"
                Role = Roles.Patient,
                WorkerId = null,
                Worker = null
            };
            await addUser(userManager, patient3, "Aaaa4@");
        }

        /* ----- Seeding Availability table: ----- */
        if (!context.Availability.Any())
        {
            // generates availability for Worker "Bob"
            var worker1 = await userManager.FindByNameAsync("bong@gmail.com");
            
            var availabilityRange1 = 
                generateContinuousAvailability(new TimeOnly(9, 0), new TimeOnly(15, 0),
                                               DayOfWeek.Monday, null, worker1!);
            context.AddRange(availabilityRange1);
            var availabilityRange2 = 
                generateContinuousAvailability(new TimeOnly(9, 0), new TimeOnly(15, 0),
                                               DayOfWeek.Tuesday, null, worker1!);
            context.AddRange(availabilityRange2);
            var availabilityRange3 = 
                generateContinuousAvailability(new TimeOnly(9, 0), new TimeOnly(15, 0),
                                               DayOfWeek.Wednesday, null, worker1!);
            context.AddRange(availabilityRange3);
            var availabilityRange4 = 
                generateContinuousAvailability(new TimeOnly(9, 0), new TimeOnly(15, 0),
                                               DayOfWeek.Thursday, null, worker1!);
            context.AddRange(availabilityRange4);
            var availabilityRange5 = 
                generateContinuousAvailability(new TimeOnly(9, 0), new TimeOnly(15, 0),
                                               DayOfWeek.Friday, null, worker1!);
            context.AddRange(availabilityRange5);
            var availabilityRange6 = 
                generateContinuousAvailability(new TimeOnly(8, 0), new TimeOnly(9, 0),
                                               DayOfWeek.Wednesday, new DateOnly(2025, 12, 17), worker1!);
            context.AddRange(availabilityRange6);
            var availabilityRange7 = 
                generateContinuousAvailability(new TimeOnly(15, 0), new TimeOnly(16, 0),
                                               DayOfWeek.Wednesday, new DateOnly(2025, 12, 17), worker1!);
            context.AddRange(availabilityRange7);
            var availabilityRange8 = 
                generateContinuousAvailability(new TimeOnly(9, 0), new TimeOnly(10, 0),
                                               DayOfWeek.Tuesday, new DateOnly(2025, 12, 18), worker1!);
            context.AddRange(availabilityRange8);
            var availabilityRange9 = 
                generateContinuousAvailability(new TimeOnly(14, 0), new TimeOnly(15, 0),
                                               DayOfWeek.Tuesday, new DateOnly(2025, 12, 18), worker1!);
            context.AddRange(availabilityRange9);
            
            await context.SaveChangesAsync();
        }

        /* ----- Seeding Events and Schedules table: ----- */

        if (!context.Events.Any())
        {
            // generates Events for Patient "Lars"
            var patient1 = await userManager.FindByNameAsync("lars@gmail.com");

            var event1 = new Event
            {
                From = new TimeOnly(8, 0),
                To = new TimeOnly(10, 0),
                Date = new DateOnly(2025, 12, 17),
                Title = "I wanna take a walk.",
                Location = "Streetname 11",
                UserId = patient1!.Id
            };
            context.Add(event1);

            var event2 = new Event
            {
                From = new TimeOnly(10, 30),
                To = new TimeOnly(12, 0),
                Date = new DateOnly(2025, 12, 18),
                Title = "Help me clean the house.",
                Location = "HouseAddress 1",
                UserId = patient1!.Id
            };
            context.Add(event2);

            // generates Events for Patient "Karl"
            var patient2 = await userManager.FindByNameAsync("karl@gmail.com");

            var event3 = new Event
            {
                From = new TimeOnly(11, 0),
                To = new TimeOnly(12, 30),
                Date = new DateOnly(2025, 12, 15),
                Title = "Help me buy groceries",
                Location = "MallStreet 23",
                UserId = patient2!.Id
            };
            context.Add(event3);

            var event4 = new Event
            {
                From = new TimeOnly(11, 0),
                To = new TimeOnly(12, 30),
                Date = new DateOnly(2025, 12, 17),
                Title = "Help me exercise",
                Location = "HouseAddress 2",
                UserId = patient2!.Id
            };
            context.Add(event4);
            
            await context.SaveChangesAsync();

            // Generates relevant schedules

            await addSchedules(context, event1, patient1.WorkerId!);
            await context.SaveChangesAsync();
            await addSchedules(context, event2, patient1.WorkerId!);
            await context.SaveChangesAsync();
            await addSchedules(context, event3, patient2.WorkerId!);
            await context.SaveChangesAsync();
            await addSchedules(context, event4, patient2.WorkerId!);

            await context.SaveChangesAsync();
        }
    }
    
    // adds User to User table
    private static async Task addUser(UserManager<User> userManager, User user, string password)
    {
        var result = await userManager.CreateAsync(user, password);
        
        // in case of not succeeding, errors are printed
        if (!result.Succeeded)
        {
            Console.WriteLine("[DbInit], something went wrong when adding " +
                             $"user {@user} to User table: \n");
            Console.WriteLine(String.Join("\n",result.Errors));
        }
    }

    // generates list of continuous Availability
    private static List<Availability> 
        generateContinuousAvailability(TimeOnly from, TimeOnly to, DayOfWeek dayOfWeek, 
                                       DateOnly? date, User worker)
    {
        var continuousAvailability = new List<Availability>();
        var userId = worker.Id;
        for (; from < to; from = from.AddMinutes(30))
        {
            var availability = new Availability
            {
                From = from,
                To = from.AddMinutes(30),
                DayOfWeek = dayOfWeek,
                Date = date,
                UserId = userId
            };
            continuousAvailability.Add(availability);
        }
        return continuousAvailability;
    }

    // adds list of Schedules to db
    private static async 
        Task addSchedules(HealthCalendarDbContext context, Event eventt, string workerId)
    {
        var date = eventt.Date;
        var dayOfWeek = date.DayOfWeek;
        
        var doWAvailability = await context.Availability
                .Where(a => a.UserId == workerId && a.DayOfWeek == dayOfWeek 
                       && a.Date == null && a.From >= eventt.From && a.To <= eventt.To)
                .ToListAsync();
        var dateAvailability = await context.Availability
                .Where(a => a.UserId == workerId && a.Date == date && 
                       a.From >= eventt.From && a.To <= eventt.To)
                .ToListAsync();
        
        var continuousAvailability = new List<Availability>();
        continuousAvailability.AddRange(doWAvailability);
        continuousAvailability.AddRange(dateAvailability);
        
        var schedules = new List<Schedule>();
        continuousAvailability.ForEach(a =>
        {
            var schedule = new Schedule
            {
                Date = date,
                AvailabilityId = a.AvailabilityId,
                Availability = a,
                EventId = eventt.EventId,
                Event = eventt
            };
            schedules.Add(schedule);
        });

        context.AddRange(schedules);
    }
}