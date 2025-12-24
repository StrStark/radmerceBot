using Microsoft.EntityFrameworkCore;
using radmerceBot.Api.Models;
using radmerceBot.Core.Models;
using System;
using System.Collections.Generic;

namespace radmerceBot.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PhoneOtp> PhoneOtps => Set<PhoneOtp>();
    public DbSet<SuperUser> SuperUsers => Set<SuperUser>();
    public DbSet<FreeVideo> FreeVideos => Set<FreeVideo>();
    public DbSet<RequestedConsultation> RequestedConsultations => Set<RequestedConsultation>();
    public DbSet<SuperUserPendingMessage> PendingMessages => Set<SuperUserPendingMessage>();
    public DbSet<SuperUserPendingSms> PendingSmsMessages => Set<SuperUserPendingSms>();


}
