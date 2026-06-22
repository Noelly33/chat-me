using Consumer.Messaging.Worker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer.Messaging.Worker.Infrastructure.Context
{
    public class ChatDbContext : DbContext
    {
        public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

        public DbSet<Conversacion> Conversaciones => Set<Conversacion>();
        public DbSet<ParticipanteConversacion> ParticipantesConversacion => Set<ParticipanteConversacion>();
        public DbSet<TipoMensaje> TiposMensaje => Set<TipoMensaje>();
        public DbSet<Mensaje> Mensajes => Set<Mensaje>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Conversacion>(entity =>
            {
                entity.ToTable("conversaciones");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EsGrupal).HasColumnName("es_grupal").HasDefaultValue(false);
                entity.Property(e => e.Nombre).HasColumnName("nombre");
                entity.Property(e => e.CreadoAt).HasColumnName("creado_at").HasDefaultValueSql("now()");
            });

            modelBuilder.Entity<ParticipanteConversacion>(entity =>
            {
                entity.ToTable("participantes_conversacion");

                entity.HasKey(e => new { e.ConversacionId, e.UsuarioId });

                entity.Property(e => e.ConversacionId).HasColumnName("conversacion_id");
                entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
                entity.Property(e => e.CreadoAt).HasColumnName("unido_at").HasDefaultValueSql("now()");
                entity.Property(e => e.UltimoLeidoAt).HasColumnName("ultimo_leido_at");

                entity.HasOne(d => d.Conversacion)
                      .WithMany(p => p.Participantes)
                      .HasForeignKey(d => d.ConversacionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TipoMensaje>(entity =>
            {
                entity.ToTable("tipos_mensaje");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Codigo).HasColumnName("codigo").IsRequired();
                entity.Property(e => e.Descripcion).HasColumnName("descripcion");

                entity.HasIndex(e => e.Codigo).IsUnique();
            });

            modelBuilder.Entity<Mensaje>(entity =>
            {
                entity.ToTable("mensajes");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever(); // IMPORTANTE: El Front genera el ID, EF Core no debe autogenerarlo
                entity.Property(e => e.ConversacionId).HasColumnName("conversacion_id");
                entity.Property(e => e.EmisorId).HasColumnName("emisor_id");
                entity.Property(e => e.TipoMensajeId).HasColumnName("tipo_mensaje_id");
                entity.Property(e => e.Contenido).HasColumnName("contenido");
                entity.Property(e => e.CreadoAt).HasColumnName("creado_at").HasDefaultValueSql("now()");

                entity.HasOne(d => d.Conversacion)
                      .WithMany(p => p.Mensajes)
                      .HasForeignKey(d => d.ConversacionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.TipoMensaje)
                      .WithMany(p => p.Mensajes)
                      .HasForeignKey(d => d.TipoMensajeId);
            });
        }
    }
}
