using Core;
using Core.Service;
using Microsoft.EntityFrameworkCore;
using Service;

namespace ImovelJaWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews(options =>
            {
                options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(
                    nome => $"O campo {nome} deve ser um número.");
            });

            builder.Services.AddDbContext<ImovelJaContext>(options =>
                options.UseMySQL(builder.Configuration.GetConnectionString("ImovelJaDatabase") ?? throw new InvalidOperationException("Conexão com o banco de dados não foi possível. Favor entrar em contato com o Administrador.")));

            builder.Services.AddTransient<IImovelService, ImovelService>();
            builder.Services.AddTransient<IPessoaService, PessoaService>();
            builder.Services.AddTransient<IMensagemService, MensagemService>();
            builder.Services.AddTransient<IPlanoService, PlanoService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
