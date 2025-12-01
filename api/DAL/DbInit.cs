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
                Name = "Adam",
                UserName = "admin@admin.ad",
                // Password = "Aaaa4@"
                Role = Roles.Admin
            };
            await addUser(userManager, admin, "Aaaa4@");

            var worker1 = new User
            {
                Name = "Bob",
                UserName = "aaa@aaa.aaa",
                // Password = "Aaaa4@"
                Role = Roles.Worker
            };
            await addUser(userManager, worker1, "Aaaa4@");

            var worker2 = new User
            {
                Name = "Arne",
                UserName = "ddd@ddd.ddd",
                // Password = "Aaaa4@"
                Role = Roles.Worker
            };
            await addUser(userManager, worker2, "Aaaa4@");

            var patient1 = new User
            {
                Name = "Beb",
                UserName = "bbb@bbb.bbb",
                // Password = "Aaaa4@"
                Role = Roles.Patient,
                WorkerId = worker1.Id,
                Worker = worker1
            };
            await addUser(userManager, patient1, "Aaaa4@");

            var patient2 = new User
            {
                Name = "Bib",
                UserName = "ccc@ccc.ccc",
                // Password = "Aaaa4@"
                Role = Roles.Patient,
                WorkerId = worker1.Id,
                Worker = worker1
            };
            await addUser(userManager, patient2, "Aaaa4@");

            var patient3 = new User
            {
                Name = "Bjarne",
                UserName = "eee@eee.eee",
                // Password = "Aaaa4@"
                Role = Roles.Patient,
                WorkerId = worker2.Id,
                Worker = worker2
            };
            await addUser(userManager, patient3, "Aaaa4@");

            var patient4 = new User
            {
                Name = "Clarne",
                UserName = "fff@fff.fff",
                // Password = "Aaaa4@"
                Role = Roles.Patient,
                WorkerId = worker2.Id,
                Worker = worker2
            };
            await addUser(userManager, patient4, "Aaaa4@");

            var patient5 = new User
            {
                Name = "Drarne",
                UserName = "ggg@ggg.ggg",
                // Password = "Aaaa4@"
                Role = Roles.Patient,
                WorkerId = worker2.Id,
                Worker = worker2
            };
            await addUser(userManager, patient5, "Aaaa4@");
        }

        /* ----- Seeding Availability table: ----- */
        if (!context.Availability.Any())
        {
            // generates availability for Worker "Bob"
            var worker1 = await userManager.FindByNameAsync("aaa@aaa.aaa");
            
            var availabilityRange1 = 
                generateContinuousAvailability(TimeOnly.Parse("09:00:00"), TimeOnly.Parse("15:00:00"),
                                               DayOfWeek.Monday, null, worker1!);
            context.AddRange(availabilityRange1);
            /*
            var availabilityRange2 = 
                generateContinuousAvailability(TimeOnly.Parse("09:00:00"), TimeOnly.Parse("15:00:00"),
                                               DayOfWeek.Tuesday, null, worker1!);
            context.AddRange(availabilityRange2);
            var availabilityRange3 = 
                generateContinuousAvailability(TimeOnly.Parse("09:00:00"), TimeOnly.Parse("15:00:00"),
                                               DayOfWeek.Wednesday, null, worker1!);
            context.AddRange(availabilityRange3);
            var availabilityRange4 = 
                generateContinuousAvailability(TimeOnly.Parse("09:00:00"), TimeOnly.Parse("15:00:00"),
                                               DayOfWeek.Thursday, null, worker1!);
            context.AddRange(availabilityRange4);
            var availabilityRange5 = 
                generateContinuousAvailability(TimeOnly.Parse("09:00:00"), TimeOnly.Parse("15:00:00"),
                                               DayOfWeek.Friday, null, worker1!);
            context.AddRange(availabilityRange5);

            // generates availability for Worker "Arne"
            var worker2 = await userManager.FindByNameAsync("ddd@ddd.ddd");

            var availabilityRange9 = 
                generateContinuousAvailability(TimeOnly.Parse("10:00:00"), TimeOnly.Parse("14:00:00"),
                                               DayOfWeek.Monday, null, worker2!);
            context.AddRange(availabilityRange9);
            var availabilityRange10 = 
                generateContinuousAvailability(TimeOnly.Parse("10:00:00"), TimeOnly.Parse("14:00:00"),
                                               DayOfWeek.Tuesday, null, worker2!);
            context.AddRange(availabilityRange10);
            var availabilityRange11 = 
                generateContinuousAvailability(TimeOnly.Parse("10:00:00"), TimeOnly.Parse("14:00:00"),
                                               DayOfWeek.Wednesday, null, worker2!);
            context.AddRange(availabilityRange11);
            var availabilityRange12 = 
                generateContinuousAvailability(TimeOnly.Parse("10:00:00"), TimeOnly.Parse("14:00:00"),
                                               DayOfWeek.Thursday, null, worker2!);
            context.AddRange(availabilityRange12);
            var availabilityRange13 = 
                generateContinuousAvailability(TimeOnly.Parse("10:00:00"), TimeOnly.Parse("14:00:00"),
                                               DayOfWeek.Friday, null, worker2!);
            context.AddRange(availabilityRange13);
            var availabilityRange14 = 
                generateContinuousAvailability(TimeOnly.Parse("14:00:00"), TimeOnly.Parse("16:00:00"),
                                               DayOfWeek.Thursday, DateOnly.Parse("4/12/2025"), worker2!);
            context.AddRange(availabilityRange14);
            var availabilityRange15 = 
                generateContinuousAvailability(TimeOnly.Parse("10:00:00"), TimeOnly.Parse("14:00:00"),
                                               DayOfWeek.Friday, DateOnly.Parse("5/12/2025"), worker2!);
            context.AddRange(availabilityRange15);
            var availabilityRange16 = 
                generateContinuousAvailability(TimeOnly.Parse("08:00:00"), TimeOnly.Parse("10:00:00"),
                                               DayOfWeek.Wednesday, DateOnly.Parse("10/12/2025"), worker2!);
            context.AddRange(availabilityRange16);*/
            
            await context.SaveChangesAsync();
        }

        /* ----- Seeding Events and Schedules table: ----- */

        if (!context.Events.Any())
        {
            // generates Events and Schedules for Patient "Beb"
            var patient1 = await userManager.FindByNameAsync("bbb@bbb.bbb");

            var event1 = new Event
            {
                From = TimeOnly.Parse("10:30:00"),
                To = TimeOnly.Parse("12:00:00"),
                Date = DateOnly.Parse("5/12/2025"),
                Title = "I wanna take a walk.",
                Location = "Outside",
                UserId = patient1!.Id
            };
            context.Add(event1);/*
            var event2 = new Event
            {
                From = TimeOnly.Parse("11:00:00"),
                To = TimeOnly.Parse("14:00:00"),
                Date = DateOnly.Parse("8/12/2025"),
                Title = "I wanna not take a walk.",
                Location = "Inside",
                UserId = patient1!.Id
            };
            context.Add(event2);

            // generates Events and Schedules for Patient "Beb"
            var patient2 = await userManager.FindByNameAsync("ccc@ccc.ccc");

            var event3 = new Event
            {
                From = TimeOnly.Parse("13:00:00"),
                To = TimeOnly.Parse("15:00:00"),
                Date = DateOnly.Parse("12/12/2025"),
                Title = "I wanna do something.",
                Location = "The Moon",
                UserId = patient2!.Id
            };
            context.Add(event3);
            var event4 = new Event
            {
                From = TimeOnly.Parse("10:00:00"),
                To = TimeOnly.Parse("12:00:00"),
                Date = DateOnly.Parse("9/12/2025"),
                Title = "I wanna do nothing.",
                Location = "The Moon",
                UserId = patient2!.Id
            };
            context.Add(event4);
            var event5 = new Event
            {
                From = TimeOnly.Parse("11:00:00"),
                To = TimeOnly.Parse("14:00:00"),
                Date = DateOnly.Parse("11/12/2025"),
                Title = "I wanna dance.",
                Location = "Mars",
                UserId = patient2!.Id
            };
            context.Add(event5);

            // generates Events and Schedules for Patient "Bjarne"
            var patient3 = await userManager.FindByNameAsync("eee@eee.eee");

            var event6 = new Event
            {
                From = TimeOnly.Parse("13:00:00"),
                To = TimeOnly.Parse("15:00:00"),
                Date = DateOnly.Parse("4/12/2025"),
                Title = "I wanna scream.",
                Location = "In public",
                UserId = patient3!.Id
            };
            context.Add(event6);
            var event7 = new Event
            {
                From = TimeOnly.Parse("10:00:00"),
                To = TimeOnly.Parse("12:00:00"),
                Date = DateOnly.Parse("8/12/2025"),
                Title = "I wanna shout.",
                Location = "In public",
                UserId = patient3!.Id
            };
            context.Add(event7);

            // generates Events and Schedules for Patient "Clarne"
            var patient4 = await userManager.FindByNameAsync("fff@fff.fff");

            var event8 = new Event
            {
                From = TimeOnly.Parse("10:00:00"),
                To = TimeOnly.Parse("13:00:00"),
                Date = DateOnly.Parse("4/12/2025"),
                Title = "Help me.",
                Location = "Outer space",
                UserId = patient4!.Id
            };
            context.Add(event8);
            var event9 = new Event
            {
                From = TimeOnly.Parse("09:00:00"),
                To = TimeOnly.Parse("11:30:00"),
                Date = DateOnly.Parse("10/12/2025"),
                Title = "Help me X2.",
                Location = "Outer space",
                UserId = patient4!.Id
            };
            context.Add(event9);

            // generates Events and Schedules for Patient "Drarne"
            var patient5 = await userManager.FindByNameAsync("ggg@ggg.ggg");

            var event10 = new Event
            {
                From = TimeOnly.Parse("10:00:00"),
                To = TimeOnly.Parse("10:30:00"),
                Date = DateOnly.Parse("9/12/2025"),
                Title = "Feed me.",
                Location = "Dinner table",
                UserId = patient5!.Id
            };
            context.Add(event10);
            var event11 = new Event
            {
                From = TimeOnly.Parse("12:30:00"),
                To = TimeOnly.Parse("13:30:00"),
                Date = DateOnly.Parse("11/12/2025"),
                Title = "Wash my dishes.",
                Location = "The kitchen",
                UserId = patient5!.Id
            };
            context.Add(event11);*/
            
            await context.SaveChangesAsync();

            await addSchedules(context, event1, patient1.WorkerId!);/*
            await addSchedules(context, event2, patient1.WorkerId!);
            await addSchedules(context, event3, patient2.WorkerId!);
            await addSchedules(context, event4, patient2.WorkerId!);
            await addSchedules(context, event5, patient2.WorkerId!);
            await addSchedules(context, event6, patient3.WorkerId!);
            await addSchedules(context, event7, patient3.WorkerId!);
            await addSchedules(context, event8, patient4.WorkerId!);
            await addSchedules(context, event9, patient4.WorkerId!);
            await addSchedules(context, event10, patient5.WorkerId!);
            await addSchedules(context, event11, patient5.WorkerId!);*/

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