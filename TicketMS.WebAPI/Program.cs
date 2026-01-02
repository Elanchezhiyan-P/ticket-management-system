using TicketMS.Infrastructure.Seed;
using TicketMS.WebAPI.Extensions;

namespace TicketMS.WebAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddDatabase(builder.Configuration);
            builder.Services.AddIdentityServices();
            builder.Services.AddJwtAuthentication(builder.Configuration);
            builder.Services.AddApplicationServices();
            builder.Services.AddSwaggerDocumentation();
            builder.Services.AddCorsPolicy(builder.Configuration, builder.Environment);

            var app = builder.Build();

            // Seed database
            await DbInitializer.InitializeAsync(app.Services);


            // Configure middleware pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TicketMS API v1");
                });
            }

            //Custom Middleware
            app.UseCorrelationId();
            app.UseGlobalExceptionHandler();
            app.UseRequestLogging();

            app.UseHttpsRedirection();
            app.UseCors("CorsPolicy");

            // Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
