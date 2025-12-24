using Microsoft.EntityFrameworkCore;
using radmerceBot.Api.Models;
using radmerceBot.Core.Models;
using System.Collections.Generic;

namespace radmerceBot.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    // OTP
    public DbSet<PhoneOtp> PhoneOtps => Set<PhoneOtp>();

    // سوپر یوزر
    public DbSet<SuperUser> SuperUsers => Set<SuperUser>();
}
