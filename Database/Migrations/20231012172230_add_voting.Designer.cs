﻿// <auto-generated />
using System;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Database.Migrations
{
    [DbContext(typeof(ScrumMasterDbContext))]
    [Migration("20231012172230_add_voting")]
    partial class add_voting
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Domain.Models.Action", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("AdditionInfo")
                        .HasColumnType("text");

                    b.Property<long>("TelegramUserId")
                        .HasColumnType("bigint");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("TelegramUserId")
                        .IsUnique();

                    b.ToTable("Actions");
                });

            modelBuilder.Entity("Domain.Models.Member", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("Role")
                        .HasColumnType("integer");

                    b.Property<int?>("ScrumTeamId")
                        .HasColumnType("integer");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("ScrumTeamId");

                    b.HasIndex("UserId");

                    b.ToTable("Members");
                });

            modelBuilder.Entity("Domain.Models.ScrumTeam", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateOnly>("CreatedAt")
                        .HasColumnType("date");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<int>("OwnerId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("OwnerId");

                    b.ToTable("ScrumTeams");
                });

            modelBuilder.Entity("Domain.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<long>("ChatId")
                        .HasColumnType("bigint");

                    b.Property<long>("TelegramUserId")
                        .HasColumnType("bigint");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("VerifyCode")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Domain.Models.Vote", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("MemberId")
                        .HasColumnType("integer");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long?>("VotingId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("MemberId");

                    b.HasIndex("VotingId");

                    b.ToTable("Votes");
                });

            modelBuilder.Entity("Domain.Models.Voting", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("ScrumTeamId")
                        .HasColumnType("integer");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ScrumTeamId");

                    b.ToTable("Votings");
                });

            modelBuilder.Entity("Domain.Models.Member", b =>
                {
                    b.HasOne("Domain.Models.ScrumTeam", null)
                        .WithMany("Members")
                        .HasForeignKey("ScrumTeamId");

                    b.HasOne("Domain.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Domain.Models.ScrumTeam", b =>
                {
                    b.HasOne("Domain.Models.User", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Owner");
                });

            modelBuilder.Entity("Domain.Models.Vote", b =>
                {
                    b.HasOne("Domain.Models.Member", "Member")
                        .WithMany()
                        .HasForeignKey("MemberId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Domain.Models.Voting", null)
                        .WithMany("Votes")
                        .HasForeignKey("VotingId");

                    b.Navigation("Member");
                });

            modelBuilder.Entity("Domain.Models.Voting", b =>
                {
                    b.HasOne("Domain.Models.ScrumTeam", "ScrumTeam")
                        .WithMany()
                        .HasForeignKey("ScrumTeamId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ScrumTeam");
                });

            modelBuilder.Entity("Domain.Models.ScrumTeam", b =>
                {
                    b.Navigation("Members");
                });

            modelBuilder.Entity("Domain.Models.Voting", b =>
                {
                    b.Navigation("Votes");
                });
#pragma warning restore 612, 618
        }
    }
}
