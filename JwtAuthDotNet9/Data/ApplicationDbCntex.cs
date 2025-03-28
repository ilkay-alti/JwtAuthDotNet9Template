using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthDotNet9.Entities;
using Microsoft.EntityFrameworkCore;

namespace JwtAuthDotNet9.Data
{
    public class ApplicationDbCntex(DbContextOptions<ApplicationDbCntex> options) : DbContext(options)
    {

        public DbSet<User> Users { get; set; }

    }
}