using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Service;

namespace ImovelJaAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddDbContext<ImovelJaContext>(options =>
                options.UseMySQL(builder.Configuration.GetConnectionString("ImovelJaDatabase") ?? throw new InvalidOperationException("Conexão com o banco de dados não foi possível. Favor entrar em contato com o Administrador.")));

            builder.Services.AddTransient<IImovelService, ImovelService>();
            builder.Services.AddTransient<IMensagemService, MensagemService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
